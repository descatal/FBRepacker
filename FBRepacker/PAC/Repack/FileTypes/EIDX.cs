using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FBRepacker.PAC.Repack.customFileInfo;

namespace FBRepacker.PAC.Repack.FileTypes
{
    public class EIDX : Internals
    {
        public Dictionary<int, EIDXFileInfo> EIDXFileInfoDic = new Dictionary<int, EIDXFileInfo>();
        public string str1, str2;
        public int ALEO_number, ALEO_offset, NUT_number, NUT_offset, NUD_number, NUD_offset;

        public EIDX() : base()
        {

        }

        public void parseEIDXMetadata(string[] EIDXMetadata)
        {
            str1 = getSpecificFileInfoProperties("EIDX_Str1: ", EIDXMetadata);
            str2 = getSpecificFileInfoProperties("EIDX_Str2: ", EIDXMetadata);
            ALEO_number = convertStringtoInt(getSpecificFileInfoProperties("EIDX_ALEO_Number: ", EIDXMetadata));
            ALEO_offset = convertStringtoInt(getSpecificFileInfoProperties("EIDX_ALEO_Offset: ", EIDXMetadata));
            NUT_number = convertStringtoInt(getSpecificFileInfoProperties("EIDX_NUT_Number: ", EIDXMetadata));
            NUT_offset = convertStringtoInt(getSpecificFileInfoProperties("EIDX_NUT_Offset: ", EIDXMetadata));
            NUD_number = convertStringtoInt(getSpecificFileInfoProperties("EIDX_NUD_Number: ", EIDXMetadata));
            NUD_offset = convertStringtoInt(getSpecificFileInfoProperties("EIDX_NUD_Offset: ", EIDXMetadata));
        }

        public void parseEIDXInfo(string[] EIDXInfo, int fileNo, string header)
        {
            EIDXFileInfo EIDX_FileInfo = new EIDXFileInfo();
            EIDX_FileInfo.file_Index = convertStringtoInt(getSpecificFileInfoProperties("EIDX_Index: ", EIDXInfo));
            EIDX_FileInfo.file_Hash = getSpecificFileInfoProperties("EIDX_Name: ", EIDXInfo);

            switch (header)
            {
                case "ALEO":
                    EIDX_FileInfo.file_Header = EIDXFileInfo.fileType.ALEO;
                    break;
                case "NTP3":
                    EIDX_FileInfo.file_Header = EIDXFileInfo.fileType.NUT;
                    break;
                case "nud":
                    EIDX_FileInfo.file_Header = EIDXFileInfo.fileType.NUD;
                    break;
                case "EIDX":
                    break;
                default:
                    throw new Exception(header + " is not a valid EIDX Header! Check file: " + fileNo);
            }

            EIDXFileInfoDic[fileNo] = EIDX_FileInfo;
        }

        public byte[] repackEIDX()
        {
            List<EIDXFileInfo> ALEO_FileList = EIDXFileInfoDic.Values.Where(s => s.file_Header == EIDXFileInfo.fileType.ALEO).ToList();
            List<EIDXFileInfo> NUT_FileList = EIDXFileInfoDic.Values.Where(s => s.file_Header == EIDXFileInfo.fileType.NUT).ToList();
            List<EIDXFileInfo> NUD_FileList = EIDXFileInfoDic.Values.Where(s => s.file_Header == EIDXFileInfo.fileType.NUD).ToList();

            long ALEO_Pointer_Offset = 0, NUT_Pointer_Offset = 0, NUD_Pointer_Offset = 0 ;

            MemoryStream EIDX = new MemoryStream();

            // EIDX Magic
            appendUIntMemoryStream(EIDX, 0x45494458, true);
            // Version number, 2.
            appendUIntMemoryStream(EIDX, 0x00000002, false);

            ALEO_number = ALEO_FileList.Count();
            appendIntMemoryStream(EIDX, ALEO_number, true);
            ALEO_Pointer_Offset = EIDX.Position;
            appendIntMemoryStream(EIDX, 0, true);

            NUT_number = NUT_FileList.Count();
            appendIntMemoryStream(EIDX, NUT_number, true);
            NUT_Pointer_Offset = EIDX.Position;
            appendIntMemoryStream(EIDX, 0, true);

            NUD_number = NUD_FileList.Count();
            appendIntMemoryStream(EIDX, NUD_number, true);
            NUD_Pointer_Offset = EIDX.Position;
            appendIntMemoryStream(EIDX, 0, true);

            // string has fixed size of 0x20
            str1 = str1 != string.Empty ? str1 : "common";
            appendStringMemoryStream(EIDX, str1, Encoding.Default, 0x20);
            appendStringMemoryStream(EIDX, str2, Encoding.Default, 0x20);

            ALEO_offset = (int)EIDX.Position;

            foreach (var ALEO in ALEO_FileList)
            {
                appendIntMemoryStream(EIDX, ALEO.file_Index, true);
                appendIntMemoryStream(EIDX, convertHexStringtoInt(ALEO.file_Hash, true), true);
            }

            NUT_offset = (int)EIDX.Position;

            foreach (var NUT in NUT_FileList)
            {
                appendIntMemoryStream(EIDX, NUT.file_Index, true);
                appendStringMemoryStream(EIDX, NUT.file_Hash, Encoding.Default, 0x20); 
            }

            NUD_offset = (int)EIDX.Position;

            foreach (var NUD in NUD_FileList)
            {
                appendIntMemoryStream(EIDX, NUD.file_Index, true);
                appendStringMemoryStream(EIDX, NUD.file_Hash, Encoding.Default, 0x20);
            }

            // Write correct metadata pointers.
            EIDX.Seek(ALEO_Pointer_Offset, SeekOrigin.Begin);
            EIDX.Write(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(ALEO_offset)), 0, 4);

            EIDX.Seek(NUT_Pointer_Offset, SeekOrigin.Begin);
            EIDX.Write(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(NUT_offset)), 0, 4);

            EIDX.Seek(NUD_Pointer_Offset, SeekOrigin.Begin);
            EIDX.Write(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(NUD_offset)), 0, 4);

            //addPaddingStream(EIDX);

            return EIDX.ToArray();
        }
    }
}
