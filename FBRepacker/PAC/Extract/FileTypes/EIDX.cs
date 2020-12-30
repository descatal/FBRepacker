using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Extract.FileTypes
{
    class EIDX : Internals
    {
        int initialFHMOffset;
        int fileSize;

        public EIDX(FileStream PAC, int FHMOffset, int size) : base()
        {
            changeStreamFile(PAC);
            initialFHMOffset = FHMOffset;
            fileSize = size;
        }

        public Dictionary<int, string> parseEIDX()
        {
            Dictionary<int, string> fileInfo = new Dictionary<int, string>();

            //parse EIDX file and write the relevant info inside PAC.info
            int version = readIntSmallEndian(Stream.Position);
            
            if (version != 0x02)
                throw new Exception("EIDX version not 2!");

            int ALEO_number = readIntBigEndian(Stream.Position);
            int ALEO_offset = readIntBigEndian(Stream.Position);

            int NUT_number = readIntBigEndian(Stream.Position);
            int NUT_offset = readIntBigEndian(Stream.Position);

            int NUD_number = readIntBigEndian(Stream.Position);
            int NUD_offset = readIntBigEndian(Stream.Position);

            string EIDX_str1 = readString(Stream.Position, 0x20);
            string EIDX_str2 = readString(Stream.Position, 0x20);

            fileInfo[0] = "EIDX";

            // Write EIDX Info
            appendPACInfo("EIDX_Str1: " + EIDX_str1);
            appendPACInfo("EIDX_Str2: " + EIDX_str2);
            appendPACInfo("EIDX_ALEO_Number: " + ALEO_number);
            appendPACInfo("EIDX_ALEO_Offset: " + ALEO_offset);
            appendPACInfo("EIDX_NUT_Number: " + NUT_number);
            appendPACInfo("EIDX_NUT_Offset: " + NUT_offset);
            appendPACInfo("EIDX_NUD_Number: " + NUD_number);
            appendPACInfo("EIDX_NUD_Offset: " + NUD_offset);

            // The offset starts from the start of the file, so all the offset are relative to the start of the file
            Stream.Seek(initialFHMOffset, SeekOrigin.Begin);

            Stream.Seek(ALEO_offset, SeekOrigin.Current);
            parseALEOList(ALEO_number, fileInfo);

            Stream.Seek(initialFHMOffset, SeekOrigin.Begin);

            Stream.Seek(NUT_offset, SeekOrigin.Current);
            parseNUTorNUDList(NUT_number, fileInfo);

            Stream.Seek(initialFHMOffset, SeekOrigin.Begin);

            Stream.Seek(NUD_offset, SeekOrigin.Current);
            parseNUTorNUDList(NUD_number, fileInfo);

            extractEIDX((int)Stream.Position);

            return fileInfo;
        }

        private void parseALEOList(int ALEO_number, Dictionary<int, string> fileInfo)
        {
            for(int i = 0; i < ALEO_number; i++)
            {
                int file_Index = readIntBigEndian(Stream.Position);
                uint file_Hash = readUIntBigEndian(Stream.Position);

                fileInfo[file_Index] = file_Hash.ToString("X8");
            }
        }

        private void parseNUTorNUDList(int NUT_number, Dictionary<int, string> fileInfo)
        {
            for (int i = 0; i < NUT_number; i++)
            {
                int file_Index = readIntBigEndian(Stream.Position);
                string file_Hash = readString(Stream.Position, 0x20);

                fileInfo[file_Index] = file_Hash.ToString();
            }
        }

        private void extractEIDX(int returnPosition)
        {
            // Reset the stream position as we need to call extract general to extract the EIDX base file from the top.
            Stream.Seek(initialFHMOffset, SeekOrigin.Begin);
            byte[] EIDXHeaderChunk = extractChunk(Stream.Position, fileSize);
            createFile("EIDX", EIDXHeaderChunk, createExtractFilePath(fileNumber));
            Stream.Seek(returnPosition, SeekOrigin.Begin);
        }
    }
}
