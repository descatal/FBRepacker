using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.extractPAC
{
    class STREAM : Internals
    {
        int audioEntries = 0, STREAMPosition = 0, STREAMHeaderChunkSize = 0, STREAMDataChunkSize = 0, audioTotalFileSize = 0, sampleRate = 0, audioDataSize = 0, audioFileNumber = 1;

        public STREAM(FileStream PAC, int FHMOffset) : base(PAC)
        {
            STREAMPosition = FHMOffset;
        }

        public void extract()
        {
            createSTREAMPACInfoTag(fileNumber, true);
            parseSTREAM();
        }

        private void parseSTREAM()
        {
            PAC.Seek(0x08, SeekOrigin.Current);
            audioEntries = readIntBigEndian(PAC.Position);
            STREAMHeaderChunkSize = readIntBigEndian(PAC.Position);
            STREAMDataChunkSize = readIntBigEndian(PAC.Position);
            
            //Write STREAM PAC Info
            appendPACInfo("Number of audio files: " + audioEntries.ToString());
            appendPACInfo("STREAM Header Chunk Size (TOC): " + STREAMHeaderChunkSize.ToString());
            appendPACInfo("Total Data Chunk Size: " + STREAMDataChunkSize.ToString());

            extractSTREAM();
            PAC.Seek(0x08, SeekOrigin.Current);
            parseAudioHeader();
        }

        private void parseAudioHeader()
        {
            while (audioFileNumber <= audioEntries)
            {
                int audioHeaderOffset = readIntBigEndian(PAC.Position);

                // Save the next position to return to
                long nextOffsetPosition = PAC.Position;

                PAC.Seek(audioHeaderOffset + STREAMPosition, SeekOrigin.Begin);

                switch (readIntBigEndian(PAC.Position))
                {
                    // at3 (wav)
                    case 0x61743300:
                        parseAT3(audioFileNumber);
                        break;

                    // is14 / BNSF
                    case 0x69733134:
                        parseis14(audioFileNumber);
                        break;

                    default:
                        break;
                }

                PAC.Seek(nextOffsetPosition, SeekOrigin.Begin);

                audioFileNumber++;
            }
        }

        private void parseAT3(int audioNumber)
        {
            PAC.Seek(0x10, SeekOrigin.Current);

            int AT3DataSize = readIntBigEndian(PAC.Position);
            int relativeAT3DataOffset = readIntBigEndian(PAC.Position);

            // Write audio Info
            appendPACInfo("#AT3: " + audioNumber);
            appendPACInfo("AT3 Data Size: " + AT3DataSize.ToString());
            appendPACInfo("relative AT3 Data Offset: " + relativeAT3DataOffset.ToString());

            PAC.Seek(STREAMPosition + STREAMHeaderChunkSize + relativeAT3DataOffset, SeekOrigin.Begin);

            extractAT3(AT3DataSize);
        }

        private void extractAT3(int AT3DataSize)
        {
            byte[] AT3Chunk = extractChunk(PAC.Position, AT3DataSize);
            extractAudio(AT3Chunk, "at3");
        }

        private void parseis14(int audioNumber)
        {
            PAC.Seek(0x10, SeekOrigin.Current);

            int BNSFDataSize = readIntBigEndian(PAC.Position);
            int relativeBNSFDataOffset = readIntBigEndian(PAC.Position);

            // Extracting the BNSF / is14 Header chunk for the extracted file. The size 0x30 might be a problem. 
            PAC.Seek(0xA0, SeekOrigin.Current);
            byte[] BNSFis14HeaderChunk = extractChunk(PAC.Position, 0x30);

            PAC.Seek(-0x2C, SeekOrigin.Current);
            audioTotalFileSize = readIntBigEndian(PAC.Position);
            PAC.Seek(0x10, SeekOrigin.Current);
            sampleRate = readIntBigEndian(PAC.Position);
            PAC.Seek(0x0C, SeekOrigin.Current);
            audioDataSize = readIntBigEndian(PAC.Position);

            // Write audio Info
            appendPACInfo("#BNSF/is14: " + audioNumber);
            appendPACInfo("BNSF Data Size: " + BNSFDataSize.ToString());
            appendPACInfo("relative BNSF Data Offset: " + relativeBNSFDataOffset.ToString());

            PAC.Seek(STREAMPosition + STREAMHeaderChunkSize + relativeBNSFDataOffset, SeekOrigin.Begin);

            extractBNSF(BNSFis14HeaderChunk, BNSFDataSize);
        }

        private void extractBNSF(byte[] BNSFis14HeaderChunk, int BNSFDataSize)
        {
            List<byte[]> BNSFBuffer = new List<byte[]>();
            byte[] BNSFData = extractChunk(PAC.Position, BNSFDataSize);
            BNSFBuffer.Add(BNSFis14HeaderChunk);
            BNSFBuffer.Add(BNSFData);

            byte[] BNSF = BNSFBuffer.SelectMany(b => b).ToArray();
            extractAudio(BNSF, "bnsf");
        }

        private void extractSTREAM()
        {
            long returnPosition = PAC.Position;
            PAC.Seek(STREAMPosition, SeekOrigin.Begin);
            byte[] STREAMHeaderChunk = extractChunk(PAC.Position, STREAMHeaderChunkSize);
            createFile("STREAM", STREAMHeaderChunk, createExtractFilePath(fileNumber));
            PAC.Seek(returnPosition, SeekOrigin.Begin);
        }

        private void extractAudio(byte[] audioBuffer, string fileExt)
        {
            string filePath = createExtractFilePath(fileNumber);
            string filePathwithExt = createExtractFilePath(fileNumber) + "." + fileExt;
            createFile(fileExt, audioBuffer, filePath);

            if (Properties.Settings.Default.outputWAV)
            {
                if(fileExt == "bnsf")
                {
                    // Using the G722.1 decoder. Pass the filePath that is created. Banwidth is always 14000.
                    byte[] WAVBuffer = convertBNSFtoWAV(filePathwithExt, sampleRate, 14000);
                    // Replace the original file with the new buffer.
                    createFile(fileExt, WAVBuffer, filePath);

                    /*
                    // Using ffmpeg to add Header to PCM. Channel is always 1.
                    byte[] WAVBuffer = convertPCMtoWAV(filePathwithExt, sampleRate, 1);
                    // Replace the original file with the WAV buffer.
                    createFile(fileExt, WAVBuffer, filePath);
                    */
                }
                else if (fileExt == "at3")
                {
                    // Using the at3tool to convert at3 to PCM 
                    byte[] WAVBuffer = convertAT3toWAV(filePathwithExt);
                    // Replace the original file with the new buffer.
                    createFile(fileExt, WAVBuffer, filePath);
                }
                renameFile(filePath + "." + fileExt, fileNumber.ToString("000") + "-" + audioFileNumber.ToString("000") + ".WAV");
            }
        }
    }
}

/* ------------------------------------------------File Structure-------------------------------------------------------
 * Offset   field Name              field Size  e.g.
 *  ------------------------------------------------STREAM Header-------------------------------------------------------   
 * 0x00     Header                  4           00 02 01 00                 /
 * 0x0C     Entries                 4           00 00 00 F9                 /
 * 0x10     totalaudioHeaderSize    4           00 01 00 00                 /       STREAM Chunk
 * 0x14     totalaudioDataSize      4           00 81 87 D0                 /
 * 0x18     STREAM Chunk Size       4           00 00 00 20                 /
 * 0x1C     firstAudioHeaderOffset  4           00 00 04 10                 /
 * 0x20..   audioHeaderOffset List  *                                       /
 *                                                                          /       audioHeader TOC
 *                                                                          /
 *-------------------------------------------------audio Header (at3)---------------------------------------------------
 * 0x00     Header                  4           61 74 33 00                 /
 * 0x14     audioDataSize           4           00 00 5B C4                 /       
 * 0x18     audioDataRelativeOffset 4           00 00 5B D0                 /       at3 (a type of .wav) audio Header
 *                                                                          /
 * ------------------------------------------------audio Header (is14)--------------------------------------------------
 * 0x00     Header                  4           61 74 33 00                 /
 * 0x14     audioDataSize           4           00 00 5B C4                 /       
 * 0x18     audioDataRelativeOffset 4           00 00 5B D0                 /       is14 (bnsf) audio Header
 * 0xBC     BNSF IS14 Header        38          *                           /
 *                                                                          /
 * ------------------------------------------------audio Data-----------------------------------------------------------   
 * 0x00..   Data                    *                                       /       audio Data
 *                                                                          /
 * ---------------------------------------------------------------------------------------------------------------------
 * 
 * 
 * --------------------------BNSFis14 Header to be appended on BNSF Data (0x30 sized)-----------------------------------
 * 0x00     BNSF                            4           42 4E 53 46                 /
 * 0x04     Size of file -0x08 (with Header)4           *                           /
 * 0x08     IS14sfmt Header                 8           49 53 31 34 73 66 6D 74     /
 * 0x10     ??                              4           00 00 00 14                 /
 * 0x14     ??                              4           00 00 00 01                 /
 * 0x18     Sample Rate                     4           00 00 BB 80                 / (Usually 48000 kHz)
 * 0x1C     Total number of Samples         4           00 00 8B 40                 / Total number of Samples / Sample Rate = Duration of audio
 * 0x20     0                               4           00 00 00 00                 /
 * 0x24     sdat Headers                    8           00 78 02 80 73 64 61 74     /
 * 0x2C     Size of data                    4           *                           / Usually Total File Size -0x30
 * 
 * Either at3 or BNSF.
*/
