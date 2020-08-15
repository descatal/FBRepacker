using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.extractPAC
{
    class ExtractPAC : Internals
    {
        string fileName = string.Empty;

        public ExtractPAC(string fileName, FileStream PAC) : base(PAC)
        {
            this.fileName = fileName;
        }

        public void extractPAC()
        {
            // Load the file into PAC filestream
            if (PAC != null)
                PAC.Close();
            PAC = File.Open(fileName, FileMode.Open);

            currDirectory = Properties.Settings.Default.ExtractPath;
            rootDirectory = Properties.Settings.Default.ExtractPath;
            initializePACInfoFileExtract();

            // Read and check Header
            int Header = readIntBigEndian(0x00);
            switch (Header)
            {
                case 0x46484D20: // FHM Header
                    parseFHM();
                    extractEndFile();
                    break;
                case 0x00020100: // Stream
                    new STREAM(PAC, (int)PAC.Position - 0x04).extract();
                    //debugtxt.Text = "STREAM file detected!";
                    break;
                default:
                    //debugtxt.Text = "Invalid file header!";
                    break;
            }

            infoStream.Close();
            PAC.Close();

            resetVariables();
        }

        private void parseFHM()
        {
            // Get the FHM starting pos in the file, subtract by 4 for FHM header.
            long FHMStartingPos = PAC.Position - 0x04;

            // Current filestream position = after FHM header, a.k.a = 0x04.
            int FHMSize = readIntBigEndian(PAC.Position + 0x08);

            // Current filestream position = after FHMSize, so no offset needed.
            int numberofFiles = readIntBigEndian(PAC.Position);

            // Set the FHMFileNumber for the extracted FHMChunk
            int FHMFileNumber = fileNumber;

            // Create a new directory for each FHM
            string newFHMPath = currDirectory + @"\FHM " + fileNumber.ToString("000");
            Directory.CreateDirectory(newFHMPath);
            currDirectory = newFHMPath;

            createFHMPACInfoTag(fileNumber, true);
            appendPACInfo("Size: " + FHMSize.ToString());
            appendPACInfo("Number of files: " + numberofFiles.ToString());

            List<int> fileOffsets = new List<int>();
            List<int> fileSizes = new List<int>();
            List<string> fileHeaders = new List<string>();

            for (int i = 0; i < numberofFiles; i++)
            {
                fileNumber++;
                createFHMPACInfoTag(fileNumber, false);

                int fileOffset = readIntBigEndian(PAC.Position); // The current fileOffset
                int SizeOffset = numberofFiles * 0x04; // The offset of the size of the nth file

                // Reading FHM Offset, update the list
                fileOffsets = writeFileOffsetInfo(fileOffsets, fileOffset);

                // Save the next position to return to
                long nextOffsetPosition = PAC.Position;

                // Reading Size of the file, update the list
                int fileSize = readIntBigEndian(PAC.Position + SizeOffset - 0x04);
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

                PAC.Seek(nextOffsetPosition, SeekOrigin.Begin);
            }

            int FHMEndFileOffset = fileOffsets.Count > 0 ? fileOffsets.Last() + fileSizes.Last() : 0x14;
            fileEndOffset.Add(FHMStartingPos + FHMEndFileOffset);

            extractFHMChunk(fileOffsets, FHMFileNumber);

            currDirectory = Directory.GetParent(currDirectory).FullName; // Navigate up 1 directory
        }

        private List<int> writeFileOffsetInfo(List<int> fileOffsets, int fileOffset)
        {
            if (fileOffsets.Contains(fileOffset)) // For cases where the FHM uses linked (shared) offsets.
            {
                int linkOffset = fileOffsets.FindIndex(off => off == fileOffset) + 1; // Find the Index of the first duplicate in FHM. 
                appendPACInfo("Link FHMOffset: " + linkOffset.ToString()); // For repacking use. If link is present, use the same file. 
            }
            else
            {
                fileOffsets.Add(fileOffset);
            }
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
                    new NTP3(PAC, (int)PAC.Position - 0x04).extract();
                    break;
                case "STREAM": // Extract STREAM (audio)
                    new STREAM(PAC, (int)PAC.Position - 0x04).extract();
                    break;
                default:
                    extractDefault(header, size);
                    break;
            }
        }

        private void extractFHMChunk(List<int> fileOffsets, int FHMFileNumber)
        {
            // Extract whole FHMChunk
            int FHMChunkSize = fileOffsets.Count > 0 ? fileOffsets.First() : 0x14; // To cater for 0 file cases in FHM, the size is always 0x14
            byte[] FHMChunk = new byte[FHMChunkSize];
            PAC.Seek(0x00, 0x00); // Seek to the start of the file
            PAC.Read(FHMChunk, 0x00, FHMChunkSize); // Extract the chunk
            createFile("fhm", FHMChunk, createExtractFilePath(FHMFileNumber));
        }

        private void extractEndFile()
        {
            fileNumber++;
            long lastOffset = fileEndOffset.Count > 0 ? fileEndOffset.Max() : 0;
            long EndFileSize = PAC.Length - lastOffset;
            byte[] EndFileChunk = new byte[EndFileSize];
            PAC.Seek(lastOffset, SeekOrigin.Begin);
            PAC.Read(EndFileChunk, 0x00, (int)EndFileSize); // Extract the chunk
            createFile("endfile", EndFileChunk, createExtractFilePath(fileNumber));

            createFHMPACInfoTag(fileNumber, false);
            appendPACInfo("Header: endfile");
            appendPACInfo("End File Offset: " + lastOffset);
            appendPACInfo("End File Size: " + EndFileSize);
        }

        private void extractDefault(string extension, int size)
        {
            byte[] buffer = new byte[size];
            int seekstart = extension != "ALEO" ? -0x04 : -0x08;
            PAC.Seek(seekstart, SeekOrigin.Current);
            PAC.Read(buffer, 0, size);
            createFile(extension, buffer, createExtractFilePath(fileNumber));
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
                    int ALEOHeader = readIntBigEndian(PAC.Position);
                    identifiedHeader = ALEOHeader == 0x414C454F ? "ALEO" : "bin";
                    PAC.Seek(-0x4, SeekOrigin.Current);
                    break;
            }
            return identifiedHeader;
        }
    }
}
