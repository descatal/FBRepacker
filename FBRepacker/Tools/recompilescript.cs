using FBRepacker.Data.DataTypes;
using FBRepacker.Data.FB_Parse.DataTypes;
using FBRepacker.PAC;
using FBRepacker.PAC.Repack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FBRepacker.Tools
{
    internal class recompilescript : Internals
    {
        string totalMBONExportFolder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Export";
        string XBScriptFolder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\XB Project\XB Script";
        string XBCombinedPsarcFolder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\XB Project\XB Combined Psarc";
        string XBReimportFolder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\XB Project\XB Units";
        string repackTemplates = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Repack Templates";

        public recompilescript()
        {
            List<string> allUnitFolders = Directory.GetDirectories(totalMBONExportFolder, "*", SearchOption.TopDirectoryOnly).ToList();

            string json = File.OpenText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(json);

            foreach (string unitFolder in allUnitFolders)
            {
                string unitFolderName = Path.GetFileName(unitFolder.TrimEnd(Path.DirectorySeparatorChar));

                string extractMBONFolder = unitFolder + @"\Extracted MBON";

                string reimportFolder = XBReimportFolder + @"\" + unitFolderName;
                string reimportConvertedfromMBONFolder = XBReimportFolder + @"\" + unitFolderName + @"\" + "Converted from MBON";
                string reimportFilestoRepack = XBReimportFolder + @"\" + unitFolderName + @"\" + "Files to Repack";
                string reimportRepackedFiles = XBReimportFolder + @"\" + unitFolderName + @"\" + "Repacked Files";

                Match unitNoMatch = Regex.Match(unitFolder, @"([0-9]{1,100}). ");
                string unitNoStr = unitNoMatch.Groups[0].Captures[0].Value;
                uint.TryParse(unitNoStr, out uint unitNo);

                int unit_ID_str_index = unitFolderName.IndexOf("- ");
                string unit_ID_str = string.Empty;
                if (unit_ID_str_index >= 0)
                    unit_ID_str = unitFolderName.Substring(unit_ID_str_index + 2, unitFolderName.Length - unit_ID_str_index - 2);

                uint unit_ID = Convert.ToUInt32(unit_ID_str);
                Unit_Files_List unit_Files = unit_Files_List.FirstOrDefault(x => x.Unit_ID == unit_ID);

                if (unit_ID < 59900 && unit_Files != null && unit_ID == 16111)
                {
                    Directory.CreateDirectory(reimportFolder);
                    Directory.CreateDirectory(reimportConvertedfromMBONFolder);
                    Directory.CreateDirectory(reimportFilestoRepack);
                    Directory.CreateDirectory(reimportRepackedFiles);

                    List<string> script1Folder = Directory.GetDirectories(extractMBONFolder, "*", SearchOption.TopDirectoryOnly).ToList();
                    script1Folder = script1Folder.Where(x => x.Contains("Script 1")).ToList();
                    if (script1Folder.Count() == 0 || script1Folder.Count() > 0x1)
                        throw new Exception();

                    List<string> dataFolder = Directory.GetDirectories(extractMBONFolder, "*", SearchOption.TopDirectoryOnly).ToList();
                    dataFolder = dataFolder.Where(x => x.Contains("Data")).ToList();
                    if (dataFolder.Count() == 0 || dataFolder.Count() > 0x1)
                        throw new Exception();

                    string script1 = script1Folder[0];

                    string data = dataFolder[0];

                    // -------------------------------------------- Script Refactor --------------------------------------------
                    Properties.Settings.Default.BABBFilePath = script1 + @"\001-FHM\002.bin";
                    Properties.Settings.Default.outputScriptFolderPath = XBScriptFolder + @"\Refactored Script";

                    Properties.Settings.Default.scriptBigEndian = false;
                    Properties.Settings.Default.CScriptFilePath = XBScriptFolder + @"\Script\" + unitFolderName + @".c";
                    Properties.Settings.Default.MinScriptPointer = 100000;

                    compileMSCwithFix(unitFolderName);

                    repackData(
                        reimportRepackedFiles,
                        reimportFilestoRepack,
                        unitFolderName,
                        data,
                        reimportConvertedfromMBONFolder,
                        unit_Files
                        );
                }
            }
        }

        public void compileMSCwithFix(string unitFolderName)
        {
            string inputCFilePath = XBScriptFolder + @"\Refactored Script\" + unitFolderName + ".c";
            string outputCFilePath = XBScriptFolder + @"\Compiled Refactored Script\" + unitFolderName + ".mscsb";

            string CS = File.ReadAllText(inputCFilePath);
            int reset_0x2E_Count = 0;

            if (CS.Contains(@"sys_2D(0x3, 0xd, var1, func_"))
                reset_0x2E_Count++;

            if (CS.Contains(@"sys_2D(0x3, 0xe, var1, func_"))
                reset_0x2E_Count++;

            if (CS.Contains(@"sys_2D(0x3, 0xf, var1, func_"))
                reset_0x2E_Count++;

            compileMSC(inputCFilePath, outputCFilePath);

            if (reset_0x2E_Count != 0)
            {
                FileStream fs = File.OpenRead(outputCFilePath);
                MemoryStream oms = new MemoryStream();
                fs.Seek(0, SeekOrigin.Begin);
                fs.CopyTo(oms);
                fs.Close();
                oms.Seek(0, SeekOrigin.Begin);

                int fix_Position = Search(oms, new byte[] { 0x8A, 0x00, 0x00, 0x00, 0x03, 0x8A, 0x00, 0x00, 0x00, 0x0D, 0x8B, 0x00, 0x00, 0x01, });
                if (fix_Position == -1)
                    throw new Exception();

                oms.Seek(fix_Position + 14, SeekOrigin.Begin);

                for (int i = 0; i < reset_0x2E_Count; i++)
                {
                    int fix_Position_1 = Search(oms, new byte[] { 0x2E }, (int)oms.Position);
                    if (fix_Position_1 == -1)
                        throw new Exception();

                    oms.Seek(fix_Position_1, SeekOrigin.Begin);
                    oms.Write(new byte[] { 0xAE }, 0, 1);
                }

                oms.Seek(0, SeekOrigin.Begin);

                FileStream ofs = File.OpenWrite(outputCFilePath);
                oms.CopyTo(ofs);
                ofs.Close();
                oms.Close();
            }
        }

        public void repackData(
            string reimportRepackedFiles,
            string reimportFilestoRepack,
            string unitFolderName,
            string data,
            string reimportConvertedfromMBONFolder,
            Unit_Files_List unit_Files
            )
        {
            RepackPAC repackInstance = new RepackPAC(Properties.Settings.Default.OutputRepackPAC);
            Properties.Settings.Default.OutputRepackPAC = reimportRepackedFiles;

            string data_folder_path = reimportFilestoRepack + @"\Data - " + unit_Files.data_and_script_PAC_hash.ToString("X8");

            Directory.CreateDirectory(data_folder_path);

            /// Repack Data Folder
            DirectoryCopy(repackTemplates + @"\Data", data_folder_path, true);

            string data_001FHM_path = data_folder_path + @"\001-FHM\";

            FileStream fs002 = File.Create(data_001FHM_path + @"\002.bin");
            FileStream vardataFS = File.OpenRead(reimportConvertedfromMBONFolder + @"\Unit Variables\UnitData.bin");
            vardataFS.Seek(0, SeekOrigin.Begin);
            vardataFS.CopyTo(fs002);
            fs002.Close();
            vardataFS.Close();

            FileStream fs003 = File.Create(data_001FHM_path + @"\003.bin");
            string customHitboxBinaryFS = reimportConvertedfromMBONFolder + @"\Hitbox_Properties\Hitbox_Properties.bin";

            FileStream MBON003FS = File.OpenRead(data + @"\001-FHM\003.bin");

            if (File.Exists(customHitboxBinaryFS))
            {
                MBON003FS.Close();
                MBON003FS = File.OpenRead(customHitboxBinaryFS);
            }

            MBON003FS.Seek(0, SeekOrigin.Begin);
            MBON003FS.CopyTo(fs003);
            fs003.Close();
            MBON003FS.Close();

            FileStream fs005 = File.Create(data_001FHM_path + @"\005.bin");
            FileStream MBON005FS = File.OpenRead(data + @"\001-FHM\005.bin");

            string customMobilityBinaryFS = reimportConvertedfromMBONFolder + @"\Mobility_Properties\Mobility_Properties.bin";

            if (File.Exists(customMobilityBinaryFS))
            {
                MBON005FS.Close();
                MBON005FS = File.OpenRead(customMobilityBinaryFS);
            }

            MBON005FS.Seek(0, SeekOrigin.Begin);
            MBON005FS.CopyTo(fs005);
            fs005.Close();
            MBON005FS.Close();

            FileStream fs006 = File.Create(data_001FHM_path + @"\006.bin");
            FileStream mscFS = File.OpenRead(XBScriptFolder + @"\Compiled Refactored Script\" + unitFolderName + ".mscsb");
            mscFS.Seek(0, SeekOrigin.Begin);
            mscFS.CopyTo(fs006);
            fs006.Close();
            mscFS.Close();

            FileStream fs008 = File.Create(data_001FHM_path + @"\008.bin");
            FileStream voicelogicFS = File.OpenRead(reimportConvertedfromMBONFolder + @"\Voice Data\006.bin");
            voicelogicFS.Seek(0, SeekOrigin.Begin);
            voicelogicFS.CopyTo(fs008);
            fs008.Close();
            voicelogicFS.Close();

            Properties.Settings.Default.OpenRepackPath = data_folder_path;

            repackInstance.initializePACInfoFileRepack();
            repackInstance.parseInfo();
            repackInstance.repackPAC();

            /*
            // Get unit's english name
            UnitIDList unit_Infos = load_UnitID();
            string unitName = unit_Infos.Unit_ID.FirstOrDefault(s => s.id == unit_Files.Unit_ID).name_english.Replace(" ", "_");
            unitName = unitName.Replace(".", "_");
            unitName = unitName.Replace("∀", "Turn_A");
            unitName = unitName.Replace("ä", "a");

            string basePsarcRepackFolder = XBCombinedPsarcFolder + @"\Units\FB_Units\" + unitName;
            string[] allRepackedPACs = Directory.GetFiles(reimportRepackedFiles, "*", SearchOption.TopDirectoryOnly);


            string Data_Path = basePsarcRepackFolder + @"\Data\PATCH" + unit_Files.data_and_script_PAC_hash.ToString("X8") + ".PAC";

            FileStream dataFS = File.OpenRead(allRepackedPACs.FirstOrDefault(s => s.Contains("Data")));

            dataFS.Seek(0, SeekOrigin.Begin);

            FileStream newDataFS = File.Create(Data_Path);

            dataFS.CopyTo(newDataFS);

            dataFS.Close();

            newDataFS.Close();
            */
        }
    }
}
