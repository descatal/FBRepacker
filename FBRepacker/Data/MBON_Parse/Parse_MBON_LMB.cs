using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.MBON_Parse
{
    class Parse_MBON_LMB : Internals
    {
        public Parse_MBON_LMB()
        {
            FileStream fs = File.OpenRead(Properties.Settings.Default.inputLMBFilePath);

            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.inputLMBFilePath);

            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Bael\Sprites Factory\Awakening Repack Combined - DFA5898F\Awakening Cut In Costume 1 Sprite - DFA5898F\001-MBON\002.LMB");
            string outputPath = Properties.Settings.Default.outputLMBFolderPath + @"\" + fileName + " - converted.LMB";
                
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Bael\Converted from MBON\Awakening.LMB";

            // MBON version:
            uint LMBmagic = readUIntBigEndian(fs);

            if (LMBmagic != 0x4C4D4200)
                throw new Exception("not .LMB header!");

            byte[] header_arr = new byte[0x3C];
            fs.Read(header_arr, 0, 0x3c);
            MemoryStream header = new MemoryStream(header_arr);
            MemoryStream header_rev = reverseEndianess(header, 4);

            uint unk_0xF001 = readUIntSmallEndian(fs);
            if (unk_0xF001 != 0xF001)
                throw new Exception("0x40 is not 0xF001!");

            int unk_0xF001_Size = (int)readUIntSmallEndian(fs);
            uint unk_temp_0xF001_Size = (uint)unk_0xF001_Size;
            uint unk_0xF001_0x8 = readUIntSmallEndian(fs);
            uint unk_0xF001_0xC = readUIntSmallEndian(fs);
            uint unk_0xF001_0x10 = readUIntSmallEndian(fs);

            unk_0xF001_Size -= 0x3;

            List<string> unk_0xF001_str = new List<string>();
            do
            {
                uint unk_0xF001_str_length = readUIntSmallEndian(fs);
                uint append_size = 0x4 - (unk_0xF001_str_length % 0x4);
                uint true_length = (append_size != 0 ? append_size : 0x4) + unk_0xF001_str_length;
                string str = readString(fs, (int)unk_0xF001_str_length);
                fs.Seek(append_size, SeekOrigin.Current);
                unk_0xF001_str.Add(str);
                unk_0xF001_Size -= (int)(true_length / 0x4 + 1);
            } while (unk_0xF001_Size > 0);

            uint unk_0xF002 = readUIntSmallEndian(fs);
            if (unk_0xF002 != 0xF002) // f002 is small endian
                throw new Exception("0xF002 not found!");

            List<ushort> unk_0xF002_shorts = new List<ushort>();

            uint unk_0xF002_Size = readUIntSmallEndian(fs);
            uint unk_0xF002_0x8 = readUIntSmallEndian(fs);
            for (int i = 0; i < (unk_0xF002_Size * 2) - 2; i++) // account for size is 4 bytes, and remove the 0x8
            {
                unk_0xF002_shorts.Add(readUShort(fs, false));
            }

            List<List<uint>> alldata = new List<List<uint>>();
            while(fs.Position < fs.Length)
            {
                uint magic = readUIntSmallEndian(fs);
                uint data_Count = 0;
                List<uint> data_list = new List<uint>();

                if (magic == 0xF005)
                {
                    data_Count = readUIntSmallEndian(fs);
                    data_list.Add(magic);
                    data_list.Add(data_Count);
                    //if (data_Count != 0x8)
                        //throw new Exception("reachh!!");
                    uint data_set_count = readUIntSmallEndian(fs);
                    data_list.Add(data_set_count);
                    for (uint i = 0; i < data_set_count; i++)
                    {
                        uint read_size = readUIntSmallEndian(fs);
                        data_list.Add(read_size);

                        uint actual_read_size = (read_size + 3 - ((read_size + 3) % 4)) / 4;
                        for(int j = 0; j < actual_read_size; j++)
                        {
                            uint readData = readUIntBigEndian(fs);
                            data_list.Add(readData);
                        }
                    }

                    /*
                    for (int i = 0; i < data_Count; i++)
                    {
                        uint readData = 0;

                        if (i == 2 || i == 4 || i == 5 || i == 6)
                        {
                            readData = readUIntBigEndian(fs);
                        }
                        else
                        {
                            readData = readUIntSmallEndian(fs);
                        }
                        data_list.Add(readData);
                    }
                    */
                }
                else if (magic == 0xF00C)
                {
                    data_Count = readUIntSmallEndian(fs);
                    data_list.Add(magic);
                    data_list.Add(data_Count);
                    //if (data_Count != 0xC)
                        //throw new Exception("reachh!!");
                    for (int i = 0; i < data_Count; i++)
                    {
                        if (i == 6)
                        {
                            readUShortasUintSmallEndian(data_list, fs);
                        }
                        else
                        {
                            uint readData = readUIntSmallEndian(fs);
                            data_list.Add(readData);
                        }
                    }
                }
                else if (magic == 0xF103)
                {
                    data_list.Add(magic);
                    
                    data_Count = readUIntSmallEndian(fs);
                    //if (data_Count != 0x9)
                        //throw new Exception("reachh!!");

                    data_list.Add(data_Count);

                    for(int i = 0; i < data_Count; i++)
                    {
                        uint readData = readUIntSmallEndian(fs);
                        data_list.Add(readData);
                    }
                }
                else if (magic == 0xF01A)
                {
                    // Don't add this
                    data_Count = readUIntSmallEndian(fs);

                    for (int i = 0; i < data_Count; i++)
                    {
                        uint readData = readUIntSmallEndian(fs);
                    }
                }
                else if(magic == 0xF022)
                {
                    data_Count = readUIntSmallEndian(fs);
                    data_list.Add(magic);
                    data_list.Add(0x4);
                    if (data_Count != 0x5)
                        throw new Exception("reachh!!");
                    for(int i = 0; i < data_Count; i++)
                    {
                        uint readData = readUIntSmallEndian(fs);
                        if (i != 3)
                            data_list.Add(readData);
                    }
                }
                else if(magic == 0xF024)
                {
                    data_Count = readUIntSmallEndian(fs);
                    data_list.Add(0xF023);
                    data_list.Add(0x12);
                    if (data_Count != 0x16)
                        throw new Exception("reachh!!");

                    uint Layer_order = 0;
                    for (int i = 0; i <= 19; i++)
                    {
                        // For some reason MBON swapped the points.
                        // In FB:
                        /*
                         *         (D) *      |      (C) * 
                         *                    |
                         *        ------------|--------------
                         *                    |
                         *         (A) *      |      (B) *
                         *         
                         *         A = {-1, -1}
                         *         B = {1, -1}
                         *         C = {1, 1}
                         *         D = {-1, 1}
                         */

                        // In MBON:
                        /*
                         *         (A) *      |      (D) * 
                         *                    |
                         *        ------------|--------------
                         *                    |
                         *         (B) *      |      (C) *
                         *         
                         *         A = {-1, 1}
                         *         B = {-1, -1}
                         *         C = {1, -1}
                         *         D = {1, 1}
                         */

                        // So we convert MBON to FB by swapping them (read by order).

                        bool addData = true;
                        if (i <= 2) // Omit the first three data, not used probably.
                        {
                            addData = false;

                            Layer_order = readUIntSmallEndian(fs);

                            uint unk_0xc = readUIntSmallEndian(fs);

                            if (unk_0xc != 0x00040041)
                                throw new Exception("here");

                            uint unk_0x10 = readUIntSmallEndian(fs);

                            if (unk_0x10 != 0x6)
                                throw new Exception("here");

                            i += 2;
                        }
                        else if(i == 3)
                        {
                            fs.Seek(0x10, SeekOrigin.Current); // Skip the first 4 4 bytes, we will read later
                        }
                        else if(i == 15)
                        {
                            fs.Seek(-0x40, SeekOrigin.Current); // Go back to read the first 4 4 bytes
                        }
                        else if(i == 19)
                        {
                            fs.Seek(0x30, SeekOrigin.Current); // go to after 

                            uint unk_0x54 = readUIntSmallEndian(fs);

                            if (unk_0x54 != 0x10000)
                                throw new Exception("here");

                            uint unk_0x58 = readUIntSmallEndian(fs);

                            if (unk_0x58 != 0x00030002)
                                throw new Exception("here");

                            uint unk_0x5C = readUIntSmallEndian(fs);

                            if (unk_0x5C != 0x00020000)
                                throw new Exception("here");

                            data_list.Add(Layer_order);
                            data_list.Add(0x00410000);
                            addData = false;
                        }

                        if (addData)
                        {
                            uint readData = readUIntSmallEndian(fs); // should be float, but use uint for now
                            data_list.Add(readData);
                        }
                    }

                    /*
                    long returnPos = fs.Position;
                    long next_0xF024_Pos = (uint)SearchFoward(fs, new byte[] { 0x24, 0xF0, 0, 0 });
                    fs.Seek(returnPos, SeekOrigin.Begin);

                    if ((int)next_0xF024_Pos == -1) // There's no 0xF024 onwards, which means that the file will go into unknown data territory until 0xF105
                    {
                        returnPos = fs.Position;
                        long next_0xF105_Pos = (uint)SearchFoward(fs, new byte[] { 0x05, 0xF1, 0, 0 });
                        fs.Seek(returnPos, SeekOrigin.Begin);
                        long goalPos = returnPos + next_0xF105_Pos;

                        if((int)next_0xF105_Pos != -1)
                        {
                            while (fs.Position < goalPos)
                            {
                                add_frame_data(data_list, fs);
                                //uint readData = readUIntSmallEndian(fs);
                                //data_list.Add(readData);
                            }
                        }
                        else
                        {
                            throw new Exception("????!");
                        }
                    }
                    */
                }
                /*
                else if (magic == 0xF105)
                {
                    
                    data_list.Add(magic);
                    long returnPos = fs.Position;
                    long next_0xF105_Pos = (uint)SearchFoward(fs, new byte[] { 0x05, 0xF1, 0, 0 });
                    fs.Seek(returnPos, SeekOrigin.Begin);
                    long goalPos = returnPos + next_0xF105_Pos;

                    if ((int)next_0xF105_Pos != -1)
                    {
                        while (fs.Position < goalPos)
                        {
                            add_frame_data(data_list, fs);
                        }
                    }
                    else
                    {
                        //throw new Exception("????!");
                    }
                    
                }
                */
                else if (magic == 0x27)
                {
                    data_list.Add(magic);

                    uint dataCount = readUIntSmallEndian(fs);

                    if (dataCount != 0x7)
                        throw new Exception("here");

                    data_list.Add(dataCount);

                    for (int i = 0; i < dataCount; i++)
                    {
                        if (i != 6)
                        {
                            uint data = readUIntSmallEndian(fs);
                            data_list.Add(data);
                        }
                        else
                        {
                            readUShortasUintSmallEndian(data_list, fs);
                        }
                    }

                    /*
                    long goalpos = 26 * 0x4 + initialPos;
                    while(fs.Position < goalpos)
                    {
                        index += 1;
                        data = readUIntSmallEndian(fs);
                        if (data == 0x2B)
                        {
                            goalpos += 0x14;
                            for(int i = 0; i < 5; i++)
                            {
                                data_list.Add(data);
                                data = readUIntSmallEndian(fs);
                            }
                            index -= 1;
                        }

                        if(index == 8 || index == 19 || index == 20)
                        {
                            readUShortasUintSmallEndian(data_list, fs);
                        }
                        index++;
                    }
                    */

                }
                else if (magic == 0x4)
                {
                    data_list.Add(magic);

                    /*
                    long goalpos = 31 * 0x4 + initialPos;
                    while (fs.Position < goalpos)
                    {
                        index += 1;
                        data = readUIntSmallEndian(fs);

                        if (index == 6 || index == 7 || index == 20 || index == 21)
                        {
                            readUShortasUintSmallEndian(data_list, fs);
                        }
                        index++;
                    }
                    */

                    uint dataCount = readUIntSmallEndian(fs);

                    if (dataCount != 0xC)
                        throw new Exception("here");

                    data_list.Add(dataCount);

                    for (int i = 0; i < dataCount; i++)
                    {
                        if (i == 4 || i == 5 || i == 6)
                        {
                            readUShortasUintSmallEndian(data_list, fs);
                        }
                        else
                        {
                            uint data = readUIntSmallEndian(fs);
                            data_list.Add(data);
                        }
                    }
                }
                else if (magic == 0x5)
                {
                    data_list.Add(magic);

                    uint dataCount = readUIntSmallEndian(fs);

                    if (dataCount != 0x2)
                        throw new Exception("here");

                    data_list.Add(dataCount);

                    for (int i = 0; i < dataCount; i++)
                    {
                        if (i == 1)
                        {
                            readUShortasUintSmallEndian(data_list, fs);
                        }
                        else
                        {
                            uint data = readUIntSmallEndian(fs);
                            data_list.Add(data);
                        }
                    }
                }
                else
                {
                    data_Count = readUIntSmallEndian(fs);
                    data_list.Add(magic);
                    data_list.Add(data_Count);
                    for (int i = 0; i < data_Count; i++)
                    {
                        uint readData = readUIntSmallEndian(fs);
                        data_list.Add(readData);
                    }
                }

                alldata.Add(data_list);
            }

            //long data_size = fs.Length - fs.Position;
            //byte[] data = new byte[data_size];
            //fs.Read(data, 0, (int)data_size);
            //fs.Close();

            //MemoryStream data_mem = new MemoryStream(data);
            MemoryStream data_rev = new MemoryStream(); //reverseEndianess(data_mem, 4);

            foreach(var a in alldata)
            {
                foreach(var b in a)
                {
                    appendUIntMemoryStream(data_rev, b, true);
                }
            }

            // Write
            MemoryStream LMB = new MemoryStream();

            appendUIntMemoryStream(LMB, 0x4C4D4200, true);

            header_rev.Seek(0, SeekOrigin.Begin);
            header_rev.CopyTo(LMB);

            // F001
            appendUIntMemoryStream(LMB, 0xF001, true);
            // Should count the size instead of using original
            // TODO for proper implementations
            appendUIntMemoryStream(LMB, (uint)unk_temp_0xF001_Size, true);
            appendUIntMemoryStream(LMB, unk_0xF001_0x8, true);
            appendUIntMemoryStream(LMB, unk_0xF001_0xC, true);
            appendUIntMemoryStream(LMB, unk_0xF001_0x10, true);
        
            for(int i = 0; i < unk_0xF001_str.Count(); i++)
            {
                string str = unk_0xF001_str[i];
                byte[] str_enc = Encoding.Default.GetBytes(str);
                uint unk_0xF001_str_length = (uint)str_enc.Length;
                uint append_size = 0x4 - (unk_0xF001_str_length % 0x4);
                uint true_length = (append_size != 0 ? append_size : 0x4);
                appendUIntMemoryStream(LMB, unk_0xF001_str_length, true);
                appendStringMemoryStream(LMB, str, Encoding.Default);
                appendZeroMemoryStream(LMB, (int)true_length);
            }

            appendUIntMemoryStream(LMB, 0xF002, true);
            appendUIntMemoryStream(LMB, unk_0xF002_Size, true);
            appendUIntMemoryStream(LMB, unk_0xF002_0x8, true);

            foreach(ushort unk_0xF002_short in unk_0xF002_shorts)
            {
                appendUShortMemoryStream(LMB, unk_0xF002_short, true);
            }

            data_rev.Seek(0, SeekOrigin.Begin);
            data_rev.CopyTo(LMB);

            FileStream ofs = File.Create(outputPath);
            LMB.Seek(0, SeekOrigin.Begin);
            LMB.CopyTo(ofs);
            ofs.Close();
        }

        private void add_frame_data(List<uint> data_list, Stream fs)
        {
            uint magic = readUIntSmallEndian(fs);
            long initialPos = fs.Position;
            uint data = 0;
            data_list.Add(magic);

            
            /*
            {
                uint dataCount = readUIntSmallEndian(fs);
                data_list.Add(dataCount);
                for (int i = 0; i < dataCount; i++)
                {
                    data = readUIntSmallEndian(fs);
                    data_list.Add(data);
                }
            }
            */
            
        }

        private void readUShortasUintSmallEndian(List<uint> data_list, Stream fs)
        {
            // Cases where the data is short, and since we design the data list as uint, this is the hacky way.
            ushort data = readUShort(fs, false);
            ushort data2 = readUShort(fs, false);

            MemoryStream concatushort = new MemoryStream();

            appendUShortMemoryStream(concatushort, data, true);
            appendUShortMemoryStream(concatushort, data2, true);

            concatushort.Seek(0, SeekOrigin.Begin);

            uint actualData = readUIntBigEndian(concatushort);
            data_list.Add(actualData);
        }
    }
}
