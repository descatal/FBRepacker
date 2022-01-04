using FBRepacker.PAC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FBRepacker.Psarc.V2.PACFileInfoV2;

namespace FBRepacker.Psarc.V2
{
    internal class RepackPsarcV2 : Internals
    {
        public RepackPsarcV2()
        {
            
        }

        public void exportTocJSON()
        {
            TOCFileInfo Toc = parseToc();

            string name = Path.GetFileNameWithoutExtension(Properties.Settings.Default.inputPsarcTBLBinary);
            if (Properties.Settings.Default.identifyPACFilesTBLParse)
                name = Path.GetFileName(Properties.Settings.Default.psarcTBLParseRepackFolder);

            string json = JsonConvert.SerializeObject(Toc, Formatting.Indented);
            StreamWriter jsonSW = File.CreateText(Properties.Settings.Default.outputPsarcTBLJson + @"\" + name + @".json");
            jsonSW.Write(json);
            jsonSW.Close();
        }

        public TOCFileInfo importTocJSON()
        {
            StreamReader sR = File.OpenText(Properties.Settings.Default.inputPsarcJSON);
            string json = sR.ReadToEnd();
            sR.Close();

            TOCFileInfo Toc = JsonConvert.DeserializeObject<TOCFileInfo>(json);
            return Toc;
        }

        public TOCFileInfo parseToc()
        {
            FileStream TBL = File.OpenRead(Properties.Settings.Default.inputPsarcTBLBinary);
            TOCFileInfo toc = new TOCFileInfo();

            uint fileCount;
            List<uint> fileNameSizes = new List<uint>();
            List<bool> fileSubFolderFlags = new List<bool>();
            List<uint> fileIndexPointers = new List<uint>();
            List<PACFileInfoV2> fileInfos = new List<PACFileInfoV2>();

            changeStreamFile(TBL);

            Stream.Seek(0, SeekOrigin.Begin);
            int magic = readIntBigEndian(Stream.Position);

            if (magic != 0x54424C20)
            {
                throw new Exception("PATCH.TBL is not in TBL format!");
            }

            Stream.Seek(0x04, SeekOrigin.Current);

            fileCount = readUIntBigEndian(Stream.Position);
            uint totalFileCount = readUIntBigEndian(Stream.Position);
            toc.totalFileEntries = totalFileCount;

            ushort subFolderFlag = readUShort(Stream.Position, true);
            bool hasSubFolder = subFolderFlag == 0x8000 ? true : false;
            fileSubFolderFlags.Add(hasSubFolder);

            ushort initFileNamePointer = readUShort(Stream.Position, true);
            uint prevFileNamePointer = initFileNamePointer;

            for (int i = 0; i < fileCount; i++)
            {
                if (i != fileCount - 1)
                {
                    subFolderFlag = readUShort(Stream.Position, true);
                    hasSubFolder = subFolderFlag == 0x8000 ? true : false;
                    fileSubFolderFlags.Add(hasSubFolder);
                    uint currentFileNamePointer = readUShort(Stream.Position, true);
                    fileNameSizes.Add(currentFileNamePointer - prevFileNamePointer);
                    prevFileNamePointer = currentFileNamePointer;
                }
                else
                {
                    fileNameSizes.Add((uint)Stream.Length - prevFileNamePointer);
                }
            }

            for (int i = 0; i < totalFileCount; i++)
            {
                fileIndexPointers.Add(readUIntBigEndian(Stream.Position));
            }

            uint fileNamePointer = initFileNamePointer;
            for (int i = 0; i < fileCount; i++)
            {
                PACFileInfoV2 pacFileInfo = new PACFileInfoV2();
                uint nameSize = fileNameSizes[i];
                Stream.Seek(fileNamePointer, SeekOrigin.Begin);
                string relativePatchPath = readString(Stream.Position, nameSize);
                pacFileInfo.relativePatchPath = relativePatchPath;
                string nameHash = Path.GetFileNameWithoutExtension(relativePatchPath);
                if (nameHash.Contains("PATCH"))
                {
                    pacFileInfo.namePrefix = prefixEnum.PATCH;
                    nameHash = nameHash.Replace("PATCH", "");
                }
                else if (nameHash.Contains("STREAM"))
                {
                    pacFileInfo.namePrefix = prefixEnum.STREAM;
                    nameHash = nameHash.Replace("STREAM", "");
                }
                else
                {
                    pacFileInfo.namePrefix = prefixEnum.NONE;
                }
                if (!uint.TryParse(nameHash, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint res))
                    throw new Exception("Failed to convert " + nameHash + " to uint!");
                pacFileInfo.hasRelativePatchSubPath = fileSubFolderFlags[i];
                pacFileInfo.fileFlags = fileFlagsEnum.hasFileName;
                //pacFileInfo.relativePathIndex = (uint)i;
                pacFileInfo.nameHash = res;
                fileNamePointer += nameSize;
                fileInfos.Add(pacFileInfo);
            }

            List<uint> non_Zero_fileIndexPointers = fileIndexPointers.Where(s => !s.Equals(0)).ToList();

            for (int i = 0; i < non_Zero_fileIndexPointers.Count; i++)
            {
                bool newInfo = false;
                Stream.Seek(non_Zero_fileIndexPointers[i], SeekOrigin.Begin);
                patchNoEnum patchNo = (patchNoEnum)readUIntBigEndian(Stream.Position);
                uint relativePathNo = readUIntBigEndian(Stream.Position);
                uint unk04 = readUIntBigEndian(Stream.Position);
                uint Size1 = readUIntBigEndian(Stream.Position);
                uint Size2 = readUIntBigEndian(Stream.Position);
                uint Size3 = readUIntBigEndian(Stream.Position);
                uint unk00 = readUIntBigEndian(Stream.Position);
                uint nameHash = readUIntBigEndian(Stream.Position);
                int fileIndex = fileIndexPointers.FindIndex(a => a.Equals(non_Zero_fileIndexPointers[i]));

                PACFileInfoV2 pacFileInfo = fileInfos.FirstOrDefault(a => a.nameHash.Equals(nameHash));

                if (pacFileInfo == null)
                {
                    pacFileInfo = new PACFileInfoV2();
                    newInfo = true;
                }
                else
                {
                    //if (relativePathNo != pacFileInfo.relativePathIndex)
                        //throw new Exception("Different relative Path Index between name and fileInfo for nameHash " + nameHash.ToString("X8"));
                }

                pacFileInfo.fileFlags |= fileFlagsEnum.hasFileInfo;
                pacFileInfo.patchNo = patchNo;
                //pacFileInfo.relativePathIndex = relativePathNo;
                pacFileInfo.unk04 = unk04;
                pacFileInfo.Size1 = Size1;
                pacFileInfo.Size2 = Size2;
                pacFileInfo.Size3 = Size3;
                pacFileInfo.unk00 = unk00;
                pacFileInfo.nameHash = nameHash;
                pacFileInfo.fileInfoIndex = fileIndex;

                if(Properties.Settings.Default.identifyPACFilesTBLParse)
                {
                    // Trying to find the file inside the folder.
                    string[] allFiles = Directory.GetFiles(Properties.Settings.Default.psarcTBLParseRepackFolder, "*", SearchOption.AllDirectories);

                    string nameHashStr = nameHash.ToString("X8");
                    string path = allFiles.FirstOrDefault(s => s.Contains(nameHashStr));

                    if (path != null)
                        pacFileInfo.fileFlags |= fileFlagsEnum.hasFilePath;
                    pacFileInfo.filePath = path;
                }

                if (newInfo)
                    fileInfos.Add(pacFileInfo);
            }

            toc.allFiles = fileInfos;
            return toc;
        }

        public MemoryStream writeToc(TOCFileInfo Toc)
        {
            List<PACFileInfoV2> fileInfos = Toc.allFiles;
            uint totalFileCount = Toc.totalFileEntries;

            Dictionary<int, long> fileInfoOffsets = new Dictionary<int, long>();

            List<PACFileInfoV2> onlyFileInfoswithNames = fileInfos.Where(a => a.fileFlags.HasFlag(fileFlagsEnum.hasFileName)).ToList();
            List<PACFileInfoV2> onlyFileInfoswithIndex = fileInfos.Where(a => a.fileFlags.HasFlag(fileFlagsEnum.hasFileInfo)).ToList();

            // Depreceated as we just use data's position in JSON as is.
            /*
            // Validating index data: 
            List<uint> relPathIndexes = onlyFileInfoswithNames.Select(s => s.relativePathIndex).ToList();
            List<int> castedIndex = relPathIndexes.ConvertAll(x => (int)x);

            // Check duplicate
            List<int> duplicates = castedIndex.GroupBy(x => x)
                                        .SelectMany(g => g.Skip(1)).ToList();

            
            //List<PACFileInfo> newFiles = fileInfos.Where(a => a.fileInfoIndex > 6144).ToList();
            //totalFileCount += (uint)newFiles.Count;
            

            if (duplicates.Count >= 1)
            {
                string dupStr = string.Empty;
                foreach (int dup in duplicates)
                {
                    dupStr += " | " + dup;
                }
                throw new Exception("Found duplicate Relative Path Indexes! Duplicates: " + dupStr.ToString());
            }
            

            // Check sequential
            int missingNo = findMissing(castedIndex.ToArray(), castedIndex.Count());
            if (missingNo != -1)
                throw new Exception("Relative Path Indexes is not Consecutive! Found missing non consecutive number: " + missingNo.ToString());
            */

            // Check if there is dulplicate hashes
            // Validating nameHash data:
            List<uint> nameHashes = fileInfos.Select(s => s.nameHash).ToList();
            List<int> castedNameHashes = nameHashes.ConvertAll(x => (int)x);

            // Check duplicate
            List<int> duplicateNameHashes = castedNameHashes.GroupBy(x => x)
                                        .SelectMany(g => g.Skip(1)).ToList();

            if (duplicateNameHashes.Count >= 1)
            {
                string dupStr = string.Empty;
                foreach (int dup in duplicateNameHashes)
                {
                    dupStr += " | " + dup;
                }
                throw new Exception("Found duplicate Name Hash Indexes! Duplicates: " + dupStr.ToString());
            }

            uint fileNo = (uint)onlyFileInfoswithNames.Count;

            MemoryStream TBL = new MemoryStream();

            appendIntMemoryStream(TBL, 0x54424C20, true);
            appendIntMemoryStream(TBL, 0x01010000, true);
            appendUIntMemoryStream(TBL, fileNo, true);
            appendUIntMemoryStream(TBL, totalFileCount, true);

            MemoryStream fileNamePointersStream = new MemoryStream();
            MemoryStream fileIndexPointersStream = new MemoryStream();
            MemoryStream fileInfosStream = new MemoryStream();
            MemoryStream fileNamesStream = new MemoryStream();

            long fileInfoStart = TBL.Length + fileNo * 0x04 + totalFileCount * 0x04;

            uint startPointer = (uint)(0x10 + (onlyFileInfoswithNames.Count * 0x4) + (totalFileCount * 0x4));
            uint paddingRequired = addHalfPaddingSizeCalculation(startPointer) - startPointer;
            long fileNameStartPointer = fileInfoStart + (onlyFileInfoswithIndex.Count * 0x20) + paddingRequired;
            
            //List<PACFileInfoV2> orderedRelativePathIndex = onlyFileInfoswithNames.OrderBy(s => s.relativePathIndex).ToList();

            List<PACFileInfoV2> actualFileNameIndex = new List<PACFileInfoV2>();
            
            for (int i = 0; i < onlyFileInfoswithNames.Count; i++)
            {
                PACFileInfoV2 fileInfo = onlyFileInfoswithNames[i];
                //if (fileInfo.relativePathIndex != i)
                //throw new Exception("ordered Relative Path Index is not continious!");

                string relativePath = fileInfo.relativePatchPath;
                long initPos = fileNamesStream.Position;
                ushort subFolderFlag = fileInfo.hasRelativePatchSubPath ? (ushort)0x8000 : (ushort)0;
                appendStringMemoryStream(fileNamesStream, relativePath, Encoding.Default);
                appendZeroMemoryStream(fileNamesStream, 1);
                appendUIntMemoryStream(fileNamePointersStream, (uint)(fileNameStartPointer + initPos), true);
                //appendUShortMemoryStream(fileNamePointersStream, subFolderFlag, true);

                actualFileNameIndex.Add(fileInfo);
            }

            for (int i = 0; i < onlyFileInfoswithIndex.Count; i++)
            {
                PACFileInfoV2 fileInfo = onlyFileInfoswithIndex[i];

                int fileIndexes = fileInfo.fileInfoIndex;
                fileInfoOffsets[fileIndexes] = fileInfoStart + fileInfosStream.Position + paddingRequired;

                //foreach(int fileIndex in fileIndexes)
                //{
                //    fileInfoOffsets[fileIndex] = fileInfoStart + fileInfosStream.Position;
                //}

                appendUIntMemoryStream(fileInfosStream, (uint)fileInfo.patchNo, true);

                if (fileInfo.fileFlags.HasFlag(fileFlagsEnum.hasFileName))
                {
                    int trueIndex = actualFileNameIndex.FindIndex(x => x == fileInfo);
                    appendUIntMemoryStream(fileInfosStream, (uint)trueIndex, true);
                }
                else
                {
                    appendUIntMemoryStream(fileInfosStream, (uint)0, true);
                }
                
                appendIntMemoryStream(fileInfosStream, 0x00040000, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.Size1, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.Size2, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.Size3, true);
                appendIntMemoryStream(fileInfosStream, 0, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.nameHash, true);
            }

            for (int i = 0; i < totalFileCount; i++)
            {
                if (fileInfoOffsets.ContainsKey(i))
                {
                    appendUIntMemoryStream(fileIndexPointersStream, (uint)fileInfoOffsets[i], true);
                }
                else
                {
                    appendZeroMemoryStream(fileIndexPointersStream, 0x04);
                }
            }

            // fileIndexPointer must be aligned to 8
            MemoryStream combinedfileNamePointersandIndexPointersStream = new MemoryStream();
            combinedfileNamePointersandIndexPointersStream.Write(fileNamePointersStream.ToArray(), 0, (int)fileNamePointersStream.Length);
            combinedfileNamePointersandIndexPointersStream.Write(fileIndexPointersStream.ToArray(), 0, (int)fileIndexPointersStream.Length);
            combinedfileNamePointersandIndexPointersStream = addHalfPaddingStream(combinedfileNamePointersandIndexPointersStream);

            //TBL.Write(fileNamePointersStream.ToArray(), 0, (int)fileNamePointersStream.Length);
            //TBL.Write(fileIndexPointersStream.ToArray(), 0, (int)fileIndexPointersStream.Length);

            TBL.Write(combinedfileNamePointersandIndexPointersStream.ToArray(), 0, (int)combinedfileNamePointersandIndexPointersStream.Length);
            TBL.Write(fileInfosStream.ToArray(), 0, (int)fileInfosStream.Length);
            TBL.Write(fileNamesStream.ToArray(), 0, (int)fileNamesStream.Length);

            return TBL;
        }

        private void updateFileSizes(List<PACFileInfoV2> fileInfos)
        {
            List<PACFileInfoV2> onlyFileInfoswithFilePaths = fileInfos.Where(a => a.fileFlags.HasFlag(fileFlagsEnum.hasFilePath)).ToList();
            foreach (PACFileInfoV2 fileInfo in onlyFileInfoswithFilePaths)
            {
                string filePath = fileInfo.filePath;
                if (!File.Exists(filePath))
                    throw new Exception(filePath + " does not exist / cannot be accessed!");

                FileInfo file = new FileInfo(filePath);
                uint size = (uint)file.Length;

                fileInfo.Size1 = size;
                fileInfo.Size2 = size;
                fileInfo.Size3 = size;
            }
        }

        // Function to return the missing element 
        public static int findMissing(int[] arr, int n)
        {

            int l = 0, h = n - 1;
            int mid;

            while (h > l)
            {

                mid = l + (h - l) / 2;

                // Check if middle element is consistent 
                if (arr[mid] - mid == arr[0])
                {

                    // No inconsistency till middle elements 
                    // When missing element is just after 
                    // the middle element 
                    if (arr[mid + 1] - arr[mid] > 1)
                        return arr[mid] + 1;
                    else
                    {

                        // Move right 
                        l = mid + 1;
                    }
                }
                else
                {

                    // Inconsistency found 
                    // When missing element is just before 
                    // the middle element 
                    if (arr[mid] - arr[mid - 1] > 1)
                        return arr[mid] - 1;
                    else
                    {

                        // Move left 
                        h = mid - 1;
                    }
                }
            }

            // No missing element found 
            return -1;
        }

        public void exportToc(TOCFileInfo Toc)
        {
            // Make backup to JSON files
            StreamReader sR = File.OpenText(Properties.Settings.Default.inputPsarcJSON);
            string json = sR.ReadToEnd();
            sR.Close();

            string name = Path.GetFileNameWithoutExtension(Properties.Settings.Default.inputPsarcJSON);
            string path = Path.GetDirectoryName(Properties.Settings.Default.inputPsarcJSON);
            StreamWriter backupsW = File.CreateText(path + @"\" + name + "_backup.json");
            backupsW.Write(json);
            backupsW.Close();

            updateFileSizes(Toc.allFiles);

            // Updated version of JSON
            json = JsonConvert.SerializeObject(Toc, Formatting.Indented);
            StreamWriter jsonSW = File.CreateText(path + @"\" + name + ".json");
            jsonSW.Write(json);
            jsonSW.Close();

            //TOCFileInfo Toc = importTocJSON();

            if (Properties.Settings.Default.outputPsarcTBLBinaryNameasPatch)
                name = "PATCH";

            MemoryStream newTBLMS = writeToc(Toc);
            FileStream TBLFS = File.Create(Properties.Settings.Default.outputPsarcTBLBinary + @"\" + name + @".TBL");
            TBLFS.Write(newTBLMS.ToArray(), 0, (int)newTBLMS.Length);
            newTBLMS.Flush();
            TBLFS.Flush();
            TBLFS.Close();
        }

        public void repackPsarc(string outputFileName)
        {
            string repackPath = Properties.Settings.Default.PsarcRepackFolder;
            string psarcexeSource = Path.Combine(Directory.GetCurrentDirectory(), @"3rd Party\Psarc\psarc.exe"), repackFilesUriArgs = string.Empty;
            string[] files = Directory.GetFiles(repackPath, "*", SearchOption.AllDirectories);

            // Create a new XML instead of passing args (due to limitation on command line length)

            StreamWriter psarcSW = File.CreateText(Path.Combine(Directory.GetCurrentDirectory(), @"3rd Party\Psarc\create.xml"));

            psarcSW.WriteLine(@"<?xml version=" + @"""" + "1.0" + @"""" + @"encoding=" + @"""" + "UTF - 8" + @"""" + @"?>");
            psarcSW.WriteLine(@"<psarc>");
            psarcSW.WriteLine(@"  <create overwrite = " + @"""" + "true" + @"""" + @"archive = " + @"""" + "Output.psarc" + @"""" + ">");

            foreach (var s in files)
            {
                Uri repackPathUri = new Uri(repackPath + @"\");
                Uri repackFilePathUri = new Uri(s);
                Uri repackFileRelativeUri = repackPathUri.MakeRelativeUri(repackFilePathUri);
                //repackFilesUriArgs += " " + repackFileRelativeUri.OriginalString;
                psarcSW.WriteLine(@"      <file path = " + @"""" + repackFileRelativeUri.OriginalString + @"""" + @"/>");
            }

            //Console.WriteLine(repackFilesUriArgs);

            psarcSW.WriteLine(@"  </create>");
            psarcSW.WriteLine(@"</psarc>");
            psarcSW.Close();

            FileStream fs = File.OpenRead(psarcexeSource);
            FileStream exeFs = File.Create(Path.Combine(repackPath, "psarc.exe"));

            fs.CopyTo(exeFs);
            exeFs.Close();
            fs.Close();

            //File.Copy(psarcexeSource, Path.Combine(repackPath, "psarc.exe"), true);

            /*
            using (Process psarc = new Process())
            {
                psarc.StartInfo.WorkingDirectory = repackPath;
                psarc.StartInfo.FileName = "psarc.exe";
                psarc.StartInfo.UseShellExecute = false;
                psarc.StartInfo.RedirectStandardOutput = true;
                psarc.StartInfo.CreateNoWindow = true;
                psarc.StartInfo.Arguments = @"create -y -v -oOutput.psarc" + repackFilesUriArgs;
                psarc.Start();
                Console.WriteLine(psarc.StandardOutput.ReadToEnd());
                psarc.WaitForExit();
            }
            */

            using (Process psarc = new Process())
            {
                psarc.StartInfo.WorkingDirectory = repackPath;
                psarc.StartInfo.FileName = "psarc.exe";
                psarc.StartInfo.UseShellExecute = false;
                psarc.StartInfo.RedirectStandardOutput = false;
                psarc.StartInfo.CreateNoWindow = false;
                psarc.StartInfo.Arguments = @"create --xml " + @"""" + Path.Combine(Directory.GetCurrentDirectory(), @"3rd Party\Psarc\create.xml") + @"""";
                psarc.Start();
                //Console.WriteLine(psarc.StandardOutput.ReadToEnd());
                psarc.WaitForExit();
            }

            File.Copy(Path.Combine(repackPath, "Output.psarc"), Path.Combine(Properties.Settings.Default.OutputRepackPsarc, outputFileName + ".psarc"), true);

            try
            {
                File.Delete(Path.Combine(repackPath, "Output.psarc"));
                File.Delete(Path.Combine(repackPath, "psarc.exe"));
            }
            catch (Exception e)
            {
                throw new Exception("Cannot delete. Error: " + e);
            }
        }
    }
}
