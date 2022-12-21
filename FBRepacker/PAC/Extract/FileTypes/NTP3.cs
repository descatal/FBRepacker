using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FBRepacker.PAC.Extract.FileTypes
{
    class NTP3 : Internals
    {
        // TODO make all these global var gone.
        short numberofDDS = 0, widthResolution = 0, heightResolution = 0;
        // PixelFormats (for uncompressed)
        // https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-pixelformat
        // dwSize = Constant 0x20
        uint dwSize = 0x20, dwFlags = 0, dwFourCC = 0;
        // Look at Xentax's x_dds.cpp for all kinds of RGBAMasks. FB only uses two different types (for now).
        uint dwRGBBitCount = 0, dwRBitMask = 0, dwGBitMask = 0, dwBBitMask = 0, dwABitMask = 0, dwCaps = 0, dwCaps2 = 0, dwCaps3 = 0, dwCaps4 = 0, dwReserved2 = 0;
        int ddsDataChunkSize = 0, NTP3HeaderChunkSize = 0, FHMOffset = 0, DDSFileNumber = 1, numberofMipmaps = 0, RGBAByteSize = 0;
        string compressionType = string.Empty, DDSFileName = string.Empty;
        bool isCompressed = false, isAlpha = false;
        byte[] remainderNTP3Chunk = new byte[0]; // 0 for initialization purpose only.
        byte[] eXtChunk = new byte[0];
        byte[] GIDXChunk = new byte[0];
        byte[] DDSHeaderChunk = new byte[0];
        byte[] maskedDataChunk = new byte[0];
        PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgba; // Default pixel format.
        PixelFormat pixelFormat = PixelFormat.Rgba; // pixelInternalFormat = GPU's pixel format, pixelFormat = format in files.
        List<int> mipmapsSizeList = new List<int>();
        List<int[]> RGBA = new List<int[]>();

        // Expand NUT support if possible. (See Smash Forge's code for reference)

        public NTP3(FileStream PAC, int FHMOffset) : base()
        {
            changeStreamFile(PAC);
            this.FHMOffset = FHMOffset;
        }

        public void extract()
        {
            if (checkLinked())
            {
                parseNTP3();
            }
            else
            {
                // see comments in checkLinked for explaination
                // create a new empty duplicate file for repacking indexing purpose
                createFile("NTP3", new byte[0] { }, createExtractFilePath(fileNumber) + "-L");
            }
                
        }

        private bool checkLinked()
        {
            /* What this does is to check if the current FHMOffset that extract() is called has already been extracted. 
             * For FHM, there will be a number of linked (duplicate) offset that points to the same NTP3 header. 
             * The duplicate offset is actually pointing to a group of NTP3, but it is impossible to tell.
             * Hence, what we do in this class is to check if the NTP3 is 'multiple' type, and extract them all at once. 
             * Hence, if the next file is pointing to the same NTP3, we will extract the same dds images again, and this is wrong. 
             * TLDR - to let the system skip the NTP3 extraction since it is already extracted under duplicate.
             */
            if (NTP3LinkedOffset.Contains(FHMOffset))
            {
                return false;
            }
            else
            {
                NTP3LinkedOffset.Add(FHMOffset);
                return true;
            }
        }

        private void parseNTP3()
        {
            readNumberofFiles();

            while(DDSFileNumber <= numberofDDS)
            {
                readMetadata();
                maskedDataChunk = extractChunk(Stream.Position, ddsDataChunkSize);
                writeDDSHeader();

                // We only need to parse Masked RGBA data if it is uncompressed, else we just copy directly.
                if (!isCompressed)
                {
                    List<byte[]> RGBAList = parseMaskedRGBA(isAlpha, maskedDataChunk, RGBAByteSize, dwRBitMask, dwGBitMask, dwBBitMask, dwABitMask);
                    maskedDataChunk = writeMaskedRGBA(pixelFormat, RGBAList);
                }

                List<byte[]> buffer = new List<byte[]>();
                buffer.Add(DDSHeaderChunk);
                buffer.Add(maskedDataChunk);
                byte[] DDSBuffer = buffer.SelectMany(b => b).ToArray();

                createFile("dds", DDSBuffer, createDDSExtractFilePath(fileNumber));

                DDSFileNumber++;
            }

            DDSFileNumber = 1;
        }

        private void readNumberofFiles()
        {
            Stream.Seek(0x02, SeekOrigin.Current);
            numberofDDS = readShort(Stream.Position, true);
            appendPACInfo("Number of Files: " + numberofDDS.ToString());
        }

        private void readMetadata()
        {
            // TODO: add compabilities to NUD files from smash forge. 
            int seekRange = DDSFileNumber > 1 ? 0x08 : 0x10; // Multiple NTP3 file has none of the 0x10 NTP3 header chunk, so we only have to skip half of it (0x08).
            Stream.Seek(seekRange, SeekOrigin.Current);
            ddsDataChunkSize = readIntBigEndian(Stream.Position);
            NTP3HeaderChunkSize = readShort(Stream.Position, true);
            Stream.Seek(0x02, SeekOrigin.Current);
            numberofMipmaps = readShort(Stream.Position, true); // In Smash Forge it says this is byte.

            // TODO: Use microsoft's fourCC method instead of fixed few cases. 
            // https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds-pguide
            // https://docs.microsoft.com/en-us/previous-versions/windows/desktop/bb153349(v=vs.85)
            compressionType = parseCompressionTypeandSetPixelFormat(readShort(Stream.Position, true)); // This is also byte in smash forge.
            // In Smash Forge there's a whole host of other types

            widthResolution = readShort(Stream.Position, true);
            heightResolution = readShort(Stream.Position, true);

            Stream.Seek(0x04, SeekOrigin.Current);
            int Caps2 = readIntBigEndian(Stream.Position);

            if (Caps2 != 0)
                throw new Exception("Does not support cube map yet!");

            Stream.Seek(0x10, SeekOrigin.Current);
            mipmapsSizeList = new List<int>();

            if(numberofMipmaps != 1)
            {
                for (int i = 0; i < numberofMipmaps; i++)
                {
                    int mipmapSize = readIntBigEndian(Stream.Position);
                    mipmapsSizeList.Add(mipmapSize);
                }
            }

            int endofMipmapSize = addPaddingSizeCalculation((int)Stream.Position);
            Stream.Seek(endofMipmapSize, SeekOrigin.Begin);
            eXtChunk = extractChunk(Stream.Position, 0x10); // eXt chunk, always the same.

            GIDXChunk = extractChunk(Stream.Position, 0x10); // GIDX Chunk have fixed 0x10 size, and is technically outside NTP3 header Chunk. (This is technically counted as DDS file data).
            DDSFileName = readIntBigEndian(Stream.Position - 0x08).ToString("X4"); // Read the DDS File Name in the GIDX Chunk.
            Stream.Seek(0x04, SeekOrigin.Current);

            appendPACInfo("#DDS: " + DDSFileNumber);
            appendPACInfo("Name: " + DDSFileName);
            appendPACInfo("DDS Data Chunk Size: " + ddsDataChunkSize);
            appendPACInfo("NTP3 Header Chunk Size: " + NTP3HeaderChunkSize);
            appendPACInfo("numberofMipmaps: " + numberofMipmaps);
            appendPACInfo("Width Resolution: " + widthResolution.ToString());
            appendPACInfo("Height Resolution: " + heightResolution.ToString());
            appendPACInfo("Compression Type: " + compressionType);
            if (!isCompressed)
                appendPACInfo("pixelFormat: " + pixelFormat.ToString());

            for(int i = 0; i < mipmapsSizeList.Count; i++)
            {
                //appendPACInfo("mipmapSize" + i.ToString() + ": " + mipmapsSizeList[i].ToString());
            }
                
            appendPACInfo("eXtChunk: " + Convert.ToBase64String(eXtChunk));
            appendPACInfo("GIDXChunk: " + Convert.ToBase64String(GIDXChunk));
        }

        // Outdated. TODO: Remove.
        /*
        private void parseRemainderNTP3Chunks()
        {
            // NTP3 header chunk (useful) metadata is always 0x28 in size, and subtracting the metadata size with whole NTP3 header chunk size will get the remainder size. 
            // Also since NTP3HeaderChunkSize is read from NTP3 metadata, -0x28 is correct since it dosen't care if multiple NTP3 header dosen't have the 0x10 header chunk.
            long NTP3RemainderSize = NTP3HeaderChunkSize - 0x28; 
            remainderNTP3Chunk = extractChunk(Stream.Position, NTP3RemainderSize); // Extracting the remainder NTP3 Chunks to be used for repacking. The chunk meaning is not known yet, so copy & paste is the only option.
            GIDXChunk = extractChunk(Stream.Position, 0x10); // GIDX Chunk have fixed 0x10 size, and is technically outside NTP3 header Chunk. (This is technically counted as DDS file data).
            DDSFileName = readIntBigEndian(Stream.Position - 0x08).ToString("X4"); // Read the DDS File Name in the GIDX Chunk.
            Stream.Seek(0x04, SeekOrigin.Current);
            appendPACInfo("#DDS: " + DDSFileNumber);
            appendPACInfo("Name: " + DDSFileName);
            appendPACInfo("DDS Data Chunk Size: " + ddsDataChunkSize);
            appendPACInfo("NTP3 Header Chunk Size: " + NTP3HeaderChunkSize);
            appendPACInfo("beforeCompressionShort: " + numberofMipmaps);
            appendPACInfo("Width Resolution: " + widthResolution.ToString());
            appendPACInfo("Height Resolution: " + heightResolution.ToString());
            appendPACInfo("Compression Type: " + compressionType);
            appendPACInfo("remainderNTP3Chunk: " + Convert.ToBase64String(remainderNTP3Chunk));
            appendPACInfo("GIDXChunk: " + Convert.ToBase64String(GIDXChunk));
        }
        */

        private void writeDDSHeader()
        {
            if (DDSHeaderChunk.Length != 0)
                DDSHeaderChunk = new byte[0];

            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, 0x44445320, true); // Fixed DDS Header
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, 0x0000007C, false); // Fixed 7C 

            // TODO: Set flag based on Microsoft's DDS header specifications
            int DDSFlags = (int)(dwDDSFlags.DDSD_CAPS | dwDDSFlags.DDSD_HEIGHT | dwDDSFlags.DDSD_WIDTH | dwDDSFlags.DDSD_PIXELFORMAT);

            if (!isCompressed)
                DDSFlags |= (int)dwDDSFlags.DDSD_PITCH;

            if (numberofMipmaps > 1)
                DDSFlags |= (int)dwDDSFlags.DDSD_MIPMAPCOUNT;

            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, (uint)DDSFlags, false);

            // Set width and height resolution info.
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, (uint)heightResolution, false); // width and height resolution is little endian int32.
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, (uint)widthResolution, false);

            // Set ddsSize info
            /* Old way. We now know it is pitchorLinearSize. 
             * Uncompressed still use the same formula until I can decipher the uncompressed bitSize.
            uint ddsSize = isCompressed ? (uint)ddsDataChunkSize : (uint)widthResolution * 4; // For uncompressed the size is always widthResolution * 4, no idea why.
            DDSHeaderChunk = appendIntArrayBuffer(DDSHeaderChunk, ddsSize, false);
            */

            uint pitchorLinearSize = calculatePitchorLinearSize();
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, pitchorLinearSize, false);

            // Set 0x4 sized null
            DDSHeaderChunk = appendZeroArrayBuffer(DDSHeaderChunk, 0x4);

            // mipmapCount
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, (uint)numberofMipmaps, false);

            // reserved 11 dword, fill it with 0s.
            DDSHeaderChunk = appendZeroArrayBuffer(DDSHeaderChunk, 0x4 * 11);

            // Pixel format.
            // Size of pixel format: always 0x20
            // https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-pixelformat
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, dwSize, false);

            // Write pixel formats for uncompressed.
            writePixelFormatandCapsHeader();
        }

        private void writePixelFormatandCapsHeader()
        {
            dwCaps = (uint) (dwCapsFlag.DDSCAPS_TEXTURE);
            if (numberofMipmaps > 0)
                dwCaps = dwCaps | (uint)(dwCapsFlag.DDSCAPS_MIPMAP | dwCapsFlag.DDSCAPS_COMPLEX); // Setting flags for mipmaps

            if (isCompressed)
            {
                // FOURCC contains the compression type. Compressed files needs the compressed FOURCC type, and this flag indicates that the data is compressed, and dwFourCC's data is valid.
                dwFlags = (uint) dwPixelFormatFlags.DDPF_FOURCC;
                dwFourCC = (uint)DDSCompressionTypeStringBytes.FirstOrDefault(s => s.Key.Contains(compressionType)).Value;

                // Since DDPF_RGB Flag is not set, these dwRGBMasks dosen't matter. Fill it with 0.
            }
            else
            {
                // Explaination on mask: 
                // In truth Uncompressed is not divided between byteReversed and nonbyteReversed.
                // The bitmask is responsible for how the data is packed. 
                // We need to provide the correct flags to let the system knows what kind of masks are there.
                switch (pixelFormat)
                {
                    case PixelFormat.R5G6B5IccSgix:
                        // There's only RGB in RGB 565, thus making transparency dwABitMask not relevant. We can also use isAlpha to check for this.
                        dwFlags = (uint) dwPixelFormatFlags.DDPF_RGB;
                        // There's 5 + 6 + 5 = 16 bits for one pixel, aka short.
                        dwRGBBitCount = 0x10;
                        // The masks value are based on Microsoft's guideline. (Note that all of these are little endian)
                        // https://docs.microsoft.com/en-us/windows/win32/directshow/working-with-16-bit-rgb
                        dwRBitMask = 0xF800;
                        dwGBitMask = 0x7E0;
                        dwBBitMask = 0x1F;
                        dwABitMask = 0x00; // Not relevant
                        break;

                    case PixelFormat.AbgrExt:
                        // Abgr = Rgba in reverse, and has alpha components.
                        dwFlags = (uint)(dwPixelFormatFlags.DDPF_RGB | dwPixelFormatFlags.DDPF_ALPHAPIXELS);
                        // Each RGBA takes 8 bits (1 byte), thus 8*4 = 32 bits.
                        dwRGBBitCount = 0x20;
                        // The masks value are based on the normal RGBA masks, but reversed in endianness. (all are little endian)
                        dwRBitMask = 0xFF00;
                        dwGBitMask = 0xFF0000;
                        dwBBitMask = 0xFF000000;
                        dwABitMask = 0xFF;
                        break;
                    
                    default:
                        throw new Exception("Does not support " + pixelFormat.ToString() + " format yet!");
                }
            }

            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, dwFlags, false);
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, dwFourCC, true);
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, dwRGBBitCount, false);
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, dwRBitMask, false);
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, dwGBitMask, false);
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, dwBBitMask, false);
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, dwABitMask, false);
            DDSHeaderChunk = appendUIntArrayBuffer(DDSHeaderChunk, dwCaps, false);
            // TODO: Placeholder for caps2, 3, 4 and reserved 2 (all 0 for now).
            DDSHeaderChunk = appendZeroArrayBuffer(DDSHeaderChunk, 0x10);
        }

        private string createDDSExtractFilePath(int fileNumber)
        {
            string filePath = currDirectory + @"\" + fileNumber.ToString("000");
            if (numberofDDS > 1)
            {
                filePath += ("-" + DDSFileNumber.ToString("000") + " (" + DDSFileName + ")");
            }
            return filePath;
        }

        // Utilities
        public static Dictionary<string, int> DDSCompressionTypeStringBytes = new Dictionary<string, int>
            {
                { "DXT3", 0x44585433 },
                { "DXT5", 0x44585435 },
                { "DXT1", 0x44585431 },
                { "No Compression", 0x00000000 }
            };

        // Copied directly from Smash Forge
        private string parseCompressionTypeandSetPixelFormat(int typet)
        {
            string DDSCompressionTypeName = "nil";

            switch (typet)
            {
                // Compressed. No Pixel Formats.
                case 0x0:
                    pixelInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
                    DDSCompressionTypeName = "DXT1";
                    isCompressed = true;
                    break;
                case 0x1:
                    pixelInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
                    DDSCompressionTypeName = "DXT3";
                    isCompressed = true;
                    break;
                case 0x2:
                    pixelInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
                    DDSCompressionTypeName = "DXT5";
                    isCompressed = true;
                    break;

                // Uncompressed. Pixel Formats based on NTP3 Type.
                case 0x7: // Not found in Smash Forge, this is FB specific I guess.
                    // R5G6B5 = Red 5 Bit, G 6 Bit, B 5 Bit and so on.
                    pixelInternalFormat = PixelInternalFormat.R5G6B5IccSgix;
                    pixelFormat = PixelFormat.R5G6B5IccSgix;
                    RGBAByteSize = 2; // 5 + 6 + 5 = 16bit = short.
                    DDSCompressionTypeName = "No Compression";
                    isCompressed = false;
                    isAlpha = false;
                    break;

                case 14:
                case 17:
                    pixelInternalFormat = PixelInternalFormat.Rgba;
                    pixelFormat = PixelFormat.AbgrExt;
                    RGBAByteSize = 4; // RGBA / AGBR = 8 bit per color, 8*4 = 32bit = 4 byte.
                    DDSCompressionTypeName = "No Compression";
                    isCompressed = false;
                    isAlpha = true;
                    break;
                
                // For more cases see Smash Forge's implementations.
                default:
                    throw new NotImplementedException($"Unknown nut texture format 0x{typet:X}");
            }

            return DDSCompressionTypeName;
        }

        // https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-pixelformat
        private uint calculatePitchorLinearSize()
        {
            /* New way: 
            * https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds-pguide
            * For block-compressed formats, compute the pitch as:
              max( 1, ((width+3)/4) ) * block-size // Although it says this, from what I seen from GIMP the formula includes height too. Possible wrong stuff?
              The block-size is 8 bytes for DXT1, BC1, and BC4 formats, and 16 bytes for other block-compressed formats.
            * For R8G8_B8G8, G8R8_G8B8, legacy UYVY-packed, and legacy YUY2-packed formats, compute the pitch as: (Very old format, not implemented for now)
              ((width+1) >> 1) * 4
            * For other formats, compute the pitch as: (a.k.a uncompressed)
              (width * bits-per-pixel + 7 ) / 8
              You divide by 8 for byte alignment.
            */
            uint pitchorLinearSize = 0;

            if (isCompressed)
            {
                int blockSize = compressionType == "DXT1" ? 8 : 16;
                pitchorLinearSize = (uint)(Math.Max(1, Math.Truncate((decimal)((widthResolution + 3) / 4))) * Math.Max(1, Math.Truncate((decimal)((heightResolution + 3) / 4))) * blockSize);
            }
            else
            {
                int bitsperPixel = 8; // default for all NUT types. See Smash Forge's GetFormatSize(uint fourCc) under DDS.cs for detail. (RGBA is alwyas 0x08).
                // this is wrong, since it is not used just return 0 for now.
                //pitchorLinearSize = (uint)(Math.Max(1, Math.Truncate((decimal)((widthResolution * bitsperPixel + 7) / 8))));
                pitchorLinearSize = 0;
            }

            return pitchorLinearSize;
        }
    }
}

