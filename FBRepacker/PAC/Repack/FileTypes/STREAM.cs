using FBRepacker.PAC.Repack.customFileInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Repack
{
    public class STREAM : Internals
    {
        int audioEntries = 0;
        int STREAM_ID = 0;
        string fileName;
        public List<STREAMFileInfo> streamFileInfoList = new List<STREAMFileInfo>();
        List<FileInfo> soundFiles = new List<FileInfo>();

        public enum supportedAudioCodec
        {
            NONE,
            PCM,
            AT3,
            BNSF,
            VAG
        }

        public STREAM() : base()
        {

        }

        public byte[] repackSTREAM(FileInfo file)
        {
            string originalFilePath = Path.GetDirectoryName(file.FullName);
            for (int i = 0; i < audioEntries; i++)
            {
                string SoundFilePath = originalFilePath + @"\" + streamFileInfoList[i].file_Name;

                if (File.Exists(SoundFilePath))
                {
                    FileInfo SoundFileInfo = new FileInfo(SoundFilePath);
                    soundFiles.Add(SoundFileInfo);
                }
                else
                {
                    throw new Exception("Cannot find Sound file with name: " + SoundFilePath);
                }
            }

            if (soundFiles.Count != streamFileInfoList.Count)
                throw new Exception("Number of sound files mismatch with the number of subsongs in info!");

            MemoryStream STREAM = repackAudioFiles();
            return STREAM.ToArray();
        }

        public MemoryStream repackAudioFiles()
        {
            MemoryStream STREAM = new MemoryStream();
            MemoryStream STREAMHeader = new MemoryStream();
            MemoryStream STREAMData = new MemoryStream();

            MemoryStream STREAMGeneralInfo = new MemoryStream();
            MemoryStream STREAMAudioInfo = new MemoryStream();

            List<uint> STREAMInfoPointers = new List<uint>();
            // Calculate the first pointer.
            // Stream general info header is 0x20 in length, and each info file occupies 4 byte pointer, alongside padding.

            uint first_Pointer = (uint)addPaddingSizeCalculation(0x20 + (soundFiles.Count * 0x4));
            STREAMInfoPointers.Add(first_Pointer);

            for (int i = 0; i < soundFiles.Count; i++)
            {
                STREAMFileInfo fileInfo = streamFileInfoList[i];
                // check the info's codec, we target the repack conversion to this.
                uint codec = fileInfo.codec;
                supportedAudioCodec targetCodec = determineInfoCodec(codec);

                FileInfo file = soundFiles[i];
                FileStream fs = file.OpenRead();
                MemoryStream ms = new MemoryStream(); 
                fs.CopyTo(ms);

                ms.Seek(0, SeekOrigin.Begin);
                fileName = Path.GetFileName(file.FullName);

                supportedAudioCodec currentCodec = supportedAudioCodec.NONE;

                MemoryStream baseFormatStream = new MemoryStream();
                // Converting the input file to pcm if the file is not the same as the targetCodec
                uint magic = readUIntBigEndian(ms);
                switch (magic)
                {
                    case 0x52494646: // RIFF (pcm, at3)
                        // Need to call this to set the correct currentCodec (differentiate between pcm and at3)
                        currentCodec = determineRIFFCodec(ms);
                        convertToBaseFormat(file, currentCodec, targetCodec).CopyTo(baseFormatStream);
                        break;
                    case 0x424E5346: // BNSF (is14)
                        currentCodec = supportedAudioCodec.BNSF;
                        convertToBaseFormat(file, currentCodec, targetCodec).CopyTo(baseFormatStream);
                        break;
                    case 0x56414770: // VAGp (vag)
                        currentCodec = supportedAudioCodec.VAG;
                        convertToBaseFormat(file, currentCodec, targetCodec).CopyTo(baseFormatStream);
                        break;
                    default:
                        break;
                }

                // format ms to be appended on STREAM header.
                MemoryStream sub_header_MS;
                // the actual stream audio data to be appended.
                MemoryStream stream_data_MS;

                baseFormatStream.Seek(0, SeekOrigin.Begin);
                uint baseFmtMagic = readUIntBigEndian(baseFormatStream);

                switch (targetCodec)
                {
                    case supportedAudioCodec.AT3:
                        if (baseFmtMagic == 0x52494646 && determineRIFFCodec(baseFormatStream) == supportedAudioCodec.PCM)
                            baseFormatStream = new MemoryStream(convertWAVtoAT3(file.FullName));
                        parseAT3(baseFormatStream, out sub_header_MS, out stream_data_MS);
                        break;
                    case supportedAudioCodec.BNSF:
                        if (baseFmtMagic == 0x52494646)
                        {
                            // the only way to know sample size (duration) of the audio file is to calculate through the unencoded RIFF file.
                            uint sample_size = determinePCMSampleSize(baseFormatStream);
                            baseFormatStream = new MemoryStream(convertPCMtoBNSF(file.FullName, 48000, 14000, sample_size));
                        }
                        parseBNSF(baseFormatStream, out sub_header_MS, out stream_data_MS);    
                        break;
                    case supportedAudioCodec.VAG:
                        if (baseFmtMagic == 0x52494646)
                            baseFormatStream = new MemoryStream(convertPCMtoVAG(file.FullName));
                        parseVAG(baseFormatStream, out sub_header_MS, out stream_data_MS);
                        break;
                    default:
                        throw new Exception("Codec not yet supported for file: " + fileName);
                }

                // Start writing STREAM Header
                MemoryStream audio_Info = new MemoryStream();

                // write codec magic
                switch (targetCodec)
                {
                    case supportedAudioCodec.AT3:
                        appendUIntMemoryStream(audio_Info, 0x61743300, true);
                        break;
                    case supportedAudioCodec.BNSF:
                        appendUIntMemoryStream(audio_Info, 0x69733134, true);
                        break;
                    case supportedAudioCodec.VAG:
                        appendUIntMemoryStream(audio_Info, 0x76616700, true);
                        break;
                    default:
                        throw new Exception("Codec not yet supported for file: " + fileName);
                }

                // general infos
                appendIntMemoryStream(audio_Info, STREAM_ID, true); // STREAM_ID
                appendUIntMemoryStream(audio_Info, (uint)i, true); // file index
                appendUIntMemoryStream(audio_Info, fileInfo.codec, true); // codec enum
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x10, always 0

                // size and offsets
                appendUIntMemoryStream(audio_Info, (uint)stream_data_MS.Length, true); // size of stream data
                appendUIntMemoryStream(audio_Info, (uint)STREAMData.Length, true); // relative offset of the stream data from the top
                appendUIntMemoryStream(audio_Info, (uint)sub_header_MS.Length, true); // subheader size

                // Loop stuff
                appendUIntMemoryStream(audio_Info, fileInfo.loop_start, true);
                appendUIntMemoryStream(audio_Info, fileInfo.loop_length, true);
                appendUIntMemoryStream(audio_Info, fileInfo.loop_flag, true);
                appendUIntMemoryStream(audio_Info, 0, true); // always 0

                // unknown section
                appendUIntMemoryStream(audio_Info, 0xFFFFFFFF, true); // unk_0x30, always 0xFFFFFFFF
                appendFloatMemoryStream(audio_Info, fileInfo.loop_float, true); // loop_float, if there's no loop this is populated.
                appendFloatMemoryStream(audio_Info, fileInfo.loop_float_2, true); // -99 float (0xC2C60000) without loop
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x3C, always 0

                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x40, always 0
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x44, always 0
                appendFloatMemoryStream(audio_Info, 1, true); // unk_0x48, always 1 float (0x3F800000)
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x4C, always 0

                appendFloatMemoryStream(audio_Info, fileInfo.var_0x50, true); // at3 / is14 = 1(Float), vag / wav = 0
                appendUIntMemoryStream(audio_Info, fileInfo.var_0x54, true); // at3/is14 = 0xA, vag = 0x4, wav = 0
                appendFloatMemoryStream(audio_Info, 1, true); // unk_0x58, always 1 float (0x3F800000)
                appendFloatMemoryStream(audio_Info, 1, true); // unk_0x5C, always 1 float (0x3F800000)

                appendUIntMemoryStream(audio_Info, fileInfo.var_0x60, true); // different for each header, not sure what.
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x64, always 0
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x68, always 0
                appendFloatMemoryStream(audio_Info, fileInfo.var_0x6C, true); // vag = 0, others = -100

                appendUIntMemoryStream(audio_Info, fileInfo.var_0x70, true); // different for each header, not sure what.
                appendUIntMemoryStream(audio_Info, 0x64, true); // unk_0x74, always 0x64
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x78, always 0
                appendUIntMemoryStream(audio_Info, 1, true); // unk_0x7C, always 1

                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x80, always 0
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x84, always 0
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x88, always 0
                appendFloatMemoryStream(audio_Info, 1, true); // unk_0x8C, always 1

                appendUIntMemoryStream(audio_Info, 0x14, true); // unk_0x90, always 0x14
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x94, always 0
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0x98, always 0
                appendUIntMemoryStream(audio_Info, fileInfo.var_0x9C, true); 

                appendUIntMemoryStream(audio_Info, 0, true); // unk_0xA0, always 0
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0xA4, always 0
                appendFloatMemoryStream(audio_Info, 1, true); // unk_0xA8, always 1
                appendUIntMemoryStream(audio_Info, fileInfo.var_0xAC, true); // vag = 1, others = 0

                appendUIntMemoryStream(audio_Info, 0, true); // unk_0xB0, always 0
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0xB4, always 0
                appendUIntMemoryStream(audio_Info, 0, true); // unk_0xB8, always 0

                sub_header_MS.Seek(0, SeekOrigin.Begin);
                sub_header_MS.CopyTo(audio_Info);
                addPaddingStream(audio_Info);

                uint nextPointer = STREAMInfoPointers.Last() + (uint)audio_Info.Length;
                STREAMInfoPointers.Add(nextPointer);

                MemoryStream stream_data_MS_appended = new MemoryStream();
                stream_data_MS.Seek(0, SeekOrigin.Begin);
                stream_data_MS.CopyTo(stream_data_MS_appended);
                addPaddingStream(stream_data_MS_appended);

                audio_Info.Seek(0, SeekOrigin.Begin);
                stream_data_MS_appended.Seek(0, SeekOrigin.Begin);
                audio_Info.CopyTo(STREAMAudioInfo);
                stream_data_MS_appended.CopyTo(STREAMData);
            }

            addPaddingStream(STREAMData);

            // Formula to know how many zero to append at the end of STREAM Header.
            // STREAM Header is stored in 2kb size sections. Hence, the size of the STREAM header must be divisible by 2048. 
            // Hence, we get the last pointer to the audio Info, use modulo to find the remains, subtract the pointer with it to get the lower 2048 factor
            // After that we add the number with 2048
            uint lastPointer = STREAMInfoPointers.Last();
            uint STREAMHeaderSize = (lastPointer - (lastPointer % 2048)) + 2048;
            uint appendSize = STREAMHeaderSize - lastPointer;

            appendUIntMemoryStream(STREAMGeneralInfo, 0x00020100, true); // STREAM Version, also functions as magic
            appendUIntMemoryStream(STREAMGeneralInfo, 0, true); // Should be 0 for all cases
            appendIntMemoryStream(STREAMGeneralInfo, STREAM_ID, true);
            appendUIntMemoryStream(STREAMGeneralInfo, (uint)soundFiles.Count, true);
            appendUIntMemoryStream(STREAMGeneralInfo, STREAMHeaderSize, true);
            appendUIntMemoryStream(STREAMGeneralInfo, (uint)STREAMData.Length, true);
            appendUIntMemoryStream(STREAMGeneralInfo, 0x20, true); // The size of general Info, should be 0x20 for all cases.
            appendUIntMemoryStream(STREAMGeneralInfo, STREAMInfoPointers.First(), true); // The starting point of the audio Info, or the first pointer

            for(int k = 0; k < STREAMInfoPointers.Count; k++)
            {
                uint pointer = STREAMInfoPointers[k];
                if (k != STREAMInfoPointers.Count - 1)
                    appendUIntMemoryStream(STREAMGeneralInfo, pointer, true);
            }

            addPaddingStream(STREAMGeneralInfo);

            STREAMGeneralInfo.Seek(0, SeekOrigin.Begin);
            STREAMAudioInfo.Seek(0, SeekOrigin.Begin);
            STREAMGeneralInfo.CopyTo(STREAMHeader);
            STREAMAudioInfo.CopyTo(STREAMHeader);
            appendZeroMemoryStream(STREAMHeader, (int)appendSize);

            STREAMHeader.Seek(0, SeekOrigin.Begin);
            STREAMData.Seek(0, SeekOrigin.Begin);
            STREAMHeader.CopyTo(STREAM);
            STREAMData.CopyTo(STREAM);
            return STREAM;
        }

        public void parseAT3(MemoryStream ms, out MemoryStream fmtMS, out MemoryStream stream_data_MS)
        {
            stream_data_MS = new MemoryStream();

            ms.Seek(0, SeekOrigin.Begin);

            uint magic = readUIntBigEndian(ms);

            if (magic != 0x52494646)
                throw new Exception("parseAT3 not getting RIFF fileStream for file: " + fileName);

            uint file_size = readUIntSmallEndian(ms);
            uint WAVE = readUIntBigEndian(ms);

            if (WAVE != 0x57415645)
                throw new Exception("RIFF WAVE mismatch for file: " + fileName);

            uint fmt = readUIntBigEndian(ms);

            if (fmt != 0x666D7420)
                throw new Exception("fmt mismatch for file: " + fileName);

            uint fmtSize = readUIntSmallEndian(ms);
            byte[] fmtChunk = extractChunk(ms, ms.Position, fmtSize);

            fmtMS = new MemoryStream(fmtChunk);

            // These two are the same regardless of format typel, but probably unused
            ushort fmt_Channel = readUShort(fmtMS, false);
            uint sample_rate = readUIntSmallEndian(fmtMS);

            // find the riff fact section by searching for "fact" keyword
            long factPos = (uint)Search(ms, new byte[] { 0x66, 0x61, 0x63, 0x74 });

            if (factPos != -1)
            {
                MemoryStream tempMS = new MemoryStream();
                fmtMS.Seek(0, SeekOrigin.Begin);
                fmtMS.CopyTo(tempMS);

                ms.Seek(factPos, SeekOrigin.Begin);
                uint fact = readUIntBigEndian(ms); // fact
                uint factSize = readUIntSmallEndian(ms);
                byte[] factChunk = extractChunk(ms, ms.Position, factSize);
                MemoryStream factMS = new MemoryStream(factChunk);
                factMS.Seek(0, SeekOrigin.Begin);
                factMS.CopyTo(tempMS);

                fmtMS = tempMS;
            }

            supportedAudioCodec codec = determineRIFFfmtType(fmtMS);

            if (codec != supportedAudioCodec.AT3)
                throw new Exception("?? Is this possible? File: " + fileName);

            // Since the game uses the whole at3 (including the RIFF header), just outputting the stream will suffice (is this necessary)?
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(stream_data_MS);
        }

        private void parseBNSF(MemoryStream ms, out MemoryStream sub_header_MS, out MemoryStream stream_data_MS)
        {
            stream_data_MS = new MemoryStream();

            ms.Seek(0, SeekOrigin.Begin);

            //https://github.com/vgmstream/vgmstream/blob/master/src/meta/bnsf.c
            uint magic = readUIntBigEndian(ms);

            if (magic != 0x424E5346)
                throw new Exception("parseBNSF not getting BNSF header from audio file: " + fileName);

            uint file_size = readUIntBigEndian(ms);
            uint IS14 = readUIntBigEndian(ms);

            if (IS14 != 0x49533134)
                throw new Exception("BNSF IS14 mismatch for file: " + fileName);

            // sfmt section
            uint sfmt = readUIntBigEndian(ms);

            if (sfmt != 0x73666D74)
                throw new Exception("sfmt mismatch for file: " + fileName);

            uint sfmtSize = readUIntSmallEndian(ms);
            uint sfmtChannel = readUIntBigEndian(ms);
            uint sfmtSampleRate = readUIntBigEndian(ms);
            uint sfmtSampleSize = readUIntBigEndian(ms);
            uint sfmtLoop = readUIntBigEndian(ms);
            ushort sfmtBlockSize = readUShort(ms, true);
            ushort sfmtBlockSamples = readUShort(ms, true);

            // sdat section
            uint sdat = readUIntBigEndian(ms);

            if (sdat != 0x73646174)
                throw new Exception("sdat mismatch for file: " + fileName);

            uint sdat_size = readUIntBigEndian(ms);

            ms.Seek(0, SeekOrigin.Begin);
            byte[] headerChunk = extractChunk(ms, ms.Position, 0x30);
            sub_header_MS = new MemoryStream(headerChunk);

            byte[] streamDataChunk = extractChunk(ms, ms.Position, sdat_size);
            stream_data_MS = new MemoryStream(streamDataChunk);
        }

        private void parseVAG(MemoryStream ms, out MemoryStream sub_header_MS, out MemoryStream stream_data_MS)
        {
            stream_data_MS = new MemoryStream();

            ms.Seek(0, SeekOrigin.Begin);

            //https://github.com/vgmstream/vgmstream/blob/master/src/meta/vag.c
            uint magic = readUIntBigEndian(ms);

            if (magic != 0x56414770)
                throw new Exception("parseVAG not getting VAGp header from audio file: " + fileName);

            uint version = readUIntBigEndian(ms); // version, doesn't matter what's the value.
            uint unk_0x8 = readUIntBigEndian(ms);
            uint stream_data_size = readUIntBigEndian(ms);
            uint sample_rate = readUIntBigEndian(ms);

            // from here onward to 0x30 will be reserved stuff, so not reading them is ok.

            // the subheader will only contain the sample rate, thus writing it like this is sufficient
            // sample rate is in big endian too.
            byte[] sample_rate_byte = BitConverter.GetBytes(sample_rate).Reverse().ToArray();
            sub_header_MS = new MemoryStream(sample_rate_byte);

            byte[] streamDataChunk = extractChunk(ms, 0x30, stream_data_size);
            stream_data_MS = new MemoryStream(streamDataChunk);
        }

        private Stream convertToBaseFormat(FileInfo file, supportedAudioCodec currentCodec, supportedAudioCodec targetCodec)
        {
            MemoryStream outputMem = new MemoryStream();
            // Checks if the targetCodec is already the same as the file, or it is already in PCM format.
            if (currentCodec != targetCodec && currentCodec != supportedAudioCodec.PCM)
            {
                // If it is not, convert the file to PCM (universal accepted sound format)
                switch (currentCodec)
                {
                    case supportedAudioCodec.AT3:
                        outputMem = new MemoryStream(convertAT3toWAV(file.FullName));
                        return outputMem;

                    case supportedAudioCodec.BNSF:
                        outputMem = new MemoryStream(convertBNSFtoPCM(file.FullName, 48000, 14000));
                        return outputMem;

                    case supportedAudioCodec.VAG:
                        outputMem = new MemoryStream(convertVAGtoWAV(file.FullName));
                        return outputMem;

                    default:
                        throw new Exception("Invalid sound file codec for file: " + fileName);
                }
            }
            else
            {
                return file.OpenRead();
            }
        }

        private supportedAudioCodec determineRIFFCodec(MemoryStream ms)
        {
            parseRIFF(ms, out MemoryStream fmtMS, out MemoryStream streamMS, out supportedAudioCodec codec);
            return codec;
        }

        private uint determinePCMSampleSize(MemoryStream ms)
        {
            parseRIFF(ms, out MemoryStream fmtMS, out MemoryStream streamMS, out supportedAudioCodec codec);

            if (codec != supportedAudioCodec.PCM)
                throw new Exception("codec is not PCM!");

            // to calculate the sample size, 
            // https://forum.lazarus.freepascal.org/index.php?topic=24547.0#:~:text=to%20calculate%20the%20length%20of,sample%2C%20that%20gives%20you%202.097.
            // duration = stream_data_size / (samplerate * #of channels * (bitspersample/8))
            // samplesize = duration * samplerate
            // samplesize = stream_data_size / (#of channels * (bitspersample/8))
            // we need to determine stream data size, #of channels and bitspersample. 

            fmtMS.Seek(0, SeekOrigin.Begin);
            uint stream_data_size = (uint)streamMS.Length;

            ushort fmt_type = readUShort(fmtMS, false); // not used.
            // These two are the same regardless of format type
            ushort fmt_Channel_Number = readUShort(fmtMS, false);
            uint fmt_sample_rate = readUIntSmallEndian(fmtMS);
            // (samplerate * #of channels * (bitspersample/8))
            uint fmt_sample_byte = readUIntSmallEndian(fmtMS);
            // too lazy to explain, but this short is skipped.
            ushort fmt_skip = readUShort(fmtMS, false);
            ushort fmt_bitspersample = readUShort(fmtMS, false);

            uint sample_size = (uint)(stream_data_size / (fmt_Channel_Number * (fmt_bitspersample / 8)));
            return sample_size;
        }

        private void parseRIFF(MemoryStream ms, out MemoryStream fmtMS, out MemoryStream streamMS, out supportedAudioCodec codec)
        {
            // https://github.com/vgmstream/vgmstream/blob/ce033e53b353ff4e8afbc60b2cbd75c0709dc255/src/meta/riff.c

            ms.Seek(0, SeekOrigin.Begin);
            uint magic = readUIntBigEndian(ms);

            if (magic != 0x52494646)
                throw new Exception("parseRIFF not getting RIFF fileStream for file: " + fileName);

            uint file_size = readUIntSmallEndian(ms);
            uint WAVE = readUIntBigEndian(ms);

            if (WAVE != 0x57415645)
                throw new Exception("RIFF WAVE mismatch for file: " + fileName);

            uint fmt = readUIntBigEndian(ms);

            if (fmt != 0x666D7420)
                throw new Exception("fmt mismatch for file: " + fileName);

            uint fmtSize = readUIntSmallEndian(ms);
            byte[] fmtChunk = extractChunk(ms, ms.Position, fmtSize);

            fmtMS = new MemoryStream(fmtChunk);
            codec = determineRIFFfmtType(fmtMS);

            // find the riff data section by searching for "data" keyword
            long dataPos = (uint)Search(ms, new byte[] { 0x64, 0x61, 0x74, 0x61 });

            if (dataPos == -1)
                throw new Exception("Cannot find RIFF data section for file: " + fileName);

            ms.Seek(dataPos, SeekOrigin.Begin);
            uint data = readUIntBigEndian(ms); // data keyword
            uint dataSize = readUIntSmallEndian(ms);

            // get the stream data
            streamMS = new MemoryStream(extractChunk(ms, ms.Position, dataSize));
        }

        private supportedAudioCodec determineInfoCodec(uint codec)
        {
            switch (codec)
            {
                // wav
                case 0x01: // 0x77617600:
                    throw new Exception("Codec for WAV is unsupported for now. File: " + fileName);

                // vag
                case 0x02: // 0x76616700:
                    return supportedAudioCodec.VAG;

                // at3 (wav)
                case 0x03: // 0x61743300:
                    return supportedAudioCodec.AT3;

                // is14 / BNSF
                case 0x07: // 0x69733134:
                    return supportedAudioCodec.BNSF;

                default:
                    throw new Exception("Unregcongized codec: " + codec + " for file: " + fileName);
            }
        }

        private supportedAudioCodec determineRIFFfmtType(MemoryStream ms)
        {
            ms.Seek(0, SeekOrigin.Begin);
            ushort fmt_Type = readUShort(ms, false);

            switch (fmt_Type)
            {
                // PCM (normal WAV)
                case 0x1:
                    return supportedAudioCodec.PCM;
                // atrac codec
                case 0xFFFE:
                    // Check GUID
                    ms.Seek(0x18, SeekOrigin.Begin);
                    uint GUID1 = readUIntSmallEndian(ms);
                    uint GUID2 = readUIntSmallEndian(ms);
                    uint GUID3 = readUIntSmallEndian(ms);
                    uint GUID4 = readUIntSmallEndian(ms);

                    if(GUID1 == 0xE923AABF && GUID2 == 0x4471CB58 && GUID3 == 0xFAFF19A1 && GUID4 == 0x62CEE401)
                    {
                        return supportedAudioCodec.AT3;
                    }
                    else
                    {
                        throw new Exception("Non AT3 RIFF not supported for file: " + fileName);
                    }
                default:
                    throw new Exception("RIFF type not supported for file: " + fileName + " Please use valid PCM or AT3 format!");
            }
        }

        public void parseSTREAMMetadata(string[] STREAMMetadata)
        {
            STREAM_ID = convertStringtoInt(getSpecificFileInfoProperties("STREAM ID: ", STREAMMetadata));
            audioEntries = convertStringtoInt(getSpecificFileInfoProperties("Number of audio files: ", STREAMMetadata));
            
            for(int i = 0; i < audioEntries; i++)
            {
                STREAMFileInfo streamFileInfo = new STREAMFileInfo();
                string from = "#Sound: " + (i + 1).ToString();
                string end = "#Sound: " + (i + 2).ToString();
                // Getting individual Info for each files
                string[] STREAMSoundInfo = getSpecificFileInfoPropertiesRegion(STREAMMetadata, from, end);

                uint codec = convertStringtoUInt(getSpecificFileInfoProperties("Codec: ", STREAMSoundInfo));
                uint loop_start = convertStringtoUInt(getSpecificFileInfoProperties("Loop Start: ", STREAMSoundInfo));
                uint loop_length = convertStringtoUInt(getSpecificFileInfoProperties("Loop Length: ", STREAMSoundInfo));
                uint loop_flag = convertStringtoUInt(getSpecificFileInfoProperties("Loop Flag: ", STREAMSoundInfo));
                float loop_float = convertStringtoFloat(getSpecificFileInfoProperties("Loop Float: ", STREAMSoundInfo));
                float loop_float_2 = convertStringtoFloat(getSpecificFileInfoProperties("Loop Float 2: ", STREAMSoundInfo));
                uint var_0x50 = convertStringtoUInt(getSpecificFileInfoProperties("var_0x50: ", STREAMSoundInfo));
                uint var_0x54 = convertStringtoUInt(getSpecificFileInfoProperties("var_0x54: ", STREAMSoundInfo));
                uint var_0x60 = convertStringtoUInt(getSpecificFileInfoProperties("var_0x60: ", STREAMSoundInfo));
                float var_0x6C = convertStringtoFloat(getSpecificFileInfoProperties("var_0x6C: ", STREAMSoundInfo));
                uint var_0x70 = convertStringtoUInt(getSpecificFileInfoProperties("var_0x70: ", STREAMSoundInfo));
                uint var_0x9C = convertStringtoUInt(getSpecificFileInfoProperties("var_0x9C: ", STREAMSoundInfo));
                uint var_0xAC = convertStringtoUInt(getSpecificFileInfoProperties("var_0xAC: ", STREAMSoundInfo));
                string file_Name = getSpecificFileInfoProperties("fileName: ", STREAMSoundInfo);

                streamFileInfo.codec = codec;
                streamFileInfo.loop_start = loop_start;
                streamFileInfo.loop_length = loop_length;
                streamFileInfo.loop_flag = loop_flag;
                streamFileInfo.loop_float = loop_float;
                streamFileInfo.loop_float_2 = loop_float_2;
                streamFileInfo.var_0x50 = var_0x50;
                streamFileInfo.var_0x54 = var_0x54;
                streamFileInfo.var_0x60 = var_0x60;
                streamFileInfo.var_0x6C = var_0x6C;
                streamFileInfo.var_0x70 = var_0x70;
                streamFileInfo.var_0x9C = var_0x9C;
                streamFileInfo.var_0xAC = var_0xAC;
                streamFileInfo.file_Name = file_Name;

                streamFileInfoList.Add(streamFileInfo);
            }

        }
    }
}
