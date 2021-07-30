using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.MBON_Parse
{
    class Parse_Melee_Variables : Internals
    {
        // 008.bin, which stores the approach info for each melee and stuff. 
        // In old script it is inside the first func
        // In new new script it is placed under func_274

        List<uint> variable_hashes = new List<uint> { 0x5A4C8756, 0xd07cfcb2, 0xdfe9fcf5, 0xdcb947f5, 0x731deff4, 0x4ccf4e89, 0x3ed2edf4, 0xe7fe70cc, 0xceb953e6, 0x98753e91, 0xc48ad129, 0xfe0a62, 0x1eb0a10d, 0x43282715, 0x829F4732, 0x96b75f4b };
        public Parse_Melee_Variables()
        {
            FileStream fs = File.OpenRead(Properties.Settings.Default.inputMeleeVarBinaryPath);

            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Wing Zero EW\Extract MBON\Data - 4A5DEE5F\001-MBON\002-FHM\008.bin");
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Infinite Justice Boss METEOR\Extract MBON\Data - EBCEFEC7\001-MBON\002-FHM\008.bin");

            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.inputMeleeVarBinaryPath);
            string outputPath = Properties.Settings.Default.outputScriptFolderPath + @"\" + fileName + " - Melee_Var.txt";
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Wing Zero EW\Converted from MBON\Melee_Var.txt";

            uint magic = readUIntBigEndian(fs);
            uint set_hash_pointer = readUIntBigEndian(fs);
            uint variable_pointer = readUIntBigEndian(fs);
            uint unit_ID = readUIntBigEndian(fs);

            uint variable_hash_count = readUIntBigEndian(fs);
            if (variable_hash_count != 0x10)
                throw new Exception("Non 0x10 count of variables!");

            for(int i = 0; i < variable_hash_count; i++)
            {
                uint var_hash = readUIntBigEndian(fs);
                if (var_hash != variable_hashes[i])
                    throw new Exception("Variable hash mismatch at: " + var_hash);
            }

            fs.Seek(set_hash_pointer, SeekOrigin.Begin);
            uint set_count = readUIntBigEndian(fs);
            Dictionary<uint, Dictionary<uint, uint>> melee_vars = new Dictionary<uint, Dictionary<uint, uint>>();

            List<uint> all_set_hash = new List<uint>();
            for (int i = 0; i < set_count; i++)
            {
                uint set_hash = readUIntBigEndian(fs);
                all_set_hash.Add(set_hash);
            }

            for(int i = 0; i < set_count; i++)
            {
                uint set_hash = all_set_hash[i];

                Dictionary<uint, uint> individual_var = new Dictionary<uint, uint>();
                for (int j = 0; j < variable_hash_count; j++)
                {
                    uint var_hash = variable_hashes[j];
                    uint var = readUIntBigEndian(fs);

                    individual_var[var_hash] = var;
                }
                melee_vars[set_hash] = individual_var;
            }

            StringBuilder MeleeVarScript = new StringBuilder();

            MeleeVarScript.AppendLine(Environment.NewLine);
            MeleeVarScript.AppendLine("int parse_Melee_Var(int set_hash, int var_hash)");
            MeleeVarScript.AppendLine("{");

            uint melee_var_count = 0;
            foreach (var melee_var in melee_vars)
            {
                uint set_hash = melee_var.Key;

                if(melee_var_count == 0)
                {
                    MeleeVarScript.AppendLine("if(set_hash == 0x" + set_hash.ToString("X") + ")");
                }
                else
                {
                    MeleeVarScript.AppendLine("else if(set_hash == 0x" + set_hash.ToString("X") + ")");
                }

                MeleeVarScript.AppendLine("{");

                uint vars_count = 0;
                Dictionary<uint, uint> vars = melee_var.Value;
                foreach(var var in vars)
                {
                    if(vars_count == 0)
                    {
                        MeleeVarScript.AppendLine("if(var_hash == 0x" + var.Key.ToString("X") + ")");
                    }
                    else
                    {
                        MeleeVarScript.AppendLine("else if(var_hash == 0x" + var.Key.ToString("X") + ")");
                    }
                    MeleeVarScript.AppendLine("{");
                    MeleeVarScript.AppendLine("return 0x" + var.Value.ToString("X") + ";");
                    MeleeVarScript.AppendLine("}");
                    vars_count++;
                }

                MeleeVarScript.AppendLine("}");

                melee_var_count++;
            }

            MeleeVarScript.AppendLine("}");

            fs.Close();
            StreamWriter txt = File.CreateText(outputPath);
            txt.Write(MeleeVarScript);

            txt.Close();
        }
    }
}
