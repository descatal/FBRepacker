using FBRepacker.PAC.Repack.customFileInfo;
using OpenTK.Graphics.OpenGL;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Repack.FileTypes
{
    public class NTP3 : Internals
    {
        public Dictionary<int, List<NTP3FileInfo>> NTP3FileInfoDic = new Dictionary<int, List<NTP3FileInfo>>();
        public Dictionary<int, List<NTP3FileInfo>> realNTP3FileDic = new Dictionary<int, List<NTP3FileInfo>>();

        public static Dictionary<int, string> StringCompareDDSCompressionType = new Dictionary<int, string>
            {
                { 0x44585433, "DXT3"},
                { 0x44585435, "DXT5" },
                { 0x44585431, "DXT1"},
                { 0x00000000, "No Compression"}
            };

        public static Dictionary<string, ushort> NTP3CompressedTypes = new Dictionary<string, ushort>
            {
                { "DXT3", 0x0001 },
                { "DXT5", 0x0002 },
                { "DXT1", 0x0000 },
            };

        public NTP3() : base()
        {
            
        }

        /* Pre Base64 conversion code: TODO remove
        public StreamReader getNTP3InfoStreamReader(string[] NTP3Info)
        {
            string PACPath = rootDirectory + @"\PAC.info";
            StreamReader allStream = new StreamReader(PACPath);
            string from = NTP3Info.First();
            string end = NTP3Info.LastOrDefault(line => line != "");
            byte[] NTP3InfoBuffer = readByteArrayinPACInfoBetweenString(allStream, from, end, false);

            MemoryStream NTP3InfoMemoryStream = new MemoryStream();
            NTP3InfoMemoryStream.Write(NTP3InfoBuffer, 0, NTP3InfoBuffer.Length);

            StreamReader NTP3InfoStream = new StreamReader(NTP3InfoMemoryStream);
            return NTP3InfoStream;
        }
        */

        public void parseNTP3Info(string[] NTP3Info, int fileNumber)
        {
            List<NTP3FileInfo> NTP3FileInfoList = new List<NTP3FileInfo>();
            int numberofFiles = convertStringtoInt(getSpecificFileInfoProperties("Number of Files: ", NTP3Info));
            for(int fileNo = 1; fileNo <= numberofFiles; fileNo++)
            {
                string from = "#DDS: " + fileNo.ToString();
                string end = "#DDS: " + (fileNo + 1).ToString();
                string[] DDSInfo = getSpecificFileInfoPropertiesRegion(NTP3Info, from, end);

                /* Pre Base64 conversion code: TODO remove
                string end = fileNo != numberofFiles ? "#DDS: " + (fileNo + 1).ToString() : NTP3Info.LastOrDefault(line => line != "");
                byte[] DDSInfoStreamInBytes = readByteArrayinPACInfoBetweenString(NTP3InfoStream, from, end, false);

                MemoryStream DDSStreamMem = new MemoryStream();
                DDSStreamMem.Write(DDSInfoStreamInBytes, 0, DDSInfoStreamInBytes.Length);
                StreamReader DDSInfoStream = new StreamReader(DDSStreamMem);
                */

                if(fileNo == numberofFiles)
                {

                }

                NTP3FileInfo newFileInfo = new NTP3FileInfo();

                newFileInfo.fileNo = fileNo;
                newFileInfo.widthReso = convertStringtoInt(getSpecificFileInfoProperties("Width Resolution: ", DDSInfo));
                newFileInfo.heightReso = convertStringtoInt(getSpecificFileInfoProperties("Height Resolution: ", DDSInfo));
                newFileInfo.hexName = convertHexStringtoByteArray(getSpecificFileInfoProperties("Name: ", DDSInfo), true);

                byte[] eXtChunk = Convert.FromBase64String(getSpecificFileInfoProperties("eXtChunk: ", DDSInfo));
                newFileInfo.eXtChunk = eXtChunk; 

                byte[] GIDXChunk = Convert.FromBase64String(getSpecificFileInfoProperties("GIDXChunk: ", DDSInfo));
                newFileInfo.GIDXChunk = GIDXChunk;

                newFileInfo.CompressionType = getSpecificFileInfoProperties("Compression Type: ", DDSInfo);
                newFileInfo.DDSDataChunkSize = convertStringtoInt(getSpecificFileInfoProperties("DDS Data Chunk Size: ", DDSInfo));
                newFileInfo.NTP3HeaderChunkSize = convertStringtoInt(getSpecificFileInfoProperties("NTP3 Header Chunk Size: ", DDSInfo));
                int numberofMipmaps = convertStringtoInt(getSpecificFileInfoProperties("numberofMipmaps: ", DDSInfo));

                string fileName = getSpecificFileInfoProperties("fileName: ", DDSInfo);
                newFileInfo.fileName = fileName;

                if (newFileInfo.CompressionType == "No Compression")
                    newFileInfo.pixelFormat = getSpecificFileInfoProperties("pixelFormat: ", DDSInfo);

                newFileInfo.numberofMipmaps = numberofMipmaps;

                for(int i = 0; i < numberofMipmaps; i++)
                {
                    //newFileInfo.mipmapsSizeList.Add(convertStringtoInt(getSpecificFileInfoProperties("mipmapSize" + i.ToString() + ": ", DDSInfo)));
                }

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
                //string baseFileName = originalFilePath + @"\" + fileNumber.ToString("000");
                //string DDSFilePath = hexName == "0000"? baseFileName + ".dds" : baseFileName + "-" + (i + 1).ToString("000") + " (" + hexName + ").dds";
                
                string DDSFilePath = originalFilePath + @"\" + NTP3FileInfoList[i].fileName;

                if (File.Exists(DDSFilePath))
                {
                    FileInfo DDSFile = new FileInfo(DDSFilePath);
                    allDDSFiles.Add(DDSFile);
                }
                else
                {
                    throw new Exception("Cannot find DDS file with name: " + DDSFilePath);
                }
            }

            List<DDSFileInfo> DDSFileList = parseDDS(NTP3FileInfoList, allDDSFiles);

            repackNTP3(NTP3FileInfoList, DDSFileList, NTP3Stream);

            byte[] NTP3Buffer = NTP3Stream.ToArray();

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
                // https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-header
                FileStream DDSStream = File.OpenRead(allDDSFiles[fileNo].FullName);
                NTP3FileInfo NTP3FileInfo = NTP3FileInfoList[fileNo];
                changeStreamFile(DDSStream);

                if (readIntBigEndian(DDSStream.Position) != 0x44445320)
                    throw new Exception("DDS header not found!");

                // 7C byte
                DDSStream.Seek(0x04, SeekOrigin.Current);

                // DDSFlags
                dwDDSFlags ddsFlags = (dwDDSFlags)readIntSmallEndian(DDSStream.Position);
                dwDDSFlags requiredDDSFlags = dwDDSFlags.DDSD_CAPS | dwDDSFlags.DDSD_HEIGHT | dwDDSFlags.DDSD_WIDTH | dwDDSFlags.DDSD_PIXELFORMAT;

                if (!ddsFlags.HasFlag(requiredDDSFlags))
                    throw new Exception("DDS Flags error!");
                
                // Resolutions
                int heightReso = readIntSmallEndian(DDSStream.Position);
                int widthReso = readIntSmallEndian(DDSStream.Position);

                if(widthReso != NTP3FileInfo.widthReso || heightReso != NTP3FileInfo.heightReso)
                    throw new Exception("Image resolution mismatch between DDS file and NTP3FileInfo!" + Environment.NewLine + "DDS File: " + allDDSFiles[fileNo].FullName);

                // pitchorLinearSize
                int pitchorLinearSize = readIntSmallEndian(DDSStream.Position); // Not used as per Microsoft's Guideline.

                // depth, skipped.
                DDSStream.Seek(0x04, SeekOrigin.Current);

                // Mipmaps
                int numberofMipmaps = readIntSmallEndian(DDSStream.Position);

                // Reserved 11*4 byte + 4 byte dwSizeforPixelFormat.
                DDSStream.Seek(0x30, SeekOrigin.Current);

                // Pixel Formats.
                dwPixelFormatFlags pixelFlags = (dwPixelFormatFlags) readIntSmallEndian(DDSStream.Position);
                string compressionType = null;
                uint dwRGBBitCount = 0, dwRBitMask = 0, dwGBitMask = 0, dwBBitMask = 0, dwABitMask = 0, dwCaps = 0, dwCaps2 = 0, dwCaps3 = 0, dwCaps4 = 0;

                DDSFileInfo newFileInfo = new DDSFileInfo();
                int DDSFileChunkSize = addPaddingSizeCalculation((int)DDSStream.Length) - 0x80;

                if (pixelFlags.HasFlag(dwPixelFormatFlags.DDPF_FOURCC))
                {
                    // Compressed.
                    compressionType = identifyCompressionType(readIntBigEndian(DDSStream.Position));
                    DDSStream.Seek(0x80, SeekOrigin.Begin);
                    DDSStream.CopyTo(newFileInfo.DDSByteStream); // If not compressed, directly copy the RGBA values. 
                }
                else
                {
                    DDSStream.Seek(0x04, SeekOrigin.Current);
                    compressionType = "No Compression";

                    dwRGBBitCount = readUIntSmallEndian(DDSStream.Position);
                    dwRBitMask = readUIntSmallEndian(DDSStream.Position);
                    dwGBitMask = readUIntSmallEndian(DDSStream.Position);
                    dwBBitMask = readUIntSmallEndian(DDSStream.Position);
                    dwABitMask = readUIntSmallEndian(DDSStream.Position);
                    dwCaps = readUIntSmallEndian(DDSStream.Position); // Caps are not used to determine stuff although we set them.
                    dwCaps2 = readUIntSmallEndian(DDSStream.Position); // TODO: caps 2 stores cubemap infos.
                    dwCaps3 = readUIntSmallEndian(DDSStream.Position);
                    dwCaps4 = readUIntSmallEndian(DDSStream.Position);

                    DDSStream.Seek(0x04, SeekOrigin.Current); // Reserved 1*4 bytes.

                    bool isAlpha = pixelFlags.HasFlag(dwPixelFormatFlags.DDPF_ALPHAPIXELS);

                    if (dwRGBBitCount % 8 != 0)
                        throw new Exception("dwRGBitCount not a multiple of 4!");

                    int RGBAByteCount = (int)(dwRGBBitCount / 8);
                    byte[] RGBAChunk = extractChunk(0x80, DDSFileChunkSize);
                    List<byte[]> RGBA = parseMaskedRGBA(isAlpha, RGBAChunk, RGBAByteCount, dwRBitMask, dwGBitMask, dwBBitMask, dwABitMask);

                    newFileInfo.pixelFormatRGBAByteSize = RGBAByteCount;

                    // We try to convert based on the pixelFormat in info file, else we use the default of Abgr.
                    byte[] maskedRGBA;
                    if(Enum.TryParse(NTP3FileInfo.pixelFormat, out PixelFormat pixelFormat))
                    {
                        maskedRGBA = writeMaskedRGBA(pixelFormat, RGBA);
                    }
                    else
                    {
                        maskedRGBA = writeMaskedRGBA(PixelFormat.AbgrExt, RGBA);
                    }

                    newFileInfo.DDSByteStream.Write(maskedRGBA, 0, maskedRGBA.Length);
                }

                if (compressionType != NTP3FileInfo.CompressionType)
                    throw new Exception("Compression Type mismatch between DDS file and NTP3FileInfo!" + Environment.NewLine + "DDS File: " + allDDSFiles[fileNo].FullName);
                
                newFileInfo.fileNo = fileNo + 1;
                newFileInfo.DDSFileChunkSize = DDSFileChunkSize;
                newFileInfo.widthReso = widthReso;
                newFileInfo.heightReso = heightReso;
                newFileInfo.CompressionType = compressionType;
                newFileInfo.hexName = convertInt32toByteArray(getDDSFileName(DDSStream.Name), true);
                newFileInfo.numberofMipmaps = numberofMipmaps;

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

                // We try to follow the compressionType

                bool writeWithNTP3Header = fileNo == 0 ? true : false;
                writeNTP3Header(writeWithNTP3Header, NTP3Info, DDSFileList, fileNo, NTP3Stream);

                // byteReverse?
                byte[] DDSDataChunkBuffer = addPaddingArrayBuffer(DDSFile.DDSByteStream.ToArray());
                NTP3Stream.Write(DDSDataChunkBuffer, 0, DDSDataChunkBuffer.Length);

                FileStream fileStream = File.Create(Directory.GetCurrentDirectory() + (@"\temp\NTP3-" + fileNumber.ToString()));
                fileStream.Write(DDSDataChunkBuffer, 0, DDSDataChunkBuffer.Length);

                fileStream.Close();
            }
        }

        private void writeNTP3Header(bool withHeader, NTP3FileInfo NTP3FileInfo, List<DDSFileInfo> DDSFileList, int fileNo, MemoryStream NTP3Stream)
        {
            DDSFileInfo DDSFile = DDSFileList[fileNo];

            int DDSDataChunkSize = DDSFile.DDSFileChunkSize;

            byte[] NTP3Header = new byte[0];
            byte[] NTP3Metadata = new byte[0];
            if (withHeader)
            {
                NTP3Header = appendUIntArrayBuffer(NTP3Header, 0x4E545033, true);
                NTP3Header = appendShortArrayBuffer(NTP3Header, 0x0001, false); // Version 1
                NTP3Header = appendShortArrayBuffer(NTP3Header, (ushort)DDSFileList.Count, true);
                NTP3Header = appendZeroArrayBuffer(NTP3Header, 0x08);
            }

            int combinedSizeOffset = NTP3Metadata.Length;
            NTP3Metadata = appendUIntArrayBuffer(NTP3Metadata, 0, true); // Placeholder, will be written once the header part is all done.
            NTP3Metadata = appendZeroArrayBuffer(NTP3Metadata, 0x04);
            NTP3Metadata = appendUIntArrayBuffer(NTP3Metadata, (uint)DDSDataChunkSize, true);
            int NTP3HeaderChunkSizeOffset = NTP3Metadata.Length;
            NTP3Metadata = appendShortArrayBuffer(NTP3Metadata, 0, true); // Placeholder, will be written once the header part is all done.
            NTP3Metadata = appendZeroArrayBuffer(NTP3Metadata, 0x02);

            NTP3Metadata = appendShortArrayBuffer(NTP3Metadata, (ushort)DDSFile.numberofMipmaps, true);

            ushort NTP3compresionType = getNTP3CompressionType(DDSFile, NTP3FileInfo);
            
            NTP3Metadata = appendShortArrayBuffer(NTP3Metadata, NTP3compresionType, true);
            NTP3Metadata = appendShortArrayBuffer(NTP3Metadata, (ushort)DDSFile.widthReso, true);
            NTP3Metadata = appendShortArrayBuffer(NTP3Metadata, (ushort)DDSFile.heightReso, true);

            // TODO: cube maps
            NTP3Metadata = appendZeroArrayBuffer(NTP3Metadata, 0x18);

            // Manually calculate mipmapsSize, not trusting Info file's.
            List<uint> mipmapsSizeList = calculateMipmapsSize(DDSFile);
            if(mipmapsSizeList.Count > 1)
            {
                // Only write the offsets if there are more than 1 mipmaps. (1 is the original)
                for (int i = 0; i < mipmapsSizeList.Count; i++)
                {
                    NTP3Metadata = appendUIntArrayBuffer(NTP3Metadata, mipmapsSizeList[i], true);
                }
            }


            NTP3Metadata = addPaddingArrayBuffer(NTP3Metadata);

            byte[] eXtChunkBuffer = NTP3FileInfo.eXtChunk;
            byte[] GIDXChunkBuffer = NTP3FileInfo.GIDXChunk; // TODO: write hash seperately.

            byte[] hexName = NTP3FileInfo.hexName;
            int a = 0x8;
            foreach(var name in hexName)
            {
                GIDXChunkBuffer[a] = name;
                a++;
            }

            byte[] NTP3MetadataBuffer = NTP3Metadata.Concat(eXtChunkBuffer).Concat(GIDXChunkBuffer).ToArray();

            // I don't like this implementation, for changing size when everything is written.
            ushort NTP3HeaderChunkSize = (ushort)NTP3MetadataBuffer.Length;
            uint combinedSize = (uint)(NTP3HeaderChunkSize + DDSDataChunkSize);

            byte[] NTP3HeaderChunkSizeBuffer = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(NTP3HeaderChunkSize));
            byte[] combinedSizeBuffer = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(combinedSize));

            for (int i = 0; i < NTP3HeaderChunkSizeBuffer.Length; i++)
            {
                NTP3MetadataBuffer[NTP3HeaderChunkSizeOffset + i] = NTP3HeaderChunkSizeBuffer[i];
            }

            for (int i = 0; i < combinedSizeBuffer.Length; i++)
            {
                NTP3MetadataBuffer[combinedSizeOffset + i] = combinedSizeBuffer[i];
            }

            byte[] NTP3HeaderBuffer = NTP3Header.Concat(NTP3MetadataBuffer).ToArray();

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
            string newFileName = Path.GetFileNameWithoutExtension(fileName);
            if (newFileName.Contains('('))
            {
                int pFrom = newFileName.IndexOf(" (") + " (".Length;
                int pTo = newFileName.LastIndexOf(")");
                newFileName = newFileName.Substring(pFrom, pTo - pFrom);
            }
            else
            {
                //throw new Exception("DDS fileName format error, '(' not found: " + fileName);
                return 0;
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

        private ushort getNTP3CompressionType(DDSFileInfo DDSFileList, NTP3FileInfo NTP3FileInfo)
        {
            string compressionType = DDSFileList.CompressionType;

            if(compressionType == "No Compression")
            {
                if(Enum.TryParse(NTP3FileInfo.pixelFormat, out PixelFormat pixelFormat))
                {
                    switch (pixelFormat)
                    {
                        case PixelFormat.AbgrExt:
                            return 14;

                        case PixelFormat.R5G6B5IccSgix:
                        default:
                            return 7;
                    }
                }
                else
                {
                    throw new Exception("Cannot parse pixelFormat!");
                }
                
            }
            else
            {
                return NTP3CompressedTypes[compressionType];
            }
        }

        private List<uint> calculateMipmapsSize(DDSFileInfo DDSFile)
        {
            List<uint> mipmapsSizeList = new List<uint>();
            int numberofMipmaps = DDSFile.numberofMipmaps;
            int widthResolution = DDSFile.widthReso;
            int heightResolution = DDSFile.heightReso;

            for(int i = 0; i  < numberofMipmaps; i++)
            {
                uint mipmapSize = 0;
                if (DDSFile.CompressionType == "No Compression")
                {
                    mipmapSize = (uint)(widthResolution * heightResolution * DDSFile.pixelFormatRGBAByteSize);
                    Math.Max(1, Math.Truncate((decimal)(widthResolution /= 2)));
                    Math.Max(1, Math.Truncate((decimal)(heightResolution /= 2)));
                    mipmapsSizeList.Add(mipmapSize);
                }
                else
                {
                    int blockSize;
                    switch (DDSFile.CompressionType)
                    {
                        case "DXT1":
                            blockSize = 8;
                            break;

                        case "DXT2":
                        case "DXT3":
                        case "DXT5":
                            blockSize = 16;
                            break;

                        default:
                            throw new Exception("DDS Compression type not supported!");
                    }
                    // should I add a check on min size?
                    mipmapSize = (uint)(Math.Max(1, Math.Truncate((decimal)((widthResolution + 3) / 4))) * Math.Max(1, Math.Truncate((decimal)((heightResolution + 3) / 4))) * blockSize);
                    Math.Max(1, Math.Truncate((decimal)(widthResolution /= 2)));
                    Math.Max(1, Math.Truncate((decimal)(heightResolution /= 2)));
                    mipmapsSizeList.Add(mipmapSize);
                }
            }
            return mipmapsSizeList;
        }

        private byte[] removePropertiesTagandConvertFromBase64(byte[] input, string start, string end, Encoding stringEncoding)
        {
            byte[] startBytes = stringEncoding.GetBytes(start);
            byte[] endBytes = stringEncoding.GetBytes(end);

            byte[] tempBuff = input.Skip(startBytes.Length).ToArray();
            byte[] finalBuff = tempBuff.Take(tempBuff.Length - endBytes.Length).ToArray();

            //Convert.FromBase64String()

            return finalBuff;
        }
    }
}
