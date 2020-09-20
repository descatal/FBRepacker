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

        public ExtractPAC(string filePath, FileStream PAC) : base()
        {
            changeStreamFile(PAC);
            this.filePath = filePath;
        }

        public void extractPAC()
        {
            // Load the file into PAC filestream
            if (Stream != null)
                Stream.Close();
            Stream = File.Open(filePath, FileMode.Open);

            string extractPath = Properties.Settings.Default.ExtractPath + @"\" + Path.GetFileNameWithoutExtension(filePath);

            Directory.CreateDirectory(extractPath);

            currDirectory = extractPath;
            rootDirectory = extractPath;
            initializePACInfoFileExtract();

            appendPACInfo("--1--");
            // Read and check Header
            int Header = readIntBigEndian(0x00);
            switch (Header)
            {
                case 0x46484D20: // FHM Header
                    appendPACInfo("FHMOffset: 0");
                    appendPACInfo("Header: fhm");
                    parseFHM();
                    extractEndFile();
                    break;
                case 0x00020100: // Stream
                    appendPACInfo("Header: STREAM");
                    new STREAM(Stream, (int)Stream.Position - 0x04).extract();
                    //debugtxt.Text = "STREAM file detected!";
                    break;
                default:
                    //debugtxt.Text = "Invalid file header!";
                    break;
            }

            infoStreamWrite.Close();
            Stream.Close();

            resetVariables();
        }

        private void parseFHM()
        {
            // Get the FHM starting pos in the file, subtract by 4 for FHM header.
            long FHMStartingPos = Stream.Position - 0x04;

            // Current filestream position = after FHM header, a.k.a = 0x04.
            int FHMSize = readIntBigEndian(Stream.Position + 0x08);

            // Current filestream position = after FHMSize, so no offset needed.
            int numberofFiles = readIntBigEndian(Stream.Position);

            // Set the FHMFileNumber for the extracted FHMChunk
            int FHMFileNumber = fileNumber;

            // Get the FHM Chunk Size by getting the first file offset.
            int FHMChunkSize = readIntBigEndian(Stream.Position);
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

            for (int i = 0; i < numberofFiles; i++)
            {
                fileNumber++;
                createFHMPACInfoTag(fileNumber, false);

                duplicateLinkedFile = false;
                int fileOffset = readIntBigEndian(Stream.Position); // The current fileOffset
                int sizeOffsetOffset = numberofFiles * 0x04; // The offset of the size of the nth file

                // Reading FHM Offset, update the list
                fileOffsets = writeFileOffsetInfo(fileOffsets, fileOffset);

                // Save the next position to return to
                long nextOffsetPosition = Stream.Position;

                // Reading Size of the file, update the list
                int fileSize = readIntBigEndian(Stream.Position + sizeOffsetOffset - 0x04);
                fileSizes = writeFileSizeInfo(fileSizes, fileSize);

                string header = identifyHeader(readIntBigEndian(FHMStartingPos + fileOffset));
                fileHeaders.Add(header);
                appendPACInfo("Header: " + header.ToString());

                if (header == "fhm")
                {
                    parseFHM();
                }
                else
                {
                    parseFiles(header, fileSize);
                }

                Stream.Seek(nextOffsetPosition, SeekOrigin.Begin);
            }

            int FHMEndFileOffset = fileOffsets.Count > 0 ? fileOffsets.Last() + fileSizes.Last() : 0x14;
            fileEndOffset.Add(FHMStartingPos + FHMEndFileOffset);

            extractFHMChunk(fileOffsets, FHMFileNumber, FHMStartingPos);

            currDirectory = Directory.GetParent(currDirectory).FullName; // Navigate up 1 directory
        }

        private List<int> writeFileOffsetInfo(List<int> fileOffsets, int fileOffset)
        {
            if (fileOffsets.Contains(fileOffset)) // For cases where the FHM uses linked (shared) offsets.
            {
                int linkOffset = fileOffsets.FindIndex(off => off == fileOffset) + 1; // Find the Index of the first duplicate in FHM. 
                appendPACInfo("Link FHMOffset: " + linkOffset.ToString()); // For repacking use. If link is present, use the same file. 
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

        private void parseFiles(string header, int size)
        {
            switch (header)
            {
                case "NTP3": // Extract dds
                    new NTP3(Stream, (int)Stream.Position - 0x04).extract();
                    break;
                case "STREAM": // Extract STREAM (audio)
                    new STREAM(Stream, (int)Stream.Position - 0x04).extract();
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
            createFile("fhm", FHMChunk, createExtractFilePath(FHMFileNumber));
        }

        private void extractEndFile()
        {
            fileNumber++;
            long lastOffset = fileEndOffset.Count > 0 ? fileEndOffset.Max() : 0;
            long EndFileSize = Stream.Length - lastOffset;
            byte[] EndFileChunk = new byte[EndFileSize];
            Stream.Seek(lastOffset, SeekOrigin.Begin);
            Stream.Read(EndFileChunk, 0x00, (int)EndFileSize); // Extract the chunk
            createFile("endfile", EndFileChunk, createExtractFilePath(fileNumber));

            createFHMPACInfoTag(fileNumber, false);
            appendPACInfo("Header: endfile");
            appendPACInfo("End File Offset: " + lastOffset);
            appendPACInfo("End File Size: " + EndFileSize);
            appendPACInfo("");
            appendPACInfo("//");
        }

        private void extractDefault(string extension, int size)
        {
            byte[] buffer = new byte[size];
            int seekstart = extension != "ALEO" ? -0x04 : -0x08;
            Stream.Seek(seekstart, SeekOrigin.Current);
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

        private string identifyHeader(int header)
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
                default:
                    // Since ALEO file has header at the 0x4 offset, we need to check this way.
                    int ALEOHeader = readIntBigEndian(Stream.Position);
                    identifiedHeader = ALEOHeader == 0x414C454F ? "ALEO" : "bin";
                    Stream.Seek(-0x4, SeekOrigin.Current);
                    break;
            }
            return identifiedHeader;
        }
    }
}
