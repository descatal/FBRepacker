using FBRepacker.PAC.Repack.customFileInfo;
using FBRepacker.PAC.Repack.FileTypes;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media.Animation;

namespace FBRepacker.PAC.Repack
{
    class RepackPAC : Internals
    {

        //Dictionary<string, string> fileInfo = new Dictionary<string, string>();
        Dictionary<int, string[]> fileInfoDic = new Dictionary<int, string[]>();
        Dictionary<string, string> extensionEquivalentDic = new Dictionary<string, string> 
        {
            {".dds", ".NTP3"},
        };

        Dictionary<int, FHMFileInfo> FHMFileInfoDic = new Dictionary<int, FHMFileInfo>();
        Dictionary<int, GeneralFileInfo> GeneralFileInfoDic = new Dictionary<int, GeneralFileInfo>();

        Dictionary<int, FHMFileInfo> realFHMFileDic = new Dictionary<int, FHMFileInfo>();
        Dictionary<int, GeneralFileInfo> realGeneralFileDic = new Dictionary<int, GeneralFileInfo>();

        List<string> tempFileList = new List<string>();

        NTP3 repackNTP3 = new NTP3();

        int fileNumberinInfoFile = 1;

        string repackName = string.Empty;

        public RepackPAC(string filePath) : base()
        {
            
        }

        public void repackPAC()
        {
            //currDirectory = Properties.Settings.Default.OpenRepackPath;
            rootDirectory = Properties.Settings.Default.OpenRepackPath;
            DirectoryInfo repackFolder = new DirectoryInfo(rootDirectory);
            repackName = repackFolder.Name;

            initializePACInfoFileRepack();
            parseInfo();
            repackFiles(rootDirectory);
            repackEndFile();
            cleanTempFiles();

            resetVariables();
        }

        private void parseInfo()
        {
            int fileNumber = 1;
            while(infoStreamRead.ReadLine() != null)
            {
                string[] retrievedProperties = getFileInfoProperties("--" + fileNumber + "--");
                if (retrievedProperties.Length > 0)
                {
                    fileInfoDic[fileNumber] = retrievedProperties;
                    fileNumber++;
                }
            }
            
            switch (fileInfoDic.First().Value.Skip(3).First())
            {
                case "--FHM--":
                    parseFHMInfo(fileNumberinInfoFile, true); // always 1
                    break;
                case "--STREAM--":

                    break;
                default:
                    break;
            }
        }

        private void parseFHMInfo(int fileNumber, bool isRootFHM)
        {
            // For the .fhm File
            int fileNumberinFHM = 1;
            string[] FHMinfos = fileInfoDic[fileNumber];
            FHMFileInfo fhmFileInfo = new FHMFileInfo();
            FHMFileInfoDic[fileNumber] = fhmFileInfo;

            // parse and get info of FHM from PAC.info
            fhmFileInfo.totalFileSize = convertStringtoInt(getSpecificFileInfoProperties("Total file size: ", FHMinfos));
            fhmFileInfo.numberofFiles = convertStringtoInt(getSpecificFileInfoProperties("Number of files: ", FHMinfos));
            fhmFileInfo.FHMChunkSize = convertStringtoInt(getSpecificFileInfoProperties("FHM chunk size: ", FHMinfos));

            for (int i = 1; i <= fhmFileInfo.numberofFiles; i++)
            {
                // For the rest of the files in the FHM tag in PAC.info
                fileNumberinInfoFile++;
                string[] newFileInfos = fileInfoDic[fileNumberinInfoFile];
                parseGeneralFileInfo(newFileInfos, fileNumberinInfoFile, fileNumberinFHM);
                fileNumberinFHM++;
            }

            // For endfile
            if (isRootFHM)
            {
                fileNumberinInfoFile++;
                string[] endFileInfos = fileInfoDic[fileNumberinInfoFile];
                parseGeneralFileInfo(endFileInfos, fileNumberinInfoFile, fileNumberinFHM);
            }
        }

        private void parseGeneralFileInfo(string[] newFileInfos, int fileNumber, int fileNoinFHM)
        {
            GeneralFileInfo generalFileInfo = new GeneralFileInfo();
            string header = getSpecificFileInfoProperties("Header: ", newFileInfos);

            if (header != "endfile")
            {
                // parse and get infos from PAC.info
                // we should link up duplicate infos, but this will suffice for now
                int FHMOffset = convertStringtoInt(getSpecificFileInfoProperties("FHMOffset: ", newFileInfos));
                int fileSize = convertStringtoInt(getSpecificFileInfoProperties("Size: ", newFileInfos));

                // parse and get infos from PAC.info
                generalFileInfo.FHMOffset = FHMOffset;
                generalFileInfo.fileSize = fileSize;
                generalFileInfo.header = header;
                generalFileInfo.fileNo = fileNumber;
                generalFileInfo.fileNoinFHM = fileNoinFHM;

                if (checkLinked(newFileInfos))
                {
                    generalFileInfo.isLinked = true;
                    generalFileInfo.linkFileNumber = convertStringtoInt(getSpecificFileInfoProperties("Link FHMOffset: ", newFileInfos));
                }

                switch (generalFileInfo.header)
                {
                    case "fhm":
                        parseFHMInfo(fileNumber, false);
                        break;
                    case "NTP3":
                        // TODO: should check if the fileNumber has been parsed before instead of checking linked. Hopefully there's no linked non-multi NTP3.
                        if (!generalFileInfo.isLinked)
                        {
                            // Pre Base64 code, TODO: Remove
                            // StreamReader NTP3Info = repackNTP3.getNTP3InfoStreamReader(newFileInfos);
                            repackNTP3.parseNTP3Info(newFileInfos, fileNumber);
                        }
                        GeneralFileInfoDic[fileNumber] = generalFileInfo;
                        break;
                    default:
                        GeneralFileInfoDic[fileNumber] = generalFileInfo;
                        break;
                }
            }
            else
            {
                // TODO: Check offset and Size with repacked file
                int EndFileOffset = convertStringtoInt(getSpecificFileInfoProperties("End File Offset: ", newFileInfos));
                int EndFileSize = convertStringtoInt(getSpecificFileInfoProperties("End File Size: ", newFileInfos));

                // parse and get infos from PAC.info
                generalFileInfo.FHMOffset = EndFileOffset;
                generalFileInfo.fileSize = EndFileSize;
                generalFileInfo.header = header;
                generalFileInfo.fileNo = fileNumber;

                GeneralFileInfoDic[fileNumber] = generalFileInfo;
            }
        }

        private bool checkLinked(string[] newFileInfos)
        {
            return newFileInfos.Any(s => s.Contains("Link"));
        }

        private void repackFiles(string directoryPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
            DirectoryInfo[] nestedDirectories = directoryInfo.GetDirectories();

            foreach (DirectoryInfo folder in nestedDirectories)
            {
                // Redirect to the deepest nested folder
                repackFiles(folder.FullName);

                int folderNumber = getFileNumberfromFileName(folder.Name);
                int fileNumberinFHM = 1, fileNumberinFolder = 1;

                string tempRepackFilePath = Directory.GetCurrentDirectory() + (@"\temp\" + folderNumber.ToString());
                Stream repackFileStream = File.Create(tempRepackFilePath);
                tempFileList.Add(tempRepackFilePath);

                string[] fileandDirectories = Directory.GetFileSystemEntries(folder.FullName);
                SortedList<int, string> sortedFileandDirectories = new SortedList<int, string>();

                SortedList<int, int[]> filePointersandSizeOffsetinFHM = new SortedList<int, int[]>();
                SortedList<int, int[]> filePointersandSizeinFHM = new SortedList<int, int[]>();
                Dictionary<int, string> filePathListinFHM = new Dictionary<int, string>();

                foreach (string path in fileandDirectories)
                {
                    FileAttributes fileAttributes = File.GetAttributes(path);
                    
                    if (fileAttributes.HasFlag(FileAttributes.Directory))
                    {
                        DirectoryInfo directory = new DirectoryInfo(path);
                        fileNumberinFolder = getFileNumberfromFileName(Path.GetFileNameWithoutExtension(directory.FullName));
                    }
                    else
                    {
                        FileInfo file = new FileInfo(path);
                        fileNumberinFolder = getFileNumberfromFileName(Path.GetFileNameWithoutExtension(file.FullName));
                    }

                    // For cases on DDS Multiple that shares the same file Number.
                    if (!sortedFileandDirectories.ContainsKey(fileNumberinFolder))
                    {
                        sortedFileandDirectories.Add(fileNumberinFolder, path);
                    }
                }

                FileInfo lastFile = new FileInfo(fileandDirectories.Last());
                foreach (KeyValuePair<int, string> pathPair in sortedFileandDirectories)
                {
                    string path = pathPair.Value;
                    int fileNumber = pathPair.Key;

                    FileAttributes fileAttributes = File.GetAttributes(path);
                    if (fileAttributes.HasFlag(FileAttributes.Directory))
                    {
                        DirectoryInfo nestedFolder = new DirectoryInfo(path);
                        int nestedFolderNumber = getFileNumberfromFileName(Path.GetFileNameWithoutExtension(nestedFolder.FullName));
                        string repackedFolderFilePath = Directory.GetCurrentDirectory() + (@"\temp\" + nestedFolderNumber.ToString());
                        FileInfo file = new FileInfo(repackedFolderFilePath);

                        byte[] fileBuffer = getFileStream(file);

                        repackFHMFolder(file, fileNumber, fileNumberinFHM, fileBuffer, repackFileStream, filePointersandSizeinFHM);
                    }
                    else
                    {
                        FileInfo file = new FileInfo(path);
                        verifyHeader(file, fileNumber);

                        byte[] fileBuffer = getFileStream(file);

                        switch (file.Extension)
                        {
                            case ".fhm":
                                repackFHM(file, fileNumber, fileBuffer, repackFileStream, filePointersandSizeOffsetinFHM);
                                fileNumberinFHM--;
                                break;
                            case ".dds":
                                byte[] NTP3Buffer = repackNTP3.repackDDStoNTP3(file, fileNumber);
                                repackGeneral(file, fileNumber, fileNumberinFHM, NTP3Buffer, repackFileStream, filePointersandSizeinFHM, filePathListinFHM, lastFile);
                                break;
                            default:
                                repackGeneral(file, fileNumber, fileNumberinFHM, fileBuffer, repackFileStream, filePointersandSizeinFHM, filePathListinFHM, lastFile);
                                break;
                        }
                    }

                    filePathListinFHM[fileNumberinFHM] = path;

                    fileNumberinFHM++;
                }

                rewriteFHMOffsetandSize(filePointersandSizeOffsetinFHM, filePointersandSizeinFHM, repackFileStream);

                repackFileStream.Close();
            }
        }

        private void repackFHM(FileInfo file, int fileNumber, byte[] fileBuffer, Stream repackStream, SortedList<int, int[]> filePointersandSizeOffsetinFHM)
        {
            if (!FHMFileInfoDic.ContainsKey(fileNumber))
                throw new Exception("FHM " + file.Name + " could not be found in FHMFileInfo Dictionary");

            int FHMChunkSize = FHMFileInfoDic[fileNumber].FHMChunkSize;
            byte[] FHMChunk = new byte[FHMChunkSize];
            Buffer.BlockCopy(fileBuffer, 0, FHMChunk, 0, FHMChunkSize);
            repackStream.Write(FHMChunk, 0, FHMChunk.Length);

            int totalFileNumberinFHMInfo = FHMFileInfoDic[fileNumber].numberofFiles;

            int startingPosition = 0x14;

            for(int i = 1; i <= totalFileNumberinFHMInfo; i++)
            {
                int currentFileOffset = startingPosition + ((i - 1) * 0x04);
                int currentSizeOffset = currentFileOffset + (0x04 * (totalFileNumberinFHMInfo));
                int[] pointerOffsets = { currentFileOffset, currentSizeOffset };
                filePointersandSizeOffsetinFHM.Add(i, pointerOffsets);
            }

            FHMFileInfo realFHMFileInfo = new FHMFileInfo();
            realFHMFileInfo.FHMChunkSize = fileBuffer.Length;
            realFHMFileDic[fileNumber] = realFHMFileInfo;
        }

        private void repackFHMFolder(FileInfo file, int fileNumber, int fileNumberinFHM, byte[] fileBuffer, Stream repackStream, SortedList<int, int[]> filePointersandSizeinFHM)
        {
            if (!FHMFileInfoDic.ContainsKey(fileNumber))
                throw new Exception("File: " + file.Name + " could not be found in GeneralFileInfo Dictionary");

            FHMFileInfo folderFileInfo = new FHMFileInfo();

            int FHMOffset = (int)repackStream.Position;
            int FileSize = fileBuffer.Length;

            repackStream.Write(fileBuffer, 0, fileBuffer.Length);

            int[] filePointerandSize = { FHMOffset, FileSize };
            filePointersandSizeinFHM.Add(fileNumberinFHM, filePointerandSize);
        }

        private void repackGeneral(FileInfo file, int fileNumber, int fileNumberinFHM, byte[] fileBuffer, Stream repackStream, SortedList<int, int[]> filePointersandSizeinFHM, Dictionary<int, string> filePathListinFHM, FileInfo lastFile)
        {
            if (!GeneralFileInfoDic.ContainsKey(fileNumber))
                throw new Exception("File: " + file.Name + " could not be found in GeneralFileInfo Dictionary");

            int fileBufferSizebeforePadding = fileBuffer.Length;

            if (!file.Equals(lastFile) && file.Extension != ".fhm")
                fileBuffer = addPaddingArrayBuffer(fileBuffer);

            GeneralFileInfo realFileInfo = new GeneralFileInfo();

            if (!GeneralFileInfoDic[fileNumber].isLinked)
            {
                int FHMOffset = (int)repackStream.Position;
                int FileSize = fileBufferSizebeforePadding;

                //TODO: Write process message comparing original Attr with actual Attr.

                repackStream.Write(fileBuffer, 0, fileBuffer.Length);

                int[] filePointerandSize = { FHMOffset, FileSize };
                filePointersandSizeinFHM.Add(fileNumberinFHM, filePointerandSize);

                // TODO: are these really needed?
                realFileInfo.FHMOffset = FHMOffset;
                realFileInfo.fileNoinFHM = fileNumberinFHM;
                realFileInfo.fileNo = fileNumber;
                realFileInfo.fileSize = fileBuffer.Length;
                realFileInfo.header = file.Extension.Replace(".", "");
                realGeneralFileDic[fileNumber] = realFileInfo;
            }
            else
            {
                int linkedFileNoinFHM = GeneralFileInfoDic[fileNumber].linkFileNumber;
                string path = filePathListinFHM[linkedFileNoinFHM];
                FileInfo linkedFile = new FileInfo(path);

                //fileBuffer = getFileStream(linkedFile);

                //repackStream.Write(fileBuffer, 0, fileBuffer.Length);

                if (filePointersandSizeinFHM.ContainsKey(fileNumberinFHM))
                    throw new Exception("fileNumberinFHM already exist in filePointersandSizeinFHM list!" + Environment.NewLine + "filenumberinFHM: " + fileNumberinFHM.ToString());

                if (!filePointersandSizeinFHM.ContainsKey(linkedFileNoinFHM))
                    throw new Exception("linkedFileNo not found in filePointersandSizeinFHM list!" + Environment.NewLine + "linkedFileNo: " + linkedFileNoinFHM.ToString());
                
                filePointersandSizeinFHM.Add(fileNumberinFHM, filePointersandSizeinFHM[linkedFileNoinFHM]);
            }
        }

        private void rewriteFHMOffsetandSize(SortedList<int, int[]> filePointersandSizeOffsetinFHM, SortedList<int, int[]> filePointersandSizeinFHM, Stream repackStream)
        {
            if(filePointersandSizeOffsetinFHM.Count != filePointersandSizeinFHM.Count)
                throw new Exception("Pointer and Size mismatch!" + Environment.NewLine + "Offset: " + filePointersandSizeOffsetinFHM.Count.ToString() + Environment.NewLine + "Value: " + filePointersandSizeinFHM.Count.ToString());

            foreach(KeyValuePair<int, int[]> PointerandSize in filePointersandSizeOffsetinFHM)
            {
                repackStream.Seek((long)PointerandSize.Value.First(), SeekOrigin.Begin);

                int Pointer = filePointersandSizeinFHM[PointerandSize.Key].First();
                Pointer = BinaryPrimitives.ReverseEndianness(Pointer);
                byte[] pointerByte = BitConverter.GetBytes(Pointer);

                repackStream.Write(pointerByte, 0, pointerByte.Length);

                repackStream.Seek((long)PointerandSize.Value.Last(), SeekOrigin.Begin);

                int Size = filePointersandSizeinFHM[PointerandSize.Key].Last();
                Size = BinaryPrimitives.ReverseEndianness(Size);
                byte[] sizeByte = BitConverter.GetBytes(Size);

                repackStream.Write(sizeByte, 0, sizeByte.Length);
            }
        }

        private void verifyHeader(FileInfo file, int fileNumber)
        {
            string fileExtension = file.Extension;
            string fileExtensionfromInfo = string.Empty;

            if (extensionEquivalentDic.ContainsKey(fileExtension))
                fileExtension = extensionEquivalentDic[fileExtension];

            if (!GeneralFileInfoDic.ContainsKey(fileNumber) && !FHMFileInfoDic.ContainsKey(fileNumber))
                throw new Exception("file Number for " + file.Name + " is not found in fileInfo Dictionary");

            else if (!GeneralFileInfoDic.ContainsKey(fileNumber))
                fileExtensionfromInfo = ".fhm";

            else if (!FHMFileInfoDic.ContainsKey(fileNumber))
                fileExtensionfromInfo = "." + GeneralFileInfoDic[fileNumber].header;

            if (!fileExtension.ToLower().Equals(fileExtensionfromInfo.ToLower()))
                throw new Exception("file Extension for " + file.Name + " is not the same extension in fileInfo's " + fileNumber.ToString() + "th file.");
        }

        private void repackEndFile()
        {
            GeneralFileInfo endFileInfo = GeneralFileInfoDic.Last().Value;
            int endFileFileNo = endFileInfo.fileNo;

            if (endFileInfo.header != "endfile")
                throw new Exception("last file in GeneralFileInfoDic is not endfile! Please recheck PAC.info." + Environment.NewLine + "File Header & FileNo: " + endFileInfo.header + " | " + endFileInfo.fileNo);

            string endFilePath = Path.Combine(Properties.Settings.Default.OpenRepackPath, endFileFileNo.ToString("000") + ".endfile");

            if(!File.Exists(endFilePath))
                throw new Exception("endFilePath not valid: " + endFilePath);

            string tempPACFilePath = tempFileList.Last();
            FileStream PACStream = new FileStream(tempPACFilePath, FileMode.Append);

            byte[] EndFileBuffer = File.ReadAllBytes(endFilePath);
            PACStream.Write(EndFileBuffer, 0, EndFileBuffer.Length);

            PACStream.Close();

            copyTempFiles();
        }

        private int getFileNumberfromFileName(string fileName)
        {
            string newFileName = fileName;
            if (fileName.Contains('-'))
            {
                newFileName = fileName.Split('-')[0];
            }
            
            if(int.TryParse(newFileName, out int fileNumber))
            {
                return fileNumber;
            }
            else
            {
                throw new Exception("fileName int to string conversion failed with fileName: " + fileName);
            }
        }

        private byte[] getFileStream(FileInfo file)
        {
            Stream fileStream = File.OpenRead(file.FullName);
            byte[] fileBuffer = new byte[fileStream.Length];

            fileStream.Read(fileBuffer, 0, (int)fileStream.Length);
            fileStream.Close();

            return fileBuffer;
        }

        private void copyTempFiles()
        {
            string repackPath = Path.Combine(Properties.Settings.Default.RepackPath, (repackName + ".PAC"));
            File.Copy(tempFileList.Last(), repackPath, true);
        }

        private void cleanTempFiles()
        {
            foreach(string tempFile in tempFileList)
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }

            tempFileList.Clear();
        }
    }
}
