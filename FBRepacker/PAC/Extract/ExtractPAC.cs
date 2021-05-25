using FBRepacker.PAC.Extract.FileTypes;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Extract
{
    class ExtractPAC : Internals
    {
        string filePath = string.Empty;
        bool duplicateLinkedFile = false;
        Dictionary<int, int> globalOffset = new Dictionary<int, int>();
        Dictionary<int, string[]> STREAM_Name_FileInfo = new Dictionary<int, string[]>();
        Dictionary<int, string> EIDXFileInfo = new Dictionary<int, string>();

        public ExtractPAC(string filePath, FileStream PAC) : base()
        {
            changeStreamFile(PAC); // reset Stream File
            this.filePath = filePath;
        }

        public void extractPAC(long startOffset, out long endPosition, string extractPath)
        {
            // Load the file into PAC filestream
            if (Stream != null)
                Stream.Close();
            Stream = File.Open(filePath, FileMode.Open);

            Stream.Seek(startOffset, SeekOrigin.Begin);

            Directory.CreateDirectory(extractPath);

            currDirectory = extractPath;
            rootDirectory = extractPath;
            initializePACInfoFileExtract();

            uint fullSize = (uint)Stream.Length;
            additionalInfo additionalInfoFlagOut = additionalInfo.NONE;

            appendPACInfo("--1--");
            // Read and check Header
            uint Header = readUIntBigEndian(Stream.Position);
            switch (Header)
            {
                case 0x46484D20: // FHM Header
                    appendPACInfo("FHMOffset: 0");
                    appendPACInfo("Header: fhm");

                    parseFHM(out additionalInfoFlagOut);
                    addFHMAdditioalInfoFlag(1, additionalInfoFlagOut);

                    extractEndFile();
                    break;
                case 0x00020100: // Stream
                    appendPACInfo("-");
                    appendPACInfo("Header: STREAM");

                    // Create a new directory for each STREAM
                    string newSTREAMPath = currDirectory + @"\" + fileNumber.ToString("000") + "-STREAM";
                    Directory.CreateDirectory(newSTREAMPath);
                    currDirectory = newSTREAMPath;

                    new STREAM(Stream, (int)Stream.Position - 0x04).extract();
                    addFHMAdditioalInfoFlag(1, additionalInfoFlagOut);

                    appendPACInfo("//");
                    break;
                case 0x9992CD90:
                    appendPACInfo("FHMOffset: 0");
                    appendPACInfo("Header: fhm");

                    parseMBON(out additionalInfoFlagOut, fullSize);
                    addFHMAdditioalInfoFlag(1, additionalInfoFlagOut);

                    extractEndFile();
                    break;
                default:
                    //debugtxt.Text = "Invalid file header!";
                    break;
            }

            writePACInfo();
            endPosition = Stream.Position;
            Stream.Close();

            // TODO: remove this, clean up those static variables
            resetVariables();
        }

        private void parseFHM(out additionalInfo additionalInfoFlag)
        {
            // TODO: seperate linked FHM additional info with non linked (i.e. if the additional info affects nested FHM)
            // For now we reset the Info for each FHM recursive as EIDX only deals with 1 EIDX
            additionalInfoFlag = additionalInfo.NONE;

            // Get the FHM starting pos in the file, subtract by 4 for FHM header.
            long FHMStartingPos = Stream.Position - 0x04;

            // Current filestream position = after FHM header, a.k.a = 0x04.
            int FHMSize = readIntBigEndian(Stream.Position + 0x08);

            // Current filestream position = after FHMSize, so no offset needed.
            int numberofFiles = readIntBigEndian(Stream.Position);

            // Set the FHMFileNumber for the extracted FHMChunk
            int FHMFileNumber = fileNumber;

            // Get the FHM Chunk Size by getting the first file offset.
            // Will have a fixed FHMChunkSize of 0x14 if there's no file inside the FHM.
            int FHMChunkSize = numberofFiles != 0? readIntBigEndian(Stream.Position) : 0x14;

            Stream.Seek(-0x04, SeekOrigin.Current);

            // Create a new directory for each FHM
            string newFHMPath = currDirectory + @"\" + fileNumber.ToString("000") + "-FHM";
            Directory.CreateDirectory(newFHMPath);
            currDirectory = newFHMPath;

            createFHMPACInfoTag(fileNumber, true);
            appendPACInfo("Total file size: " + FHMSize.ToString());
            appendPACInfo("Number of files: " + numberofFiles.ToString());
            appendPACInfo("FHM chunk size: " + FHMChunkSize.ToString());

            List<int> fileOffsets = new List<int>();
            List<int> fileSizes = new List<int>();
            List<string> fileHeaders = new List<string>();

            for (int file_Index = 0; file_Index < numberofFiles; file_Index++)
            {
                fileNumber++;
                createFHMPACInfoTag(fileNumber, false);

                duplicateLinkedFile = false;
                int filePointerOffset = (int)Stream.Position;
                int filePointer = readIntBigEndian(Stream.Position); // The current fileOffset
                int sizeOffset = numberofFiles * 0x04; // The offset of the size of the nth file
                int assetLoadEnumOffset = (numberofFiles * 2) * 0x04;
                int unkEnumOffset = (numberofFiles * 3) * 0x04;

                int globalOff = (int)FHMStartingPos + filePointer;
                globalOffset[fileNumber] = globalOff;
                //writeInsert(globalOff, fileNumber);

                // Reading FHM Offset, update the list (including link & insert)
                fileOffsets = writeFileOffsetInfo(fileOffsets, filePointer, globalOff);
                // Save the next position to return to
                long nextOffsetPosition = Stream.Position;

                Stream.Seek(filePointerOffset, SeekOrigin.Begin);
                // Reading Size of the file, update the list
                int fileSize = readIntBigEndian(Stream.Position + sizeOffset);
                Stream.Seek(filePointerOffset, SeekOrigin.Begin);
                fileSizes = writeFileSizeInfo(fileSizes, fileSize);

                // Reading Size of the file, update the list
                int assetLoadEnum = readIntBigEndian(Stream.Position + assetLoadEnumOffset);
                Stream.Seek(filePointerOffset, SeekOrigin.Begin);
                int unkEnum = readIntBigEndian(Stream.Position + unkEnumOffset);
                //Stream.Seek(filePointerOffset, SeekOrigin.Begin);

                if (unkEnum != 0)
                    throw new Exception("Not an error, just let me know if there's a non 0 unkEnum since I want to find what does it do. Remove this if statement if you want to continue.");

                string header = identifyHeader(readIntBigEndian(globalOff), additionalInfoFlag);
                fileHeaders.Add(header);
                appendPACInfo("FHMAssetLoadEnum: " + assetLoadEnum);
                appendPACInfo("FHMunkEnum: " + unkEnum); // should always be 0.
                appendPACInfo("FHMFileNo: " + FHMFileNumber);
                appendPACInfo("Header: " + header.ToString());

                if (header == "fhm")
                {
                    int FHMFileNo = fileNumber;
                    parseFHM(out additionalInfo additionalInfoFlagOut);
                    addFHMAdditioalInfoFlag(FHMFileNo, additionalInfoFlagOut);
                }
                else
                {
                    parseFiles(header, file_Index, fileSize);
                    additionalInfoFlag = setAdditionalInfoFlag(header, additionalInfoFlag);
                    writeExtraFileInfo(file_Index, additionalInfoFlag, FHMFileNumber);
                }

                Stream.Seek(nextOffsetPosition, SeekOrigin.Begin);
            }

            int maxFileOffsetIndex = 0;

            if (fileOffsets.Count != 0)
                maxFileOffsetIndex = fileOffsets.IndexOf(fileOffsets.Max());

            int FHMEndFileOffset = fileOffsets.Count > 0 ? fileOffsets[maxFileOffsetIndex] + fileSizes[maxFileOffsetIndex] : 0x14;
            fileEndOffset.Add(FHMStartingPos + FHMEndFileOffset);

            extractFHMChunk(fileOffsets, FHMFileNumber, FHMStartingPos);

            currDirectory = Directory.GetParent(currDirectory).FullName; // Navigate up 1 directory

            // Reset the global EIDXFileInfo in as 1 EIDX only deals with 1 FHM.
            if (EIDXFileInfo != null)
                EIDXFileInfo = new Dictionary<int, string>();

            if (STREAM_Name_FileInfo != null)
                STREAM_Name_FileInfo = new Dictionary<int, string[]>();
        }

        private void parseMBON(out additionalInfo additionalInfoFlag, uint fullSize)
        {
            // TODO: seperate linked FHM additional info with non linked (i.e. if the additional info affects nested FHM)
            // For now we reset the Info for each FHM recursive as EIDX only deals with 1 EIDX
            additionalInfoFlag = additionalInfo.NONE;
            long FHMStartingPos = Stream.Position - 0x04;

            Stream.Seek(0x0C, SeekOrigin.Current);
            int filePointer = readIntSmallEndian(Stream.Position);

            Stream.Seek(0x20, SeekOrigin.Current);

            // Current filestream position = after FHMSize, so no offset needed.
            int numberofFiles = readIntSmallEndian(Stream.Position);

            // Set the FHMFileNumber for the extracted FHMChunk
            int FHMFileNumber = fileNumber;

            // Get the FHM Chunk Size by getting the first file offset.
            // Will have a fixed FHMChunkSize of 0x14 if there's no file inside the FHM.
            int FHMChunkSize = 0x10000;

            // Create a new directory for each FHM
            string newFHMPath = currDirectory + @"\" + fileNumber.ToString("000") + "-MBON";
            Directory.CreateDirectory(newFHMPath);
            currDirectory = newFHMPath;

            createFHMPACInfoTag(fileNumber, true);
            appendPACInfo("Total file size: " + 0);
            appendPACInfo("Number of files: " + numberofFiles.ToString());
            appendPACInfo("FHM chunk size: " + FHMChunkSize.ToString());

            List<int> fileOffsets = new List<int>();
            List<int> fileSizes = new List<int>();
            List<string> fileHeaders = new List<string>();

            if (numberofFiles != 1)
            {
                Stream.Seek(0x18, SeekOrigin.Current);
                Stream.Seek(0x40, SeekOrigin.Current); // First two files

                if (numberofFiles > 2)
                {
                    Stream.Seek((0xC * (numberofFiles - 0x2)), SeekOrigin.Current); // Each file with 0xc length.
                }
                Stream.Seek(0x10, SeekOrigin.Current); // Padding 0x10
                Stream.Seek(0x18, SeekOrigin.Current); // Padding 0x18
            }

            for (int file_Index = 0; file_Index < numberofFiles; file_Index++)
            {
                fileNumber++;
                createFHMPACInfoTag(fileNumber, false);

                duplicateLinkedFile = false;
                int filePointerOffset = (int)Stream.Position;
                if (numberofFiles > 1 && file_Index != 0)
                {
                    filePointer = readIntSmallEndian(Stream.Position) + 0x10000; // The current fileOffset
                }

                int globalOff = (int)FHMStartingPos + filePointer;
                globalOffset[fileNumber] = globalOff;
                //writeInsert(globalOff, fileNumber);

                uint currentFileSize = 0;
                if (file_Index < numberofFiles - 1)
                {
                    uint returnOff = (uint)Stream.Position;
                    if (file_Index > 0)
                    {
                        Stream.Seek(0x1C, SeekOrigin.Current);
                    }
                    currentFileSize = readUIntSmallEndian() - (uint)filePointer + 0x10000;
                    Stream.Seek(returnOff, SeekOrigin.Begin);
                }
                else if (file_Index == numberofFiles - 1)
                {
                    currentFileSize = fullSize - (uint)filePointer;
                }

                fileSizes.Add((int)currentFileSize);
                // Reading FHM Offset, update the list (including link & insert)
                fileOffsets.Add(filePointer);

                long nextOffsetPosition = Stream.Position;
                // Save the next position to return to
                if (file_Index != 0)
                {
                    nextOffsetPosition = Stream.Position + 0x1C;
                }

                // Reading Size of the file, update the list
                int assetLoadEnum = 0;
                int unkEnum = 0;

                string header = identifyHeader(readIntBigEndian(globalOff), additionalInfoFlag);
                fileHeaders.Add(header);
                appendPACInfo("FHMAssetLoadEnum: " + assetLoadEnum);
                appendPACInfo("FHMunkEnum: " + unkEnum); // should always be 0.
                appendPACInfo("FHMFileNo: " + FHMFileNumber);
                appendPACInfo("Header: " + header.ToString());

                if (header == "fhm")
                {
                    int FHMFileNo = fileNumber;
                    parseFHM(out additionalInfo additionalInfoFlagOut);
                    addFHMAdditioalInfoFlag(FHMFileNo, additionalInfoFlagOut);
                }
                else
                {
                    parseFiles(header, file_Index, (int)currentFileSize);
                    additionalInfoFlag = setAdditionalInfoFlag(header, additionalInfoFlag);
                    writeExtraFileInfo(file_Index, additionalInfoFlag, FHMFileNumber);
                }

                Stream.Seek(nextOffsetPosition, SeekOrigin.Begin);
            }

            int maxFileOffsetIndex = 0;

            if (fileOffsets.Count != 0)
                maxFileOffsetIndex = fileOffsets.IndexOf(fileOffsets.Max());

            int FHMEndFileOffset = fileOffsets.Count > 0 ? fileOffsets[maxFileOffsetIndex] + fileSizes[maxFileOffsetIndex] : 0x14;
            fileEndOffset.Add(FHMStartingPos + FHMEndFileOffset);

            extractFHMChunk(fileOffsets, FHMFileNumber, FHMStartingPos);

            currDirectory = Directory.GetParent(currDirectory).FullName; // Navigate up 1 directory

            // Reset the global EIDXFileInfo in as 1 EIDX only deals with 1 FHM.
            if (EIDXFileInfo != null)
                EIDXFileInfo = new Dictionary<int, string>();

            if (STREAM_Name_FileInfo != null)
                STREAM_Name_FileInfo = new Dictionary<int, string[]>();
        }

        private List<int> writeFileOffsetInfo(List<int> fileOffsets, int fileOffset, int globalOff)
        {
            bool isInsert = globalOffset.Any(off => off.Value > globalOff);
            bool isLinked = fileOffsets.Contains(fileOffset);

            if (isInsert && !isLinked)
            {
                int insertFileNumber = globalOffset.FirstOrDefault(file => file.Value > globalOff).Key;
                //appendPACInfo("Insert before file no: " + insertFileNumber);
            }

            if (isLinked) // For cases where the FHM uses linked (shared) offsets.
            {
                int linkOffset = fileOffsets.FindIndex(off => off == fileOffset) + 1; // Find the Index of the first duplicate in FHM. 
                appendPACInfo("LinkedFileNo_in_FHM: " + linkOffset.ToString()); // For repacking use. If link is present, use the same file. 
                duplicateLinkedFile = true;
            }

            fileOffsets.Add(fileOffset);

            appendPACInfo("FHMOffset: " + fileOffset.ToString());
            return fileOffsets;
        }

        private List<int> writeFileSizeInfo(List<int> fileSizes, int fileSize)
        {
            fileSizes.Add(fileSize);
            appendPACInfo("Size: " + fileSize.ToString());
            return fileSizes;
        }

        private void parseFiles(string header, int file_Index, int size)
        {
            switch (header)
            {
                case "NTP3": // Extract dds
                    new NTP3(Stream, (int)Stream.Position - 0x04).extract();
                    break;
                case "STREAM": // Extract STREAM (audio)
                    new STREAM(Stream, (int)Stream.Position - 0x04).extract();
                    break;
                case "EIDX":
                    EIDXFileInfo = new EIDX(Stream, (int)Stream.Position - 0x04, size + 0x04).parseEIDX();
                    break;
                case "soundhashes":
                    int returnPointer = (int)Stream.Position;
                    STREAM_Name_FileInfo = new STREAM(Stream, (int)Stream.Position - 0x04).parse_Sound_Files_Hash();
                    Stream.Seek(returnPointer, SeekOrigin.Begin);
                    extractDefault(header, size);
                    break; 
                default:
                    extractDefault(header, size);
                    break;
            }
        }

        private void extractFHMChunk(List<int> fileOffsets, int FHMFileNumber, long startingPos)
        {
            // Extract whole FHMChunk
            int FHMChunkSize = fileOffsets.Count > 0 ? fileOffsets.First() : 0x14; // To cater for 0 file cases in FHM, the size is always 0x14
            byte[] FHMChunk = new byte[FHMChunkSize];
            Stream.Seek(startingPos, SeekOrigin.Begin); // Seek to the start of the file
            Stream.Read(FHMChunk, 0x00, FHMChunkSize); // Extract the chunk
            createFile("fhm", FHMChunk, createExtractFilePath(FHMFileNumber), FHMFileNumber);
        }

        private void extractEndFile()
        {
            fileNumber++;
            //var asd = Stream.Position;
            long lastOffset = fileEndOffset.Count > 0 ? fileEndOffset.Max() : 0;

            byte[] FHMByte = new byte[] { 0x46, 0x48, 0x4D, 0x20 };

            Stream.Seek(lastOffset, SeekOrigin.Begin);
            var offset = GetPatternPositions(Stream, FHMByte);

            long EndFileSize = offset == -1? Stream.Length - lastOffset: offset;
            byte[] EndFileChunk = new byte[EndFileSize];
            
            Stream.Read(EndFileChunk, 0x00, (int)EndFileSize); // Extract the chunk

            createFHMPACInfoTag(fileNumber, false);
            appendPACInfo("Header: endfile");
            appendPACInfo("End File Offset: " + lastOffset);
            appendPACInfo("End File Size: " + EndFileSize);

            createFile("endfile", EndFileChunk, createExtractFilePath(fileNumber));

            appendPACInfo("");
            appendPACInfo("//");
        }

        private void extractEmptyEndFile()
        {
            fileNumber++;

            createFHMPACInfoTag(fileNumber, false);
            appendPACInfo("Header: endfile");
            appendPACInfo("End File Offset: " + 0);
            appendPACInfo("End File Size: " + 0);

            createFile("endfile", new byte[] { }, createExtractFilePath(fileNumber));

            appendPACInfo("");
            appendPACInfo("//");
        }

        private void extractDefault(string extension, int size)
        {
            byte[] buffer = new byte[size];
            //int seekstart = extension != "ALEO" ? -0x04 : -0x04; // already seek -0x04 in identify header, so ALEO is same as -0x04
            Stream.Seek(-0x04, SeekOrigin.Current);
            Stream.Read(buffer, 0, size);

            if (!duplicateLinkedFile)
            {
                createFile(extension, buffer, createExtractFilePath(fileNumber));
            }
            else
            {
                createFile(extension, new byte[0] { }, createExtractFilePath(fileNumber) + "-L");
            }
        }

        private string identifyHeader(int header, additionalInfo additionalInfoFlag)
        {
            string identifiedHeader = "bin";
            byte[] j = BitConverter.GetBytes(header);
            Array.Reverse(j);

            //byte[] j = Encoding.Default.GetBytes(header.ToString("X2"));
            //byte[] j = new byte[] { 0x46, 0x48, 0x4D, 0x20 };
            string q = Encoding.Default.GetString(j).ToLower();
            switch (fileHeadersList.FindIndex(h => h.ToLower().Equals(q)))
            {
                case 0:
                    identifiedHeader = "fhm";
                    break;
                case 1:
                    identifiedHeader = "omo";
                    break;
                case 2:
                    identifiedHeader = "NTP3";
                    break;
                case 3:
                    identifiedHeader = "LMB";
                    break;
                case 4:
                    identifiedHeader = "nud";
                    break;
                case 5:
                    identifiedHeader = "vbn";
                    break;
                case 6:
                    identifiedHeader = "STREAM";
                    break;
                case 7:
                    identifiedHeader = "EIDX";
                    break;
                case 8:
                    identifiedHeader = "PNG";
                    break;
                case 9: // case for 00 00 00 00, to identify the sound file name list.
                    /*
                    if (additionalInfoFlag.HasFlag(additionalInfo.STREAM))
                    {
                        identifiedHeader = "soundhashes";
                    }
                    */
                    if(checkSoundHashes())
                        identifiedHeader = "soundhashes";
                    break;
                case 10:
                    Stream.Seek(0x4, SeekOrigin.Current);
                    if (readUIntBigEndian() == 0x42414E4B) // BANK
                    { 
                        identifiedHeader = "nus3bank";
                    }
                    else
                    {
                        identifiedHeader = "nus3audio";
                    }
                    Stream.Seek(-0x8, SeekOrigin.Current);
                    break;
                default:
                    // Since ALEO file has header at the 0x4 offset, we need to check this way.
                    int ALEOHeader = readIntBigEndian(Stream.Position);
                    identifiedHeader = (ALEOHeader == 0x414C454F || ALEOHeader == 0x4F454C41) ? "ALEO" : "bin"; // big and small endian ALEO
                    Stream.Seek(-0x4, SeekOrigin.Current);
                    break;
            }
            return identifiedHeader;
        }

        private bool checkSoundHashes()
        {
            uint returnAddress = (uint)Stream.Position;
            /*
            MemoryStream stream = new MemoryStream();
            Stream.CopyTo(stream);
            int pos = Search(stream, new byte[] { 0x53, 0x54, 0x5F, 0x56, 0x4F });

            if (pos != -1)
                return true;

            Stream.Seek(returnAddress, SeekOrigin.Begin);
            return false;
            */

            Stream.Seek(0x1C, SeekOrigin.Current);
            string identifier = readString(Stream.Position, 0x20);

            Stream.Seek(returnAddress, SeekOrigin.Begin);
            if(identifier.Contains("SE_") || identifier.Contains("ST_") || identifier.Contains("VO_"))
                return true;

            return false;
        }

        private void writeExtraFileInfo(int FHM_file_Index, additionalInfo additionalInfoFlag, int FHMFileNumber)
        {
            if(additionalInfoFlag.HasFlag(additionalInfo.EIDX) && EIDXFileInfo != null)
            {
                if (!EIDXFileInfo.ContainsKey(FHM_file_Index))
                    throw new Exception(FHM_file_Index + " does not exist in EIDXFileInfo!");

                appendPACInfo("EIDX_Index: " + FHM_file_Index);
                appendPACInfo("EIDX_Name: " + EIDXFileInfo[FHM_file_Index]);
            }

            if (additionalInfoFlag.HasFlag(additionalInfo.SOUNDNAME) && STREAM_Name_FileInfo.Count != 0)
            {
                appendPACInfo("Number of Sound Hashes: " + STREAM_Name_FileInfo.Count);
                for (int i = 1; i <= STREAM_Name_FileInfo.Count; i++)
                {
                    string[] names = STREAM_Name_FileInfo[i];

                    appendPACInfo("#SoundHash: " + i);

                    for (int j = 0; j < names.Length; j++)
                    {
                        string name = names[j];
                        var arrayOfBytes = Encoding.ASCII.GetBytes(name);

                        var crc32 = new Crc32();
                        string hash = crc32.Get(arrayOfBytes).ToString("X");

                        if(j == 0)
                        {
                            appendPACInfo("Name: " + name);
                            appendPACInfo("Hash: " + hash);
                        }
                        else
                        {
                            appendPACInfo("FileName: " + name);
                            appendPACInfo("FileHash: " + hash);
                        }
                    }
                }
            }
        }

        private additionalInfo setAdditionalInfoFlag(string header, additionalInfo additionalInfoFlag)
        {
            switch (header)
            {
                case "EIDX":
                    additionalInfoFlag |= additionalInfo.EIDX;
                    break;
                case "soundhashes":
                    additionalInfoFlag |= additionalInfo.SOUNDNAME;
                    break;
                default:
                    break;
            }

            return additionalInfoFlag;
        }

        private void addFHMAdditioalInfoFlag(int FHMFileNo, additionalInfo additionalInfoFlag)
        {
            appendPACInfo(FHMFileNo, "Additional info flag: " + (int)additionalInfoFlag);
        }
    }
}
