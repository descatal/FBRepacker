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
    class Internals
    {
        protected static StreamWriter infoStreamWrite;
        protected static StreamReader infoStreamRead;
        protected static List<long> fileEndOffset = new List<long>();
        protected static List<int> NTP3LinkedOffset = new List<int>();
        protected static string[] infoFileString = new string[0];

        protected static int fileNumber = 1;
        protected static string currDirectory = string.Empty, rootDirectory = string.Empty;
        protected FileStream Stream;

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

        protected void initializePACInfoFileExtract()
        {
            string path = rootDirectory + @"\PAC.info";
            infoStreamWrite = new StreamWriter(path, false, Encoding.Default);
        }

        protected void initializePACInfoFileRepack()
        {
            string path = rootDirectory + @"\PAC.info";
            if (File.Exists(path))
            {
                infoStreamRead = new StreamReader(path);
                infoFileString = File.ReadAllLines(path);
            }
            else
            {
                throw new Exception("PAC.info file not found! Make sure that the file is present in the root folder of your repack folder.");
            }
        }

        protected void createFHMPACInfoTag(int fileNumber, bool FHM)
        {
            if (!FHM)
            {
                infoStreamWrite.WriteLine("");
                infoStreamWrite.WriteLine("//");
                infoStreamWrite.WriteLine("--" + fileNumber.ToString() + "--");
            }
            else
            {
                infoStreamWrite.WriteLine("--FHM--");
            }
        }

        protected void createSTREAMPACInfoTag(int fileNumber, bool STREAM)
        {
            if (!STREAM)
            {
                infoStreamWrite.WriteLine("");
                infoStreamWrite.WriteLine("//");
                infoStreamWrite.WriteLine("--" + fileNumber.ToString() + "--");
            }
            else
            {
                infoStreamWrite.WriteLine("--STREAM--");
            }
        }

        protected void appendPACInfo(string append)
        {
            infoStreamWrite.WriteLine(append);
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

        protected byte[] convertStringtoByteArray(string input, bool reverseArray)
        {
            // Encode the string input. e.g. /0/0/0/0/0/0 -> 000000
            byte[] ByteArray = Encoding.Default.GetBytes(input);

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

        protected byte[] addPadding(byte[] buffer)
        {
            int moduloResult = (buffer.Length % 0x10);
            int paddingRequired = 0;
            if (moduloResult != 0)
                paddingRequired = 0x10 - moduloResult;
            byte[] zeroBuffer = new byte[paddingRequired];
            byte[] newBuffer = new byte[buffer.Length + paddingRequired];

            Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
            Buffer.BlockCopy(zeroBuffer, 0, newBuffer, buffer.Length, paddingRequired);

            return newBuffer;
        }

        protected int addPaddingSizeCalculation(int size)
        {
            // size % 0x10 = Modulus of size and 0x10, the remainder will be the number that is extra. 
            // 0x10 - extra = Number of bytes needed to be padded to the file.
            int paddingRequired = 0x10 - (size % 0x10);
            return paddingRequired + size;
        }

        protected byte[] extractChunk(long startpos, long size)
        {
            byte[] buffer = new byte[size];
            Stream.Seek(startpos, SeekOrigin.Begin);
            Stream.Read(buffer, 0, (int)size);
            return buffer;
        }

        protected int readIntBigEndian(long offset)
        {
            Stream.Seek(offset, 0);
            byte[] buffer = new byte[4];
            Stream.Read(buffer, 0x00, 0x04);
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
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
        
        protected static byte[] reverseEndianess(byte[] buffer, int reverseThreshold)
        {
            // I don't know why using toArray() in a loop causes significant slowdown.
            List<byte[]> tempBuffer = new List<byte[]>();
            for (int i = 0; i < (buffer.Length) / reverseThreshold; i++)
            {
                byte[] revBytes = new byte[reverseThreshold];
                Buffer.BlockCopy(buffer, i * 4, revBytes, 0, 4); 
                tempBuffer.Add(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(revBytes, 0))));
            }

            return tempBuffer.SelectMany(i => i).ToArray();
        }

        protected string createExtractFilePath(int fileNumber)
        {
            string filePath = currDirectory + @"\" + fileNumber.ToString("000");
            return filePath;
        }

        // TODO: use Stream instead of byte[] buffer
        protected byte[] appendIntByteStream(byte[] buffer, uint intval, bool bigEndian)
        {
            List<byte[]> tempBuffer = new List<byte[]>(); // Initialize temprary buffer for writing. Needs to be in List to append
            if(bigEndian)
                intval = BinaryPrimitives.ReverseEndianness(intval); // Convert value to big endian.
            tempBuffer.Add(buffer);
            tempBuffer.Add(BitConverter.GetBytes(intval)); // Convert int into byte[], then addedinto temporary buffer
            buffer = tempBuffer.SelectMany(i => i).ToArray(); // Convert List back into byte[]. More work compared to old "unclean" way?
            return buffer;
        }

        protected byte[] appendShortByteStream(byte[] buffer, ushort shortval, bool bigEndian)
        {
            List<byte[]> tempBuffer = new List<byte[]>(); // Initialize temprary buffer for writing. Needs to be in List to append
            if (bigEndian)
                shortval = BinaryPrimitives.ReverseEndianness(shortval); // Convert value to big endian.
            tempBuffer.Add(buffer);
            tempBuffer.Add(BitConverter.GetBytes(shortval)); // Convert int into byte[], then addedinto temporary buffer
            buffer = tempBuffer.SelectMany(i => i).ToArray(); // Convert List back into byte[]. More work compared to old "unclean" way?
            return buffer;
        }

        protected byte[] appendZeroByteStream(byte[] buffer, int size)
        {
            List<byte[]> tempBuffer = new List<byte[]>(); // Initialize temprary buffer for writing. Needs to be in List to append
            byte[] zeroBuffer = new byte[size];
            Array.Clear(zeroBuffer, 0, size);
            tempBuffer.Add(buffer);
            tempBuffer.Add(zeroBuffer);
            buffer = tempBuffer.SelectMany(i => i).ToArray(); // Convert List back into byte[]. More work compared to old "unclean" way?
            return buffer;
        }

        protected void createFile(string fileExt, byte[] buffer, string filePath)
        {
            filePath += "." + fileExt;
            FileStream newFile = File.Create(filePath);
            newFile.Write(buffer, 0x00, buffer.Length);
            newFile.Close();
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
    }
}
