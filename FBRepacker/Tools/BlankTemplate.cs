using FBRepacker.PAC;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Tools
{
    internal class BlankTemplate : Internals
    {
        public BlankTemplate()
        {
            resizeLMB((float)0.6667);
        }

        public void resizeLMB(float multiplier)
        {
            FileStream fs = File.OpenRead(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Bael\Files to Repack\Awakening Sprite\001-FHM\002-FHM\009-FHM\010.LMB");

            MemoryStream ms = new MemoryStream();
            fs.CopyTo(ms);
            fs.Seek(0, SeekOrigin.Begin);

            List<int> all_F003 = SearchAllOccurences(ms, new byte[] { 0, 0, 0xF0, 0x03 });
            List<int> all_F023 = SearchAllOccurences(ms, new byte[] { 0, 0, 0xF0, 0x23 });
            List<int> all_F103 = SearchAllOccurences(ms, new byte[] { 0, 0, 0xF1, 0x03 });

            List<F003> total_f003 = new List<F003>();
            for (int j = 0; j < all_F003.Count; j++)
            {
                fs.Seek(all_F003[j], SeekOrigin.Begin);

                uint Prop_ID = readUIntBigEndian(fs);
                uint Prop_Size = readUIntBigEndian(fs);

                F003 f003 = new F003();
                List<List<float>> f003_List = new List<List<float>>();

                if (Prop_ID == 0xF003)
                {
                    uint set_count = readUIntBigEndian(fs);
                    for (int set = 0; set < set_count; set++)
                    {
                        List<float> data = new List<float>();
                        for (int i = 0; i < 6; i++)
                        {
                            float value = readFloat(fs, true);
                            if(i != 0 && i != 1 && i != 2 && i != 3)
                                value *= multiplier;
                            data.Add(value);
                        }
                        f003_List.Add(data);
                    }
                    f003.f003 = f003_List;
                }
                total_f003.Add(f003);
            }

            List<F103> total_f103 = new List<F103>();
            for (int j = 0; j < all_F103.Count; j++)
            {
                fs.Seek(all_F103[j], SeekOrigin.Begin);

                F103 f103 = new F103();
                List<List<float>> f103_List = new List<List<float>>();

                uint Prop_ID = readUIntBigEndian(fs);
                uint Prop_Size = readUIntBigEndian(fs);

                if (Prop_ID == 0xF103)
                {
                    uint set_count = readUIntBigEndian(fs);
                    for (int set = 0; set < set_count; set++)
                    {
                        List<float> data = new List<float>();
                        for (int i = 0; i < 2; i++)
                        {
                            float value = readFloat(fs, true);
                            value *= multiplier;
                            data.Add(value);
                        }
                        f103_List.Add(data);
                    }
                    f103.f103 = f103_List;
                }

                total_f103.Add(f103);
            }


            List<F023> total_f023 = new List<F023>();
            for (int j = 0; j < all_F023.Count; j++)
            {
                fs.Seek(all_F023[j], SeekOrigin.Begin);

                F023 f023 = new F023();
                List<List<float>> f023_List = new List<List<float>>();

                uint Prop_ID = readUIntBigEndian(fs);
                uint Prop_Size = readUIntBigEndian(fs);

                if (Prop_ID == 0xF023)
                {
                    uint set_count = 4; // should be fixed 4 sets
                    for (int set = 0; set < set_count; set++)
                    {
                        List<float> data = new List<float>();
                        for (int i = 0; i < 4; i++)
                        {
                            float value = readFloat(fs, true);
                            if(i == 0 || i == 1)
                                value *= multiplier;
                            data.Add(value);
                        }
                        f023_List.Add(data);
                    }
                    f023.f023 = f023_List;
                    f023.layer_index = readUIntBigEndian(fs);
                    f023.unk_0x44 = readUIntBigEndian(fs);
                }

                total_f023.Add(f023);
            }

            FileStream ofs = File.Create(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Bael\Files to Repack\Awakening Sprite\001-FHM\002-FHM\003-FHM\aa.bin");
            fs.Seek(0, SeekOrigin.Begin);
            fs.CopyTo(ofs);
            fs.Close();

            for (int j = 0; j < all_F003.Count; j++)
            {
                ofs.Seek(all_F003[j] + 0xc, SeekOrigin.Begin);

                F003 f003 = total_f003[j];
                List<List<float>> f003_List = f003.f003;

                uint set_count = (uint)f003_List.Count;
                for (int set = 0; set < set_count; set++)
                {
                    List<float> data = f003_List[set];
                    for (int i = 0; i < data.Count; i++)
                    {
                        float value = data[i];
                        byte[] value_float = BitConverter.GetBytes(value).Reverse().ToArray();
                        ofs.Write(value_float, 0, 4);
                    }
                }
            }

            for (int j = 0; j < all_F103.Count; j++)
            {
                ofs.Seek(all_F103[j] + 0xc, SeekOrigin.Begin);

                F103 f103 = total_f103[j];
                List<List<float>> f103_List = f103.f103;

                uint set_count = (uint)f103_List.Count;
                for (int set = 0; set < set_count; set++)
                {
                    List<float> data = f103_List[set];
                    for (int i = 0; i < data.Count; i++)
                    {
                        float value = data[i];
                        byte[] value_float = BitConverter.GetBytes(value).Reverse().ToArray();
                        ofs.Write(value_float, 0, 4);
                    }
                }
            }

            for (int j = 0; j < all_F023.Count; j++)
            {
                ofs.Seek(all_F023[j] + 0x8, SeekOrigin.Begin);

                F023 f023 = total_f023[j];
                List<List<float>> f023_List = f023.f023;

                uint set_count = (uint)f023_List.Count;
                for (int set = 0; set < set_count; set++)
                {
                    List<float> data = f023_List[set];
                    for (int i = 0; i < data.Count; i++)
                    {
                        float value = data[i];
                        byte[] value_float = BitConverter.GetBytes(value).Reverse().ToArray();
                        ofs.Write(value_float, 0, 4);
                    }
                }

                byte[] layer_index = BitConverter.GetBytes(f023.layer_index).Reverse().ToArray();
                ofs.Write(layer_index, 0, 4);

                byte[] unk_0x44 = BitConverter.GetBytes(f023.unk_0x44).Reverse().ToArray();
                ofs.Write(unk_0x44, 0, 4);
            }

            ofs.Close();
        }
    }

    class F003
    {
        public List<List<float>> f003 { get; set; }
        public F003()
        {
            f003 = new List<List<float>>();
        }
    }

    class F103
    {
        public List<List<float>> f103 { get; set; }
        public F103()
        {
            f103 = new List<List<float>>();
        }
    }

    class F023
    {
        public List<List<float>> f023 { get; set; }
        public uint layer_index { get; set; }
        public uint unk_0x44 { get; set; }
        public F023()
        {
            f023 = new List<List<float>>();
        }
    }
}
