using FBRepacker.PAC.Repack.customFileInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Repack.FileTypes
{
    class NTP3 : Internals
    {
        public Dictionary<int, List<NTP3FileInfo>> NTP3FileInfoDic = new Dictionary<int, List<NTP3FileInfo>>();
        public Dictionary<int, List<NTP3FileInfo>> realNTP3FileDic = new Dictionary<int, List<NTP3FileInfo>>();

        public static Dictionary<int, string> StringCompareDDSCompressionType = new Dictionary<int, string>
            {
                { 0x44585435, "DXT5" },
                { 0x44585431, "DXT1"},
                { 0x00000000, "No Compression"}
            };

        public static Dictionary<string, short> NTP3DDSCompressionType = new Dictionary<string, short>
            {
                { "DXT5", 0x0002 },
                { "DXT1", 0x0000 },
                { "No Compression byteReversed", 0x000E },
                { "No Compression", 0x0007 }
            };

        public NTP3() : base()
        {

        }

        public void parseNTP3Info(string[] NTP3Info, int fileNumber)
        {
            List<NTP3FileInfo> NTP3FileInfoList = new List<NTP3FileInfo>();
            int numberofFiles = convertStringtoInt(getSpecificFileInfoProperties("Number of Files: ", NTP3Info));
            for(int fileNo = 1; fileNo <= numberofFiles; fileNo++)
            {
                string from = "#DDS: " + fileNo.ToString();
                string end = "#DDS: " + (fileNo + 1).ToString();
                string[] DDSInfo = getSpecificFileInfoPropertiesRegion(NTP3Info, from, end);
                //var werdf = convertByteArraytoString(convertStringtoByteArray("GIDX\0\0\0\u0010\u001cD�D\0\0\0\0", false), false);
                //var weqr = convertByteArraytoInt32(convertHexStringtoByteArray("1C44CC44", true), false);
                NTP3FileInfo newFileInfo = new NTP3FileInfo();

                newFileInfo.fileNo = fileNo;
                newFileInfo.widthReso = convertStringtoInt(getSpecificFileInfoProperties("Width Resolution: ", DDSInfo));
                newFileInfo.heightReso = convertStringtoInt(getSpecificFileInfoProperties("Height Resolution: ", DDSInfo));
                newFileInfo.hexName = convertHexStringtoByteArray(getSpecificFileInfoProperties("Name: ", DDSInfo), true);
                newFileInfo.remainderNTP3Chunk = convertStringtoByteArray(getSpecificFileInfoProperties("remainderNTP3Chunk: ", DDSInfo), false);
                newFileInfo.GIDXChunk = convertStringtoByteArray(getSpecificFileInfoProperties("GIDXChunk: ", DDSInfo), false);
                newFileInfo.CompressionType = getSpecificFileInfoProperties("Compression Type: ", DDSInfo);
                newFileInfo.DDSDataChunkSize = convertStringtoInt(getSpecificFileInfoProperties("DDS Data Chunk Size: ", DDSInfo));
                newFileInfo.NTP3HeaderChunkSize = convertStringtoInt(getSpecificFileInfoProperties("NTP3 Header Chunk Size: ", DDSInfo));
                newFileInfo.beforeCompressionShort = convertStringtoInt(getSpecificFileInfoProperties("beforeCompressionShort: ", DDSInfo));

                NTP3FileInfoList.Add(newFileInfo);
            }

            NTP3FileInfoDic[fileNumber] = NTP3FileInfoList;
        }

        public byte[] repackDDStoNTP3(FileInfo file, int fileNumber)
        {
            if (!NTP3FileInfoDic.ContainsKey(fileNumber))
                throw new Exception("File number: " + fileNumber + " for dds file could not be found in NTP3FileInfoDic Dictionary");
            
            List<NTP3FileInfo> NTP3FileInfoList = NTP3FileInfoDic[fileNumber];
            List<FileInfo> allDDSFiles = new List<FileInfo>();
            MemoryStream NTP3Stream = new MemoryStream();
            int numberofFilesinNTP3 = NTP3FileInfoList.Count;
            string originalFilePath = Path.GetDirectoryName(file.FullName);

            for (int i = 0; i < numberofFilesinNTP3; i++)
            {
                string hexName = convertByteArraytoString(NTP3FileInfoList[i].hexName, true);
                string DDSFilePath = originalFilePath + @"\" + fileNumber.ToString("000") + "-" + (i + 1).ToString("000") + " (" + hexName + ").dds";

                if (File.Exists(DDSFilePath))
                {
                    FileInfo DDSFile = new FileInfo(DDSFilePath);
                    allDDSFiles.Add(DDSFile);
                }
                else
                {
                    throw new Exception("Cannot found DDS file with name: " + DDSFilePath);
                }
            }

            List<DDSFileInfo> DDSFileList = parseDDS(NTP3FileInfoList, allDDSFiles);

            repackNTP3(NTP3FileInfoList, DDSFileList, NTP3Stream);

            byte[] NTP3Buffer = NTP3Stream.GetBuffer();

            FileStream fileStream = File.Create(Directory.GetCurrentDirectory() + (@"\temp\NTP3" + fileNumber.ToString()));
            fileStream.Write(NTP3Buffer, 0, NTP3Buffer.Length);

            fileStream.Close();
            NTP3Stream.Close();

            return NTP3Buffer;
        }

        private List<DDSFileInfo> parseDDS(List<NTP3FileInfo> NTP3FileInfoList, List<FileInfo> allDDSFiles)
        {
            List<DDSFileInfo> realNTP3FileList = new List<DDSFileInfo>();
            int numberofDDS = allDDSFiles.Count;

            for(int fileNo = 0; fileNo < numberofDDS; fileNo++)
            {
                FileStream DDSStream = File.OpenRead(allDDSFiles[fileNo].FullName);
                NTP3FileInfo NTP3FileInfo = NTP3FileInfoList[fileNo];
                changeStreamFile(DDSStream);
                DDSStream.Seek(0x0C, SeekOrigin.Begin);

                int heightReso = readIntSmallEndian(DDSStream.Position);
                int widthReso = readIntSmallEndian(DDSStream.Position);

                if(widthReso != NTP3FileInfo.widthReso || heightReso != NTP3FileInfo.heightReso)
                    throw new Exception("Image resolution mismatch between DDS file and NTP3FileInfo!" + Environment.NewLine + "DDS File: " + allDDSFiles[fileNo].FullName);

                DDSStream.Seek(0x40, SeekOrigin.Current);
                string CompressionType = identifyCompressionType(readIntBigEndian(DDSStream.Position));

                if (CompressionType == "No Compression")
                {
                    DDSStream.Seek(-0x08, SeekOrigin.Current);
                    CompressionType = readIntBigEndian(DDSStream.Position) == 0x40000000 ? "No Compression byteReversed" : "No Compression";
                    DDSStream.Seek(0x04, SeekOrigin.Current);
                }

                if (CompressionType != NTP3FileInfo.CompressionType)
                    throw new Exception("Compression Type mismatch between DDS file and NTP3FileInfo!" + Environment.NewLine + "DDS File: " + allDDSFiles[fileNo].FullName);

                int DDSFileChunkSize = (int)DDSStream.Length - 0x80;
                DDSStream.Seek(0x80, SeekOrigin.Begin);
                // TODO: make a log that DDS file size is different (new file)

                DDSFileInfo newFileInfo = new DDSFileInfo();

                newFileInfo.fileNo = fileNo + 1;
                newFileInfo.DDSFileChunkSize = DDSFileChunkSize;
                newFileInfo.widthReso = widthReso;
                newFileInfo.heightReso = heightReso;
                newFileInfo.CompressionType = CompressionType;
                newFileInfo.hexName = convertInt32toByteArray(getDDSFileName(DDSStream.Name), true);
                newFileInfo.beforeCompressionShort = NTP3FileInfo.beforeCompressionShort;
                DDSStream.CopyTo(newFileInfo.DDSByteStream);

                realNTP3FileList.Add(newFileInfo);
                DDSStream.Close();
            }

            return realNTP3FileList;
        }

        private void repackNTP3(List<NTP3FileInfo> NTP3FileInfoList, List<DDSFileInfo> DDSFileList, MemoryStream NTP3Stream)
        {
            for(int fileNo = 0; fileNo < DDSFileList.Count; fileNo++)
            {
                NTP3FileInfo NTP3Info = NTP3FileInfoList[fileNo];
                DDSFileInfo DDSFile = DDSFileList[fileNo];

                bool writeWithNTP3Header = fileNo == 0 ? true : false;
                writeNTP3Header(writeWithNTP3Header, NTP3Info, DDSFileList, fileNo, NTP3Stream);

                byte[] DDSDataChunkBuffer = DDSFile.DDSByteStream.GetBuffer();
                NTP3Stream.Write(DDSDataChunkBuffer, 0, DDSDataChunkBuffer.Length);

                byte[] temp = NTP3Stream.GetBuffer();
                FileStream fileStream = File.Create(Directory.GetCurrentDirectory() + (@"\temp\NTP3-" + fileNumber.ToString()));
                fileStream.Write(DDSDataChunkBuffer, 0, DDSDataChunkBuffer.Length);

                fileStream.Close();
            }
        }

        private void writeNTP3Header(bool withHeader, NTP3FileInfo NTP3FileInfo, List<DDSFileInfo> DDSFileList, int fileNo, Stream NTP3Stream)
        {
            int NTP3HeaderChunkSize = NTP3FileInfo.NTP3HeaderChunkSize;
            int DDSDataChunkSize = NTP3FileInfo.DDSDataChunkSize;
            int combinedSize = NTP3HeaderChunkSize + DDSDataChunkSize;
            DDSFileInfo DDSFile = DDSFileList[fileNo];

            byte[] NTP3HeaderMetadata = new byte[0];
            if (withHeader)
            {
                NTP3HeaderMetadata = appendIntByteStream(NTP3HeaderMetadata, 0x4E545033, true);
                NTP3HeaderMetadata = appendShortByteStream(NTP3HeaderMetadata, 0x0001, false);
                NTP3HeaderMetadata = appendShortByteStream(NTP3HeaderMetadata, (ushort)DDSFileList.Count, true);
                NTP3HeaderMetadata = appendZeroByteStream(NTP3HeaderMetadata, 0x08);
            }

            NTP3HeaderMetadata = appendIntByteStream(NTP3HeaderMetadata, (uint)combinedSize, true);
            NTP3HeaderMetadata = appendZeroByteStream(NTP3HeaderMetadata, 0x04);
            NTP3HeaderMetadata = appendIntByteStream(NTP3HeaderMetadata, (uint)DDSDataChunkSize, true);
            NTP3HeaderMetadata = appendShortByteStream(NTP3HeaderMetadata, (ushort)NTP3HeaderChunkSize, true);
            NTP3HeaderMetadata = appendZeroByteStream(NTP3HeaderMetadata, 0x02);

            if(!NTP3DDSCompressionType.ContainsKey(DDSFile.CompressionType))
                throw new Exception("Cannot find Compression Type: " + DDSFile.CompressionType + " in NTP3DDSCompressionType dic");

            NTP3HeaderMetadata = appendShortByteStream(NTP3HeaderMetadata, (ushort)DDSFile.beforeCompressionShort, true);
            NTP3HeaderMetadata = appendShortByteStream(NTP3HeaderMetadata, (ushort)NTP3DDSCompressionType[DDSFileList[fileNo].CompressionType], true);
            NTP3HeaderMetadata = appendShortByteStream(NTP3HeaderMetadata, (ushort)DDSFile.widthReso, true);
            NTP3HeaderMetadata = appendShortByteStream(NTP3HeaderMetadata, (ushort)DDSFile.heightReso, true);

            byte[] remainderNTP3ChunkBuffer = NTP3FileInfo.remainderNTP3Chunk;
            byte[] GIDXChunkBuffer = NTP3FileInfo.GIDXChunk;
            byte[] NTP3HeaderBuffer = NTP3HeaderMetadata.Concat(remainderNTP3ChunkBuffer).Concat(GIDXChunkBuffer).ToArray();

            NTP3Stream.Write(NTP3HeaderBuffer, 0, NTP3HeaderBuffer.Length);
        }

        // Utilities
        private string identifyCompressionType(int magic)
        {
            if (StringCompareDDSCompressionType.ContainsKey(magic))
            {
                return StringCompareDDSCompressionType[magic];
            }
            else
            {
                throw new Exception("Compression magic unidentified.");
            }
        }

        private int getDDSFileName(string fileName)
        {
            string newFileName = fileName;
            if (fileName.Contains('('))
            {
                int pFrom = fileName.IndexOf(" (") + " (".Length;
                int pTo = fileName.LastIndexOf(")");
                newFileName = fileName.Substring(pFrom, pTo - pFrom);
            }
            else
            {
                throw new Exception("DDS fileName format error, '(' not found: " + fileName);
            }

            if (int.TryParse(newFileName, System.Globalization.NumberStyles.HexNumber, null, out int fileNumber))
            {
                return fileNumber;
            }
            else
            {
                throw new Exception("fileName int to string conversion failed with fileName: " + fileName);
            }
        }
    }
}
