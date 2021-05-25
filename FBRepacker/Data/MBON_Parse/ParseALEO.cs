using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.MBON_Parse
{
    class ParseALEO : Internals
    {
        public enum extra_data_type
        {
            Normal,
            NUTName,
            NUTName_2,
            unk_0x20,
            unk_0x110,
            unk_0x168,
            unk_0x1F4,
        }

        public ParseALEO()
        {
            string path = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\Extreme Mk-II AXE ALEO\001-FHM\test\";//@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Infinite Justice METEOR\Extract MBON\Common EIDX - FABAA98C\001-FHM\002-FHM\";
            string outputpath = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\Extreme Mk-II AXE ALEO\001-FHM\res\";//@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Infinite Justice METEOR\Extract MBON\EIDX_Common_Test";
            List<string> allFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList();
            List<string> ALEOFiles = allFiles.Where(s =>
            {
                string aleo = Path.GetExtension(s).ToLower();
                return aleo.Equals(".aleo");
            }).ToList();

            List<uint> all_pointer_index_FB = new List<uint>() { 8, 31, 32, 33, 37, 38, 39, 60, 61, 64, 65, 68, 70, 72, 74, 81, 85, 87, 90, 94, 95, 96, 100, 101, 102, 106, 107, 108, 125 };

            for (int i = 0; i < ALEOFiles.Count; i++)
            {
                string fileName = Path.GetFileName(ALEOFiles[i]);
                FileStream ALEO = File.OpenRead(ALEOFiles[i]);

                uint fileSize = readUIntSmallEndian(ALEO);
                uint magic = readUIntSmallEndian(ALEO);
                uint unk_0x08 = readUIntCD(ALEO, false); // always 6D 
                uint unk_0x0C = readUIntCD(ALEO, false);

                uint unk_0x10 = readUIntCD(ALEO, false);
                uint unk_0x14 = readUIntCD(ALEO, false);
                uint unk_0x18 = readUIntCD(ALEO, false);
                uint unk_0x1C = readUIntCD(ALEO, false); // should be float, but nevertheless we are not changing anything so there's no need to convert

                uint unk_0x20 = readUIntCD(ALEO, false);
                uint unk_0x24 = readUIntCD(ALEO, false);
                uint unk_0x28 = readUIntCD(ALEO, false);
                uint unk_0x2C = readUIntCD(ALEO, false); // should be float

                uint unk_0x30 = readUIntCD(ALEO, false); // should be float
                uint unk_0x34 = readUIntCD(ALEO, false);
                uint unk_0x38 = readUIntCD(ALEO, false); // flags?

                // nud set
                uint setCount = readUIntCD(ALEO, false);
                uint setPointer = readUIntCD(ALEO, false);
                uint unk_0x44 = readUIntCD(ALEO, false);

                checkif0(unk_0x44, "nudset" + fileName);

                // nud name (at the end)
                uint nutCount = readUIntCD(ALEO, false);
                uint nutPointer = readUIntCD(ALEO, false);
                uint unk_0x50 = readUIntCD(ALEO, false);

                checkif0(unk_0x50, "nudname");

                // nut name (at the end)
                uint nudCount = readUIntCD(ALEO, false);
                uint nudPointer = readUIntCD(ALEO, false);
                uint unk_0x5C = readUIntCD(ALEO, false);

                checkif0(unk_0x5C, "nutname");

                ALEO.Seek(setPointer, SeekOrigin.Begin);

                List<KeyValuePair<uint, extra_data_type>> allPointers = new List<KeyValuePair<uint, extra_data_type>>();
                List<uint> allPointersCount = new List<uint>();
                MemoryStream all_set_data = new MemoryStream();

                List<uint> all_pointer_index_MBON = new List<uint>();
                for (int indexCount = 0; indexCount < all_pointer_index_FB.Count(); indexCount++)
                {
                    uint index = all_pointer_index_FB[indexCount];
                    uint mbon_index = index + (uint)indexCount;
                    all_pointer_index_MBON.Add(mbon_index);
                }

                List<List<uint>> all_converted_set_data = new List<List<uint>>();
                List<uint> set_offsets = new List<uint>(); // to be used for unk_153 determination
                // parse each nud info set
                // each is 0x26C length?
                for (int set = 0; set < setCount; set++)
                {
                    List<uint> set_data = new List<uint>();
                    MemoryStream setMS = new MemoryStream();

                    set_offsets.Add((uint)ALEO.Position);

                    // parse and read each set data
                    for (uint set_data_count = 0; set_data_count < 155; set_data_count++) // MBON 1 set is 0x26C long, or 155 4 byte entries
                    {
                        bool bigendian = false;
                        if (set_data_count >= 139 && set_data_count <= 147) // for string nud names
                            bigendian = true;

                        uint data = readUIntCD(ALEO, bigendian);

                        // We assume all Pointers to be consecutive, (i.e. the more pointer the more back the start point of the extra data will be)
                        // Also, some of the extra data will contain string, so we need to read them in Big Endian to preserve the endianess.
                        if (all_pointer_index_MBON.Contains(set_data_count))
                        {
                            ALEO.Seek(-0x8, SeekOrigin.Current);
                            uint pointer_count = readUIntCD(ALEO, bigendian);
                            allPointersCount.Add(pointer_count);
                            ALEO.Seek(0x4, SeekOrigin.Current);

                            // Determine the content of the extra data, some of them contain special pointers which needs 00 removal too.
                            // e.g. 101 = 101th 4 byte in the 0x26C MBON set data.
                            extra_data_type pointer_type = extra_data_type.Normal;
                            switch (set_data_count)
                            {
                                case 8:
                                    pointer_type = extra_data_type.unk_0x20;
                                    break;
                                case 79:
                                    pointer_type = extra_data_type.unk_0x110;
                                    break;
                                case 101:
                                    pointer_type = extra_data_type.NUTName;
                                    break;
                                case 104:
                                    pointer_type = extra_data_type.NUTName_2;
                                    break;
                                case 108:
                                    pointer_type = extra_data_type.unk_0x168;
                                    break;
                                case 153:
                                    pointer_type = extra_data_type.unk_0x1F4;
                                    break;
                                default:
                                    pointer_type = extra_data_type.Normal;
                                    break;
                            }
                            allPointers.Add(new KeyValuePair<uint, extra_data_type>(data, pointer_type)); // for pointer rewriting purposes
                        }
                            
                        set_data.Add(data);
                    }

                    // Copy a new list instead of reference to set_data
                    List<uint> converted_set_data = set_data.Select(s => s).ToList();
                    // remove the extra 0 after pointers
                    for (int indexCount = 0; indexCount < all_pointer_index_MBON.Count; indexCount++)
                    {
                        // Before we remove, we need to check if the removed 4 byte is actually zero, so we check using this.
                        uint index = all_pointer_index_MBON[indexCount];
                        uint zero = set_data[(int)index + 1];
                        checkif0(zero, "Pointer Index: " + index.ToString() + " Set: " + set.ToString() + fileName);

                        // Instead of removing using the index listed by MBON, we remove it based on the FB ones because
                        // if we removed the entry, the next element will occupy the original space, making the index "correct" in FB terms
                        converted_set_data.RemoveAt((int)all_pointer_index_FB[indexCount] + 1); // If we are referencing to set_data, we will remove the entry to check on loop
                    }

                    all_converted_set_data.Add(converted_set_data);
                }

                // read extra data (info for nut and else)
                // the only way we can know the start and end point for each extra data is by knowing the start pointer of next data set.
                // for the last pointer, theoretically the next pointer will be the start of the nud name section, or nudPointer
                MemoryStream all_extra_data = new MemoryStream();
                List<uint> all_extra_data_length = new List<uint>();
                List<uint> non_zero_pointers = allPointers.Where(s => s.Key != 0).Select(x => x.Key).ToList();
                List<KeyValuePair<uint, uint>> extra_data_pointer_relative_offset = new List<KeyValuePair<uint, uint>>();
                
                // First extra data is after the set data section
                uint extra_data_pointer = addPaddingSizeCalculation(0x60 + (setCount * 0x1F8));
                uint extra_data_MBON_pointer = (uint)ALEO.Position;
                int non_zero_count = 0;
                for (int pointerCount = 0; pointerCount < allPointers.Count(); pointerCount++)
                {
                    uint pointer = allPointers[pointerCount].Key;
                    uint base_all_extra_length = (uint)all_extra_data.Length;
                    uint pointer_count = allPointersCount[pointerCount];

                    if(pointer != 0)
                    {
                        extra_data_type pointer_type = allPointers[pointerCount].Value;
                        uint nextpointer = nutPointer;
                        
                        if (non_zero_count + 1 != non_zero_pointers.Count())
                        {
                            // get the next pointer
                            nextpointer = non_zero_pointers[non_zero_count + 1];
                            non_zero_count++;
                        }

                        int length = (int)(nextpointer - pointer);
                        if (length < 0)
                            throw new Exception("Length < 0");
                        // Check if can be divided by 4 byte, or 32 bit.
                        if (length % 0x4 != 0)
                            throw new Exception("extra info length not a factor of 4!");

                        uint uint32Count = (uint)length / 0x4;
                        uint hasNUTNameCount = (uint)(Math.Round(uint32Count / 4.0) * 4) - 4;
                        if (pointer_type == extra_data_type.NUTName)
                            length = (int)(hasNUTNameCount * 0x4);

                        ALEO.Seek(pointer, SeekOrigin.Begin);

                        MemoryStream extra_data = new MemoryStream();
                        
                        for (int extra_data_count = 0; extra_data_count < uint32Count; extra_data_count++)
                        {
                            uint extra_data_uint32 = readUIntCD(ALEO, false);
                            
                            //if (pointer_type == extra_data_type.NUTName && uint32Count % 0x4 != 0x3)
                                //throw new Exception("NUT name info length does not have 0xC!");

                            // Reverse nut name.
                            if (pointer_type == extra_data_type.NUTName || pointer_type == extra_data_type.NUTName_2)
                            {
                                if (extra_data_count < 8)
                                {
                                    ALEO.Seek(-0x4, SeekOrigin.Current);
                                    extra_data_uint32 = readUIntCD(ALEO, true);
                                }
                                else if (pointer_count == 2 && extra_data_count >= 35 && extra_data_count <= 43)
                                {
                                    ALEO.Seek(-0x4, SeekOrigin.Current);
                                    extra_data_uint32 = readUIntCD(ALEO, true);
                                }
                            }

                            if (pointer_type == extra_data_type.Normal)
                            {
                                if (extra_data_uint32 > pointer && extra_data_uint32 < ALEO.Length)
                                {
                                    uint returnAddress = (uint)ALEO.Position;
                                    ALEO.Seek(-0x8, SeekOrigin.Current);
                                    uint pointer_enum = readUIntCD(ALEO, false);
                                    if (pointer_enum != 0 && pointer_enum < 0xFF)
                                        throw new Exception("Unidentified Pointer!");
                                    ALEO.Seek(returnAddress, SeekOrigin.Begin);
                                }
                                appendUIntMemoryStream(extra_data, extra_data_uint32, true);
                            }
                            else if (pointer_type == extra_data_type.unk_0x20)
                            {
                                if (extra_data_uint32 != 0)
                                {
                                    // For some reason this "pointer" does not have 00 appended, so no reduce needed.
                                    if (extra_data_count == 3)
                                    {
                                        uint relative_offset = extra_data_uint32 - pointer;
                                        extra_data_uint32 = (uint)(extra_data_pointer + base_all_extra_length + relative_offset);
                                    }
                                }

                                // Check if there is any pointers in the pointer subsections
                                if (extra_data_count * 4 > 0x20)
                                {
                                    if (extra_data_uint32 > pointer && extra_data_uint32 < ALEO.Length)
                                    {
                                        uint returnAddress = (uint)ALEO.Position;
                                        ALEO.Seek(-0x8, SeekOrigin.Current);
                                        uint pointer_enum = readUIntCD(ALEO, false);
                                        if (pointer_enum != 0 && pointer_enum <= 0xFF)
                                            throw new Exception("Unidentified Pointer!");
                                        ALEO.Seek(returnAddress, SeekOrigin.Begin);
                                    }
                                }

                                appendUIntMemoryStream(extra_data, extra_data_uint32, true);
                            }
                            else if (pointer_type == extra_data_type.unk_0x110)
                            {
                                uint reduce_pointer = 0;
                                uint relative_offset = extra_data_uint32 - pointer;

                                if (pointer_count == 1)
                                {
                                    if(extra_data_count == 1)
                                        reduce_pointer = 0x4;

                                    if(extra_data_count == 10)
                                        reduce_pointer = 0x8;

                                    if(extra_data_count == 1 || extra_data_count == 10)
                                    {
                                        extra_data_uint32 = (uint)(extra_data_pointer + base_all_extra_length + relative_offset - reduce_pointer);
                                    }

                                    if(extra_data_uint32 > pointer && extra_data_uint32 < ALEO.Length && extra_data_count != 1 && extra_data_count != 10)
                                    {
                                        uint returnAddress = (uint)ALEO.Position;
                                        ALEO.Seek(-0x8, SeekOrigin.Current);
                                        uint pointer_enum = readUIntCD(ALEO, false);
                                        if (pointer_enum != 0 && pointer_enum <= 0xFF)
                                            throw new Exception("Unidentified Pointer unk_0x110!");
                                        ALEO.Seek(returnAddress, SeekOrigin.Begin);
                                    }

                                    if (extra_data_count != 2 && extra_data_count != 11)
                                    {
                                        appendUIntMemoryStream(extra_data, extra_data_uint32, true);
                                    }
                                }

                                // Mk-II AXE's 019.ALEO
                                if (pointer_count == 2)
                                {
                                    if (extra_data_count == 1)
                                        reduce_pointer = 0x8;

                                    if (extra_data_count == 4)
                                        reduce_pointer = 0xC;

                                    if (extra_data_count == 11)
                                        reduce_pointer = 0xC;

                                    if (extra_data_count == 28)
                                        reduce_pointer = 0x10;

                                    if (extra_data_count == 1 || extra_data_count == 4 || extra_data_count == 11 || extra_data_count == 28)
                                    {
                                        extra_data_uint32 = (uint)(extra_data_pointer + base_all_extra_length + relative_offset - reduce_pointer);
                                    }

                                    if (extra_data_uint32 > pointer && extra_data_uint32 < ALEO.Length && extra_data_count != 1 && extra_data_count != 4 && extra_data_count != 11 && extra_data_count != 28)
                                    {
                                        uint returnAddress = (uint)ALEO.Position;
                                        ALEO.Seek(-0x8, SeekOrigin.Current);
                                        uint pointer_enum = readUIntCD(ALEO, false);
                                        if (pointer_enum != 0 && pointer_enum <= 0xFF)
                                            throw new Exception("Unidentified Pointer unk_0x110!");
                                        ALEO.Seek(returnAddress, SeekOrigin.Begin);
                                    }

                                    // second pointer's 00 is always removed
                                    if (extra_data_count != 2 && extra_data_count != 5 && extra_data_count != 12 && extra_data_count != 29)
                                    {
                                        appendUIntMemoryStream(extra_data, extra_data_uint32, true);
                                    }
                                }

                                if (pointer_count > 2)
                                {
                                    throw new Exception("0x110 pointer count > 2!");
                                }
                            }
                            else if(pointer_type == extra_data_type.NUTName)
                            {
                                //0x154
                                uint reduce_pointer = 0;
                                uint relative_offset = extra_data_uint32 - pointer;

                                if(pointer_count == 1)
                                {
                                    // 0x8C length in MBON
                                    if (extra_data_uint32 != 0)
                                    {
                                        if (extra_data_count == 26 || extra_data_count == 30 || extra_data_count == 33)
                                        {
                                            extra_data_uint32 = (uint)(extra_data_pointer + base_all_extra_length + relative_offset - 0xC);
                                        }
                                    }

                                    // Check if there is any pointers in the pointer subsections
                                    if (extra_data_count * 4 > 0x8C)
                                    {
                                        if (extra_data_uint32 > pointer && extra_data_uint32 < ALEO.Length)
                                        {
                                            uint returnAddress = (uint)ALEO.Position;
                                            ALEO.Seek(-0x8, SeekOrigin.Current);
                                            uint pointer_enum = readUIntCD(ALEO, false);
                                            if (pointer_enum != 0 && pointer_enum <= 0xFF)
                                                throw new Exception("Unidentified Pointer!");
                                            ALEO.Seek(returnAddress, SeekOrigin.Begin);
                                        }
                                    }

                                    // We somehow need to remove the extra 0xC for nut Name string
                                    if (extra_data_count != 27 && extra_data_count != 31 && extra_data_count != 34)
                                    {
                                        appendUIntMemoryStream(extra_data, extra_data_uint32, true);
                                    }
                                }

                                if (pointer_count == 2)
                                {
                                    if (extra_data_uint32 != 0)
                                    {
                                        if (extra_data_count == 26 || extra_data_count == 30 || extra_data_count == 33)
                                            reduce_pointer = 0x18;

                                        if (extra_data_count == 61 || extra_data_count == 65 || extra_data_count == 68)
                                            reduce_pointer = 0x18;

                                        if (extra_data_count == 26 || extra_data_count == 30 || extra_data_count == 33 || extra_data_count == 61 || extra_data_count == 65 || extra_data_count == 68)
                                        {
                                            extra_data_uint32 = (uint)(extra_data_pointer + base_all_extra_length + relative_offset - reduce_pointer);
                                        }
                                    }

                                    if (extra_data_uint32 > pointer && extra_data_uint32 < ALEO.Length && extra_data_count != 26 && extra_data_count != 30 && extra_data_count != 33 && extra_data_count != 61 && extra_data_count != 65 && extra_data_count != 68)
                                    {
                                        uint returnAddress = (uint)ALEO.Position;
                                        ALEO.Seek(-0x8, SeekOrigin.Current);
                                        uint pointer_enum = readUIntCD(ALEO, false);
                                        if (pointer_enum != 0 && pointer_enum <= 0xFF)
                                            throw new Exception("Unidentified Pointer NUTName!");
                                        ALEO.Seek(returnAddress, SeekOrigin.Begin);
                                    }

                                    if (extra_data_count != 27 && extra_data_count != 31 && extra_data_count != 34 && extra_data_count != 62 && extra_data_count != 66 && extra_data_count != 69)
                                    {
                                        appendUIntMemoryStream(extra_data, extra_data_uint32, true);
                                    }
                                }

                                if (pointer_count >= 3)
                                {
                                    throw new Exception("NUT name have more han 3 pointer!");
                                }
                            }
                            else if (pointer_type == extra_data_type.NUTName_2)
                            {
                                //0x15C
                                uint reduce_pointer = 0;
                                uint relative_offset = extra_data_uint32 - pointer;

                                if (pointer_count == 1)
                                {
                                    // 0x8C length in MBON
                                    if (extra_data_uint32 != 0)
                                    {
                                        if (extra_data_count == 26 || extra_data_count == 30 || extra_data_count == 33)
                                        {
                                            extra_data_uint32 = (uint)(extra_data_pointer + base_all_extra_length + relative_offset - 0xC);
                                        }
                                    }

                                    // Check if there is any pointers in the pointer subsections
                                    if (extra_data_count * 4 > 0x8C)
                                    {
                                        if (extra_data_uint32 > pointer && extra_data_uint32 < ALEO.Length)
                                        {
                                            uint returnAddress = (uint)ALEO.Position;
                                            ALEO.Seek(-0x8, SeekOrigin.Current);
                                            uint pointer_enum = readUIntCD(ALEO, false);
                                            if (pointer_enum != 0 && pointer_enum <= 0xFF)
                                                throw new Exception("Unidentified Pointer!");
                                            ALEO.Seek(returnAddress, SeekOrigin.Begin);
                                        }
                                    }

                                    // We somehow need to remove the extra 0xC for nut Name string
                                    if (extra_data_count != 27 && extra_data_count != 31 && extra_data_count != 34)
                                    {
                                        appendUIntMemoryStream(extra_data, extra_data_uint32, true);
                                    }
                                }

                                if (pointer_count == 2)
                                {
                                    if (extra_data_count == 26 || extra_data_count == 30 || extra_data_count == 33)
                                        reduce_pointer = 0xC;

                                    if (extra_data_count == 61 || extra_data_count == 65 || extra_data_count == 68)
                                        reduce_pointer = 0xC;

                                    if (extra_data_count == 26 || extra_data_count == 30 || extra_data_count == 33 || extra_data_count == 61 || extra_data_count == 65 || extra_data_count == 68)
                                    {
                                        extra_data_uint32 = (uint)(extra_data_pointer + base_all_extra_length + relative_offset - reduce_pointer);
                                    }

                                    if (extra_data_uint32 > pointer && extra_data_uint32 < ALEO.Length && extra_data_count != 26 && extra_data_count != 30 && extra_data_count != 33 && extra_data_count != 61 && extra_data_count != 65 && extra_data_count != 68)
                                    {
                                        uint returnAddress = (uint)ALEO.Position;
                                        ALEO.Seek(-0x8, SeekOrigin.Current);
                                        uint pointer_enum = readUIntCD(ALEO, false);
                                        if (pointer_enum != 0 && pointer_enum <= 0xFF)
                                            throw new Exception("Unidentified Pointer NUTName!");
                                        ALEO.Seek(returnAddress, SeekOrigin.Begin);
                                    }

                                    if (extra_data_count != 27 && extra_data_count != 31 && extra_data_count != 34 && extra_data_count != 62 && extra_data_count != 66 && extra_data_count != 69)
                                    {
                                        appendUIntMemoryStream(extra_data, extra_data_uint32, true);
                                    }
                                }

                                if (pointer_count >= 3)
                                {
                                    throw new Exception("NUT name 2 have more han 3 pointer!");
                                }
                            }
                            else if (pointer_type == extra_data_type.unk_0x168)
                            {
                                uint reduce_pointer = 0;
                                uint relative_offset = extra_data_uint32 - pointer;

                                // 009.ALEO MBON Common - 6580
                                if (pointer_count == 2)
                                {
                                    if (extra_data_count == 2)
                                        reduce_pointer = 0x8;

                                    if (extra_data_count == 5)
                                        reduce_pointer = 0x8;

                                    if (extra_data_count == 2 || extra_data_count == 5)
                                    {
                                        extra_data_uint32 = (uint)(extra_data_pointer + base_all_extra_length + relative_offset - reduce_pointer);
                                    }

                                    if (extra_data_uint32 > pointer && extra_data_uint32 < ALEO.Length && extra_data_count != 2 && extra_data_count != 5)
                                    {
                                        uint returnAddress = (uint)ALEO.Position;
                                        ALEO.Seek(-0x8, SeekOrigin.Current);
                                        uint pointer_enum = readUIntCD(ALEO, false);
                                        if (pointer_enum != 0 && pointer_enum <= 0xFF)
                                            throw new Exception("Unidentified Pointer unk_0x168!");
                                        ALEO.Seek(returnAddress, SeekOrigin.Begin);
                                    }

                                    if (extra_data_count != 3 && extra_data_count != 6)
                                    {
                                        appendUIntMemoryStream(extra_data, extra_data_uint32, true);
                                    }
                                }

                                // 052.ALEO MBON Common - 2556
                                if (pointer_count > 2)
                                {
                                    if (extra_data_count == 3)
                                        reduce_pointer = 0x8;

                                    if (extra_data_count == 6)
                                        reduce_pointer = 0x8;

                                    if (extra_data_count == 3 || extra_data_count == 6)
                                    {
                                        extra_data_uint32 = (uint)(extra_data_pointer + base_all_extra_length + relative_offset - reduce_pointer);
                                    }

                                    if (extra_data_uint32 > pointer && extra_data_uint32 < ALEO.Length && extra_data_count != 3 && extra_data_count != 6)
                                    {
                                        uint returnAddress = (uint)ALEO.Position;
                                        ALEO.Seek(-0x8, SeekOrigin.Current);
                                        uint pointer_enum = readUIntCD(ALEO, false);
                                        if (pointer_enum != 0 && pointer_enum <= 0xFF)
                                            throw new Exception("Unidentified Pointer unk_0x168!");
                                        ALEO.Seek(returnAddress, SeekOrigin.Begin);
                                    }

                                    if (extra_data_count != 4 && extra_data_count != 7)
                                    {
                                        appendUIntMemoryStream(extra_data, extra_data_uint32, true);
                                    }
                                }

                                // No case for this?
                                if (pointer_count == 1)
                                {
                                    
                                }
                            }
                            else if (pointer_type == extra_data_type.unk_0x1F4)
                            {
                                // 107.ALEO MBON Common - 8620
                                if (pointer_count == 2)
                                {
                                    int set_index = set_offsets.IndexOf(extra_data_uint32);

                                    uint offset = (uint)(0x60 + (set_index * 0x1F8));

                                    if (extra_data_count == 0 || extra_data_count == 6)
                                    {
                                        extra_data_uint32 = offset;
                                    }

                                    // Check unidentified pointers (set)
                                    if (set_offsets.Contains(extra_data_uint32) && extra_data_count == 0 && extra_data_count == 6)
                                    {
                                        throw new Exception("Unidentified Pointer unk_0x1F4!");
                                    }

                                    if (extra_data_uint32 > pointer && extra_data_uint32 < ALEO.Length && extra_data_count != 0 && extra_data_count != 6)
                                    {
                                        uint returnAddress = (uint)ALEO.Position;
                                        ALEO.Seek(-0x8, SeekOrigin.Current);
                                        uint pointer_enum = readUIntCD(ALEO, false);
                                        if (pointer_enum != 0 && pointer_enum <= 0xFF)
                                            throw new Exception("Unidentified Pointer unk_0x168!");
                                        ALEO.Seek(returnAddress, SeekOrigin.Begin);
                                    }

                                    if (extra_data_count != 1 && extra_data_count != 7)
                                    {
                                        appendUIntMemoryStream(extra_data, extra_data_uint32, true);
                                    }
                                }

                                // 029.ALEO MBON Common - 4428
                                if (pointer_count > 2)
                                {
                                    int set_index = set_offsets.IndexOf(extra_data_uint32);

                                    uint offset = (uint)(0x60 + (set_index * 0x1F8));

                                    if (extra_data_count == 0 || extra_data_count == 6 || extra_data_count == 12)
                                    {
                                        extra_data_uint32 = offset;
                                    }

                                    // Check unidentified pointers (set)
                                    if (set_offsets.Contains(extra_data_uint32) && extra_data_count == 0 && extra_data_count == 6 && extra_data_count == 12)
                                    {
                                        throw new Exception("Unidentified Pointer unk_0x1F4!");
                                    }

                                    if (extra_data_uint32 > pointer && extra_data_uint32 < ALEO.Length && extra_data_count != 0 && extra_data_count != 6 && extra_data_count != 12)
                                    {
                                        uint returnAddress = (uint)ALEO.Position;
                                        ALEO.Seek(-0x8, SeekOrigin.Current);
                                        uint pointer_enum = readUIntCD(ALEO, false);
                                        if (pointer_enum != 0 && pointer_enum <= 0xFF)
                                            throw new Exception("Unidentified Pointer unk_0x168!");
                                        ALEO.Seek(returnAddress, SeekOrigin.Begin);
                                    }

                                    if (extra_data_count != 1 && extra_data_count != 7 && extra_data_count != 13)
                                    {
                                        appendUIntMemoryStream(extra_data, extra_data_uint32, true);
                                    }
                                }
                            }
                        }

                        all_extra_data_length.Add((uint)extra_data.Length);
                        extra_data.Seek(0, SeekOrigin.Begin);
                        extra_data.CopyTo(all_extra_data);
                    }
                    else
                    {
                        all_extra_data_length.Add(0);
                    }
                }

                // Rewriting all of the Pointers
                int count = 0;
                for (int set = 0; set < setCount; set++)
                {
                    List<uint> converted_set_data = all_converted_set_data[set];
                    MemoryStream setMS = new MemoryStream();

                    // rewrite pointer
                    foreach(int index in all_pointer_index_FB)
                    {
                        if(converted_set_data[index] != 0)
                            converted_set_data[index] = extra_data_pointer;
                        extra_data_pointer += all_extra_data_length[count];
                        count++;
                    }

                    // write to file
                    foreach (var data in converted_set_data)
                    {
                        appendUIntMemoryStream(setMS, data, true);
                    }

                    setMS.Seek(0, SeekOrigin.Begin);
                    setMS.CopyTo(all_set_data);
                }

                MemoryStream nut_names = new MemoryStream();

                ALEO.Seek(nutPointer, SeekOrigin.Begin);
                for(int j = 0; j < nutCount; j++)
                {
                    string nud_name = readString(ALEO, 0x20);
                    appendStringMemoryStream(nut_names, nud_name, Encoding.Default, 0x20);
                }

                MemoryStream nud_names = new MemoryStream();

                ALEO.Seek(nudPointer, SeekOrigin.Begin);
                for (int j = 0; j < nudCount; j++)
                {
                    string nut_name = readString(ALEO, 0x20);
                    appendStringMemoryStream(nud_names, nut_name, Encoding.Default, 0x20);
                }

                addPaddingStream(all_set_data);
                addPaddingStream(all_extra_data);
                addPaddingStream(nud_names);
                addPaddingStream(nut_names);

                MemoryStream header = new MemoryStream();

                // 0x60 is fixed header size, from what i've seen there's not much differences
                uint size = (uint)(0x60 + all_set_data.Length + all_extra_data.Length + nud_names.Length + nut_names.Length);
                appendUIntMemoryStream(header, size, true);
                appendUIntMemoryStream(header, 0x414C454F, true);
                appendUIntMemoryStream(header, 0x6D, true);
                appendUIntMemoryStream(header, unk_0x0C, true);

                appendUIntMemoryStream(header, unk_0x10, true);
                appendUIntMemoryStream(header, unk_0x14, true);
                appendUIntMemoryStream(header, unk_0x18, true);
                appendUIntMemoryStream(header, unk_0x1C, true);

                appendUIntMemoryStream(header, unk_0x20, true);
                appendUIntMemoryStream(header, unk_0x24, true);
                appendUIntMemoryStream(header, unk_0x28, true);
                appendUIntMemoryStream(header, unk_0x2C, true);

                appendUIntMemoryStream(header, unk_0x30, true);
                appendUIntMemoryStream(header, unk_0x34, true);
                appendUIntMemoryStream(header, unk_0x38, true);

                appendUIntMemoryStream(header, setCount, false); // small endian
                appendUIntMemoryStream(header, 0x60, true); // header always ends at 0x60.

                uint newNUTPointer = (uint)(size - nud_names.Length - nut_names.Length);
                appendUIntMemoryStream(header, nutCount, false); // small endian
                appendUIntMemoryStream(header, newNUTPointer, true); // header always ends at 0x60.

                uint newNUDPointer = (uint)(size - nud_names.Length);
                appendUIntMemoryStream(header, nudCount, false); // small endian
                appendUIntMemoryStream(header, newNUDPointer, true); // header always ends at 0x60.

                addPaddingStream(header);

                header.Seek(0, SeekOrigin.Begin);
                all_set_data.Seek(0, SeekOrigin.Begin);
                all_extra_data.Seek(0, SeekOrigin.Begin);
                nut_names.Seek(0, SeekOrigin.Begin);
                nud_names.Seek(0, SeekOrigin.Begin);

                FileStream newALEO = File.Create(outputpath + @"\" + fileName);

                header.CopyTo(newALEO);
                all_set_data.CopyTo(newALEO);
                all_extra_data.CopyTo(newALEO);
                nut_names.CopyTo(newALEO);
                nud_names.CopyTo(newALEO);

                ALEO.Close();
                newALEO.Close();
            }
        }

        private void checkif0(uint value, string extraInfo)
        {
            if (value != 0)
                throw new Exception("Warning: the assumed 4 byte next to pointer is not 0! Extra Info: " + extraInfo);
        }

        public void findStuff()
        {
            List<string> allFiles = Directory.GetFiles(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Infinite Justice METEOR\Psarc Output\test\Combined EIDX (RX-78 to Shambolo)\", "*", SearchOption.AllDirectories).ToList();
            List<string> ALEOFiles = allFiles.Where(s =>
            {
                string aleo = Path.GetExtension(s).ToLower();
                return aleo.Equals(".aleo");
            }).ToList();

            for (int i = 0; i < ALEOFiles.Count; i++)
            {
                string fileName = ALEOFiles[i];
                FileStream ALEO = File.OpenRead(ALEOFiles[i]);

                ALEO.Seek(0x3C, SeekOrigin.Begin);
                uint infoSetCount = readUIntSmallEndian(ALEO);
                uint infoStartPointer = readUIntBigEndian(ALEO);

                ALEO.Seek(infoStartPointer, SeekOrigin.Begin);

                for (int j = 0; j < infoSetCount; j++)
                {
                    ALEO.Seek(0x10C, SeekOrigin.Current);

                    uint check_0x10C = readUIntBigEndian(ALEO);

                    // 0x10C is sometimes 1
                    // if (check_0x10C != 0)
                    // throw new Exception();

                    uint check_0x110 = readUIntBigEndian(ALEO);

                    // 0x110 is pointer
                    // if (check_0x110 != 0)
                    // throw new Exception();

                    uint check_0x114 = readUIntBigEndian(ALEO);

                    if (check_0x114 != 0)
                        throw new Exception();

                    uint check_0x118 = readUIntBigEndian(ALEO);

                    if (check_0x118 != 0)
                        throw new Exception();

                    ALEO.Seek(0x4, SeekOrigin.Current);

                    uint debug = readUIntBigEndian(ALEO);

                    ALEO.Seek(0x34, SeekOrigin.Current);

                    uint check_0x158 = readUIntBigEndian(ALEO);

                    // 0x158 is sometimes 1
                    // if (check_0x158 != 0)
                    // throw new Exception();

                    uint check_0x15C = readUIntBigEndian(ALEO);

                    // 0x15C is pointer  // Combined EIDX 10 folder / 10.ALEO
                    // if (check_0x15C != 0)
                    // throw new Exception();

                    uint check_0x160 = readUIntBigEndian(ALEO);

                    // 0x160 is sometimes 1
                    // if (check_0x160 != 0)
                    //throw new Exception();

                    uint check_0x164 = readUIntBigEndian(ALEO);

                    // 0x164 is sometimes 2
                    // if (check_0x164 != 0)
                    // throw new Exception();

                    uint check_0x168 = readUIntBigEndian(ALEO);

                    // 0x168 is pointer
                    // if (check_0x168 != 0)
                    // throw new Exception();

                    ALEO.Seek(0x8C, SeekOrigin.Current);
                }
            }
        }
    }
}
