using System;
using System.IO;
using FBRepacker.PAC;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FBRepacker.Psarc.PACFileInfo;
using System.Globalization;

namespace FBRepacker.Psarc
{
    class Toc : Internals
    {
        public List<PACFileInfo> fileInfos = new List<PACFileInfo>();
        public uint totalFileCount { get; set; }

        public Toc()
        {

        }

        public void parseToc(FileStream TBL, string PsarcFolderPath)
        {
            uint fileCount;
            List<uint> fileNameSizes = new List<uint>();
            List<bool> fileSubFolderFlags = new List<bool>();
            List<uint> fileIndexPointers = new List<uint>();
            fileInfos = new List<PACFileInfo>();

            changeStreamFile(TBL);

            Stream.Seek(0, SeekOrigin.Begin);
            int magic = readIntBigEndian(Stream.Position);

            if(magic != 0x54424C20)
            {
                throw new Exception("PATCH.TBL is not in TBL format!");
            }

            Stream.Seek(0x04, SeekOrigin.Current);

            fileCount = readUIntBigEndian(Stream.Position);
            totalFileCount = readUIntBigEndian(Stream.Position);

            ushort subFolderFlag = readUShort(Stream.Position, true);
            bool hasSubFolder = subFolderFlag == 0x8000 ? true : false;
            fileSubFolderFlags.Add(hasSubFolder);

            ushort initFileNamePointer = readUShort(Stream.Position, true);
            uint prevFileNamePointer = initFileNamePointer;

            for(int i = 0; i < fileCount; i++)
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

            for(int i = 0; i < totalFileCount; i++)
            {
                fileIndexPointers.Add(readUIntBigEndian(Stream.Position));
            }

            uint fileNamePointer = initFileNamePointer;
            for (int i = 0; i < fileCount; i++)
            {
                PACFileInfo pacFileInfo = new PACFileInfo();
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
                pacFileInfo.relativePathIndex = (uint)i;
                pacFileInfo.nameHash = res;
                fileNamePointer += nameSize;
                fileInfos.Add(pacFileInfo);
            }

            string[] allFiles = Directory.GetFiles(PsarcFolderPath, "*", SearchOption.AllDirectories);

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

                PACFileInfo pacFileInfo = fileInfos.FirstOrDefault(a => a.nameHash.Equals(nameHash));

                if(pacFileInfo == null)
                {
                    pacFileInfo = new PACFileInfo();
                    newInfo = true;
                }
                else
                {
                    if (relativePathNo != pacFileInfo.relativePathIndex)
                        throw new Exception("Different relative Path Index between name and fileInfo for nameHash " + nameHash.ToString("X8"));
                }

                pacFileInfo.fileFlags |= fileFlagsEnum.hasFileInfo;
                pacFileInfo.patchNo = patchNo;
                pacFileInfo.relativePathIndex = relativePathNo;
                pacFileInfo.unk04 = unk04;
                pacFileInfo.Size1 = Size1;
                pacFileInfo.Size2 = Size2;
                pacFileInfo.Size3 = Size3;
                pacFileInfo.unk00 = unk00;
                pacFileInfo.nameHash = nameHash;
                pacFileInfo.fileInfoIndex = fileIndex;

                // Trying to find the file inside the folder.
                string nameHashStr = nameHash.ToString("X8");
                string path = allFiles.FirstOrDefault(s => s.Contains(nameHashStr));

                if (path != null)
                    pacFileInfo.fileFlags |= fileFlagsEnum.hasFilePath;
                    pacFileInfo.filePath = path;

                if (newInfo)
                    fileInfos.Add(pacFileInfo);
            }

            /*
            if (fileIndexPointers.Count != fileNameSizes.Count)
                throw new Exception("Index and Name count mismatch!");

            var fileNameandIndexes = fileNameSizes.Zip(fileIndexPointers, (s, p) => new { fileNameSize = s, fileIndexPointer = p });

            uint fileNamePointer = initFileNamePointer;

            string[] allFiles = Directory.GetFiles(PsarcFolderPath, "*", SearchOption.AllDirectories);

            foreach (var fileNameandIndex in fileNameandIndexes)
            {
                List<int> fileIndex = Enumerable.Range(0, fileIndexPointers.Count)
                                 .Where(i => fileIndexPointers[i] == fileNameandIndex.fileIndexPointer)
                                 .ToList();

                Stream.Seek(fileNameandIndex.fileIndexPointer, SeekOrigin.Begin);
                PACFileInfo pacFileInfo = new PACFileInfo();
                pacFileInfo.patchNo = (patchNoEnum)readUIntBigEndian(Stream.Position);
                pacFileInfo.fileNo = readUIntBigEndian(Stream.Position);
                pacFileInfo.unk04 = readUIntBigEndian(Stream.Position);
                pacFileInfo.Size1 = readUIntBigEndian(Stream.Position);
                pacFileInfo.Size2 = readUIntBigEndian(Stream.Position);
                pacFileInfo.Size3 = readUIntBigEndian(Stream.Position);
                pacFileInfo.unk00 = readUIntBigEndian(Stream.Position);
                pacFileInfo.nameHash = readUIntBigEndian(Stream.Position);
                pacFileInfo.fileIndex = fileIndex;

                string nameHash = pacFileInfo.nameHash.ToString("X8");
                string path = allFiles.FirstOrDefault(s => s.Contains(nameHash));

                if (path == null)
                    throw new Exception("Cannot find file: " + nameHash + " in Psarc Folder");

                pacFileInfo.filePath = path;

                uint nameSize = fileNameandIndex.fileNameSize;
                Stream.Seek(fileNamePointer, SeekOrigin.Begin);
                pacFileInfo.relativePatchPath = readString(Stream.Position, nameSize);
                fileNamePointer += nameSize;

                fileInfos.Add(pacFileInfo);
            }
             
             */
        }

        private void addFile(patchNoEnum patchNo, uint Size1, uint Size2, uint Size3, uint nameHash, string filePath, int fileIndex)
        {
            PACFileInfo pacFileInfo = new PACFileInfo();
            pacFileInfo.patchNo = patchNo;
            pacFileInfo.relativePathIndex = (uint)fileInfos.Count(); // Count starts from 1, but the fileNo Index starts from 0.
            pacFileInfo.unk04 = 0x00040000;
            pacFileInfo.Size1 = Size1;
            pacFileInfo.Size2 = Size2;
            pacFileInfo.Size3 = Size3;
            pacFileInfo.unk00 = 0;
            pacFileInfo.nameHash = nameHash;
            pacFileInfo.relativePatchPath = filePath;
            pacFileInfo.fileInfoIndex = fileIndex;
        }

        public MemoryStream writeToc()
        {
            Dictionary<int, long> fileInfoOffsets = new Dictionary<int, long>();

            List<PACFileInfo> onlyFileInfoswithNames = fileInfos.Where(a => a.fileFlags.HasFlag(fileFlagsEnum.hasFileName)).ToList();
            List<PACFileInfo> onlyFileInfoswithIndex = fileInfos.Where(a => a.fileFlags.HasFlag(fileFlagsEnum.hasFileInfo)).ToList();

            // Validating index data:
            List<uint> relPathIndexes = onlyFileInfoswithNames.Select(s => s.relativePathIndex).ToList();
            List<int> castedIndex = relPathIndexes.ConvertAll(x => (int)x);

            // Check duplicate
            List<int> duplicates = castedIndex.GroupBy(x => x)
                                        .SelectMany(g => g.Skip(1)).ToList();

            /*
            List<PACFileInfo> newFiles = fileInfos.Where(a => a.fileInfoIndex > 6144).ToList();
            totalFileCount += (uint)newFiles.Count;
            */

            if (duplicates.Count >= 1)
            {
                string dupStr = string.Empty;
                foreach(int dup in duplicates)
                {
                    dupStr += " | " + dup;
                }
                throw new Exception("Found duplicate Relative Path Indexes! Duplicates: " + dupStr.ToString());
            }
                
            // Check sequential
            int missingNo = findMissing(castedIndex.ToArray(), castedIndex.Count());
            if (missingNo != -1)
                throw new Exception("Relative Path Indexes is not Consecutive! Found missing non consecutive number: " + missingNo.ToString());

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

            updateFileSizes();

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

            for (int i = 0; i < onlyFileInfoswithIndex.Count; i++)
            {
                PACFileInfo fileInfo = onlyFileInfoswithIndex[i];

                int fileIndexes = fileInfo.fileInfoIndex;
                fileInfoOffsets[fileIndexes] = fileInfoStart + fileInfosStream.Position;

                //foreach(int fileIndex in fileIndexes)
                //{
                //    fileInfoOffsets[fileIndex] = fileInfoStart + fileInfosStream.Position;
                //}

                appendUIntMemoryStream(fileInfosStream, (uint)fileInfo.patchNo, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.relativePathIndex, true);
                appendIntMemoryStream(fileInfosStream, 0x00040000, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.Size1, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.Size2, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.Size3, true);
                appendIntMemoryStream(fileInfosStream, 0, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.nameHash, true);
            }

            /*
            for (int i = 0; i < fileNo; i++)
            {
                PACFileInfo fileInfo = fileInfos[i];

                int fileIndexes = fileInfo.fileInfoIndex;
                
                //foreach(int fileIndex in fileIndexes)
                //{
                //    fileInfoOffsets[fileIndex] = fileInfoStart + fileInfosStream.Position;
                //}
                
                appendUIntMemoryStream(fileInfosStream, (uint)fileInfo.patchNo, true);
                appendIntMemoryStream(fileInfosStream, i, true);
                appendIntMemoryStream(fileInfosStream, 0x00040000, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.Size1, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.Size2, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.Size3, true);
                appendIntMemoryStream(fileInfosStream, 0, true);
                appendUIntMemoryStream(fileInfosStream, fileInfo.nameHash, true);
            }
            */

            long fileNameStart = fileInfoStart + fileInfosStream.Length;
            List<PACFileInfo> orderedRelativePathIndex = onlyFileInfoswithNames.OrderBy(s => s.relativePathIndex).ToList();

            for (int i = 0; i < orderedRelativePathIndex.Count; i++)
            {
                PACFileInfo fileInfo = orderedRelativePathIndex[i];
                if (fileInfo.relativePathIndex != i)
                    throw new Exception("ordered Relative Path Index is not continious!");

                string relativePath = fileInfo.relativePatchPath;
                long initPos = fileNamesStream.Position;
                ushort subFolderFlag = fileInfo.hasRelativePatchSubPath ? (ushort)0x8000 : (ushort)0 ;
                appendStringMemoryStream(fileNamesStream, relativePath, Encoding.Default);
                appendZeroMemoryStream(fileNamesStream, 1);
                appendUShortMemoryStream(fileNamePointersStream, subFolderFlag, true);
                appendUShortMemoryStream(fileNamePointersStream, (ushort)(fileNameStart + initPos), true);
            }

            /*
            for (int i = 0; i < fileNo; i++)
            {
                PACFileInfo fileInfo = fileInfos[i];
                appendStringMemoryStream(fileNamesStream, fileInfo.relativePatchPath, Encoding.Default);
                appendUIntMemoryStream(fileNamePointersStream, (uint)(fileNameStart + fileNamesStream.Position), true);
            }
            */

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

            TBL.Write(fileNamePointersStream.ToArray(), 0, (int)fileNamePointersStream.Length);
            TBL.Write(fileIndexPointersStream.ToArray(), 0, (int)fileIndexPointersStream.Length);
            TBL.Write(fileInfosStream.ToArray(), 0, (int)fileInfosStream.Length);
            TBL.Write(fileNamesStream.ToArray(), 0, (int)fileNamesStream.Length);

            return TBL;
        }

        private void updateFileSizes()
        {
            List<PACFileInfo> onlyFileInfoswithFilePaths = fileInfos.Where(a => a.fileFlags.HasFlag(fileFlagsEnum.hasFilePath)).ToList();
            foreach(PACFileInfo fileInfo in onlyFileInfoswithFilePaths)
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
    }
}
