using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Extract.FileTypes
{
    class STREAM : Internals
    {
        int audioEntries = 0, STREAMPosition = 0, STREAMHeaderChunkSize = 0, STREAMDataChunkSize = 0, audioTotalFileSize = 0, sampleRate = 0, audioDataSize = 0, audioFileNumber = 1;
        uint STREAM_ID = 0;

        public STREAM(FileStream PAC, int FHMOffset) : base()
        {
            changeStreamFile(PAC);
            STREAMPosition = FHMOffset;
        }

        public Dictionary<int, string[]> parse_Sound_Files_Hash()
        {
            Dictionary<int, string[]> file_Name_Dic = new Dictionary<int, string[]>();

            //parse EIDX file and write the relevant info inside PAC.info
            Stream.Seek(STREAMPosition + 0x8, SeekOrigin.Begin);
            int file_Number = readIntBigEndian(Stream.Position);

            Stream.Seek(0x4, SeekOrigin.Current);
            int starting_Pointer = readIntBigEndian(Stream.Position);

            Stream.Seek(STREAMPosition + starting_Pointer, SeekOrigin.Begin);
            for(int i = 1; i <= file_Number; i++)
            {
                string name = readString(Stream.Position, 0x40);
                string fileName = readString(Stream.Position, 0x40);

                //Stream.Seek(0x40, SeekOrigin.Current);
                file_Name_Dic[i] = new string[] { name, fileName };
            }

            return file_Name_Dic;
        }

        public void extract()
        {
            createSTREAMPACInfoTag(fileNumber, true);
            parseSTREAM();
        }

        private void parseSTREAM()
        {
            // https://github.com/vgmstream/vgmstream/blob/ba032029089add2a87a3dd87eb59438f5ceed107/src/meta/nub.c
            Stream.Seek(0x04, SeekOrigin.Current);
            STREAM_ID = readUIntBigEndian(); // STREAM_ID
            audioEntries = readIntBigEndian(Stream.Position);
            STREAMHeaderChunkSize = readIntBigEndian(Stream.Position);
            STREAMDataChunkSize = readIntBigEndian(Stream.Position);
            uint headerSize = readUIntBigEndian();
            uint firstPointer = readUIntBigEndian();

            //Write STREAM PAC Info
            appendPACInfo("STREAM ID: " + STREAM_ID);
            appendPACInfo("Number of audio files: " + audioEntries.ToString());
            //appendPACInfo("STREAM Header Chunk Size (TOC): " + STREAMHeaderChunkSize.ToString());
            //appendPACInfo("Total Data Chunk Size: " + STREAMDataChunkSize.ToString());

            extractSTREAM();
            parseAudioHeader();
        }

        private void parseAudioHeader()
        {
            // https://github.com/vgmstream/vgmstream/blob/ba032029089add2a87a3dd87eb59438f5ceed107/src/meta/nub.c
            for (int i = 0; i < audioEntries; i++)
            {
                // Read Pointer
                uint audioHeaderOffset = readUIntBigEndian();
                // Save the next position to return to
                uint returnAddress = (uint)Stream.Position;
                Stream.Seek(audioHeaderOffset + STREAMPosition, SeekOrigin.Begin);

                uint magic = readUIntBigEndian();
                uint subsongsCount = readUIntBigEndian();
                uint subsongIndex = readUIntBigEndian();
                uint codec = readUIntBigEndian();
                uint unk_0x10 = readUIntBigEndian(); // 0

                uint stream_size = readUIntBigEndian();
                uint stream_relative_offset = readUIntBigEndian();
                uint subheader_size = readUIntBigEndian();

                // Info Header (in samples)
                uint loop_start = readUIntBigEndian();
                uint loop_length = readUIntBigEndian();
                uint loop_flag = readUIntBigEndian();
                uint loop_null = readUIntBigEndian();

                // unk section, most of these are speculation
                uint unk_0x30 = readUIntBigEndian(); // Should be permanent FFFFFFFF
                float loop_Float = readFloat(true); // If there's no loop, this value will be populated.
                float loop_Float_2 = readFloat(true); // unknown -99 (Float), but with loop is 0
                uint unk_0x3C = readUIntBigEndian(); // 0
                uint unk_0x40 = readUIntBigEndian(); // 0
                uint unk_0x44 = readUIntBigEndian(); // 0
                float unk_0x48 = readFloat(true); // 1 (Float)
                uint unk_0x4C = readUIntBigEndian(); // 0
                float var_0x50 = readFloat(true); // at3/is14 = 1 (Float), vag/wav = 0
                uint var_0x54 = readUIntBigEndian(); // at3/is14 = 0xA, vag = 0x4, wav = 0
                float unk_0x58 = readFloat(true); // 1 (Float)
                float unk_0x5C = readFloat(true); // 1 (Float)
                uint var_0x60 = readUIntBigEndian(); // different for each header, not sure what.
                uint unk_0x64 = readUIntBigEndian(); // 0
                uint unk_0x68 = readUIntBigEndian(); // 0
                float var_0x6C = readFloat(true); // vag = 0, others = -100
                uint var_0x70 = readUIntBigEndian(); // different for each header
                uint unk_0x74 = readUIntBigEndian(); // 0x64
                uint unk_0x78 = readUIntBigEndian(); // 0
                uint unk_0x7C = readUIntBigEndian(); // 1
                uint unk_0x80 = readUIntBigEndian(); // 0
                uint unk_0x84 = readUIntBigEndian(); // 0
                uint unk_0x88 = readUIntBigEndian(); // 0
                float unk_0x8C = readFloat(true); // 1
                uint unk_0x90 = readUIntBigEndian(); // 0x14
                uint unk_0x94 = readUIntBigEndian(); // 0
                uint unk_0x98 = readUIntBigEndian(); // 0
                uint var_0x9C = readUIntBigEndian(); // 1
                uint unk_0xA0 = readUIntBigEndian(); // 0
                uint unk_0xA4 = readUIntBigEndian(); // 0
                float unk_0xA8 = readFloat(true); // 1
                uint var_0xAC = readUIntBigEndian(); // vag = 1, others = 0
                uint unk_0xB0 = readUIntBigEndian(); // 0
                uint unk_0xB4 = readUIntBigEndian(); // 0
                uint unk_0xB8 = readUIntBigEndian(); // 0

                appendPACInfo("#Sound: " + audioFileNumber);
                appendPACInfo("Codec: " + codec);
                appendPACInfo("Subheader Size: " + subheader_size);
                appendPACInfo("Loop Start: " + loop_start);
                appendPACInfo("Loop Length: " + loop_length);
                appendPACInfo("Loop Flag: " + loop_flag);
                appendPACInfo("Loop Float: " + loop_Float);
                appendPACInfo("Loop Float 2: " + loop_Float_2);
                appendPACInfo("var_0x50: " + var_0x50);
                appendPACInfo("var_0x54: " + var_0x54);
                appendPACInfo("var_0x60: " + var_0x60);
                appendPACInfo("var_0x6C: " + var_0x6C);
                appendPACInfo("var_0x70: " + var_0x70);
                appendPACInfo("var_0x9C: " + var_0x9C);
                appendPACInfo("var_0xAC: " + var_0xAC);

                // from this point: subheaders

                // Following VGM's implementation for codec determination
                switch (codec)
                {
                    // wav
                    case 0x01: // 0x77617600:
                        throw new Exception("Codec for WAV is unsupported for now. Rename the file to .nub extension and open it in foobar2000: ");
                        //parseWAV(audioFileNumber, nextAudioHeaderOffset);
                        //break;

                    // vag
                    case 0x02: // 0x76616700:
                        // Write audio Info
                        appendPACInfo("Format: VAG");
                        parseVAG(subheader_size, stream_size, stream_relative_offset);
                        break;

                    // at3 (wav)
                    case 0x03: // 0x61743300:
                        // Write audio Info
                        appendPACInfo("Format: AT3");
                        parseAT3(subheader_size, stream_size, stream_relative_offset);
                        break;

                    // is14 / BNSF
                    case 0x07: // 0x69733134:
                        // Write audio Info
                        appendPACInfo("Format: BNSF/is14");
                        parseis14(audioFileNumber, subheader_size, stream_size, stream_relative_offset);
                        break;

                    default:
                        throw new Exception("Unregcongized codec: " + codec);
                }

                Stream.Seek(returnAddress, SeekOrigin.Begin);
                audioFileNumber++;
            }
        }

        private void parseAT3(uint subheader_size, uint stream_size, uint stream_relative_offset)
        {
            // subheaders
            uint subheaderCount = (uint)subheader_size / 0x4;

            appendPACInfo("Subheader Count: " + subheaderCount); // Theoretically we count like this

            // But to simplify the repack proccess we just assume all at3 has 0x40 long subheader length.
            if (subheader_size != 0x40)
                throw new Exception("Non 0x40 at3 subheader is not supported yet.");

            // All subheaders are small endians.
            // For at3, this section onward is actually the format (fmt) chunk in the RIFF header
            // https://docs.fileformat.com/audio/wav/
            // After "Length of format data as listed above"
            uint unk_0x00 = readUIntSmallEndian(); // 0x0001FFFE
            uint unk_0x04 = readUIntSmallEndian(); // 0xBB80
            uint unk_0x08 = readUIntSmallEndian(); // 0x3EFD
            uint unk_0x0C = readUIntSmallEndian(); // 0x02B0
            uint unk_0x10 = readUIntSmallEndian(); // 0x08000022
            uint unk_0x14 = readUIntSmallEndian(); // 0x01
            uint unk_0x18 = readUIntSmallEndian(); // 0xE923AABF
            uint unk_0x1C = readUIntSmallEndian(); // 0x4471CB58
            uint unk_0x20 = readUIntSmallEndian(); // 0xFAFF19A1
            uint unk_0x24 = readUIntSmallEndian(); // 0x62CEE401
            uint unk_0x28 = readUIntSmallEndian(); // 0x55440001
            uint unk_0x2C = readUIntSmallEndian(); // 0
            uint unk_0x30 = readUIntSmallEndian(); // 0
            uint sampleCount = readUIntSmallEndian();
            uint unk_0x38 = readUIntSmallEndian(); // 0x800
            uint unk_0x3C = readUIntSmallEndian(); // 0x8B8

            Stream.Seek(STREAMPosition + STREAMHeaderChunkSize + stream_relative_offset, SeekOrigin.Begin);

            extractAT3(stream_size);
        }

        private void extractAT3(uint AT3DataSize)
        {
            byte[] AT3Chunk = extractChunk(Stream.Position, AT3DataSize);
            extractAudio(AT3Chunk, "at3");
        }

        private void parseWAV(int audioNumber, int nextAudioHeaderOffset)
        {
            long returnPos = Stream.Position;
            // Write audio Info
            appendPACInfo("#WAV: " + audioNumber);

            byte[] headerChunk = extractChunk(Stream.Position, nextAudioHeaderOffset);
            string base64 = Convert.ToBase64String(headerChunk);

            appendPACInfo("Sound original Info " + base64);

            Stream.Seek(returnPos, SeekOrigin.Begin);
            Stream.Seek(0x10, SeekOrigin.Current);

            int AT3DataSize = readIntBigEndian(Stream.Position);
            int relativeAT3DataOffset = readIntBigEndian(Stream.Position);

            appendPACInfo("WAV Data Size: " + AT3DataSize.ToString());
            appendPACInfo("relative WAV Data Offset: " + relativeAT3DataOffset.ToString());

            Stream.Seek(STREAMPosition + STREAMHeaderChunkSize + relativeAT3DataOffset, SeekOrigin.Begin);

            extractWAV(AT3DataSize);
        }

        private void extractWAV(int AT3DataSize)
        {
            byte[] AT3Chunk = extractChunk(Stream.Position, AT3DataSize);
            extractAudio(AT3Chunk, "pcm");
        }

        private void parseVAG(uint subheader_size, uint stream_size, uint stream_relative_offset)
        {
            // subheaders
            uint subheaderCount = (uint)subheader_size / 0x4;
            appendPACInfo("Subheader Count: " + subheaderCount);

            // vag should only have 1 subheader, which is the sample rate.
            if (subheaderCount != 1)
                throw new Exception("Subheader count for vag is not 1!");

            // subheaders are in small endian
            uint sampleRate = readUIntSmallEndian();

            Stream.Seek(STREAMPosition + STREAMHeaderChunkSize + stream_relative_offset, SeekOrigin.Begin);
            extractVAG(stream_size);
        }

        private void extractVAG(uint AT3DataSize)
        {
            MemoryStream VAG = new MemoryStream();
            byte[] VAGChunk = extractChunk(Stream.Position, AT3DataSize);

            // Create Header for VAG.
            // this is not accurate.
            // https://wiki.xentax.com/index.php/VAG_Audio
            // use vgmstream's info.
            appendIntMemoryStream(VAG, 0x56414770, true); // VAG Header
            appendIntMemoryStream(VAG, 0x00000003, true); // Version 3
            appendIntMemoryStream(VAG, 0x00000000, true); // Source start offset, always "0"
            appendIntMemoryStream(VAG, VAGChunk.Length, true); // Size of the ADPCM data
            appendIntMemoryStream(VAG, 0x0000BB80, true); // Sample Rate, 48000
            appendZeroMemoryStream(VAG, 0x1C); // Reserved 0x1C bytes

            VAG.Write(VAGChunk, 0, VAGChunk.Length);

            extractAudio(VAG.ToArray(), "vag");
        }

        private void parseis14(int audioNumber, uint subheader_size, uint stream_size, uint stream_relative_offset)
        {
            // https://github.com/vgmstream/vgmstream/blob/master/src/meta/bnsf.c
            // subheaders
            uint subheaderCount = (uint)subheader_size / 0x4;

            appendPACInfo("Subheader Count: " + subheaderCount); // Theoretically we count like this

            // But to simplify the repack proccess we just assume all is14 has 0x30 long subheader BNSF length.
            if (subheader_size != 0x30)
                throw new Exception("Non 0x30 is14 subheader is not supported yet.");

            uint returnAddress = (uint)Stream.Position;

            uint BNSF_magic = readUIntBigEndian(); // BNSF
            uint BNSF_size = readUIntBigEndian(); // Total size including header
            uint BNSF_codec = readUIntBigEndian(); // IS14
            
            if (BNSF_codec != 0x49533134)
                throw new Exception("Non IS14 codec is not supported!");

            // sfmt 
            uint BNSF_sfmt = readUIntBigEndian(); // sfmt

            if (BNSF_sfmt != 0x73666D74)
                throw new Exception("BNSF offset for 0xC is not sfmt!");

            uint sfmt_size = readUIntBigEndian(); // size should be 0x14 (a.k.a the size after this point until sdat)

            if (sfmt_size != 0x14)
                throw new Exception("sfmt_size is not 0x14!");

            uint sfmt_flags = readUShort(true);
            uint sfmt_channel_count = readUShort(true);
            uint sfmt_sample_rate = readUIntBigEndian();
            uint sfmt_num_samples = readUIntBigEndian();
            uint sfmt_loop_adjust = readUIntBigEndian(); /* 0 when no loop */
            uint sfmt_block_size = readUShort(true);
            uint sfmt_block_samples = readUShort(true);

            // sdat
            uint BNSF_sdat = readUIntBigEndian();

            if (BNSF_sdat != 0x73646174)
                throw new Exception("BNSF offset for 0x28 is not sdat!");

            uint sdat_size = readUIntBigEndian(); // Size of stream (exlcude header)


            Stream.Seek(returnAddress, SeekOrigin.Begin);

            byte[] BNSFis14HeaderChunk = extractChunk(Stream.Position, 0x30);

            Stream.Seek(STREAMPosition + STREAMHeaderChunkSize + stream_relative_offset, SeekOrigin.Begin);
            extractBNSF(BNSFis14HeaderChunk, sdat_size);
        }

        private void extractBNSF(byte[] BNSFis14HeaderChunk, uint BNSFDataSize)
        {
            List<byte[]> BNSFBuffer = new List<byte[]>();
            byte[] BNSFData = extractChunk(Stream.Position, BNSFDataSize);
            BNSFBuffer.Add(BNSFis14HeaderChunk);
            BNSFBuffer.Add(BNSFData);

            byte[] BNSF = BNSFBuffer.SelectMany(b => b).ToArray();
            extractAudio(BNSF, "bnsf");
        }

        private void extractSTREAM()
        {
            long returnPosition = Stream.Position;
            Stream.Seek(STREAMPosition, SeekOrigin.Begin);
            byte[] STREAMHeaderChunk = extractChunk(Stream.Position, STREAMHeaderChunkSize);
            createFile("STREAM", STREAMHeaderChunk, createExtractFilePath(fileNumber));
            Stream.Seek(returnPosition, SeekOrigin.Begin);
        }

        private void extractAudio(byte[] audioBuffer, string fileExt)
        {
            string filePath = createExtractFilePath(fileNumber) + "-" + audioFileNumber.ToString("000");
            string filePathwithExt = filePath + "." + fileExt;
            createFile(fileExt, audioBuffer, filePath);

            if (Properties.Settings.Default.outputWAV)
            {
                if(fileExt == "bnsf")
                {
                    // Using the G722.1 decoder. Pass the filePath that is created. Banwidth is always 14000.
                    byte[] WAVBuffer = convertBNSFtoPCM(filePathwithExt, sampleRate, 14000);
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
                else if (fileExt == "pcm")
                {
                    byte[] WAVBuffer = convertPCMtoWAV(filePathwithExt, 48000, 1);
                    // Replace the original file with the new buffer.
                    createFile(fileExt, WAVBuffer, filePath);
                }
                else if (fileExt == "vag")
                {
                    // Using the VGMSTREAM to convert vag to PCM 
                    byte[] WAVBuffer = convertVAGtoWAV(filePathwithExt);
                    // Replace the original file with the new buffer.
                    createFile(fileExt, WAVBuffer, filePath);
                }
                renameFile(filePathwithExt, Path.GetFileNameWithoutExtension(filePath) + ".wav");
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
