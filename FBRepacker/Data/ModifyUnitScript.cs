using AhoCorasick;
using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace FBRepacker.Data
{
    class ModifyUnitScript : Internals
    {
        public ModifyUnitScript()
        {
            List<uint> funcPointers = readBABB(Properties.Settings.Default.BABBFilePath, Properties.Settings.Default.scriptBigEndian);

            string CS = File.ReadAllText(Properties.Settings.Default.CScriptFilePath);
            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.CScriptFilePath);

            // Check if the script is new or old
            bool isNewScript = false;
            if (ifWordExists("sys_0(0x30013,", CS)) // the sys to be used to read new version of sys_2C binary data.
                isNewScript = true;

            if (Properties.Settings.Default.scriptModifyLink && isNewScript)
            {
                Trie trie = new Trie();

                funcPointers.Sort();

                Match search_func_name_match = Regex.Match(CS, @"\s+while [(]var1 < var0[)](\r\n|\r|\n)+\s+{(\r\n|\r|\n)+\s+sys_2D[(]0x3, 0xd, var1, (func_[0-9]{1,100})[(]func_[0-9]{1,100}[(]var1, 0x[a-fA-F0-9]{1,100}[)][)][)];(\r\n|\r|\n)+\s+sys_2D[(]0x3, 0xe, var1, (func_[0-9]{1,100})");
                string search_func_name_d = search_func_name_match.Groups[3].Captures[0].Value;
                string search_func_name_e = search_func_name_match.Groups[5].Captures[0].Value;


                for (int i = 0; i < funcPointers.Count; i++)
                {
                    if (funcPointers[i] > Properties.Settings.Default.MinScriptPointer)
                    {
                        string funcPointerHex = "0x" + funcPointers[i].ToString("X");
                        trie.Add(funcPointerHex.ToLower());
                    }
                }
                trie.Build();

                List<string> all_search_func_name = new List<string>();
                all_search_func_name.Add(search_func_name_d);
                
                if (search_func_name_d != search_func_name_e)
                {
                    all_search_func_name.Add(search_func_name_e);

                    string func_name_e_number = Regex.Match(search_func_name_e, @"func_([0-9]{1,100})").Groups[1].Value;
                    uint.TryParse(func_name_e_number, out uint func_name_e);

                    string search_func_name_0x25 = "func_" + (func_name_e - 1).ToString();

                    all_search_func_name.Add(search_func_name_0x25);
                }

                string log = string.Empty;
                foreach (string search_func_name in all_search_func_name)
                {
                    // get the whole func
                    Match search_func_match = Regex.Match(CS, @"(int " + search_func_name + @"[(]int arg0[)](\r\n|\r|\n)+{(\r\n|\r|\n)+\s+int var1;[\s\S]*?(?=(\r\n|\r|\n)+void))");
                    string search_func = search_func_match.Groups[1].Value;
                    //string modified_search_func = search_func;

                    Dictionary<string, string> addedWord = new Dictionary<string, string>();
                    foreach (string word in trie.Find(search_func))
                    {
                        // Check if the pointer to replace is actually pointer, not hash.
                        // format: varx = 0xpointer
                        bool isMatch = Regex.IsMatch(search_func, @"var[0-9]{1,100} = " + word);

                        if (!addedWord.Keys.Contains(word) && isMatch)
                        {
                            uint.TryParse(word.Remove(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint funcPointer);
                            int funcNumber = funcPointers.IndexOf(funcPointer);
                            string funcStr = "func_" + funcNumber;
                            addedWord[word] = funcStr;
                        }
                    }

                    foreach (var word in addedWord)
                    {
                        search_func = Regex.Replace(search_func, @"(var[0-9]{1,100} = )" + word.Key, @"$1" + word.Value);
                        //search_func = search_func.Replace(word.Key, word.Value);
                        log += (word.Key + " - " + word.Value);
                        log += Environment.NewLine;
                    }

                    CS = Regex.Replace(CS, @"(int " + search_func_name + @"[(]int arg0[)](\r\n|\r|\n)+{(\r\n|\r|\n)+\s+int var1;[\s\S]*?(?=(\r\n|\r|\n)+void))", search_func);

                }

                StreamWriter logFile = File.CreateText(Properties.Settings.Default.outputScriptFolderPath + @"\" + fileName + "-link_log.txt");
                logFile.Write(log);
                logFile.Close();
            }
            
            if(Properties.Settings.Default.scriptModifyRefactor)
                CS = refactorMBON(CS);

            StreamWriter replacedCScript = File.CreateText(Properties.Settings.Default.outputScriptFolderPath + @"\" + fileName + ".c");
            replacedCScript.Write(CS);
            //MessageBox.Show("Replaced lines: " + Environment.NewLine + log, "Link Complete", MessageBoxButton.OK);
            replacedCScript.Close();
        }

        private List<uint> readBABB(string path, bool bigendian)
        {
            FileStream BABBfs = File.OpenRead(Properties.Settings.Default.BABBFilePath);
            changeStreamFile(BABBfs);

            uint Magic = readUIntBigEndian(Stream.Position);
            if (Magic != 0xB2ACBCBA)
                throw new Exception("Not a valid BABB File!");

            Stream.Seek(0xC, SeekOrigin.Current);

            List<uint> funcPointers = new List<uint>();

            if (bigendian)
            {
                uint funcListPointer = readUIntBigEndian(Stream.Position) + 0x30;
                funcListPointer = addPaddingSizeCalculation(funcListPointer);

                Stream.Seek(0x04, SeekOrigin.Current);
                uint funcCount = readUIntBigEndian(Stream.Position);

                Stream.Seek(funcListPointer, SeekOrigin.Begin);
                for (int i = 0; i < funcCount; i++)
                {
                    funcPointers.Add(readUIntBigEndian(Stream.Position));
                }
            }
            else
            {
                uint funcListPointer = readUIntSmallEndian(Stream.Position) + 0x30;
                funcListPointer = addPaddingSizeCalculation(funcListPointer);

                Stream.Seek(0x04, SeekOrigin.Current);
                uint funcCount = readUIntSmallEndian(Stream.Position);

                Stream.Seek(funcListPointer, SeekOrigin.Begin);
                for (int i = 0; i < funcCount; i++)
                {
                    funcPointers.Add(readUIntSmallEndian(Stream.Position));
                }
            }

            BABBfs.Close();

            return funcPointers;
        }

        public bool ifWordExists(string word, string Alltext)
        {
            Trie trie = new Trie();
            trie.Add(word.ToLower());
            trie.Build();
            List<string> str = trie.Find(Alltext).ToList();
            return str.Count() != 0 ? true : false ;
        }

        private string refactorMBON(string CS)
        {
            // Check if the script is new or old
            bool isNewScript = false;
            if (ifWordExists("sys_0(0x30013,", CS)) // the sys to be used to read new version of sys_2C binary data.
                isNewScript = true;

            string B4ACFolder = Properties.Settings.Default.inputScriptRefactorTxtFolder;
            string B4ACCountTxt = B4ACFolder + @"\B4ACCount.txt";
            if (!File.Exists(B4ACCountTxt))
                throw new Exception("B4ACCount txt not found!");

            string B4ACTxt = B4ACFolder + @"\B4AC.txt";
            if (!File.Exists(B4ACTxt))
                throw new Exception("B4AC txt not found!");

            string extraB4ACCountTxt = B4ACFolder + @"\extraB4ACCount.txt";
            if (!File.Exists(extraB4ACCountTxt))
                throw new Exception("extraB4ACCount txt not found!");

            string extraB4ACTxt = B4ACFolder + @"\Extra_B4AC.txt";
            if (!File.Exists(extraB4ACTxt))
                throw new Exception("extra B4AC txt not found!");

            string meleeVarTxt = B4ACFolder + @"\007 - Melee_Var.txt";
            if (!File.Exists(meleeVarTxt))
                throw new Exception("meleeVar txt not found!");


            StringBuilder sw = new StringBuilder(CS);
            sw.Append(@"int deadface;
int awakenType;
int var_e004b_0;
int var_e004b_0x1;
int var_e004b_0x2;
int var_e004b_0x3;
int var_e0051;
int var_sys_9_0x8_0x1;
int var_sys_9_0x8_0x3;
int var_sys_9_0x8_0x4;
int var_sys_9_0x8_0x5;
int var_sys_9_0x8_0x6;
int var_sys_9_0x8_0x7;
int var_sys_9_0x8_0x8;
int var_sys_9_0x8_0x9;
int var_sys_9_0x8_0xa;
int var_sys_9_0x8_0xb;
int var_sys_9_0x8_0xc;
int var_sys_9_0x8_0xd;
int var_sys_9_0x8_0xe;
int var_sys_9_0x8_0xf;
int var_sys_9_0x8_0x10;
int var_sys_9_0x8_0x12;
int var_sys_9_0x8_0x36_1;
int var_sys_9_0x8_0x36_2;
int var_sys_9_0x8_0x37;
int var_sys_9_0x8_0x38;
int var_sys_9_0x8_0x39;
int var_sys_9_0x8_0x3d;
int var_sys_9_0x8_0x79;
int var_sys_9_0x8_0x98;
int var_sys_9_0x8_0x99;
int var_sys_9_0x8_0x9a;
int var_sys_9_0x8_0x9c;
int ragingEffectFlag;
int ragingShootFlag;
int ragingMeleeContiniousFlag;
int ragingMeleeFlag;
int showBurstALEOFlag;
int hideBurstALEOFlag;
int changeBurstTypeFlag;
int deadface2;

void var_sys_9_0x8()
{
    deadface = 0xDEADFACE;
    deadface2 = 0xDEADFACE;

    var_sys_9_0x8_0x1 = 0;
    var_sys_9_0x8_0x3 = 0;
    var_sys_9_0x8_0x4 = 0;
    var_sys_9_0x8_0x5 = 0;
    var_sys_9_0x8_0x6 = 0;
    var_sys_9_0x8_0x7 = 0x4b;
    var_sys_9_0x8_0x8 = 0x41;
    var_sys_9_0x8_0x9 = 0;
    var_sys_9_0x8_0xa = 0;
    var_sys_9_0x8_0xb = 0;
    var_sys_9_0x8_0xc = 0x4b;
    var_sys_9_0x8_0xd = 0x41;
    var_sys_9_0x8_0xe = 0;
    var_sys_9_0x8_0xf = 0;
    var_sys_9_0x8_0x10 = 0;
    var_sys_9_0x8_0x12 = 0x190;
    var_sys_9_0x8_0x36_1 = 0;
    var_sys_9_0x8_0x36_2 = 0;
    var_sys_9_0x8_0x37 = 0;
    var_sys_9_0x8_0x38 = 0;
    var_sys_9_0x8_0x39 = 0;
    var_sys_9_0x8_0x3d = 0;
    var_sys_9_0x8_0x79 = 0x64;
    var_sys_9_0x8_0x98 = 0;
    var_sys_9_0x8_0x99 = 0x1;
    var_sys_9_0x8_0x9a = 0x78; // Action multiplier 120%
    var_sys_9_0x8_0x9c = 0x1; // Diagonal step flag

    if (awakenType == 0) // F 
    {
        var_sys_9_0x8_0x1 = 0;
        var_sys_9_0x8_0x79 = 0x6E;
        var_sys_9_0x8_0x98 = 0x1;
        var_sys_9_0x8_0x99 = 0;
        var_sys_9_0x8_0x9a = 0x78;
        var_sys_9_0x8_0x9c = 0;
    }
    else if (awakenType == 0x1) // S
    {
        var_sys_9_0x8_0x1 = 0x1;
        var_sys_9_0x8_0x79 = 0x64;
        var_sys_9_0x8_0x98 = 0;
        var_sys_9_0x8_0x99 = 0;
        var_sys_9_0x8_0x9a = 0x78;
        var_sys_9_0x8_0x9c = 0;
    }
    else if (awakenType == 0x2) // E
    {
        /* Original MBON
        var_sys_9_0x8_0x1 = 0;
        var_sys_9_0x8_0x79 = 0x64;
        var_sys_9_0x8_0x98 = 0;
        var_sys_9_0x8_0x99 = 0x1;
        var_sys_9_0x8_0x9a = 0x78;
        var_sys_9_0x8_0x9c = 0x1;
        */
        // XB Version of C Burst
        var_sys_9_0x8_0x1 = 0;
        var_sys_9_0x8_0x79 = 0x64;
        var_sys_9_0x8_0x98 = 0;
        var_sys_9_0x8_0x99 = 0x1; // Allowing you to tech out of burst
        var_sys_9_0x8_0x9a = 0x78;
        var_sys_9_0x8_0x9c = 0;
    }
    else if (awakenType == 0x3) // Mobility
    {
        var_sys_9_0x8_0x1 = 0;
        var_sys_9_0x8_0x79 = 0x64;
        var_sys_9_0x8_0x98 = 0;
        var_sys_9_0x8_0x99 = 0;
        var_sys_9_0x8_0x9a = 0x78;
        var_sys_9_0x8_0x9c = 0x1; // Make it possible to diagonal step
    }
    else if (awakenType == 0x4) // Raging
    {
        var_sys_9_0x8_0x1 = 0;
        var_sys_9_0x8_0x79 = 0x64;
        var_sys_9_0x8_0x98 = 0;
        var_sys_9_0x8_0x99 = 0;
        var_sys_9_0x8_0x9a = 0x78;
        var_sys_9_0x8_0x9c = 0;
    }
}

// custom Bursts triggers
int customBurstSystemCounter;
int customBurstSystemInputDelay;
int customBurstSystemInit;
int customBurstSystemDebug;
int customBurstSystemInputDelayResetFlag;
int frameCount;
int alreadySetAwakenType;

void customBurstSystem_Condition()
{
    if (customBurstSystemCounter == 0 && global159 == 0x200) // Activation: Communication button
    {
        customBurstSystemDebug = 0x999999999;
        customBurstSystemInit = 1;
        customBurstSystemInputDelay = 0;
        customBurstSystemCounter++;
        customBurstSystemInputDelayResetFlag = 0;
    }

    if (customBurstSystemInit == 1)
    {
        if (customBurstSystemInputDelay >= 0x1) //  && customBurstSystemInputDelay <= 0x1E)
        {
            // Instead of making a generalized input delay, makes it differ so that we can continue change the burst before game start
            if (customBurstSystemCounter == 1 && global159 == 0x4) // Continued activation: Down button
            {
                customBurstSystemDebug = 0x12345678;
                customBurstSystemCounter++;
                customBurstSystemInputDelay = 0;
                customBurstSystemInputDelayResetFlag = 0;
            }
            if (customBurstSystemCounter == 2 && (global159 == 0x8 || global159 == 0x4 || global159 == 0x2 || global159 == 0x1)) // Continued activation: Directional button
            {
                customBurstSystemDebug = 0x1337BEEF;
                //customBurstSystemCounter = customBurstSystemCounter + 1;
                customBurstSystemInputDelay = 0;
                //customBurstSystemInit = 0;
                //customBurstSystemCounter = 0;
                customBurstSystemInputDelayResetFlag = 0;

                changeBurstTypeFlag = 1;
                showBurstALEOFlag = 0;

                if (global159 == 0x8) // Up
                {
                    awakenType = 0x2; // C
                }
                else if (global159 == 0x4) // Down
                {
                    awakenType = 0; // F
                }
                else if (global159 == 0x2) // Left
                {
                    awakenType = 0x3; // M
                }
                else if (global159 == 0x1) // Right
                {
                    awakenType = 0x4; // R
                }

                /* If this system is activated, we need to store the changed EX Burst enum on a memory region that will not be resetted when the unit respawns.
                * The only way I know is through using sys_1(0xe0009
                * For the nth set, we use 0xa as it is far back enough that it does not exceed the reserved memory region but still back enough to not interfere with others
                * We also store a flag so to mark we have set a custom Burst type
                */

                sys_1(0xe0009, 0xa, awakenType); // Store to temporary memory storage location at nth arg0. arg1 is the value to store.
            }
        }
        if (customBurstSystemInputDelay > 0x20 && customBurstSystemInputDelayResetFlag == 0x1)
        {
            customBurstSystemInputDelay = 0;
            customBurstSystemInit = 0;
            customBurstSystemCounter = 0;
        }
        customBurstSystemInputDelay++;
    }
}

void raging_Burst_Check()
{
    // FB Change: 
    // Add superarmor for Raging Burst
    // global103 = to make sure it only works on startup
    // global130 == 0. Taken from func_264. global130 will not be 1 for cases where hasei is done
    // global17 == 1. If it is EX Burst Attack (if disable actions flag)
    if (awakenType == 0x4 && (global139 == 1 || global139 == 2) && global103 > 0 && global130 == 0 && global17 != 1)
    {
        // global51 & 0x1 = Shooting
        // ragingShootFlag is derived from func_266 and func_267 where it checks for cancel route of shooting weapons during burst
        // once the projectile is shot, ragingShootFlag will be 1, thus ending the SA
        // global51 & 0x2 = Melee
        // global103 < 0x2 = Melee before slash
        // ragingMeleeFlag = 1 for func_459 melee template (so that moves like pin melee or special movement does not trigger SA)
        if ((global19 & 0x3) != 0 && ((global51 & 0x1 && ragingShootFlag == 0) || (global51 & 0x2 && global103 <= 0x2 && ragingMeleeFlag == 0x1 && global218 != 0)))
        {
            // global97 = the ultimate flag for filtering out moves that should have SA effect.
            // For cases where it has special movements, it will write 1 at init, and turn to 0.
            // Hence, we need to check if the 1 is still there, or it becomes 0.
            // Normal melee approach that is raging burst SA worthy has flag of 1.
            if (global97 == 1)
            {
                ragingMeleeContiniousFlag++; // Increment the flag
            }
            // If the weapon is shooting, just proceed.
            // If the weapon is Melee, the increment flag must be > than 1.
            if (global51 & 0x1 || ragingMeleeContiniousFlag >= 0x1)
            {
                global67 = 0x2;
                if (ragingEffectFlag == 0)
                {
                    sys_6(0xD7732CE4, global212, 0x1, 0x3, 0xC);
                    global241 = global212;
                    ragingEffectFlag++;
                }
            }
        }
    }
    // If it ever changes to 0, the flag is resetted to 0.
    if (global97 == 0)
    {
        ragingMeleeContiniousFlag = 0;
    }
    if (ragingEffectFlag > 0)
    {
        if ((global51 & 0x1 && ragingShootFlag == 0x1) || (global51 & 0x2 && global103 > 0x2) || global103 == 0 || global130 != 0 || global17 == 1)
        {
            sys_35(0x3, 0xc, 0, 0x1);
            ragingEffectFlag = 0;
        }
    }
}

int M_Boost_ALEO_Elapsed_Frame;
int C_Increase_Burst_ALEO_Flag;
int C_Increase_Burst_ALEO_Mode;
int X_S_Burst_ALEO_Flag;
int X_S_Burst_ALEO_Mode;
int X_C_Burst_ALEO_Flag;
int X_C_Burst_ALEO_Mode;
int X_F_ALEO_Elapsed_Frame;
int X_M_Boost_ALEO_Elapsed_Frame;
int X_R_ALEO_Elapsed_Frame;

void add_Burst_Effects()
{
    if (X_F_ALEO_Elapsed_Frame >= 0x1)
    {
        if (X_F_ALEO_Elapsed_Frame >= 0x14) // Only lets it lasts for max 20 frames
        {
            sys_35(0x3, 0xc, 0, 0x1);
            X_F_ALEO_Elapsed_Frame = 0;
        }
        else
        {
            X_F_ALEO_Elapsed_Frame++; // If the frame is activated, not 0, we count the frames
        }
    }

    if (X_M_Boost_ALEO_Elapsed_Frame >= 0x1)
    {
        if (X_M_Boost_ALEO_Elapsed_Frame >= 0x14)
        {
            sys_35(0x3, 0xc, 0, 0x1);
            X_M_Boost_ALEO_Elapsed_Frame = 0;
        }
        else
        {
            X_M_Boost_ALEO_Elapsed_Frame++;
        }
    }

    if (X_R_ALEO_Elapsed_Frame >= 0x1)
    {
        if (X_R_ALEO_Elapsed_Frame >= 0x14)
        {
            sys_35(0x3, 0xc, 0, 0x1);
            X_R_ALEO_Elapsed_Frame = 0;
        }
        else
        {
            X_R_ALEO_Elapsed_Frame++;
        }
    }

    if (M_Boost_ALEO_Elapsed_Frame >= 0x1)
    {
        if (M_Boost_ALEO_Elapsed_Frame >= 0x20)
        {
            sys_35(0x4, 0xa, 0, 0x1);
            M_Boost_ALEO_Elapsed_Frame = 0;
        }
        else
        {
            M_Boost_ALEO_Elapsed_Frame++;
        }
    }

    int effect_Enum = sys_0(0xe0009, 0x11); // 17th reserved memory
    int long_ALEO_effect_enum = sys_0(0xe0009, 0x12); // 18th reserved memory

    if ((long_ALEO_effect_enum & 0x1) == 0x1) // C Burst add EX
    {
        if (C_Increase_Burst_ALEO_Flag == 0)
        {
            sys_6(0x0ea626af, global212, 0x1, 0x5, 0xC); // Charge ALEO
            global241 = global212;
            C_Increase_Burst_ALEO_Flag = 1;
            C_Increase_Burst_ALEO_Mode = global306; // mode enum
        }
    }
    else if ((long_ALEO_effect_enum & 0x1) == 0x0)
    {
        if (C_Increase_Burst_ALEO_Flag == 1)
        {
            sys_35(0x5, 0xc, 0, 0x1);
            C_Increase_Burst_ALEO_Flag = 0;
            sys_6(0x78E1F497, global212, 0x1, 0x5, 0xC); // White ALEO signifies done adding it
        }
    }

    if ((long_ALEO_effect_enum & 0x2) == 0x2) // S Burst Cross Burst
    {
        if (X_S_Burst_ALEO_Flag == 0)
        {
            sys_6(0x8bce6c6e, global212, 0x1, 0x6, 0xC); // Blue Shooting Circle ALEO
            global241 = global212;
            X_S_Burst_ALEO_Flag = 1;
            X_S_Burst_ALEO_Mode = global306; // mode enum
        }
    }
    else if ((long_ALEO_effect_enum & 0x2) == 0x0)
    {
        if (X_S_Burst_ALEO_Flag == 1)
        {
            sys_35(0x6, 0xc, 0, 0x1);
            X_S_Burst_ALEO_Flag = 0;
            sys_6(0xE1E8A52D, global212, 0x1, 0x6, 0xC); // Green ALEO signifies done cross burst
        }
    }

    if ((long_ALEO_effect_enum & 0x4) == 0x4) // C Burst Cross Burst
    {
        if (X_C_Burst_ALEO_Flag == 0)
        {
            sys_6(0x962bbe48, global212, 0x1, 0x6, 0xC); // Green Covering Circle ALEO
            global241 = global212;
            X_C_Burst_ALEO_Flag = 1;
            X_C_Burst_ALEO_Mode = global306; // mode enum
        }
    }
    else if ((long_ALEO_effect_enum & 0x4) == 0)
    {
        if (X_C_Burst_ALEO_Flag == 1)
        {
            sys_35(0x6, 0xc, 0, 0x1);
            X_C_Burst_ALEO_Flag = 0;
            sys_6(0xE1E8A52D, global212, 0x1, 0x6, 0xC); // Green ALEO signifies done adding it
        }
    }

    if (C_Increase_Burst_ALEO_Mode != global306)
    {
        C_Increase_Burst_ALEO_Flag = 0;
    }

    if (X_S_Burst_ALEO_Mode != global306)
    {
        X_S_Burst_ALEO_Flag = 0;
    }

    if (X_C_Burst_ALEO_Mode != global306)
    {
        X_C_Burst_ALEO_Flag = 0;
    }

    if (effect_Enum & 0x2) // F burst cross burst
    {
        if (X_F_ALEO_Elapsed_Frame > 0)
        {
            sys_35(0x3, 0xc, 0, 0x1);
        }

        X_F_ALEO_Elapsed_Frame = 1; // If the effect is already activated, activate it again and reset the flags. 
        sys_6(0xa0a3454b, global212, 0x1, 0x3, 0xC);
        global241 = global212;
    }

    if (effect_Enum & 0x4) // M burst cross burst
    {
        if (X_M_Boost_ALEO_Elapsed_Frame > 0)
        {
            sys_35(0x3, 0xc, 0, 0x1); // If the effect is already activated, activate it again and reset the flags. 
        }

        X_M_Boost_ALEO_Elapsed_Frame = 1;
        sys_6(0x030192c8, global212, 0x1, 0x3, 0xC);
        global241 = global212;
    }

    if (effect_Enum & 0x8) // R burst cross burst
    {
        if (X_R_ALEO_Elapsed_Frame > 0)
        {
            sys_35(0x3, 0xc, 0, 0x1);
        }

        X_R_ALEO_Elapsed_Frame = 1; // If the effect is already activated, activate it again and reset the flags. 
        sys_6(0xf53fa0d7, global212, 0x1, 0x3, 0xC);
        global241 = global212;
    }
}

// To be used for the SPRX script to determine the multiplier to be used for damage and down values on bursts
void write_Weapon_Type_Enum()
{
    if (global51 & 0x1) // Shooting
    {
        sys_1(0xe0009, 0xd, 0x1); // 13th reserved memory
    }
    else if (global51 & 0x2) // Melee
    {
        sys_1(0xe0009, 0xd, 0x2); // 13th reserved memory
    }
}
");
            CS = sw.ToString();

            StreamReader B4ACCountSR = File.OpenText(B4ACCountTxt);
            string B4ACCountStr = B4ACCountSR.ReadLine();
            if(!uint.TryParse(B4ACCountStr, out uint B4ACCount))
                throw new Exception("Invalid B4AC Count!");

            // for new script
            if (B4ACCount > 1)
            {
                // Search for sys_74(0x8, 0)'s second func
                string inputFunc1 = Regex.Match(CS,
                    @"(int (func_[0-9]{1,100})[(][)])(\r\n|\r|\n)+{\s+int var0;\s+var0 = func_[0-9]{1,100}[(][)];(\r\n|\r|\n)+\s+if [(]var0[)]\s+{(\r\n|\r|\n)+\s+func_[0-9]{1,100}[(]sys_74[(]0x8, 0[)]. 0x1f[)];(\r\n|\r|\n)+\s+}(\r\n|\r|\n)+\s+return var0;(\r\n|\r|\n)+}")
                    .Groups[2]
                    .Captures[0].
                    Value;

                // Search for sys_74(0x8, 0)'s first func
                Match inputFunc2Match = Regex.Match(CS,
                    @"(void (func_[0-9]{1,100})[(][)])(\r\n|\r|\n)+{\s+int var0;\s+int var1;(\r\n|\r|\n)+\s+int var2;(\r\n|\r|\n)+\s+var0 = 0;(\r\n|\r|\n)+\s+var1 = 0;(\r\n|\r|\n)+\s+var2 = sys_74[(]0x6, 0x2000[)];(\r\n|\r|\n)+\s+if [(]var2 > 0[)](\r\n|\r|\n)+\s+{(\r\n|\r|\n)+\s+(func_[0-9]{1,100})[(]sys_74[(]0x8, 0[)], 0x1f[)];(\r\n|\r|\n)+\s+}(\r\n|\r|\n)+}");

                string inputFunc2 = inputFunc2Match.Groups[2].Captures[0].Value;

                // Search func_138, we can get from inputFunc2.

                string inputFunc3 = inputFunc2Match.Groups[11].Captures[0].Value;

                // append input func
                string allInputFuncs = @"
int assign_B4AC_Weapon_Inputs_Support(int arg0, int arg1, int arg2, int arg3)
{
    int var4;
    int var5;
    var4 = 0;
    var5 = arg0;
    arg0 = arg0 % 0x64;
    if (var5 != 0x190 && arg0 != 0x1f && arg3 != 0x27) // In MBON it changes to 0x27 instead of 0x26
    {
        var4 += 0x1;
    }
    if (arg0 == 0x1)
    {
        var4 += 0x10;
    }
    else if (arg0 == 0xa)
    {
        var4 += 0x20;
    }
    else if (arg0 == 0xb)
    {
        var4 += 0x40;
    }
    else if (arg0 == 0xc)
    {
        var4 += 0x80;
    }
    if (arg1 != 0x5) // If the ammo is not the null ammo index, we need to check if ammo by setting these flags
    {
        if (arg1 >= 0x64)
        {
            var4 += 0x800;
        }
        else
        {
            var4 += 0x100; // For normal checks, will do the action even without the ammo, but there won't be any projectiles
        }
    }
    if (arg2 == 0x1) // 9th var
    {
        var4 += 0x200; // If this is set, sys_74(0x3) will return 0x10000000, thus making the action not doable while displaying no ammo
    }
    else if (arg2 == 0x2)
    {
        var4 += 0x400; // Same as above, but this is for cases where the input has different cancel variations depending on the directional inputs
    }
    return var4;
}

/// <summary>
/// Support func that reads specific input data from weapon data and pass them into sys_74(0x1) based on input data, and will eventually be retrieved by sys_74(0x3).
/// </summary>
/// <param name=""""arg0""""></param>
void assign_B4AC_Weapon_Inputs(int arg0)
{
    int var1;
    int var2;
    int var3;
    int var4;
    int var5;
    int var6;
    int var7;
    int var8;
    var1 = sys_2C(0x3, 0x11 + arg0 - 0x1, 0x4);
    var2 = sys_2C(0x3, 0x11 + arg0 - 0x1, 0x3);
    var3 = sys_2C(0x3, 0x11 + arg0 - 0x1, 0x8);
    if (var1 >= 0x64)
    {
        var4 = var1 - 0x64;
        var5 = var1 - 0x64;
    }
    else
    {
        var4 = 0;
        if (var1 == 0x17)
        {
            var5 = 0;
        }
        else
        {
            var5 = var1;
        }
    }
    if (var3 >= 0x64) // For weird case where the ammo slot is > 0x64
    {
        var6 = var3 - 0x64; // Make the var6 normal ammo index
        var7 = var3 - 0x64; // Make the var7 normal ammo index
        sys_2D(0x3, 0x11 + arg0 - 0x1, 0x8, 0x5); // Replace the ammo index with 0x5, where it should not use any ammo
    }
    else
    {
        var6 = var3; // Ammo index, used for determining if there's ammo to do this action
        var7 = 0x5; // Change to 0x5 since it is supposed to be nth ammo
    }
    if (var2 == 0x12c)
    {
        var8 = 0x2000;
    }
    else
    {
        var8 = 0x1 << var2 % 0x64; // Equivalent to old script's global81
    }
    sys_74(0x1, arg0, var8, var5, assign_B4AC_Weapon_Inputs_Support(var2, var3, sys_2C(0x3, 0x11 + arg0 - 0x1, 0x9), sys_2C(0x3, 0x11 + arg0 - 0x1, 0xa)), var2 / 0x64 + 0x1, sys_2C(0x3, 0x11 + arg0 - 0x1, 0x1), sys_2C(0x3, 0x11 + arg0 - 0x1, 0x5), sys_2C(0x3, 0x11 + arg0 - 0x1, 0x6), var6, sys_2C(0x3, 0x11 + arg0 - 0x1, 0x7), sys_2C(0x3, 0x11 + arg0 - 0x1, 0x2e), var1 == 0x17, var4, var7);
}

/// <summary>
/// FB style input func.
/// </summary>
void input()
{
    int var0;
    int var1;
    int var2;
    int var3;
    int var4;
    int var5;
    int var6;
    int var7;
    int var8;
    int var9;
    // Note: Need to change this to the appropiate func.
    // Search sys_74(0x8, 0), take the second func
    if (" + inputFunc1 + @"() == 0x1)
    {
        return;
    }
    if (func_194(0x1))
    {
        // Note: Need to change this to the appropiate func.
        // Search sys_74(0x6, 0x2000);
        " + inputFunc2 + @"();
        return;
    }
    var0 = 0;
    var1 = 0;
    var2 = 0;
    var3 = 0x1;
    var4 = sys_0(0x70001);
    var5 = 0x2c24;
    if (-var5 < var4 && var4 < var5 || sys_0(0x80000) < 0)
    {
        var6 = 0;
    }
    else
    {
        var6 = 0x1;
    }

    // sys_74(0x3, global81 ...) -> return nth data set that uses this input.
    // For some reason this syscall ignores everything behind global81.
    var7 = sys_74(0x3, global81, global25, global306, (global19 & 0x1000000) != 0, global139 != 0, (global19 & 0x3) != 0, global81 & 0x40, var6, global157, (global19 & 0x4000) != 0, func_246());

    if (var7) // If data set index is not 0.
    {
        if (var7 & 0x10000000) // For cases where you can't do the starting animation unless you have ammo.
        {
            func_243(var7 & 0xfffffff);
        }
        else
        {
            var0 = sys_2C(0x3, 0x11 + var7 - 0x1, 0x3) % 0x64; // Modulo 100
            var8 = sys_2C(0x3, 0x11 + var7 - 0x1, 0x4);

            if (var0 == 0x1)
            {
                if (var8 == 0x1)
                {
                    var0 = 0x4;
                }
                else if (var8 == 0x2)
                {
                    var0 = 0x3;
                }
                else if (var8 == 0x4)
                {
                    var0 = 0x5;
                }
                else if (var8 == 0x8)
                {
                    var0 = 0x2;
                }
                else if (var8 == 0xc)
                {
                    var0 = 0x2;
                }
                else if (var8 == 0x3)
                {
                    var0 = 0x4;
                }
                else if (var8 == 0xf)
                {
                    var0 = 0x2;
                }
            }
            // This is for cases where you should not be able to use certain weapon if the flag is set.
            // Some weapon use this system, such as releasing Reborn's funnels will cause the condition to be true.
            // func_243 accepts the nth weapon to call for no ammo (from sys_2D 8th data)
            // ------- The old way of checking -----------
            // var0 is left shifted by 1, e.g. Reborn's Cannon AC var0 is 8, 1 << 8 = 0x100 (a.k.a global68 & 0x100 = AC)
            // So if the global var input_Supp is ever set, it will return non 0 value.
            /*
            if (input_Supp_3(0x1 << var0))
            {
                var9 = sys_2C(0x3, 0x11 + var7 - 0x1, 0x8);
                func_243(var9);
            }
            */
            // ------- The new way of checking -----------
            // For MBON, instead of using the global81 (old global68) flag (e.g. 0x80 = sub, 0x100 = AC), they use the hash stored at 0x2e th of data.
            // 0x2e hash is used for assigning extra_B4AC, and for script that does not have extra_B4AC it is a normal ID.
            // In MBON, the flag is set by using sys_74(0xd, arg0, 0x1)
            // Since sys_74(0xd is not present in FB, we need to do a custom made flag assigning func.
            // sys_74(0xd, 0x12345678, 0) // For cases where sys_2e value is hash
            // sys_74(0xd, 0x2a, 0x1) // For cases where sys_2e value is ID
            if (input_Supp_3(0x1 << var0))
            {
                var9 = sys_2C(0x3, 0x11 + var7 - 0x1, 0x8);
                func_243(var9);
            }
            else
            {
                // Note: Need to change this to the appropiate func.
                // Main weapon logic func. Search func_138
                " + inputFunc3 + @"(var7, var0);
            }
        }
    }
}

int input_Supp;

void input_Supp_1(int arg0)
{
    int var3 = parse_B4AC_0x2e(arg0); // Get concentated global81 value
    int input = 1 << (var3 % 0x64); // Get value equivalent to global81 in old type script
    input_Supp |= input; // Add global81 flag so that it won't be done if the input matches
}

void input_Supp_2()
{
    input_Supp = 0; // Reset the flag
}

int input_Supp_3(int arg0)
{
    return input_Supp & arg0; // Check if the flag exists
}

";

                sw.Append(allInputFuncs);

                // include parse_B4AC_0x2e() and add_B4AC()
                StreamReader B4ACsr = File.OpenText(B4ACTxt);
                string B4AC = B4ACsr.ReadToEnd();
                sw.Append(B4AC);

                StreamReader extraB4ACsr = File.OpenText(extraB4ACTxt);
                string extraB4AC = extraB4ACsr.ReadToEnd();
                sw.Append(extraB4AC);

            }
            else
            {
                if (isNewScript)
                    throw new Exception("new script type with no B4AC?");
            }

            StreamReader meleeVarsr = File.OpenText(meleeVarTxt);
            string meleeVar = meleeVarsr.ReadToEnd();
            sw.Append(meleeVar);

            CS = sw.ToString();

            CS = CS.Replace("func_0", "main");

            CS = CS.Replace("sys_9(0x8, 0x1)", "var_sys_9_0x8_0x1");
            CS = CS.Replace("sys_9(0x8, 0x3)", "var_sys_9_0x8_0x3");
            CS = CS.Replace("sys_9(0x8, 0x4)", "var_sys_9_0x8_0x4");
            CS = CS.Replace("sys_9(0x8, 0x5)", "var_sys_9_0x8_0x5");
            CS = CS.Replace("sys_9(0x8, 0x6)", "var_sys_9_0x8_0x6");
            CS = CS.Replace("sys_9(0x8, 0x7)", "var_sys_9_0x8_0x7");
            CS = CS.Replace("sys_9(0x8, 0x8)", "var_sys_9_0x8_0x8");
            CS = CS.Replace("sys_9(0x8, 0x9)", "var_sys_9_0x8_0x9");
            CS = CS.Replace("sys_9(0x8, 0xa)", "var_sys_9_0x8_0xa");
            CS = CS.Replace("sys_9(0x8, 0xb)", "var_sys_9_0x8_0xb");
            CS = CS.Replace("sys_9(0x8, 0xc)", "var_sys_9_0x8_0xc");
            CS = CS.Replace("sys_9(0x8, 0xd)", "var_sys_9_0x8_0xd");
            CS = CS.Replace("sys_9(0x8, 0xe)", "var_sys_9_0x8_0xe");
            CS = CS.Replace("sys_9(0x8, 0xf)", "var_sys_9_0x8_0xf");
            CS = CS.Replace("sys_9(0x8, 0x10)", "var_sys_9_0x8_0x10");
            CS = CS.Replace("sys_9(0x8, 0x12)", "var_sys_9_0x8_0x12");
            CS = CS.Replace("sys_9(0x8, 0x36, 0x1)", "var_sys_9_0x8_0x36_1");
            CS = CS.Replace("sys_9(0x8, 0x36, 0x2)", "var_sys_9_0x8_0x36_2");
            CS = CS.Replace("sys_9(0x8, 0x37)", "var_sys_9_0x8_0x37");
            CS = CS.Replace("sys_9(0x8, 0x38)", "var_sys_9_0x8_0x38");
            CS = CS.Replace("sys_9(0x8, 0x39)", "var_sys_9_0x8_0x39");
            CS = CS.Replace("sys_9(0x8, 0x3d)", "var_sys_9_0x8_0x3d");
            CS = CS.Replace("sys_9(0x8, 0x79)", "var_sys_9_0x8_0x79");
            CS = CS.Replace("sys_9(0x8, 0x98)", "var_sys_9_0x8_0x98");
            CS = CS.Replace("sys_9(0x8, 0x99)", "var_sys_9_0x8_0x99");
            CS = CS.Replace("sys_9(0x8, 0x9a)", "var_sys_9_0x8_0x9a");
            CS = CS.Replace("sys_9(0x8, 0x9c)", "var_sys_9_0x8_0x9c");

            // Search for sys_74(0xd
            CS = Regex.Replace(CS, @"sys_74[(]0xd, 0x([\S]*?(?=,)), ([\S]*?(?=[)]))[)];", @"input_Supp_1(0x$1);");

            // First main func:
            CS = CS.Replace("sys_11(0, func_95);", 
                
            @"sys_11(0, func_95);

    // FB Change (Burst system):
    // This is to assign the awakenType only once (this func is not looped)
    // Every function and variables that is related to this BABB will always be resetted to 0 when you respawn
    // The flag is just to make sure that this will not be accidentally changed (probably impossible)
    if (alreadySetAwakenType == 0)
    {
        alreadySetAwakenType++;
        // Get the actual EX Type. In FB there's only 2 types, so the rest we need to manually change this awakenType using combos
        awakenType = sys_9(0x4, 0x1);
    }
    "
            );

            // func_1:
            CS = CS.Replace(
            
            @"void func_1()
{",

            @"void func_1()
{
    frameCount = sys_0(0x10000); // Get the frameCount after respawn
    // FB Change (Burst system):
    // Check frameCount so that we only allow the player to change at start of the game (before game start screen is done)
    // Check sys_0(0xe0009, 0xb) flag. If this is already set, we can't change it anymore, so player can only change it on game start, not during respawn.
    // Check awakenType if it is blast (0x1), which cannot activate this
    if (frameCount < 0x6000 && sys_0(0xe0009, 0xb) != 1 && awakenType != 0x1)
    {
        customBurstSystem_Condition();
    }
    else if (frameCount >= 0x8100)
    {
        sys_1(0xe0009, 0xb, 0x1); // One way set, will never return to 0 after initial set. 
    }

    if (awakenType == 0x1)
    {
        sys_1(0xe0009, 0xa, 0x1);
    }

    if (sys_0(0xe0009, 0xb) == 1 && awakenType != 0x1) // For respawn, we retrieve the awakenType again (unless you are blast burst)
    {
        awakenType = sys_0(0xe0009, 0xa); // Retrieve the awakenType from the temp memory storage
    }

    if (sys_0(0xe0009, 0xb) != 1 && (frameCount < 0x8000 && showBurstALEOFlag != 0x1) || changeBurstTypeFlag == 0x1)
    {
        if (awakenType == 0) // F
        {
            sys_6(0x2131A3C3, global212, 0, 0x3, 0xc);
        }
        else if (awakenType == 0x1) // S
        {
            sys_6(0xFDAE6D0A, global212, 0, 0x3, 0xc);
        }
        else if (awakenType == 0x2) // C
        {
            sys_6(0xB6F517C6, global212, 0, 0x3, 0xc);
        }
        else if (awakenType == 0x3) // M
        {
            sys_6(0x85F5B59A, global212, 0, 0x3, 0xc);
        }
        else if (awakenType == 0x4) // R
        {
            sys_6(0xBF7B8FF9, global212, 0, 0x3, 0xc);
        }

        changeBurstTypeFlag = 0;
        showBurstALEOFlag = 1;
    }

    if (frameCount > 0x8000 && hideBurstALEOFlag == 0 && sys_0(0xe0009, 0xb) != 1)
    {
        sys_35(0x3, 0xc, 0, 0x1);
        hideBurstALEOFlag = 0x1;
    }

    if (awakenType == 0)
    {
        var_e004b_0 = 0xa;
        var_e004b_0x1 = 0xf;
        var_e004b_0x2 = 0xa;
        var_e004b_0x3 = 0xa;
    }
    else
    {
        var_e004b_0 = 0;
        var_e004b_0x1 = 0;
        var_e004b_0x2 = 0;
        var_e004b_0x3 = 0;
    }

    if (global334 == 0x1)
    {
        var_e0051 = 0x1;
    }
    else
    {
        var_e0051 = 0;
    }

    //----------------------------------------------------
    "
            );

            // Probably only for new script, but add in nontheless
            CS = CS.Replace(

            @"    if (global5 == 0)
    {
        if (global6)
        {
            if (global7 >= 0)
            {
                func_70(global7, global8);
            }
            global6 = 0;
            global7 = 0xffffffff;
            global8 = 0x1;
        }
    }",

            @"    if (global5 == 0)
    {
        if (global6)
        {
            if (global7 >= 0)
            {
                func_70(global7, global8);
            }
            global6 = 0;
            global7 = 0xffffffff;
            global8 = 0x1;
        }
    }
    // FB Change:
    // Note this func, add input_supp reset "
            );

            // func_1 (to fix the blur bug):
            CS = CS.Replace(

            @"sys_1(0xf0009, global12, global13, global14);",

            @"    
    // FB Change:
    // This will cause to have weird camera effect on respawn after using S Burst shoot
    // global14 stores the cancel route from S Burst, not sure why it is used here.
    // if we make it 0 then it will not cause the bug.
    // sys_1(0xf009 is not found in original FB
    // sys_1(0xf0009, global12, global13, global14);
    sys_1(0xf0009, global12, global13, 0);
    "
            );

            // inputs
            if(isNewScript)
            {
                CS = Regex.Replace(CS, @"(\s+global81 = func_184[(]global22[)];(\r\n|\r|\n)+\s+global76 = 0;)(\r\n|\r|\n)+\s+(.*)",
            @"        
        global81 = func_184(global22);
        global76 = 0;

        input();
        var_sys_9_0x8();

        //$4");
            }
            else
            {
                CS = Regex.Replace(CS, @"(\s+global81 = func_184[(]global22[)];(\r\n|\r|\n)+\s+global76 = 0;)(\r\n|\r|\n)+\s+(.*)",
            @"        
        global81 = func_184(global22);
        global76 = 0;

        var_sys_9_0x8();
        
        $4");
            }

            // full burst +50hp
            CS = CS.Replace(

            @"                    global139 = 0x1;
                    global33 = 0xa;
                    global135 = 0x1;",

            @"
                    // FB Change:
                    if (sys_0(0x2000c))
                    {
                        sys_1(0xe0025, 0); // add 50HP on full EX Burst
                    }

                    global139 = 0x1;
                    global33 = 0xa;
                    global135 = 0x1;"
            );

            // Awakening Flags
            CS = CS.Replace(

            @"    global26 = sys_0(0xe003e);
    global145 = sys_0(0xe003f);
    global131 = sys_0(0xe003d);
    global146 = sys_0(0xe0049);",

            @"
    // FB Change
    // all these sys are not applicable
    /*
    global26 = sys_0(0xe003e);
    global145 = sys_0(0xe003f);
    global131 = sys_0(0xe003d); // determine if gun or sword?
    global146 = sys_0(0xe0049);
    */

    global26 = global139; // Check if in EX, same for global139
    global145 = 0; // Always 0 for some reason
    global131 = awakenType; // sys_9(0x4, 0x1); // Check the EX type. 0 = F (Assault), 1 = S (Blast), 2 = E (not applicable in FB)
    global146 = 0; // Always 0 for some reason
"
            );

            // Awakening Mobility
            CS = CS.Replace(

            @"        var7 = sys_0(0xe002e);
        if (var7 == 0x1)
        {
            var8 = 0xeca33688;
        }
        else if (var7 == 0x2)
        {
            var8 = 0x778726a3;
        }
        else
        {
            var8 = 0x51ac65a2;
        }
        var2 = var2 * sys_9(0x2, var8) / 0x64;",

            @"        var7 = sys_0(0xe002e);
        //
        /* FB Change: Mobility of unit in Bursts.
        if (var7 == 0x1)
        {
            var8 = 0xeca33688;
        }
        else if (var7 == 0x2)
        {
            var8 = 0x778726a3;
        }
        else
        {
            var8 = 0x51ac65a2;
        }
        */
        if (awakenType == 0) // F
        {
            var8 = 0xe48ecc9a; // Assault Burst Mobility Multiplier
            var2 = var2 * sys_9(0x2, var8) / 0x64;
        }
        else if (awakenType == 0x1) // S
        {
            var8 = 0xa02fe982; // Blast Burst Mobility Multiplier
            var2 = var2 * sys_9(0x2, var8) / 0x64;
        }
        else if (awakenType == 0x2) // C
        {
            var8 = 0x778726a3; // E Burst Mobility Multiplier
            var2 = var2 * sys_9(0x2, var8) / 0x64;
        }
        else if (awakenType == 0x3) // M
        {
            var2 = var2 * 125 / 0x64;
        }
        else if (awakenType == 0x4) // R
        {
            var8 = 0x778726a3; // E Burst Mobility Multiplier
            var2 = var2 * sys_9(0x2, var8) / 0x64;
        }"
            );

            // Raging Flags
            CS = CS.Replace(

            @"    global224 = 0;
    global225 = 0;
    global226 = 0;",

            @"    global224 = 0;
    global225 = 0;
    global226 = 0;

    // FB Change Raging Burst Flags:
    ragingMeleeFlag = 0;
    ragingShootFlag = 0;
    ragingMeleeContiniousFlag = 0;"
            );

            // Comment stuff (optional)
            CS = CS.Replace(

            @"    sys_21(0x1, 0x1770, 0xffffe69c, 0x2c24, 0xffffd3dc);",

            @"    sys_21(0x1, 0x1770, 0xffffe69c, 0x2c24, 0xffffd3dc);
    // FB Change:
    // Note this func, this is the parent func we use to write weapon data into memory"
            );

            // Raging Flag 2
            CS = CS.Replace(

            @"    global212 = sys_0(0x80004);",

            @"    // FB Change: New Flags for raging Burst
    ragingMeleeFlag = 0;
    ragingShootFlag = 0;
    ragingMeleeContiniousFlag = 0;

    global212 = sys_0(0x80004);"
            );

            // func_99
            CS = CS.Replace(

            @"void func_99()
{",

            @"void func_99()
{
    // FB Change
    raging_Burst_Check();
    write_Weapon_Type_Enum();
    add_Burst_Effects();
"
            );

            // random health decrement
            CS = CS.Replace(

            @"    sys_1(0xe0038, 0);",

            @"    // FB Change
    // sys_1(0xe0038, 0);"
            );

            // EX Burst Gauge reduce cancel
            CS = CS.Replace(

            @"void func_247()
{
    int var0;
    var0 = sys_9(0x4, 0x4);
    if (var0 > 0)
    {
        sys_1(0x30011, 0x7, var0);
    }
    if (sys_0(0x100003) == 0)
    {
        sys_1(0xe0035, 0);
    }
}",

            @"void func_247()
{
    int var0;
    var0 = sys_9(0x4, 0x4); // Retrieve EX Burst Atk EX Gauge reduce level.
    if (var0 > 0)
    {
        // FB Change:
        // In FB this will cause Blast Burst to deplete EX Gauge when EX Atk is used.
        // In MBON the var0 will always be 0.
        // sys_1(0x30011, 0x7, var0); // Reduce EX Gauge by var0
    }
    if (sys_0(0x100003) == 0)
    {
        sys_1(0xe0035, 0);
    }
}"
            );

            // EX Burst Boost Gauge reduce cancel
            CS = CS.Replace(

            @"void func_248()
{
    int var0;
    var0 = sys_9(0x4, 0x3);
    if (var0 > 0)
    {
        sys_1(0xe0014, 0x186a0);
    }
}",

            @"void func_248()
{
    int var0;
    var0 = sys_9(0x4, 0x3); // Retrieve EX Burst Boost Gauge reduce level.
    if (var0 > 0)
    {
        // FB Change:
        // In FB this will cause Blast Burst to deplete Boost Gauge when EX Atk is used.
        // In MBON the var0 will always be 0.
        // sys_1(0xe0014, 0x186a0); // Empty Boost Gauge
    }
}"
            );

            // S & F Bursts (func_266)
            CS = CS.Replace(

            @"int func_266(int arg0)
{
    if ((global26 == 0x1 || global27 == 0x1) && global218 == 0x1 && global75 == 0 && global139 == 0x1)
    {
        if (global221 == 0x1 && global381 == 0x1)
        {
            return 0;
        }
        if (global222 == 0x1 && !((global51 & 0x1) != 0))
        {
            return 0;
        }
        if ((global26 == 0x1 || global27 == 0x1) && global131 == 0 && global225 == 0)
        {
            if (global215 == 0 || arg0 >= global213 * 0x64)
            {
                func_262();
                global225 = 0x1;
            }
        }
        if ((global26 == 0x1 || global27 == 0x1) && global131 == 0x1 && global226 == 0)
        {
            if (global216 == 0 || arg0 >= global214 * 0x64)
            {
                func_263();
                global226 = 0x1;
            }
        }
        if (global225 == 0x1 && global226 == 0x1)
        {
            return 0x1;
        }
    }
    return 0;
}",

            @"int func_266(int arg0) // S or F burst cancel route 
{
    if ((global26 == 0x1 || global27 == 0x1) && global218 == 0x1 && global75 == 0 && global139 == 0x1)
    {
        if (global221 == 0x1 && global381 == 0x1)
        {
            return 0;
        }
        if (global222 == 0x1 && !((global51 & 0x1) != 0))
        {
            return 0;
        }
        // FB Change: (global131 == 0 || awakenType == 0x4)
        if ((global26 == 0x1 || global27 == 0x1) && (global131 == 0 || awakenType == 0x4) && global225 == 0)
        {
            if (global215 == 0 || arg0 >= global213 * 0x64)
            {
                // FB Change:
                ragingShootFlag = 0x1;

                if (global131 == 0)
                {
                    // F Burst Cancel Route
                    func_262();
                    global225 = 0x1;
                }
            }
        }
        // FB Change: (global131 == 0x1 || awakenType == 0x4)
        if ((global26 == 0x1 || global27 == 0x1) && (global131 == 0x1 || awakenType == 0x4) && global226 == 0)
        {
            if (global216 == 0 || arg0 >= global214 * 0x64)
            {
                // FB Change:
                ragingShootFlag = 0x1;

                if (global131 == 0x1)
                {
                    // S Burst Cancel Route
                    func_263();
                    global226 = 0x1;
                }
            }
        }
        if (global225 == 0x1 && global226 == 0x1)
        {
            return 0x1;
        }
    }
    return 0;
}"
            );

            // S & F Bursts (func_267)
            CS = CS.Replace(

            @"void func_267(int arg0, int arg1)
{
    global220 = arg0;
    if (arg1 == 0x1)
    {
        if ((global26 == 0x1 || global27 == 0x1) && global218 == 0x1)
        {
            if ((global26 == 0x1 || global27 == 0x1) && global131 == 0)
            {
                func_262();
            }
            if ((global26 == 0x1 || global27 == 0x1) && global131 == 0x1)
            {
                func_263();
            }
        }
    }
}",

            @"void func_267(int arg0, int arg1)
{
    global220 = arg0;
    if (arg1 == 0x1)
    {
        if ((global26 == 0x1 || global27 == 0x1) && global218 == 0x1)
        {
            // FB Change: (global131 == 0 || awakenType == 0x4)
            if ((global26 == 0x1 || global27 == 0x1) && (global131 == 0 || awakenType == 0x4))
            {
                // FB Change:
                ragingShootFlag = 0x1;

                if (global131 == 0)
                {
                    func_262();
                }
            }
            // FB Change: (global131 == 0x1 || awakenType == 0x4)
            if ((global26 == 0x1 || global27 == 0x1) && (global131 == 0x1 || awakenType == 0x4))
            {
                // FB Change:
                ragingShootFlag = 0x1;

                if (global131 == 0x1)
                {
                    func_263();
                }
            }
        }
    }
}"
            );


            // Melee var comments
            CS = CS.Replace(

            @"    if (arg0 == 0)
    {
        global509 = 0xaa;
        global510 = 0x5;
        global511 = 0x10e;
        global512 = 0xaa;
        global513 = 0x5;
        global514 = 0x10e;
        global515 = 0x12c;
        global516 = 0x1e;
        global517 = 0x3e8;
        global518 = 0x7d0;
        global519 = 0x62;
        global520 = 0xa;
        global521 = 0x23;
        global522 = 0xa;
        global523 = 0;
    }
    else
    {",

            @"    if (arg0 == 0)
    {
        global509 = 0xaa;
        global510 = 0x5;
        global511 = 0x10e;
        global512 = 0xaa;
        global513 = 0x5;
        global514 = 0x10e;
        global515 = 0x12c;
        global516 = 0x1e;
        global517 = 0x3e8;
        global518 = 0x7d0;
        global519 = 0x62;
        global520 = 0xa;
        global521 = 0x23;
        global522 = 0xa;
        global523 = 0;
    }
    else
    {
        // FB Change:
        // Parse Melee Var"
            );

            // func_299() M and S burst effect activations
            CS = CS.Replace(

            @"void func_299()
{
    sys_6(0x2e686593, global212, 0x1, 0x3, 0xa);
    sys_37(0, 0x3, 0xa, 0x64, 0xfffffe0c, 0);
    sys_37(0x1, 0x3, 0xa, 0x2328, 0, 0);
}",

            @"void func_299()
{
    // FB Change:
    if (awakenType == 0x3 && global139 == 0x1) // M Burst and in burst
    {
        if (M_Boost_ALEO_Elapsed_Frame > 0)
        {
            sys_35(0x4, 0xa, 0, 0x1);
        }

        M_Boost_ALEO_Elapsed_Frame = 1; // If the effect is already activated, activate it again and reset the flags. 
        sys_6(0x2a44a758, global212, 0x1, 0x4, 0xa);
        global241 = global212;
    }

    sys_6(0x2e686593, global212, 0x1, 0x3, 0xa);
    sys_37(0, 0x3, 0xa, 0x64, 0xfffffe0c, 0);
    sys_37(0x1, 0x3, 0xa, 0x2328, 0, 0);
}"
            );

            // Burst Mobility func_456()
            CS = CS.Replace(

            @"    var0 = sys_0(0x2000c);
    if (var0)
    {
        var1 = sys_9(0x4, 0x5);
    }
    else
    {
        var1 = sys_9(0x4, 0x7);
    }
    if (var1 > 0)
    {
        sys_1(0x30011, 0x8, var1);
    }",

            @"// FB Change:
    int burstGaugePercentage;
    var0 = sys_0(0x2000c); // If EX gauge is full (tech out)
    burstGaugePercentage = sys_0(0xe0009, 0xe); // read 14th reserve memory
    if (var0) // On Full Burst
    {
        // Get the EX gauge reduce amount for each burst.
        // var1 = sys_9(0x4, 0x5); 

        if (awakenType == 0)
        {
            // For fighting burst the reduction is only 30%
            // write 16th reserve memory, to set the flag for teching out of EX burst (reduce the boost increment)
            sys_1(0xe0009, 0x10, 2); // 1 = reduce by 50%, 2 = reduce by 30%
        }
        else
        {
            // write 16th reserve memory, to set the flag for teching out of EX burst (reduce the boost increment)
            sys_1(0xe0009, 0x10, 1); // 1 = reduce by 50%, 2 = reduce by 30%
        }

        if (awakenType == 0x2) // for case of C Burst, FB does not have any data for E Burst with sys_9(0x4, 0x5)
        {
            // In EXVS2XB, the amount reduced for C burst is always 60%
            var1 = burstGaugePercentage * 60; // It is actually burstGaugePercentage * 100 * 0.6, but we can just simplify it as 60
        }
        else
        {
            // In EXVS2XB, the amount reduced is fixed at 30%
            var1 = 0xBB8; // 3000
        }
    }
    else // On Half Burst
    {
        if (awakenType == 0x2) // for case of E Burst
        {
            // In EXVS2XB, the amount reduced for C burst is always 60%
            var1 = burstGaugePercentage * 60;
        }
        //var1 = sys_9(0x4, 0x7); // this does not work on FB
    }
    if (var1 > 0)
    {
        // FB Change:
        // MBON uses 0x8 instead of 0x7 to reduce the EX Gauge.
        // sys_1(0x30011, 0x8, var1);
        sys_1(0x30011, 0x7, var1);
    }"
            );

            // func_459 R Burst Flag
            CS = CS.Replace(

            @"void func_459()
{",

            @"void func_459()
{
    // FB Change:
    // To make sure raging burst Melee activates only when this template is used
    // This template is for normal Melee
    ragingMeleeFlag = 0x1;
"
            );

            // new script stuff
            if(isNewScript)
            {
                Match linkdande = Regex.Match(CS,
                    @"(void (func_[0-9]{1,100})[(][)])(\r\n|\r|\n)+{\s+int var0;(\r\n|\r|\n)+\s+int var1;(\r\n|\r|\n)+\s+var0 = sys_0[(]0x30014, 0[)];(\r\n|\r|\n)+\s+var1 = 0x1;(\r\n|\r|\n)+\s+while [(]var1 < var0[)](\r\n|\r|\n)+\s+{(\r\n|\r|\n)+\s+sys_2D[(]0x3. 0xd, var1, (func_[a-fA-F0-9]{1,100})[(](func_[0-9]{1,100})[(]var1, 0x[a-fA-F0-9]{1,100}[)][)][)];(\r\n|\r|\n)+\s+sys_2D[(]0x3, 0xe, var1. (func_[0-9]{1,100})[(](func_[0-9]{1,100})[(]var1. 0x[a-fA-F0-9]{1,100}[)][)][)];");

                string linkdandefunc = linkdande.Groups[2].Captures[0].Value;

                CS = CS.Replace(@"void " + linkdandefunc + @"()
{",
                @"/// <summary>
/// Manually link info of sets d and e (sometimes f) through a loop.
/// </summary>
void " + linkdandefunc + @"()
{
    // This func is the condensed version of func_713 in old FB script."
                );



                string linkdandefuncplus1 = linkdandefunc.TrimStart("func_".ToCharArray());
                if (!uint.TryParse(linkdandefuncplus1, out uint linkdandefuncplus1int))
                    throw new Exception();

                linkdandefuncplus1int += 1;

                CS = CS.Replace(@"void func_" + linkdandefuncplus1int + @"()
{",
                @"/// <summary>
/// Writing weapon data into memory stream, allowing then to be fetched.
/// </summary>
void func_" + linkdandefuncplus1int + @"()
{
    // For some reason sys_74(0); is needed before add_B4AC();
    sys_74(0);
    // The custom made add_B4AC func generated from MBON's 011.bin
    add_B4AC();
    // For some reason sys_74(0x2); is needed before linking func to set d and e;
    sys_74(0x2);"
                );

                uint linkdandefuncplus3int = linkdandefuncplus1int + 2;

                CS = Regex.Replace(CS,
                    @"\s+ (func_[0-9]{1,100}[(][)];)(\r\n|\r|\n)+\s+sys_1[(]0x60005, global306[)];(\r\n|\r|\n)+}(\r\n|\r|\n)+(\r\n|\r|\n)+void func_" + linkdandefuncplus3int + @"[(][)]",
                    @"

    // FB Change:
    input_Supp_2(); // Reset the input freeze flag

    $1
    sys_1(0x60005, global306);
}

void func_" + linkdandefuncplus3int + @"()
"
                    );



                CS = CS.Replace(@"var0 = sys_0(0x30014, 0);", @"// FB Change: 
    // Need to manually specify the number of data sets for weapons. Check MBON's data 011.bin's 0x20 int32.
    var0 = 0x" + B4ACCount.ToString("X") +"; // sys_0(0x30014, 0); - Probably used for knowing how many data sets are there in 012.bin.");


                //Match input_supp_reset_func_match = Regex.Match(CS, @"[/][/] Note this func, add input_supp reset (\r\n|\r|\n)+\s+(func_[0-9]{1,10}[(][)]);");
                //string input_supp_reset_func = input_supp_reset_func_match.Groups[2].Captures[0].Value;

                Regex.Replace(CS, @"\s+func_([0-9]{1,100})[(][)];(\r\n|\r|\n)+\s+sys_1[(]0x60005, global306[)];", @"
    // FB Change:
    input_Supp_2(); // Reset the input freeze flag

    func_$1();
    sys_1(0x60005, global306);");

                /*
                CS = Regex.Replace(CS,
                    @"\s+else if [(]var6 == 0x3[)](\r\n|\r|\n)+\s+{(\r\n|\r|\n)+\s+func_138[(](func_[0-9]{1,100})",
                    @"    else if (var6 == 0xc)
    {
        // follow for extra_B4AC
        func_138($3, ");
                */


                CS = CS.Replace("    var2 = sys_0(0x30013, 0x1, arg0, 0x1);",
                @"    // FB Change:
    // sys_0(0x30013, 0....) is the subsitute to sys_2C(0x3, 0x11 + arg0 - 0x1, arg1)
    // For sys_0(0x30013, 0x1....) however, is new as it is used to read the """"extra"""" variables after the B4AC main 0x80 batch.
    // In 011.bin (or 012.bin), the start of the 0x80 section is always denoted by A8 BB BA B9, followed by number of sets and variable length, and padded to 0x10.
    // After this section, the """"extra"""" bits of the variable is denoted by A8 BA A9 BA, with the same info appended to the back.
    // For Infinite Justice Boss Meteor, there is 0xF9 sets, and each set will contain 0x23 variables.
    // Since EBOOT does not allow us to exceed the 0x20 set count, we cannot add the """"extra"""" variables to 0x81th and so on.
    // Thus the only way is to create more """"sets"""" to be read, and retrieve the variable using the same sys_2C method.
    // This func is mainly used for determining the variable flag to trigger specific func to be used by each weapon (which is usually the 4th variable in this case).
    var2 = sys_0(0x30013, 0x1, arg0, 0x1);");

                CS = Regex.Replace(CS, @"sys_0[(]0x30013, 0x1, arg0, 0x([a-fA-F0-9]{1,2})+[)]",
                @"parse_Extra_B4AC_0x$1(arg0)");

                CS = CS.Replace(@"        var2 = sys_0(0x30013, 0, arg0, arg1);",
                    @"        // FB Change:
        /// <summary>
        /// Read data from the weapon datasets.
        /// </summary>
        /// <param name=""""arg0"""" > nth data set to be retrieved</param>
        /// <param name=""""arg1"""" > nth data to be retrieved</param>
        /// 
        // Instead of using sys_0(0x30013), we revert back to usual sys_2C retrieval method.
        // sys_2C(0x3, nth_data_set, nth_data);
        // var2 = sys_0(0x30013, 0, arg0, arg1);
        var2 = sys_2C(0x3, 0x11 + arg0 - 0x1, arg1);"
                    );

                CS = CS.Replace(@"    var3 = sys_74(0x9, var2, 0x1 << global306);",
                    @"    // FB Change:
    // MBON uses 0x1 << global306 instead of global306 directly, which causes Melee to never hasei into anything.
    // sys_74(0x9, weapon_ID, unit_mode_flag) -> return the nth data set that has this weapon ID.
    var3 = sys_74(0x9, var2, global306); // sys_74(0x9, var2, 0x1 << global306);");

                CS = CS.Replace(@"    var2 = sys_74(0x9, arg1, 0x1 << global306);",
    @"    // FB Change:
    // MBON uses 0x1 << global306 instead of global306 directly, which causes Melee to never hasei into anything.
    // sys_74(0x9, weapon_ID, unit_mode_flag) -> return the nth data set that has this weapon ID.
    var2 = sys_74(0x9, arg1, global306); // sys_74(0x9, var2, 0x1 << global306);");
            }

            // EX Burst Multipliers
            CS = CS.Replace(@"sys_0(0xe0040)", "0");
            CS = CS.Replace(@"sys_0(0xe0041)", "0");
            CS = CS.Replace(@"sys_0(0xe0042)", "0");

            CS = CS.Replace(@"sys_0(0xe0051)", "1");
            CS = CS.Replace(@"sys_0(0xe0053)", "0");

            CS = CS.Replace(@"sys_0(0xe0051, global334)", "var_e0051");
            CS = CS.Replace(@"sys_0(0xe004b, 0)", "var_e004b_0");
            CS = CS.Replace(@"sys_0(0xe004b, 0x1)", "var_e004b_0x1");
            CS = CS.Replace(@"sys_0(0xe004b, 0x2)", "var_e004b_0x2");
            CS = CS.Replace(@"sys_0(0xe004b, 0x3)", "var_e004b_0x3");

            CS = Regex.Replace(CS, @"sys_0[(]0x30012, (.+), 0x([a-fA-F0-9]{1,8})+[)]",
            @"parse_Melee_Var($1, 0x$2)");


            CS = CS.Replace(@"[];", @"");

            CS = CS.Replace(@"[]", @"");

            CS = CS.Replace(@"None", @"int");

            // Melee use ammo
            CS = Regex.Replace(CS, @"sys_28[(](0x[a-fA-F0-9]{1,100}), 0, 0x1[)];",
            @"sys_28($1, 0x1184a19f);");

            //----------------------------------------------------- Ammo --------------------------------------------------------
            CS = CS.Replace(
            @"        else if (var0 == 0x3)
        {
            var1 = 0x3;
        }
        else
        {
            var1 = 0x4;
        }",

            @"        // FB Change: There's no 5th ammo so there no need for this else if
        /*
        else if (var0 == 0x3)
        {
            var1 = 0x3;
        }
        */
        else
        {
            //var1 = 0x4;
            var1 = 0x3;
            // Maximum number is 4 slots
        }"
            );

            // 5th ammo slot swap
            CS = CS.Replace(@"sys_2B(0x4", @"//sys_2B(0x4");

            return CS;
        }
    }
}
