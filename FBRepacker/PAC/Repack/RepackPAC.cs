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
    public class RepackPAC : Internals
    {

        //Dictionary<string, string> fileInfo = new Dictionary<string, string>();
        Dictionary<int, string[]> fileInfoDic = new Dictionary<int, string[]>();
        Dictionary<string, string> extensionEquivalentDic = new Dictionary<string, string>
        {
            {".dds", ".NTP3"},
        };

        // turn this into list??
        public Dictionary<int, GeneralFileInfo> parsedFileInfo = new Dictionary<int, GeneralFileInfo>();
        List<FileInfo> realFileInfos = new List<FileInfo>();

        /*
        Dictionary<int, FHMFileInfo> FHMFileInfoDic = new Dictionary<int, FHMFileInfo>();
        Dictionary<int, GeneralFileInfo> GeneralFileInfoDic = new Dictionary<int, GeneralFileInfo>();

        Dictionary<int, FHMFileInfo> realFHMFileDic = new Dictionary<int, FHMFileInfo>();
        Dictionary<int, GeneralFileInfo> realGeneralFileDic = new Dictionary<int, GeneralFileInfo>();

        List<string> tempFileList = new List<string>();
        */

        public NTP3 repackNTP3 = new NTP3();
        public EIDX repackEIDX = new EIDX();

        int fileNumberinInfoFile = 1;

        string repackName = string.Empty;

        public RepackPAC(string filePath) : base()
        {
            
        }

        public void initializePACInfoFileRepack()
        {
            // TODO: make info file read byte easier, instead of using readalllines (since there are binary data inside, encoding / decoding will cause different results)
            rootDirectory = Properties.Settings.Default.OpenRepackPath;
            string path = rootDirectory + @"\PAC.info";
            if (File.Exists(path))
            {
                infoStreamRead = new StreamReader(path);
                infoFileString = File.ReadAllLines(path, Encoding.UTF8);
            }
            else
            {
                throw new Exception("PAC.info file not found! Make sure that the file is present in the root folder of your repack folder.");
            }
        }

        public void repackPAC()
        {
            //currDirectory = Properties.Settings.Default.OpenRepackPath;
            DirectoryInfo repackFolder = new DirectoryInfo(rootDirectory);
            repackName = repackFolder.Name;

            repackFHMFilesV2(rootDirectory);
            //repackFiles(rootDirectory);
            //repackEndFile();
            //cleanTempFiles();

            resetVariables();
        }

        public void parseInfo()
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

            infoStreamRead.Close();
            
            switch (fileInfoDic.First().Value.Skip(3).First())
            {
                case "--FHM--":
                    parseFHMInfo(fileNumberinInfoFile, true); // always 1
                    parseFHMFileInfoReferences(parsedFileInfo[1]); // always pass the 1st file, as the first file is the first FHM.
                    break;
                case "--STREAM--":
                    // TODO: Read Stream file infos
                    break;
                default:
                    break;
            }

            checkInfoSequence();
        }

        private void checkInfoSequence()
        {
            List<int> keys = parsedFileInfo.Keys.ToList();
            List<GeneralFileInfo> fileInfos = parsedFileInfo.Values.ToList();

            if (!keys.SequenceEqual(Enumerable.Range(1, keys.Count())))
                throw new Exception("parsedFileInfo's keys are not sequential!");

            for (int i = 0; i < parsedFileInfo.Count(); i++)
            {
                if (keys[i] != fileInfos[i].fileNo)
                    throw new Exception("Key is not the same as fileInfo's fileNo!");
            }
        }

        private void parseFHMFileInfoReferences(GeneralFileInfo fileInfo)
        {
            int FHMFileNumber = fileInfo.FHMFileNumber;

            if(FHMFileNumber != 0)
            {
                if (!parsedFileInfo.ContainsKey(FHMFileNumber))
                    throw new Exception("Cannot find corresponding FHMFileNumber of: " + FHMFileNumber + " in parsedFileInfo!");

                string FHMFileName = parsedFileInfo[FHMFileNumber].fileName;
                fileInfo.FHMFileName = FHMFileName;
            }

            if (fileInfo.header == "fhm")
            {
                List<int> filePointers = new List<int>();
                //List<GeneralFileInfo> allChildFileInfos =;

                List<int> allChildFileInfoIndexes = parsedFileInfo.Where(s => s.Value.FHMFileNumber == fileInfo.fileNo).Select(x => x.Key).ToList();

                if (fileInfo.numberofFiles != allChildFileInfoIndexes.Count)
                    throw new Exception("number of child files in FHM dosen't match with the total FHM file number!");

                if(fileInfo.numberofFiles != 0)
                {
                    for(int i = 0; i < allChildFileInfoIndexes.Count(); i++)
                    {
                        var childFileInfos = parsedFileInfo[allChildFileInfoIndexes[i]];

                        if (childFileInfos.isLinked)
                        {
                            int linkedFileNo = childFileInfos.linkedFileNo - 1;

                            if (allChildFileInfoIndexes.Count() < linkedFileNo)
                                throw new Exception("linkedFileNo exceed index of allChildFiles in index!");

                            string linkedFileName = parsedFileInfo[allChildFileInfoIndexes[linkedFileNo]].fileName;
                            parsedFileInfo[allChildFileInfoIndexes[i]].linkedFileName = linkedFileName;
                        }

                        parseFHMFileInfoReferences(childFileInfos);
                    }
                }
            }
        }

        private void parseFHMInfo(int fileNumber, bool isRootFHM)
        {
            // For the .fhm File
            int fileNumberinFHM = 1;
            string[] FHMinfos = fileInfoDic[fileNumber];
            //FHMFileInfo fhmFileInfo = new FHMFileInfo();
            //FHMFileInfoDic[fileNumber] = fhmFileInfo;

            GeneralFileInfo fhmFile = new GeneralFileInfo();

            if (parsedFileInfo.ContainsKey(fileNumber))
                fhmFile = parsedFileInfo[fileNumber];

            // parse and get info of FHM from PAC.info
            //fhmFileInfo.totalFileSize = convertStringtoInt(getSpecificFileInfoProperties("Total file size: ", FHMinfos));
            //fhmFileInfo.numberofFiles = convertStringtoInt(getSpecificFileInfoProperties("Number of files: ", FHMinfos));
            //fhmFileInfo.FHMChunkSize = convertStringtoInt(getSpecificFileInfoProperties("FHM chunk size: ", FHMinfos));
            //fhmFileInfo.additionalInfoFlag = (additionalInfo)convertStringtoInt(getSpecificFileInfoProperties("Additional info flag: ", FHMinfos));

            fhmFile.totalFileSize = convertStringtoInt(getSpecificFileInfoProperties("Total file size: ", FHMinfos));
            fhmFile.numberofFiles = convertStringtoInt(getSpecificFileInfoProperties("Number of files: ", FHMinfos));
            fhmFile.FHMChunkSize = convertStringtoInt(getSpecificFileInfoProperties("FHM chunk size: ", FHMinfos));
            fhmFile.additionalInfoFlag = (additionalInfo)convertStringtoInt(getSpecificFileInfoProperties("Additional info flag: ", FHMinfos));
            fhmFile.fileName = getSpecificFileInfoProperties("fileName: ", FHMinfos);
            fhmFile.fileNo = fileNumber;
            fhmFile.header = "fhm";

            parsedFileInfo[fileNumber] = fhmFile;

            for (int i = 1; i <= fhmFile.numberofFiles; i++)
            {
                // For the rest of the files in the FHM tag in PAC.info
                fileNumberinInfoFile++;
                string[] newFileInfos = fileInfoDic[fileNumberinInfoFile];
                parseGeneralFileInfo(newFileInfos, fileNumberinInfoFile, fileNumberinFHM, fhmFile.additionalInfoFlag);
                fileNumberinFHM++;
            }

            // For endfile
            if (isRootFHM)
            {
                fileNumberinInfoFile++;
                string[] endFileInfos = fileInfoDic[fileNumberinInfoFile];
                parseGeneralFileInfo(endFileInfos, fileNumberinInfoFile, fileNumberinFHM, fhmFile.additionalInfoFlag);
            }
        }

        private void parseGeneralFileInfo(string[] newFileInfos, int fileNo, int fileNoinFHM, additionalInfo additionalInfoFlag)
        {
            //GeneralFileInfo generalFileInfo = new GeneralFileInfo();

            string header = getSpecificFileInfoProperties("Header: ", newFileInfos);

            GeneralFileInfo fhmFile = new GeneralFileInfo();

            if (header != "endfile")
            {
                // parse and get infos from PAC.info
                // we should link up duplicate infos, but this will suffice for now
                int FHMOffset = convertStringtoInt(getSpecificFileInfoProperties("FHMOffset: ", newFileInfos));
                int fileSize = convertStringtoInt(getSpecificFileInfoProperties("Size: ", newFileInfos));
                int FHMassetEnum = convertStringtoInt(getSpecificFileInfoProperties("FHMAssetLoadEnum: ", newFileInfos));
                int FHMunkEnum = convertStringtoInt(getSpecificFileInfoProperties("FHMunkEnum: ", newFileInfos));
                int FHMFileNo = convertStringtoInt(getSpecificFileInfoProperties("FHMFileNo: ", newFileInfos));
                string fileName = getSpecificFileInfoProperties("fileName: ", newFileInfos);

                // parse and get infos from PAC.info
                //generalFileInfo.FHMOffset = FHMOffset;
                //generalFileInfo.fileSize = fileSize;
                //generalFileInfo.header = header;
                //generalFileInfo.fileNo = fileNumber;
                //generalFileInfo.fileNoinFHM = fileNoinFHM;

                fhmFile.FHMOffset = FHMOffset;
                fhmFile.fileSize = fileSize;
                fhmFile.header = header;
                fhmFile.fileNo = fileNo;
                //fhmFile.fileNoinFHM = fileNoinFHM;
                fhmFile.FHMAssetLoadEnum = FHMassetEnum;
                fhmFile.FHMunkEnum = FHMunkEnum;
                fhmFile.FHMFileNumber = FHMFileNo;
                fhmFile.fileName = fileName;

                if (checkLinked(newFileInfos))
                {
                    //generalFileInfo.isLinked = true;
                    //generalFileInfo.linkFileNumber = convertStringtoInt(getSpecificFileInfoProperties("LinkFHMOffset: ", newFileInfos));
                    fhmFile.isLinked = true;
                    fhmFile.linkedFileNo = convertStringtoInt(getSpecificFileInfoProperties("LinkedFileNo_in_FHM: ", newFileInfos));
                }

                parsedFileInfo[fileNo] = fhmFile;

                switch (fhmFile.header)//generalFileInfo.header
                {
                    case "fhm":
                        parseFHMInfo(fileNo, false);
                        break;
                    case "NTP3":
                        // TODO: should check if the fileNumber has been parsed before instead of checking linked. Hopefully there's no linked non-multi NTP3.
                        if (!fhmFile.isLinked)//!generalFileInfo.isLinked
                        {
                            // Pre Base64 code, TODO: Remove
                            // StreamReader NTP3Info = repackNTP3.getNTP3InfoStreamReader(newFileInfos);
                            repackNTP3.parseNTP3Info(newFileInfos, fileNo);
                        }
                        break;
                    case "EIDX":
                        repackEIDX.parseEIDXMetadata(newFileInfos);
                        break;
                    default:
                        break;
                }

                if(fhmFile.header != "fhm") // generalFileInfo.header != "fhm"
                {
                    //GeneralFileInfoDic[fileNo] = fhmFile; //generalFileInfo
                    //parsedFileInfo[fileNumber] = fhmFile;
                }

                parseAdditionalInfo(additionalInfoFlag, newFileInfos, fileNo, header);
            }
            else
            {
                // TODO: Check offset and Size with repacked file
                int EndFileOffset = convertStringtoInt(getSpecificFileInfoProperties("End File Offset: ", newFileInfos));
                int EndFileSize = convertStringtoInt(getSpecificFileInfoProperties("End File Size: ", newFileInfos));
                string fileName = getSpecificFileInfoProperties("fileName: ", newFileInfos);

                // parse and get infos from PAC.info
                //generalFileInfo.FHMOffset = EndFileOffset;
                //generalFileInfo.fileSize = EndFileSize;
                //generalFileInfo.header = header;
                //generalFileInfo.fileNo = fileNumber;

                fhmFile.FHMOffset = EndFileOffset;
                fhmFile.fileSize = EndFileSize;
                fhmFile.header = header;
                fhmFile.fileNo = fileNo;
                //fhmFile.fileNoinFHM = fileNoinFHM;
                fhmFile.fileName = fileName;

                //GeneralFileInfoDic[fileNo] = fhmFile; //generalFileInfo;
                parsedFileInfo[fileNo] = fhmFile;
            }
        }

        private void parseAdditionalInfo(additionalInfo additionalInfoFlag, string[] newFileInfos, int fileNo, string header)
        {
            if (additionalInfoFlag.HasFlag(additionalInfo.EIDX))
            {
                repackEIDX.parseEIDXInfo(newFileInfos, fileNo, header);
            }
        }

        private bool checkLinked(string[] newFileInfos)
        {
            return newFileInfos.Any(s => s.Contains("Link"));
        }
        
        private void repackFHMFilesV2(string directoryPath)
        {
            //string[] allfiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

            Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories).ToList().ForEach( s => {
                FileInfo tempFileInfo = new FileInfo(s);
                realFileInfos.Add(tempFileInfo);
             });

            byte[] repackedFile = repackFHMFiles(parsedFileInfo.First().Value, out int unused);
            repackedFile = appendEndFile(repackedFile, parsedFileInfo.FirstOrDefault(s => s.Value.header == "endfile").Value);

            string repackPath = Path.Combine(Properties.Settings.Default.RepackPath, (repackName + ".PAC"));
            FileStream PAC = File.Create(repackPath);
            PAC.Write(repackedFile, 0, repackedFile.Length);
            PAC.Close();
            //verifyFileNumber(filesandDirectories, number_of_files, directoryPath);
        }

        private byte[] repackFHMFiles(GeneralFileInfo fileInfo, out int beforeAppendSize)
        {
            int fileNumber = fileInfo.fileNo;
            beforeAppendSize = 0;

            if (fileInfo.header == "fhm")
            {
                MemoryStream FHMStream = new MemoryStream();
                List<int> filePointers = new List<int>();
                List<int> fileSizes = new List<int>();
                List<int> assetLoadEnums = new List<int>();
                List<byte[]> filesByteArray = new List<byte[]>();

                appendUIntMemoryStream(FHMStream, 0x46484D20, true); // FHM Magic
                appendUIntMemoryStream(FHMStream, 0x01010010, true); // Unknown Flag, constant
                appendZeroMemoryStream(FHMStream, 4);
                appendIntMemoryStream(FHMStream, fileInfo.totalFileSize, true); // Supposed to be the FHM size, but very weird, so we reuse it for identification purposes (system does not read these)

                appendIntMemoryStream(FHMStream, fileInfo.numberofFiles, true);

                List<GeneralFileInfo> allChildFileInfos = parsedFileInfo.Values.Where(s => s.FHMFileNumber == fileNumber).ToList();
                
                if (fileInfo.numberofFiles != allChildFileInfos.Count)
                    throw new Exception("number of child files in FHM dosen't match with the total FHM file number!");

                if(fileInfo.numberofFiles != 0)
                {
                    // Calculate the FHM Chunk Size based on the number of files
                    // each child files has 4 4 byte info, pointer, size, assetLoadEnum and unkEnum.
                    int FHMChunkSize = addPaddingSizeCalculation((allChildFileInfos.Count * 0x04) * 4 + (int)FHMStream.Length);

                    // The first pointer is the end of FHM, or the FHMChunkSize.
                    filePointers.Add(FHMChunkSize);

                    foreach (var childFileInfos in allChildFileInfos)
                    {
                        if (!childFileInfos.isLinked)
                        {
                            byte[] tempByteArray = repackFHMFiles(childFileInfos, out beforeAppendSize);
                            filePointers.Add(tempByteArray.Length + filePointers.Last());
                            fileSizes.Add(beforeAppendSize);
                            assetLoadEnums.Add(childFileInfos.FHMAssetLoadEnum);
                            filesByteArray.Add(tempByteArray);
                        }
                        else
                        {
                            int linkedFileNo = allChildFileInfos.IndexOf(allChildFileInfos.FirstOrDefault( s => s.fileName == childFileInfos.linkedFileName));
                            byte[] tempByteArray = new byte[0];

                            // Assuming the same pointer has already been added, since we sort the pointers when repacking.
                            if (filePointers.Count() < linkedFileNo)
                                throw new Exception("Cannot find file number: " + linkedFileNo + " in filePointers!");

                            filePointers.Insert(filePointers.Count() - 2, filePointers[linkedFileNo]);
                            fileSizes.Insert(fileSizes.Count() - 1, fileSizes[linkedFileNo]);

                            //fileSizes.Add(fileSizes[linkedFileNo - 1]);
                            assetLoadEnums.Insert(assetLoadEnums.Count() - 1, assetLoadEnums[linkedFileNo]);
                            filesByteArray.Add(tempByteArray);
                        }
                    }

                    for (int i = 0; i < allChildFileInfos.Count; i++)
                    {
                        appendIntMemoryStream(FHMStream, filePointers[i], true);
                    }

                    for (int i = 0; i < allChildFileInfos.Count; i++)
                    {
                        appendIntMemoryStream(FHMStream, fileSizes[i], true);
                    }

                    for (int i = 0; i < allChildFileInfos.Count; i++)
                    {
                        appendIntMemoryStream(FHMStream, assetLoadEnums[i], true);
                    }

                    for (int i = 0; i < allChildFileInfos.Count; i++)
                    {
                        appendZeroMemoryStream(FHMStream, 0x04);
                    }

                    addPaddingStream(FHMStream);

                    for (int i = 0; i < allChildFileInfos.Count; i++)
                    {
                        FHMStream.Write(filesByteArray[i], 0, filesByteArray[i].Length);
                    }
                }

                beforeAppendSize = (int)FHMStream.Length; // FHM file size is unused, so no matter if they are appended or not.

                return FHMStream.ToArray();
            }
            else
            {
                bool ifFileExist = realFileInfos.Any(s => s.Name == fileInfo.fileName);

                if (!ifFileExist)
                    throw new Exception("Cannot find file " + fileInfo.fileName);

                FileInfo file = realFileInfos.FirstOrDefault(s => s.Name == fileInfo.fileName);

                switch (fileInfo.header)
                {
                    case "NTP3":
                        byte[] NTP3Buffer = repackNTP3.repackDDStoNTP3(file, fileNumber);
                        beforeAppendSize = NTP3Buffer.Length;
                        NTP3Buffer = addPaddingArrayBuffer(NTP3Buffer);
                        return NTP3Buffer;

                    case "EIDX":
                        byte[] EIDXBuffer = repackEIDX.repackEIDX();
                        beforeAppendSize = EIDXBuffer.Length;
                        EIDXBuffer = addPaddingArrayBuffer(EIDXBuffer);
                        return EIDXBuffer;

                    default:
                        byte[] generalBuffer = new byte[file.OpenRead().Length];
                        file.OpenRead().Read(generalBuffer, 0, generalBuffer.Length);
                        beforeAppendSize = generalBuffer.Length;
                        generalBuffer = addPaddingArrayBuffer(generalBuffer);
                        return generalBuffer;
                }

                
            }
        }

        private byte[] appendEndFile(byte[] arraytoAppend, GeneralFileInfo endFileInfo)
        {
            bool ifFileExist = realFileInfos.Any(s => s.Name == endFileInfo.fileName);

            if(!ifFileExist)
                throw new Exception("Cannot find endFile " + endFileInfo.fileName);

            FileInfo file = realFileInfos.FirstOrDefault(s => s.Name == endFileInfo.fileName);

            byte[] endFileBuffer = new byte[file.OpenRead().Length];
            file.OpenRead().Read(endFileBuffer, 0, endFileBuffer.Length);

            int originalSize = arraytoAppend.Length;
            Array.Resize(ref arraytoAppend, arraytoAppend.Length + endFileBuffer.Length);

            Buffer.BlockCopy(endFileBuffer, 0, arraytoAppend, originalSize, endFileBuffer.Length);

            return arraytoAppend;
        }
        
        /*
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
                            case ".EIDX":
                                byte[] EIDXBuffer = repackEIDX.repackEIDX();
                                repackGeneral(file, fileNumber, fileNumberinFHM, EIDXBuffer, repackFileStream, filePointersandSizeinFHM, filePathListinFHM, lastFile);
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
                int linkedFileNoinFHM = GeneralFileInfoDic[fileNumber].linkedFileNo;
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

        private void verifyFileNumber(string[] filesandDirectories, int number_of_files, string directoryPath)
        {
            int number_of_real_files = filesandDirectories.Length;

            foreach (var files in filesandDirectories)
            {
                if (files.Contains(".endfile") || files.Contains(".info"))
                    number_of_real_files -= 1;
            }

            if (number_of_files != number_of_real_files)
                throw new Exception("Error: " + directoryPath + " have " + number_of_real_files + " number of files while info file only has " + number_of_files + " in this FHM.");
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
        */

        public void addPACInfo(Dictionary<int, GeneralFileInfo> parsedFileInfos, List<GeneralFileInfo> newFileInfos)
        {
            checkInfoSequence();
            List<GeneralFileInfo> originalFileInfoList = parsedFileInfos.Values.ToList();

            //foreach()
        }

        public void rebuildPACInfo(Dictionary<int, GeneralFileInfo> newFileInfos)
        {
            if (newFileInfos.First().Value.header != "fhm")
                throw new Exception("First info is not an fhm!");

            foreach (var FileInfos in newFileInfos)
            {
                if(FileInfos.Value.header != "endfile")
                {
                    if (!FileInfos.Value.isLinked)
                    {
                        if (FileInfos.Key != 1)
                        {
                            createFHMPACInfoTag(FileInfos.Key, false);

                            appendPACInfo("FHMOffset: " + FileInfos.Value.FHMOffset);
                            appendPACInfo("Size: " + FileInfos.Value.fileSize);
                            appendPACInfo("FHMAssetLoadEnum: " + FileInfos.Value.FHMAssetLoadEnum);
                            appendPACInfo("FHMunkEnum: " + FileInfos.Value.FHMunkEnum);
                            appendPACInfo("FHMFileNo: " + FileInfos.Value.FHMFileNumber);
                            appendPACInfo("Header: " + FileInfos.Value.header);

                            if (FileInfos.Value.header != "fhm" && FileInfos.Value.header != "NTP3")
                                appendPACInfo("fileName: " + FileInfos.Value.fileName);

                            switch (FileInfos.Value.header)
                            {
                                case "fhm":
                                    createFHMPACInfoTag(FileInfos.Key, true);
                                    appendPACInfo("Total file size: " + FileInfos.Value.totalFileSize);
                                    appendPACInfo("Number of files: " + FileInfos.Value.numberofFiles);
                                    appendPACInfo("FHM chunk size: " + FileInfos.Value.FHMChunkSize);
                                    appendPACInfo("fileName: " + FileInfos.Value.fileName);
                                    appendPACInfo("Additional info flag: " + (int)FileInfos.Value.additionalInfoFlag);
                                    break;

                                case "NTP3":
                                    if (!repackNTP3.NTP3FileInfoDic.ContainsKey(FileInfos.Key))
                                        throw new Exception("Cannot find fileNumber: " + FileInfos.Key + " in NTP3FileInfoDic!");

                                    List<NTP3FileInfo> allDDS = repackNTP3.NTP3FileInfoDic[FileInfos.Key];

                                    appendPACInfo("Number of Files: " + allDDS.Count);

                                    foreach (var dds in allDDS)
                                    {
                                        appendPACInfo("#DDS: " + dds.fileNo);
                                        appendPACInfo("Name: " + BitConverter.ToString(dds.hexName).Replace("-", ""));
                                        appendPACInfo("DDS Data Chunk Size: " + dds.DDSDataChunkSize);
                                        appendPACInfo("NTP3 Header Chunk Size: " + dds.NTP3HeaderChunkSize);
                                        appendPACInfo("numberofMipmaps: " + dds.numberofMipmaps);
                                        appendPACInfo("Width Resolution: " + dds.widthReso);
                                        appendPACInfo("Height Resolution: " + dds.heightReso);
                                        appendPACInfo("Compression Type: " + dds.CompressionType);
                                        appendPACInfo("fileName: " + dds.fileName);

                                        if (dds.CompressionType == "No Compression")
                                            appendPACInfo("pixelFormat: " + dds.pixelFormat);

                                        appendPACInfo("eXtChunk: " + Convert.ToBase64String(dds.eXtChunk));
                                        appendPACInfo("GIDXChunk: " + Convert.ToBase64String(dds.GIDXChunk));
                                    }
                                    break;

                                case "EIDX":
                                    appendPACInfo("EIDX_Str1: " + repackEIDX.str1);
                                    appendPACInfo("EIDX_Str2: " + repackEIDX.str2);
                                    appendPACInfo("EIDX_ALEO_Number: " + repackEIDX.ALEO_number);
                                    appendPACInfo("EIDX_ALEO_Offset: " + repackEIDX.ALEO_offset);
                                    appendPACInfo("EIDX_NUT_Number: " + repackEIDX.NUT_number);
                                    appendPACInfo("EIDX_NUT_Offset: " + repackEIDX.NUT_offset);
                                    appendPACInfo("EIDX_NUD_Number: " + repackEIDX.NUD_number);
                                    appendPACInfo("EIDX_NUD_Offset: " + repackEIDX.NUD_offset);
                                    break;

                                default:

                                    break;
                            }

                            var FHMAdditionalInfoFlag = newFileInfos[FileInfos.Value.FHMFileNumber].additionalInfoFlag;

                            if (FHMAdditionalInfoFlag != 0)
                            {
                                if (FHMAdditionalInfoFlag.HasFlag(additionalInfo.EIDX))
                                {
                                    if (!repackEIDX.EIDXFileInfoDic.ContainsKey(FileInfos.Key))
                                        throw new Exception("Cannot find fileNumber: " + FileInfos.Key + " in EIDXFileInfoDic!");

                                    var EIDXFileInfoDic = repackEIDX.EIDXFileInfoDic[FileInfos.Key];

                                    appendPACInfo("EIDX_Index: " + EIDXFileInfoDic.file_Index);
                                    appendPACInfo("EIDX_Name: " + EIDXFileInfoDic.file_Hash);
                                }
                            }
                        }
                        else
                        {
                            appendPACInfo("--1--");
                            appendPACInfo("FHMOffset: " + FileInfos.Value.FHMOffset);
                            appendPACInfo("Header: " + FileInfos.Value.header);
                            appendPACInfo("--FHM--");
                            appendPACInfo("Total file size: " + FileInfos.Value.totalFileSize);
                            appendPACInfo("Number of files: " + FileInfos.Value.numberofFiles);
                            appendPACInfo("FHM chunk size: " + FileInfos.Value.FHMChunkSize);
                            appendPACInfo("fileName: " + FileInfos.Value.fileName);
                            appendPACInfo("Additional info flag: " + (int)FileInfos.Value.additionalInfoFlag);
                        }
                    }
                    else
                    {
                        createFHMPACInfoTag(FileInfos.Key, false);

                        appendPACInfo("LinkedFileNo_in_FHM: " + FileInfos.Value.linkedFileNo);
                        appendPACInfo("FHMOffset: " + FileInfos.Value.FHMOffset);
                        appendPACInfo("Size: " + FileInfos.Value.fileSize);
                        appendPACInfo("FHMAssetLoadEnum: " + FileInfos.Value.FHMAssetLoadEnum);
                        appendPACInfo("FHMunkEnum: " + FileInfos.Value.FHMunkEnum);
                        appendPACInfo("FHMFileNo: " + FileInfos.Value.FHMFileNumber);
                        appendPACInfo("Header: " + FileInfos.Value.header);

                        if (FileInfos.Value.header != "fhm")
                            appendPACInfo("fileName: " + FileInfos.Value.fileName);
                    }
                    
                }
                else
                {
                    createFHMPACInfoTag(FileInfos.Key, false);
                    appendPACInfo("Header: " + FileInfos.Value.header);
                    appendPACInfo("End File Offset: " + FileInfos.Value.FHMOffset);
                    appendPACInfo("End File Size: " + FileInfos.Value.fileSize);
                    appendPACInfo("fileName: " + FileInfos.Value.fileName);
                    appendPACInfo("");
                    appendPACInfo("//");
                    appendPACInfo("");
                }

                fileNumber++;
            }

            fileNumber = 1;
        }
    }
}
