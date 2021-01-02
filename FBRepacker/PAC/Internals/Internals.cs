﻿using OpenTK.Graphics.OpenGL;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Documents;
using System.Windows.Media.Converters;

namespace FBRepacker.PAC
{
    public class Internals
    {
        protected static StreamWriter infoStreamWrite;
        protected static StreamReader infoStreamRead;
        protected static List<long> fileEndOffset = new List<long>();
        protected static List<int> NTP3LinkedOffset = new List<int>();
        protected static string[] infoFileString = new string[0];

        protected static int fileNumber = 1;
        protected static string currDirectory = string.Empty, rootDirectory = string.Empty;
        protected FileStream Stream;
        protected BinaryReader streamBinaryReader;

        [Flags]
        public enum additionalInfo
        {
            // add if you need to read more extra info appended for each file.
            NONE = 0x0,
            EIDX = 0x1
        }

        // The second unknown FHM info, presumably asset loading.
        public enum FHMAssetLoadEnum
        {
            IMAGE = 0x1,
            MODEL = 0x2,
            SOUND = 0x3
        }

        protected static List<string> fileHeadersList = new List<string>
        {
            "FHM ", "OMO\0", "NTP3", "LMB\0", "NDP3", "VBN ", "\0\u0002\u0001\0", "EIDX"
        };

        protected Internals()
        {

        }

        protected void changeStreamFile(FileStream stream)
        {
            Stream = stream;
            //streamBinaryReader = new BinaryReader(Stream); // For String reading. Can't use other functionalities since it is small endian only || Update: not used.
        }

        protected void resetVariables()
        {
            fileNumber = 1;
            fileEndOffset = new List<long>();
            NTP3LinkedOffset = new List<int>();
            currDirectory = string.Empty;
            rootDirectory = string.Empty;
            infoFileString = new string[0];
        }

        public void initializePACInfoFileExtract()
        {
            string path = rootDirectory + @"\PAC.info";
            infoStreamWrite = new StreamWriter(path, false, Encoding.Default);
        }

        // TODO: Remove
        protected byte[] readByteArrayinPACInfoBetweenString(StreamReader inputInfoStreamReader, string start, string end, bool onlyContains)
        {
            int startPos = 0, endPos = 0;
            bool hasStarted = false;
            
            StreamReader infoStream = new StreamReader(inputInfoStreamReader.BaseStream); // IDK why, but if I don't create an new instance it will cause the original StreamReader to change.

            TrackingTextReader TrackingInfoStream = new TrackingTextReader(infoStream);
            MemoryStream memStream = new MemoryStream();
            infoStream.BaseStream.Seek(0, SeekOrigin.Begin);
            infoStream.BaseStream.CopyTo(memStream);
            byte[] memBuffer = memStream.ToArray();

            string PACPath = rootDirectory + @"\PAC.info";
            FileStream fileStream = File.OpenRead(PACPath);
            File.ReadLines(PACPath);

            infoStream.BaseStream.Seek(0, SeekOrigin.Begin);

            var asd = new List<string>();

            while (!infoStream.EndOfStream)
            {
                asd.Add(infoStream.ReadLine());
            }

            infoStream.BaseStream.Seek(0, SeekOrigin.Begin);

            string line = string.Empty;
            do
            {
                if (infoStream.EndOfStream)
                    throw new Exception("Can't find string: " + start + " or " + end + " in PAC.info to extract ByteArray");

                int beforeReadPos = TrackingInfoStream.Position;

                line = TrackingInfoStream.ReadLine();
                //int lineByteLength = (Encoding.Default.GetBytes(line).Length + 2); // +2 for /n

                if (onlyContains)
                {
                    if (line.Contains(start))
                    {
                        startPos = beforeReadPos;
                        hasStarted = true;
                    }

                    if (line.Contains(end) && hasStarted == true)
                    {
                        endPos = TrackingInfoStream.Position;
                        break;
                    }
                }
                else
                {
                    if (line == start)
                    {
                        startPos = beforeReadPos;
                        hasStarted = true;
                    }

                    if (line == end && hasStarted == true)
                    {
                        endPos = TrackingInfoStream.Position;
                        break;
                    }
                }
            } while (line != end);

            byte[] extractedBuffer = memBuffer.Skip(startPos).Take(endPos - startPos).ToArray();
            return extractedBuffer;
        }

        public static Dictionary<int, List<string>> PACFileInfo = new Dictionary<int, List<string>> {
            // Self initialize the 1st file info, which is not called by createFHMPACInfoTag
            { 1, new List<string>() }
        };

        protected void createFHMPACInfoTag(int fileNumber, bool FHM)
        {
            List<string> fileInfo = new List<string>();

            if (PACFileInfo.ContainsKey(fileNumber))
                fileInfo = PACFileInfo[fileNumber];

            if (!FHM)
            {
                //infoStreamWrite.WriteLine("");
                //infoStreamWrite.WriteLine("//");
                //infoStreamWrite.WriteLine("--" + fileNumber.ToString() + "--");

                fileInfo.Add("\n");
                fileInfo.Add("//");
                fileInfo.Add("--" + fileNumber.ToString() + "--");
            }
            else
            {
                //infoStreamWrite.WriteLine("--FHM--");
                fileInfo.Add("--FHM--");
            }

            PACFileInfo[fileNumber] = fileInfo;
        }

        protected void createSTREAMPACInfoTag(int fileNumber, bool STREAM)
        {
            List<string> fileInfo = new List<string>();

            if (!STREAM)
            {
                //infoStreamWrite.WriteLine("");
                //infoStreamWrite.WriteLine("//");
                //infoStreamWrite.WriteLine("--" + fileNumber.ToString() + "--");

                fileInfo.Add("\n");
                fileInfo.Add("//");
                fileInfo.Add("--" + fileNumber.ToString() + "--");
            }
            else
            {
                //infoStreamWrite.WriteLine("--FHM--");
                fileInfo.Add("--STREAM--");
            }

            PACFileInfo[fileNumber] = fileInfo;
        }

        /// <summary>
        /// append PACInfo 
        /// </summary>
        /// <param name="append"> the string to be appended </param>
        protected void appendPACInfo(string append)
        {
            //infoStreamWrite.WriteLine(append);
            if (!PACFileInfo.ContainsKey(fileNumber))
                throw new Exception("file info tag: " + fileNumber + " has not been created yet!");

            List<string> fileInfo = PACFileInfo[fileNumber];
            fileInfo.Add(append);
        }

        /// <summary>
        /// overload function for original appendPACInfo to write info based on the given fileNumber.
        /// </summary>
        /// <param name="fileNumber"> the fileNumber to append </param>
        /// <param name="append"> the string to be appended </param>
        protected void appendPACInfo(int fileNumber, string append)
        {
            //infoStreamWrite.WriteLine(append);
            if (!PACFileInfo.ContainsKey(fileNumber))
                throw new Exception("file info tag: " + fileNumber + " has not been created yet!");

            List<string> fileInfo = PACFileInfo[fileNumber];
            fileInfo.Add(append);
        }

        /// <summary>
        /// Write the PACInfo using infoStreamWrite
        /// </summary>
        public void writePACInfo()
        {
            List<List<string>> allFileInfo = PACFileInfo.Values.ToList();
            foreach(List<string> fileInfos in allFileInfo)
            {
                foreach(string fileInfo in fileInfos)
                {
                    infoStreamWrite.WriteLine(fileInfo);
                }
            }

            PACFileInfo = new Dictionary<int, List<string>> {
                // Reset the PACFileInfo
                { 1, new List<string>() }
            };

            infoStreamWrite.Close();
        }

        protected string[] getFileInfoProperties(string tag)
        {
            // Read the info file, seek to the file tag, take all the lines between tags.
            var FHMTag = infoFileString.SkipWhile(line => !line.Contains(tag)).TakeWhile(line => !line.Contains("//")).ToArray();
            return FHMTag;
        }

        protected string[] getSpecificFileInfoPropertiesRegion(string[] allProperties, string from, string end)
        {
            return allProperties.SkipWhile(line => !line.Contains(from)).TakeWhile(line => !line.Contains(end)).ToArray();
        }

        protected string getSpecificFileInfoProperties(string propertiesTag, string[] allProperties)
        {
            if (allProperties.Any(s => s.Contains(propertiesTag)))
            {
                string properties = allProperties.FirstOrDefault(line => line.Contains(propertiesTag));

                if (properties == null)
                {
                    throw new Exception("No properties found with tag: " + propertiesTag);
                }
                else
                {
                    return properties.Split(new string[] { ": " }, StringSplitOptions.None)[1];
                }
            }
            else
            {
                throw new Exception("No properties found with tag: " + propertiesTag);
            }
        }

        protected void getSpecificFileInfoPropertiesByteStream(string propertiesTag, byte[] allProperties)
        {
            MemoryStream allPropertiesStream = new MemoryStream();
            allPropertiesStream.Write(allProperties, 0, allProperties.Length);

            StreamReader allPropertiesStreamReader = new StreamReader(allPropertiesStream);

            string line = allPropertiesStreamReader.ReadLine();
            //do
            //{
                line = allPropertiesStreamReader.ReadLine();

                if (line.Contains(propertiesTag))
                {
                    MemoryStream newStream = new MemoryStream();
                    allPropertiesStreamReader.BaseStream.CopyTo(newStream);

                    byte[] newByteArr = newStream.ToArray();
                    //memStream.Write(newByteArr, 0, newByteArr.Length);
                }


            //} //while (line != end);


        }

        protected int convertStringtoInt(string str)
        {
            bool conversionSuccessful = int.TryParse(str, out int result);
            if (conversionSuccessful)
            {
                return result;
            }
            else
            {
                throw new Exception("string to int conversion failed with str: " + str);
            }
        }

        protected byte[] convertHexStringtoByteArray(string input, bool reverseArray)
        {
            byte[] ByteArray = SoapHexBinary.Parse(input).Value;

            if (reverseArray)
                Array.Reverse(ByteArray);

            return ByteArray;
        }

        protected int convertHexStringtoInt(string input, bool reverseArray)
        {
            byte[] ByteArray = SoapHexBinary.Parse(input).Value;

            if (reverseArray)
                Array.Reverse(ByteArray);

            return BitConverter.ToInt32(ByteArray, 0);
        }

        protected byte[] convertStringtoByteArray(string input, bool reverseArray)
        {
            // Encode the string input. e.g. /0/0/0/0/0/0 -> 000000
            byte[] ByteArray = Encoding.UTF8.GetBytes(input);

            if (reverseArray)
                Array.Reverse(ByteArray);

            return ByteArray;
        }

        protected string convertByteArraytoString(byte[] ByteArray, bool reverseArray)
        {
            if (reverseArray)
                Array.Reverse(ByteArray);

            // SoapHexBinary automatically converts ByteArray into String.
            // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.remoting.metadata.w3cxsd2001.soaphexbinary?view=netframework-4.8
            SoapHexBinary HexString = new SoapHexBinary(ByteArray);
            return HexString.ToString();
        }

        protected int convertByteArraytoInt32(byte[] ByteArray, bool reverseArray)
        {
            if (reverseArray)
                Array.Reverse(ByteArray);

            return BitConverter.ToInt32(ByteArray, 0);
        }

        protected byte[] convertInt32toByteArray(int value, bool reverseArray)
        {
            byte[] ByteArray = BitConverter.GetBytes(value);

            if (reverseArray)
                Array.Reverse(ByteArray);

            return ByteArray;
        }

        //protected string getfileinfoproperties(string tag, string properties)
        //{
        //    // read the info file, seek to the file tag, take all the lines between tags.
        //    var fhmtag = infofilestring.skipwhile(line => !line.contains(tag)).takewhile(line => !line.contains("//"));
        //    return fhmtag.firstordefault(prop => prop.contains(properties)); // in the extracted file tag, search for which line has the correct properties, and get the value after :
        //}

        protected byte[] addPaddingArrayBuffer(byte[] buffer)
        {
            int moduloResult = (buffer.Length % 0x10);
            int paddingRequired = 0;
            if (moduloResult != 0)
                paddingRequired = 0x10 - moduloResult;
            byte[] zeroBuffer = new byte[paddingRequired];
            byte[] newBuffer = new byte[buffer.Length + paddingRequired];

            System.Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
            System.Buffer.BlockCopy(zeroBuffer, 0, newBuffer, buffer.Length, paddingRequired);

            return newBuffer;
        }

        protected MemoryStream addPaddingStream(MemoryStream memStream)
        {
            memStream.Seek(0, SeekOrigin.End);
            int moduloResult = ((int)memStream.Length % 0x10);
            int paddingRequired = 0;
            if (moduloResult != 0)
                paddingRequired = 0x10 - moduloResult;
            byte[] zeroBuffer = new byte[paddingRequired];
            memStream.Write(zeroBuffer, 0, paddingRequired);
            return memStream;
        }

        protected int addPaddingSizeCalculation(int size)
        {
            // size % 0x10 = Modulus of size and 0x10, the remainder will be the number that is extra. 
            // 0x10 - extra = Number of bytes needed to be padded to the file.
            int moduloResult = (size % 0x10);
            int paddingRequired = 0;
            if (moduloResult != 0)
                paddingRequired = 0x10 - moduloResult;
            return paddingRequired + size;
        }

        protected byte[] extractChunk(long startpos, long size)
        {
            byte[] buffer = new byte[size];
            Stream.Seek(startpos, SeekOrigin.Begin);
            Stream.Read(buffer, 0, (int)size);
            return buffer;
        }

        protected string readString(long offset, int size)
        {
            Stream.Seek(offset, 0);
            byte[] strBytes = extractChunk(Stream.Position, size);
            string str = Encoding.Default.GetString(strBytes);
            str = str.Trim('\0');
            return str;
        }

        protected int readIntBigEndian(long offset)
        {
            Stream.Seek(offset, 0);
            byte[] buffer = new byte[4];
            Stream.Read(buffer, 0x00, 0x04);
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        protected uint readUIntBigEndian(long offset)
        {
            Stream.Seek(offset, 0);
            byte[] buffer = new byte[4];
            Stream.Read(buffer, 0x00, 0x04);
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        protected uint readUIntSmallEndian(long offset)
        {
            Stream.Seek(offset, 0);
            byte[] buffer = new byte[4];
            Stream.Read(buffer, 0x00, 0x04);
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        protected int readIntSmallEndian(long offset)
        {
            Stream.Seek(offset, 0);
            byte[] buffer = new byte[4];
            Stream.Read(buffer, 0x00, 0x04);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        // readShort(long offset, bool bigEndian);
        // if bigEndian = true, return big endian short. 
        protected short readShort(long offset, bool bigEndian)
        {
            Stream.Seek(offset, 0);
            byte[] buffer = new byte[2];
            Stream.Read(buffer, 0x00, 0x02);
            short result = bigEndian ? BinaryPrimitives.ReadInt16BigEndian(buffer) : BinaryPrimitives.ReadInt16LittleEndian(buffer);
            return result;
        }

        protected ushort readUShort(long offset, bool bigEndian)
        {
            Stream.Seek(offset, 0);
            byte[] buffer = new byte[2];
            Stream.Read(buffer, 0x00, 0x02);
            ushort result = bigEndian ? BinaryPrimitives.ReadUInt16BigEndian(buffer) : BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            return result;
        }

        protected float readFloat(long offset, bool bigEndian)
        {
            Stream.Seek(offset, 0);
            byte[] buffer = new byte[4];
            Stream.Read(buffer, 0x00, 0x04);
            if (bigEndian)
                Array.Reverse(buffer);
            float result = BitConverter.ToSingle(buffer, 0);
            return result;
        }

        protected ushort readUShortMemoryStream(MemoryStream stream, long offset, bool bigEndian)
        {
            stream.Seek(offset, 0);
            byte[] buffer = new byte[2];
            stream.Read(buffer, 0x00, 0x02);
            ushort result = bigEndian ? BinaryPrimitives.ReadUInt16BigEndian(buffer) : BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            return result;
        }

        protected uint readUIntMemoryStream(MemoryStream stream, long offset, bool bigEndian)
        {
            stream.Seek(offset, 0);
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0x00, 0x04);
            uint result = bigEndian ? BinaryPrimitives.ReadUInt32BigEndian(buffer) : BinaryPrimitives.ReadUInt32LittleEndian(buffer);
            return result;
        }

        protected static byte[] reverseEndianess(byte[] buffer, int reverseThreshold)
        {
            // I don't know why using toArray() in a loop causes significant slowdown.
            List<byte[]> tempBuffer = new List<byte[]>();
            for (int i = 0; i < (buffer.Length) / reverseThreshold; i++)
            {
                byte[] revBytes = new byte[reverseThreshold];
                System.Buffer.BlockCopy(buffer, i * reverseThreshold, revBytes, 0, 4);
                Array.Reverse(revBytes);
                tempBuffer.Add(revBytes);
            }

            return tempBuffer.SelectMany(i => i).ToArray();
        }

        protected string createExtractFilePath(int fileNumber)
        {
            string filePath = currDirectory + @"\" + fileNumber.ToString("000");
            return filePath;
        }

        // TODO: use Stream instead of byte[] buffer
        protected byte[] appendUIntArrayBuffer(byte[] buffer, uint intval, bool bigEndian)
        {
            List<byte[]> tempBuffer = new List<byte[]>(); // Initialize temprary buffer for writing. Needs to be in List to append
            if(bigEndian)
                intval = BinaryPrimitives.ReverseEndianness(intval); // Convert value to big endian.
            tempBuffer.Add(buffer);
            tempBuffer.Add(BitConverter.GetBytes(intval)); // Convert int into byte[], then addedinto temporary buffer
            buffer = tempBuffer.SelectMany(i => i).ToArray(); // Convert List back into byte[]. More work compared to old "unclean" way?
            return buffer;
        }

        protected byte[] appendShortArrayBuffer(byte[] buffer, ushort shortval, bool bigEndian)
        {
            List<byte[]> tempBuffer = new List<byte[]>(); // Initialize temprary buffer for writing. Needs to be in List to append
            if (bigEndian)
                shortval = BinaryPrimitives.ReverseEndianness(shortval); // Convert value to big endian.
            tempBuffer.Add(buffer);
            tempBuffer.Add(BitConverter.GetBytes(shortval)); // Convert int into byte[], then addedinto temporary buffer
            buffer = tempBuffer.SelectMany(i => i).ToArray(); // Convert List back into byte[]. More work compared to old "unclean" way?
            return buffer;
        }

        protected byte[] appendZeroArrayBuffer(byte[] buffer, int size)
        {
            List<byte[]> tempBuffer = new List<byte[]>(); // Initialize temprary buffer for writing. Needs to be in List to append
            byte[] zeroBuffer = new byte[size];
            Array.Clear(zeroBuffer, 0, size);
            tempBuffer.Add(buffer);
            tempBuffer.Add(zeroBuffer);
            buffer = tempBuffer.SelectMany(i => i).ToArray(); // Convert List back into byte[]. More work compared to old "unclean" way?
            return buffer;
        }

        protected void appendIntMemoryStream(MemoryStream memStream, int intval, bool bigEndian)
        {
            if (bigEndian)
                intval = BinaryPrimitives.ReverseEndianness(intval);
            byte[] tempBuffer = BitConverter.GetBytes(intval);
            memStream.Write(tempBuffer, 0, tempBuffer.Length);
        }

        protected void appendUIntMemoryStream(MemoryStream memStream, uint intval, bool bigEndian)
        {
            if (bigEndian)
                intval = BinaryPrimitives.ReverseEndianness(intval);
            byte[] tempBuffer = BitConverter.GetBytes(intval);
            memStream.Write(tempBuffer, 0, tempBuffer.Length);
        }

        protected void appendUShortMemoryStream(MemoryStream memStream, ushort shortval, bool bigEndian)
        {
            if (bigEndian)
                shortval = BinaryPrimitives.ReverseEndianness(shortval);
            byte[] tempBuffer = BitConverter.GetBytes(shortval);
            memStream.Write(tempBuffer, 0, tempBuffer.Length);

        }

        protected void appendFloatMemoryStream(MemoryStream memStream, float floatval, bool bigEndian)
        {
            byte[] tempBuffer = BitConverter.GetBytes(floatval);
            if (bigEndian)
                Array.Reverse(tempBuffer);
            memStream.Write(tempBuffer, 0, tempBuffer.Length);
        }

        protected void appendZeroMemoryStream(MemoryStream memStream, int size)
        {
            byte[] zeroBuffer = new byte[size];
            Array.Clear(zeroBuffer, 0, size);
            memStream.Write(zeroBuffer, 0, zeroBuffer.Length);
        }

        /// <summary>
        /// appending String to a memory stream
        /// </summary>
        /// <param name="memStream">the memory stream to append</param>
        /// <param name="str">the string to append</param>
        /// <param name="encoding">specify the encoding used to encode the string (If unsure use Encoding.Default)</param>
        protected void appendStringMemoryStream(MemoryStream memStream, string str, Encoding encoding)
        {
            byte[] tempBuffer = encoding.GetBytes(str);
            memStream.Write(tempBuffer, 0, tempBuffer.Length);
        }

        /// <summary>
        /// appending String to a memory stream
        /// </summary>
        /// <param name="memStream">the memory stream to append</param>
        /// <param name="str">the string to append</param>
        /// <param name="encoding">specify the encoding used to encode the string (If unsure use Encoding.Default)</param>
        /// <param name="size">byte array required size, will pad with 0 if short, and trunc if larger</param>
        protected void appendStringMemoryStream(MemoryStream memStream, string str, Encoding encoding, int size)
        {
            byte[] tempBuffer = encoding.GetBytes(str);

            if(tempBuffer.Length >= size)
            {
                tempBuffer = tempBuffer.Take(size).ToArray();
            }
            else if (tempBuffer.Length < size)
            {
                byte[] newBuffer = new byte[size];
                int padSize = size - tempBuffer.Length;
                byte[] zeroBuffer = new byte[padSize];
                Array.Clear(zeroBuffer, 0, padSize);
                System.Buffer.BlockCopy(tempBuffer, 0, newBuffer, 0, tempBuffer.Length);
                System.Buffer.BlockCopy(zeroBuffer, 0, newBuffer, tempBuffer.Length, zeroBuffer.Length);
                tempBuffer = newBuffer;
            }

            memStream.Write(tempBuffer, 0, tempBuffer.Length);
        }

        protected void createFile(string fileExt, byte[] buffer, string filePath, int appendPACInfoFileNumber)
        {
            filePath += "." + fileExt;
            FileStream newFile = File.Create(filePath);
            newFile.Write(buffer, 0x00, buffer.Length);
            newFile.Close();

            FileInfo temp = new FileInfo(filePath);
            appendPACInfo(appendPACInfoFileNumber, "fileName: " + temp.Name);
        }

        protected void createFile(string fileExt, byte[] buffer, string filePath)
        {
            filePath += "." + fileExt;
            FileStream newFile = File.Create(filePath);
            newFile.Write(buffer, 0x00, buffer.Length);
            newFile.Close();

            FileInfo temp = new FileInfo(filePath);
            appendPACInfo("fileName: " + temp.Name);
        }

        protected void renameFile(string filePath, string newFileName)
        {
            string fileDirectory = Path.GetDirectoryName(filePath);
            byte[] inputAudioBuffer = readFileinbyteStream(filePath);
            writeFileinbyteStream(fileDirectory + @"\" + newFileName, inputAudioBuffer);
            File.Delete(filePath);
        }

        protected byte[] readFileinbyteStream(string filePath)
        {
            FileStream fs = File.OpenRead(filePath);
            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, (int)fs.Length);
            fs.Close();
            return buffer;
        }

        protected void writeFileinbyteStream(string filePath, byte[] buffer)
        {
            // Reason why repack don't use this: opening and closing Stream is expensive.
            FileStream fs = File.Create(filePath);
            fs.Write(buffer, 0, buffer.Count());
            fs.Close();
        }

        // For DDS Images.
        [Flags]
        protected enum dwDDSFlags
        {
            DDSD_CAPS = 0x1, // Required in every .dds file.
            DDSD_HEIGHT = 0x2, // Required in every .dds file.
            DDSD_WIDTH = 0x4, //  Required in every .dds file.
            DDSD_PITCH = 0x8, // Required when pitch is provided for an uncompressed texture.
            DDSD_PIXELFORMAT = 0x1000, // Required in every .dds file.
            DDSD_MIPMAPCOUNT = 0x20000, // Required in a mipmapped texture.	
            DDSD_LINEARSIZE = 0x80000, // Required when pitch is provided for a compressed texture.
            DDSD_DEPTH = 0x800000 // Required in a depth texture.
        }

        // https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-pixelformat
        [Flags]
        protected enum dwPixelFormatFlags
        {
            DDPF_ALPHAPIXELS = 0x1, // Texture contains alpha data; dwRGBAlphaBitMask contains valid data.
            DDPF_ALPHA = 0x2, // Used in some older DDS files for alpha channel only uncompressed data (dwRGBBitCount contains the alpha channel bitcount; dwABitMask contains valid data)
            DDPF_FOURCC = 0x4, // Texture contains compressed RGB data; dwFourCC contains valid data.
            DDPF_RGB = 0x40, // Texture contains uncompressed RGB data; dwRGBBitCount and the RGB masks (dwRBitMask, dwGBitMask, dwBBitMask) contain valid data.
            DDPF_YUV = 0x200, // Used in some older DDS files for YUV uncompressed data (dwRGBBitCount contains the YUV bit count; dwRBitMask contains the Y mask, dwGBitMask contains the U mask, dwBBitMask contains the V mask)
            DDPF_LUMINANCE = 0x20000 // Used in some older DDS files for single channel color uncompressed data (dwRGBBitCount contains the luminance channel bit count; dwRBitMask contains the channel mask). Can be combined with DDPF_ALPHAPIXELS for a two channel DDS file.
        }

        // https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-header
        [Flags]
        protected enum dwCapsFlag
        {
            DDSCAPS_COMPLEX = 0x8, // Optional; must be used on any file that contains more than one surface (a mipmap, a cubic environment map, or mipmapped volume texture).
            DDSCAPS_MIPMAP = 0x400000, // Optional; should be used for a mipmap.
            DDSCAPS_TEXTURE = 0x1000 // Required
        }

        // Masked RGBA Image data parse (for uncompressed)
        // https://gitlab.gnome.org/GNOME/gimp/-/blob/master/plug-ins/file-dds/ddsread.c
        public List<byte[]> parseMaskedRGBA(bool isAlpha, byte[] RGBADataChunk, int RGBAByteSize, uint dwRBitMask, uint dwGBitMask, uint dwBBitMask, uint dwABitMask)
        {
            List<byte[]> RGBAList = new List<byte[]>();
            // Here we parse the Image data chunk and makes it fit the mask in dds.

            // Not sure if this is the good idea, make it stream?
            int dataPos = 0;

            uint[] masks = isAlpha ? new uint[] { dwRBitMask, dwGBitMask, dwBBitMask, dwABitMask } : new uint[] { dwRBitMask, dwGBitMask, dwBBitMask };
            int[] colorShifts = color_shift(masks);
            int[] colorBits = color_bits(masks);
            uint[] colorMasks = color_mask(masks, colorShifts, colorBits);

            MemoryStream RGBAStream = new MemoryStream();
            RGBAStream.Write(RGBADataChunk, 0, RGBADataChunk.Length);
            RGBAStream.Seek(0, SeekOrigin.Begin);

            while (dataPos < RGBADataChunk.Length)
            {
                //byte[] RGBAChunk = RGBADataChunk.Skip(dataPos).Take(RGBAByteSize).ToArray();
                uint RGBAInt = 0;

                if (RGBAByteSize == 2)
                    RGBAInt = readUShortMemoryStream(RGBAStream, RGBAStream.Position, false);

                if (RGBAByteSize == 4)
                    RGBAInt = readUIntMemoryStream(RGBAStream, RGBAStream.Position, false);

                byte[] RGBA = parseRGBA(RGBAInt, colorMasks, colorShifts, colorBits);
                RGBAList.Add(RGBA);

                dataPos += RGBAByteSize;
            }

            return RGBAList;
        }

        public byte[] writeMaskedRGBA(PixelFormat pixelFormat, List<byte[]> RGBAList)
        {
            MemoryStream DDSRGBADataStream = new MemoryStream();
            foreach (byte[] RGBA in RGBAList)
            {
                int red = 0, green = 0, blue = 0, alpha = 0;
                dynamic pixelRGBA = 0;
                byte[] pixelRGBABuffer = new byte[] { };
                switch (pixelFormat)
                {
                    case PixelFormat.R5G6B5IccSgix:

                        if (RGBA.Length != 3)
                            throw new Exception("RGBA count mismatch!");

                        //https://gitlab.gnome.org/GNOME/gimp/-/blob/master/plug-ins/file-dds/color.h
                        red = mul8bit(RGBA[0], 31);
                        green = mul8bit(RGBA[1], 63);
                        blue = mul8bit(RGBA[2], 31);
                        pixelRGBA = (ushort)((red << 11) | (green << 5) | blue);
                        break;

                    case PixelFormat.AbgrExt:

                        if (RGBA.Length != 4)
                            throw new Exception("RGBA count mismatch!");

                        red = RGBA[0];
                        green = RGBA[1];
                        blue = RGBA[2];
                        alpha = RGBA[3];
                        pixelRGBA = (uint)((red << 8) | (green << 16) | (blue << 24) | (alpha << 0));
                        break;

                    default:
                        throw new Exception("Does not support " + pixelFormat + " DDS export yet!");
                }

                pixelRGBABuffer = BitConverter.GetBytes(pixelRGBA);
                DDSRGBADataStream.Write(pixelRGBABuffer, 0, pixelRGBABuffer.Length);
            }
            DDSRGBADataStream.Close();
            return DDSRGBADataStream.ToArray();
        }

        private int[] color_shift(uint[] masks)
        {
            List<int> shift = new List<int>();

            foreach (uint mask in masks)
            {
                int i = 0;

                if (mask == 0)
                    i = 0;

                while (((mask >> i) & 1) == 0)
                    i++;

                shift.Add(i);
            }

            return shift.ToArray();
        }

        private int[] color_bits(uint[] masks)
        {
            List<int> bits = new List<int>();

            foreach (uint mask_ in masks)
            {
                uint mask = mask_;
                int i = 0;

                while (mask != 0)
                {
                    if ((mask & 1) != 0) ++i;
                    mask >>= 1;
                }

                bits.Add(i);
            }

            return bits.ToArray();
        }

        private uint[] color_mask(uint[] masks, int[] colorShift, int[] colorBits)
        {
            List<uint> colorMaskList = new List<uint>();
            // I have no idea what this is, just following GIMP DDS's implementations.
            if (masks.Length != colorShift.Length || masks.Length != colorBits.Length)
                throw new Exception("mask shift and bits RGBA count not same!");

            for (int count = 0; count < masks.Length; count++)
            {
                var maskShifted = masks[count] >> colorShift[count];
                var actualBitNumber = 8 - colorBits[count];
                uint colorMask = (maskShifted) << (actualBitNumber);
                colorMaskList.Add(colorMask);
            }

            return colorMaskList.ToArray();
        }

        private byte[] parseRGBA(uint RGBAInt, uint[] colorMasks, int[] colorShifts, int[] colorBits)
        {
            List<byte> RGBAList = new List<byte>();

            if (colorMasks.Length != colorShifts.Length || colorMasks.Length != colorBits.Length)
                throw new Exception("mask shift and bits RGBA count not same!");

            for (int count = 0; count < colorMasks.Length; count++)
            {
                var shiftBit = colorShifts[count];
                var shiftedPixel = RGBAInt >> shiftBit;
                var acutalBit = (8 - colorBits[count]);
                var ratio = shiftedPixel << acutalBit & colorMasks[count];
                byte RGBA = (byte) (ratio * 255 / colorMasks[count]);
                if(RGBA != 0)
                {

                }
                RGBAList.Add(RGBA);
            }

            return RGBAList.ToArray();
        }

        // https://gitlab.gnome.org/GNOME/gimp/-/blob/master/plug-ins/file-dds/imath.h
        private int mul8bit(int a, int b)
        {
            int t = a * b + 128;

            return (t + (t >> 8)) >> 8;
        }


        // Sound parts.
        /* G722.1 Codec decoder is broken, use VGMSTREAM instead. (test.exe)
        protected byte[] convertBNSFtoPCM(string inputPath, int sampleRate, int bandwidth)
        {
            string G7221DecoderPath = Directory.GetCurrentDirectory() + @"\3rd Party\G7221\Decoder\";
            byte[] inputAudioBuffer = readFileinbyteStream(inputPath);
            byte[] BNSFbitStream = inputAudioBuffer.Skip(0x30).Take(inputAudioBuffer.Length - 0x30).ToArray();
            writeFileinbyteStream((G7221DecoderPath + "input.bnsf"), BNSFbitStream);

            // Using G7221 decoder to convert BNSF bitStream to PCM.
            using (Process G7221Decoder = new Process())
            {
                G7221Decoder.StartInfo.UseShellExecute = false;
                G7221Decoder.StartInfo.FileName = G7221DecoderPath + "decode.exe";
                G7221Decoder.StartInfo.CreateNoWindow = false;
                G7221Decoder.StartInfo.WorkingDirectory = G7221DecoderPath;
                G7221Decoder.StartInfo.RedirectStandardOutput = true;
                G7221Decoder.StartInfo.Arguments = "0 input.bnsf output.pcm " + sampleRate.ToString() + " " + bandwidth.ToString();
                G7221Decoder.Start();
                G7221Decoder.WaitForExit();
            }

            byte[] outputAudioBuffer = readFileinbyteStream(G7221DecoderPath + "output.pcm");
            return outputAudioBuffer;
        }
        */

        protected byte[] convertBNSFtoWAV(string inputPath, int sampleRate, int bandwidth)
        {
            string VGMSTREAMDecoderPath = Directory.GetCurrentDirectory() + @"\3rd Party\VGMSTREAM\";
            byte[] inputAudioBuffer = readFileinbyteStream(inputPath);
            //byte[] BNSFbitStream = inputAudioBuffer.Skip(0x30).Take(inputAudioBuffer.Length - 0x30).ToArray();
            writeFileinbyteStream((VGMSTREAMDecoderPath + "input.bnsf"), inputAudioBuffer);

            // Using G7221 decoder to convert BNSF bitStream to PCM.
            using (Process VGMSTREAMDecoder = new Process())
            {
                VGMSTREAMDecoder.StartInfo.UseShellExecute = false;
                VGMSTREAMDecoder.StartInfo.FileName = VGMSTREAMDecoderPath + "test.exe";
                VGMSTREAMDecoder.StartInfo.CreateNoWindow = true;
                VGMSTREAMDecoder.StartInfo.WorkingDirectory = VGMSTREAMDecoderPath;
                VGMSTREAMDecoder.StartInfo.RedirectStandardOutput = true;
                VGMSTREAMDecoder.StartInfo.Arguments = "-o output.wav input.bnsf";
                VGMSTREAMDecoder.Start();
                VGMSTREAMDecoder.WaitForExit();
            }

            byte[] outputAudioBuffer = readFileinbyteStream(VGMSTREAMDecoderPath + "output.wav");
            return outputAudioBuffer;
        }

        /* ATRAC3 Codecs have problem decoding some of the at3 extracted due to not supported birate. Hence, ffmpeg is used instead. Encoding is still useable.
        protected byte[] convertat3towav(string inputpath)
        {
            string atrac3codecpath = directory.getcurrentdirectory() + @"\3rd party\atrac3 codec\";
            byte[] inputaudiobuffer = readfileinbytestream(inputpath);
            writefileinbytestream((atrac3codecpath + "input.at3"), inputaudiobuffer);

            // using g7221 decoder to convert bnsf bitstream to pcm.
            using (process atrac3codec = new process())
            {
                atrac3codec.startinfo.useshellexecute = false;
                atrac3codec.startinfo.filename = atrac3codecpath + "ps3_at3tool.exe";
                atrac3codec.startinfo.workingdirectory = atrac3codecpath;
                atrac3codec.startinfo.createnowindow = true;
                atrac3codec.startinfo.redirectstandardoutput = true;
                atrac3codec.startinfo.arguments = "-d input.at3 output.wav";
                atrac3codec.start();
                atrac3codec.waitforexit();
            }

            byte[] outputaudiobuffer = readfileinbytestream(atrac3codecpath + "output.wav");
            return outputaudiobuffer;
        }
        */

        protected byte[] convertAT3toWAV(string inputPath)
        {
            string VGMSTREAMDecoderPath = Directory.GetCurrentDirectory() + @"\3rd Party\VGMSTREAM\";
            byte[] inputAudioBuffer = readFileinbyteStream(inputPath);
            writeFileinbyteStream((VGMSTREAMDecoderPath + "input.at3"), inputAudioBuffer);

            // Using G7221 decoder to convert BNSF bitStream to PCM.
            using (Process VGSTREAMDecoder = new Process())
            {
                VGSTREAMDecoder.StartInfo.UseShellExecute = false;
                VGSTREAMDecoder.StartInfo.FileName = VGMSTREAMDecoderPath + "test.exe";
                VGSTREAMDecoder.StartInfo.WorkingDirectory = VGMSTREAMDecoderPath;
                VGSTREAMDecoder.StartInfo.CreateNoWindow = true;
                VGSTREAMDecoder.StartInfo.RedirectStandardOutput = true;
                VGSTREAMDecoder.StartInfo.Arguments = "-o output.wav input.at3";
                // Alternative arg for ffmpeg decode.
                // ATRAC3Codec.StartInfo.Arguments = "-i input.at3 -f wav -bitexact -acodec pcm_s16le -y -ar 48000 -ac 1 output.wav";
                VGSTREAMDecoder.Start();
                VGSTREAMDecoder.WaitForExit();
            }

            byte[] outputAudioBuffer = readFileinbyteStream(VGMSTREAMDecoderPath + "output.wav");
            return outputAudioBuffer;
        }

        protected byte[] convertPCMtoWAV(string inputPath, int sampleRate, int channel)
        {
            string ffmpegPath = Directory.GetCurrentDirectory() + @"\3rd Party\ffmpeg\";
            byte[] inputAudioBuffer = readFileinbyteStream(inputPath);
            writeFileinbyteStream((ffmpegPath + "input.pcm"), inputAudioBuffer);

            // Convert PCM (Raw WAV w/o header) to WAV using ffmpeg.
            using (Process ffmpeg = new Process())
            {
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.FileName = ffmpegPath + "ffmpeg.exe";
                ffmpeg.StartInfo.WorkingDirectory = ffmpegPath;
                ffmpeg.StartInfo.CreateNoWindow = true;
                ffmpeg.StartInfo.RedirectStandardOutput = true;
                ffmpeg.StartInfo.Arguments = "-f s16le -y -ar " + sampleRate.ToString() + " -ac " + channel.ToString() + " -i input.pcm output.wav";
                ffmpeg.Start();
                ffmpeg.WaitForExit();
            }

            byte[] outputAudioBuffer = readFileinbyteStream(ffmpegPath + "output.wav");
            return outputAudioBuffer;
        }

        protected byte[] convertWAVtoPCM(string inputPath, int sampleRate, int channel)
        {
            string ffmpegPath = Directory.GetCurrentDirectory() + @"\3rd Party\ffmpeg\";
            byte[] inputAudioBuffer = readFileinbyteStream(inputPath);
            writeFileinbyteStream((ffmpegPath + "input.wav"), inputAudioBuffer);

            // Convert PCM (Raw WAV w/o header) to WAV using ffmpeg.
            using (Process ffmpeg = new Process())
            {
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.FileName = ffmpegPath + "ffmpeg.exe";
                ffmpeg.StartInfo.WorkingDirectory = ffmpegPath;
                ffmpeg.StartInfo.CreateNoWindow = true;
                ffmpeg.StartInfo.RedirectStandardOutput = true;
                ffmpeg.StartInfo.Arguments = "-i input.wav -f wav -bitexact -acodec pcm_s16le -y -ar " + sampleRate.ToString() + " -ac " + channel.ToString() + " output.wav";
                ffmpeg.Start();
                ffmpeg.WaitForExit();
            }

            byte[] outputAudioBuffer = readFileinbyteStream(ffmpegPath + "output.pcm");
            return outputAudioBuffer;
        }

        protected long GetPatternPositions(Stream stream, byte[] pattern)
        {
            long initPos = stream.Position;
            int patternPosition = 0; //Track of how much of the array has been matched
            long filePosition = 0;
            long bufferSize = Math.Min(stream.Length, 100_000);

            byte[] buffer = new byte[bufferSize];
            int readCount = 0;

            while ((readCount = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < readCount; i++)
                {
                    byte currentByte = buffer[i];

                    if (currentByte == pattern[0])
                        patternPosition = 0;

                    if (currentByte == pattern[patternPosition])
                    {
                        patternPosition++;
                        if (patternPosition == pattern.Length)
                        {
                            stream.Seek(initPos, SeekOrigin.Begin);
                            return filePosition + 1 - pattern.Length;
                        }
                    }
                    filePosition++;
                }
            }

            stream.Seek(initPos, SeekOrigin.Begin);
            return -1;
        }
    }
}
