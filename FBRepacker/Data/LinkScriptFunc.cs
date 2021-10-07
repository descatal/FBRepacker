using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FBRepacker.Data
{
    class LinkScriptFunc : Internals
    {
        public LinkScriptFunc()
        {
            List<uint> funcPointers = readBABB(Properties.Settings.Default.BABBFilePath, Properties.Settings.Default.scriptBigEndian);

            string CS = File.ReadAllText(Properties.Settings.Default.CScriptFilePath);
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

            AhoCorasick.Trie trie = new AhoCorasick.Trie();

            funcPointers.Sort();

            for (int i = 0; i < funcPointers.Count; i++)
            {
                if(funcPointers[i] > Properties.Settings.Default.MinScriptPointer)
                {
                    string funcPointerHex = "0x" + funcPointers[i].ToString("X");
                    trie.Add(funcPointerHex.ToLower());
                }
            }
            trie.Build();

            Dictionary<string, string> addedWord = new Dictionary<string, string>();
            foreach (string word in trie.Find(CS))
            {
                if (!addedWord.Keys.Contains(word))
                {
                    uint.TryParse(word.Remove(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint funcPointer);
                    int funcNumber = funcPointers.IndexOf(funcPointer);
                    string funcStr = "func_" + funcNumber;
                    addedWord[word] = funcStr;
                }
            }

            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.CScriptFilePath);
            StreamWriter replacedCScript = File.CreateText(Properties.Settings.Default.outputScriptFolderPath + @"\" + fileName + ".c");
            string log = string.Empty;
            foreach(var word in addedWord)
            {
                CS = CS.Replace(word.Key, word.Value);
                log += (word.Key + " - " + word.Value);
                log += Environment.NewLine;
            }

            replacedCScript.Write(CS);
            StreamWriter logFile = File.CreateText(Properties.Settings.Default.outputScriptFolderPath + @"\" + fileName + "-link_log.txt");
            logFile.Write(log);
            logFile.Close();
            MessageBox.Show("Replaced lines: " + Environment.NewLine + log, "Link Complete", MessageBoxButton.OK);

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
    }
}
