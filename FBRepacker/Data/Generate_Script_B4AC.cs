using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data
{
    class Generate_Script_B4AC : Internals
    {
        public Generate_Script_B4AC()
        {
            List<List<uint>> dataSets = readB4AC(out Dictionary<uint, Dictionary<uint, List<uint>>> extra_data_set);
            writeB4AC(dataSets);
            writeExtraB4AC(extra_data_set);
        }

        private List<List<uint>> readB4AC(out Dictionary<uint, Dictionary<uint, List<uint>>> extra_data_set)
        {
            FileStream B4ACfs = File.OpenRead(Properties.Settings.Default.B4ACFilePath);
            changeStreamFile(B4ACfs);

            List<List<uint>> dataSets = new List<List<uint>>();

            uint Magic = readUIntBigEndian();
            ushort version = readUShort(true);
            ushort unk = readUShort(true);
            uint unitHash = readUIntBigEndian();
            Stream.Seek(0xc, SeekOrigin.Current);
            uint dataSize = readUIntBigEndian();
            uint unkHash = readUIntBigEndian();
            uint dataSetCount = readUIntBigEndian();
            uint dataTypeCount = readUIntBigEndian();

            Stream.Seek(0x8, SeekOrigin.Current);

            for(int i = 0; i < dataSetCount; i++)
            {
                List<uint> dataSet = new List<uint>();

                for(int j = 0; j < dataTypeCount; j++)
                {
                    dataSet.Add(readUIntBigEndian());
                }

                dataSets.Add(dataSet);
            }

            // ------------- Extra variables ---------------
            // In 011.bin (or 012.bin), the start of the 0x80 section is always denoted by A8 BB BA B9, followed by number of sets and variable length, and padded to 0x10.
            // After this section, the "extra" bits of the variable is denoted by A8 BA A9 BA, with the same info appended to the back.
            // For Infinite Justice Boss Meteor, there is 0xF9 sets, and each set will contain 0x23 variables.
            // Since EBOOT does not allow us to exceed the 0x20 set count, we cannot add the "extra" variables to 0x81th and so on.
            // Thus the only way is to create more "sets" to be read, and retrieve the variable using the same sys_2C method.

            if (readUIntBigEndian(Stream.Position - 0x4) != 0xA8BAA9BA) // The magic to determine the "extra" section, will always be there even if there's no extra variables.
                throw new Exception("Extra section magic not found!");

            uint extra_set_count = readUIntBigEndian();
            uint extra_set_variable_count = readUIntBigEndian();

            extra_data_set = new Dictionary<uint, Dictionary<uint, List<uint>>>();
            uint seekAmount = addPaddingSizeCalculation((uint)Stream.Position);
            Stream.Seek(seekAmount, SeekOrigin.Begin);
            // Only parse if there's more than 1
            if (extra_set_count >= 1 && extra_set_variable_count >= 1)
            {
                for(uint i = 0; i < extra_set_variable_count; i++)
                {
                    uint returnPos = (uint)Stream.Position;
                    Dictionary<uint, List<uint>> variable = new Dictionary<uint, List<uint>>();
                    for(uint j = 0; j < extra_set_count; j++)
                    {
                        List<uint> set_index = new List<uint>();
                        uint value = readUIntBigEndian();

                        if (!variable.ContainsKey(value))
                            variable[value] = set_index;

                        set_index = variable[value];
                        set_index.Add(j);

                        Stream.Seek((extra_set_variable_count * 4) - 0x4, SeekOrigin.Current);
                    }
                    Stream.Seek(returnPos + 0x4, SeekOrigin.Begin);
                    extra_data_set[i] = variable;
                }
            }

            B4ACfs.Close();
            return dataSets;
        }

        private void writeB4AC(List<List<uint>> dataSets)
        {
            string path = Path.GetDirectoryName(Properties.Settings.Default.B4ACFilePath);

            StringBuilder B4ACScript = new StringBuilder();

            B4ACScript.AppendLine("void add_B4AC()");
            B4ACScript.AppendLine("{");

            string sys_2D_Start = "sys_2D(0x3, ";
            uint starting_DataSet_Index = 0x10;

            for(int j = 1; j < dataSets.Count; j++)
            {
                List<uint> dataSet = dataSets[j];

                for (int i = 0; i < dataSet.Count; i++)
                {
                    string new_sys_2D = sys_2D_Start + "0x" + starting_DataSet_Index.ToString("X") + ", ";
                    new_sys_2D += "0x" + (i + 1).ToString("X") + ", ";
                    new_sys_2D += "0x" + dataSet[i].ToString("X") + ");";

                    B4ACScript.AppendLine(new_sys_2D);
                }
                B4ACScript.AppendLine("assign_B4AC_Weapon_Inputs(" + "0x" + (starting_DataSet_Index - 0xf).ToString("X") + ");");
                starting_DataSet_Index++;
            }

            B4ACScript.AppendLine("}");

            StreamWriter txt = File.CreateText(path + @"\B4AC.txt");
            txt.Write(B4ACScript);

            txt.Close();
        }

        private void writeExtraB4AC(Dictionary<uint, Dictionary<uint, List<uint>>> dataSets)
        {
            string path = Path.GetDirectoryName(Properties.Settings.Default.B4ACFilePath);

            StringBuilder ExtraScript = new StringBuilder();

            /*
            for(int i = 0; i < dataSets.Count; i++)
            {
                string set_name = "extra_set_" + "0x" + i.ToString("X");
                for (int j = 0; j < dataSets[i].Count; j++)
                {
                    string variable_name = set_name + "_variable_" + "0x" + j.ToString("X"); 
                    ExtraScript.AppendLine("int " + variable_name + ";");
                }
            }

            ExtraScript.AppendLine(Environment.NewLine);
            ExtraScript.AppendLine("void add_Extra_B4AC()");
            ExtraScript.AppendLine("{");

            for (int j = 0; j < dataSets.Count; j++)
            {
                List<uint> dataSet = dataSets[j];

                string set_name = "extra_set_" + "0x" + j.ToString("X");
                for (int i = 0; i < dataSet.Count; i++)
                {
                    string variable_name = set_name + "_variable_" + "0x" + i.ToString("X");
                    ExtraScript.AppendLine(variable_name + " = " + "0x" + dataSet[i].ToString("X") + ";");
                }
            }

            ExtraScript.AppendLine("}");

            ExtraScript.AppendLine(Environment.NewLine);
            ExtraScript.AppendLine("int parse_Extra_B4AC(int set, int variable)");
            ExtraScript.AppendLine("{");

            for (int j = 0; j < dataSets.Count; j++)
            {
                List<uint> dataSet = dataSets[j];


                if(j == 0)
                {
                    ExtraScript.AppendLine("if (set == 0)");
                }
                else
                {
                    ExtraScript.AppendLine("else if (set == " + "0x" + j.ToString("X") + ")");
                }

                ExtraScript.AppendLine("{");

                string set_name = "extra_set_" + "0x" + j.ToString("X");
                for (int i = 0; i < dataSet.Count; i++)
                {
                    string variable_name = set_name + "_variable_" + "0x" + i.ToString("X");
                    if (i == 0)
                    {
                        ExtraScript.AppendLine("if (variable == 0)");
                    }
                    else
                    {
                        ExtraScript.AppendLine("else if (variable == " + "0x" + i.ToString("X") + ")");
                    }

                    ExtraScript.AppendLine("{");

                    ExtraScript.AppendLine("return " + variable_name + ";");

                    ExtraScript.AppendLine("}");
                }

                ExtraScript.AppendLine("}");
            }
            */

            List<string> var_parse_func = new List<string>();

            for(uint i = 0; i < dataSets.Count; i++)
            {
                Dictionary<uint, List<uint>> data = dataSets[i];

                ExtraScript.AppendLine(Environment.NewLine);
                ExtraScript.AppendLine("int parse_Extra_B4AC_0x" + (i + 1).ToString("x") + " (int set)");
                ExtraScript.AppendLine("{");

                //ExtraScript.AppendLine("switch(set)");
                //ExtraScript.AppendLine("{");

                uint max_count_var = data.FirstOrDefault(s => s.Value.Count() == data.Values.Max(a => a.Count())).Key;
                uint count = 0;
                foreach(var dat in data)
                {
                    List<uint> set_index = dat.Value;
                    if(dat.Key != max_count_var)
                    {
                        string ifcondition;
                        if (count == 0)
                        {
                            ifcondition = "if(";
                        }
                        else
                        {
                            ifcondition = "else if(";
                        }

                        // https://stackoverflow.com/questions/20469416/linq-to-find-series-of-consecutive-numbers
                        var list = set_index.ToArray();
                        var filtered = list.Zip(Enumerable.Range(0, list.Length), Tuple.Create)
                                    .Where((x, t) => t == 0 || list[t - 1] != x.Item1 - 1).ToArray();

                        var result = filtered.Select((x, t) => t == filtered.Length - 1
                                        ? Tuple.Create(x.Item1, list.Length - x.Item2)
                                        : Tuple.Create(x.Item1, filtered[t + 1].Item2 - x.Item2)).ToList();

                        uint result_count = 0;
                        foreach (var t in result)
                        {
                            uint range_start = t.Item1;
                            int range_count = t.Item2;

                            if (result_count != 0)
                                ifcondition += " || ";

                            if(range_count <= 1)
                            {
                                ifcondition += "set == " + range_start;
                            }
                            else
                            {
                                ifcondition += "(" + "set >= " + range_start + " && set <= " + (range_start + range_count - 1) + ")";
                            }

                            result_count++;
                        }

                        ifcondition += ")";
                        ExtraScript.AppendLine(ifcondition);
                        ExtraScript.AppendLine("{");
                        ExtraScript.AppendLine("return " + "0x" + dat.Key.ToString("X") + ";");
                        ExtraScript.AppendLine("}");
                        count++;
                    }
                }

                if(count == 0)
                {
                    ExtraScript.AppendLine("return " + "0x" + max_count_var.ToString("X") + ";");
                }
                else
                {
                    ExtraScript.AppendLine("else");
                    ExtraScript.AppendLine("{");
                    ExtraScript.AppendLine("return " + "0x" + max_count_var.ToString("X") + ";");
                    ExtraScript.AppendLine("}");
                }

                ExtraScript.AppendLine("}");
            }

            /*
            for (int j = 0; j < dataSets.Count; j++)
            {
                List<uint> dataSet = dataSets[j];

                if (j == 0)
                {
                    ExtraScript.AppendLine("if (set == 0)");
                }
                else
                {
                    ExtraScript.AppendLine("else if (set == " + "0x" + j.ToString("X") + ")");
                }

                ExtraScript.AppendLine("{");

                string set_name = "extra_set_" + "0x" + j.ToString("X");
                for (int i = 0; i < dataSet.Count; i++)
                {
                    string variable_name = set_name + "_variable_" + "0x" + (i + 1).ToString("X");
                    if (i == 0)
                    {
                        ExtraScript.AppendLine("if (variable == 0x1)");
                    }
                    else
                    {
                        ExtraScript.AppendLine("else if (variable == " + "0x" + (i + 1).ToString("X") + ")");
                    }

                    ExtraScript.AppendLine("{");

                    // ExtraScript.AppendLine("//" + variable_name);
                    ExtraScript.AppendLine("return " + "0x" + dataSet[i].ToString("X") + ";");

                    ExtraScript.AppendLine("}");
                }

                ExtraScript.AppendLine("}");
            }
            */

            StreamWriter txt = File.CreateText(path + @"\Extra_B4AC.txt");
            txt.Write(ExtraScript);

            txt.Close();
        }
    }
}
