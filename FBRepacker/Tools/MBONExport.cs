using FBRepacker.Data.DataTypes;
using FBRepacker.Data.FB_Parse.DataTypes;
using FBRepacker.PAC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Tools
{
    class MBONBinaries
    {
        public uint unitID { get; set; }
        public List<uint> binaryHashes { get; set; }

        public MBONBinaries()
        {
            binaryHashes = new List<uint>();
        }
    }

    class MBONExport : Internals
    {
        public MBONExport()
        {
            //extractScript();
            extractNPCSounds();
        }

        public void extractNPCImages()
        {
            string totalMBONExportFolder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Export";
            List<string> allUnitFolders = Directory.GetDirectories(totalMBONExportFolder, "*", SearchOption.TopDirectoryOnly).ToList();

            string json = File.OpenText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(json);

            json = File.OpenText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Unit List MBON.json").ReadToEnd();
            List<Unit_Info_List> unit_Info_List = JsonConvert.DeserializeObject<List<Unit_Info_List>>(json);

            List<string> allbossunitimagesfolder = Directory.GetDirectories(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\All Boss Unit Image & Sound Effects", "*", SearchOption.TopDirectoryOnly).ToList();

            foreach (string unitFolder in allUnitFolders)
            {
                string unitFolderName = Path.GetFileName(unitFolder.TrimEnd(Path.DirectorySeparatorChar));

                int unit_ID_str_index = unitFolderName.IndexOf("- ");
                string unit_ID_str = string.Empty;
                if (unit_ID_str_index >= 0)
                    unit_ID_str = unitFolderName.Substring(unit_ID_str_index + 2, unitFolderName.Length - unit_ID_str_index - 2);

                uint unit_ID = Convert.ToUInt32(unit_ID_str);
                Unit_Files_List unit_Files = unit_Files_List.FirstOrDefault(x => x.Unit_ID == unit_ID);
                Unit_Info_List unit_Infos = unit_Info_List.FirstOrDefault(x => x.unit_ID == unit_ID);

                if (unit_ID >= 0x13880 && unit_ID <= 0x13a00)
                {
                    if (unit_Files != null && unit_Files.MBONAdded) // Bosses
                    {
                        // Sound Effects
                        string BossFolder = allbossunitimagesfolder.FirstOrDefault(s => s.Contains(unit_ID.ToString()));

                        string arcadeUnitSpriteFolder = Directory.GetDirectories(BossFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Arcade Sprite"));
                        string arcadeUnitSprite = arcadeUnitSpriteFolder + @"\001-MBON\002.dds";

                        string arcadePilotSpriteFolder = Directory.GetDirectories(BossFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Arcade Sprite"));
                        string arcadePilotSprite = arcadePilotSpriteFolder + @"\001-MBON\002.dds";

                        if (!File.Exists(arcadeUnitSprite))
                            throw new Exception();

                        string extractedExportArcadeFolder = unitFolder + @"\Extracted MBON\Arcade_Sprites - " + unit_Infos.arcade_selection_sprite_costume_1_hash;
                        string extractedExportFreeBattleSelectionFolder = unitFolder + @"\Extracted MBON\Free_Battle_Selection_Sprite_Costume_1 - " + unit_Infos.free_battle_selection_sprite_costume_1_hash;
                        string extractedExportInGameSortieFolder = unitFolder + @"\Extracted MBON\In_Game_Sortie_and_Awakening_Sprite_Costume_1 - " + unit_Files.sortie_and_awakening_sprites_PAC_hash;
                        string extractedExportLoadingAllyFolder = unitFolder + @"\Extracted MBON\Loading_Ally_Sprite_Costume_1 - " + unit_Infos.loading_ally_sprite_costume_1_hash;
                        string extractedExportLoadingEnemyFolder = unitFolder + @"\Extracted MBON\Loading_Enemy_Sprite_Costume_1 - " + unit_Infos.loading_enemy_sprite_costume_1_hash;
                        string extractedExportLoadingEnemyTargetPilotFolder = unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Pilot_Sprite_Costume_1 - " + unit_Infos.loading_enemy_target_pilot_sprite_costume_1_hash;
                        string extractedExportLoadingEnemyTargetUnitFolder = unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Unit_Sprite_Costume_1 - " + unit_Infos.loading_enemy_target_unit_sprite_costume_1_hash;
                        string extractedExportFigurineSpriteFolder = unitFolder + @"\Extracted MBON\Figurine_Sprite - " + unit_Infos.figurine_sprite_hash;
                        string extractedExportResultSmallSpriteFolder = unitFolder + @"\Extracted MBON\Result_Small_Sprite - " + unit_Infos.result_small_sprite_hash;
                        string extractedExportTargetSmallSpriteFolder = unitFolder + @"\Extracted MBON\Target_Small_Sprite - " + unit_Infos.figurine_sprite_hash;

                        Directory.CreateDirectory(extractedExportArcadeFolder);
                        Directory.CreateDirectory(extractedExportFreeBattleSelectionFolder);
                        Directory.CreateDirectory(extractedExportInGameSortieFolder);
                        Directory.CreateDirectory(extractedExportLoadingAllyFolder);
                        Directory.CreateDirectory(extractedExportLoadingEnemyFolder);
                        Directory.CreateDirectory(extractedExportLoadingEnemyTargetPilotFolder);
                        Directory.CreateDirectory(extractedExportLoadingEnemyTargetUnitFolder);
                        Directory.CreateDirectory(extractedExportFigurineSpriteFolder);
                        Directory.CreateDirectory(extractedExportResultSmallSpriteFolder);
                        Directory.CreateDirectory(extractedExportTargetSmallSpriteFolder);

                        resize_dds_precise(arcadeUnitSprite, extractedExportArcadeFolder + @"\arcade_unit.dds", 0, true, 1280, 720);
                    }
                }
            }
        }

        public void extractNPCSounds()
        {
            string totalMBONExportFolder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Export";
            List<string> allUnitFolders = Directory.GetDirectories(totalMBONExportFolder, "*", SearchOption.TopDirectoryOnly).ToList();

            string json = File.OpenText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(json);

            List<string> allbossunitsoundeffectsfolder = Directory.GetDirectories(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\All Boss Unit Image & Sound Effects", "*", SearchOption.TopDirectoryOnly).ToList();

            foreach (string unitFolder in allUnitFolders)
            {
                string unitFolderName = Path.GetFileName(unitFolder.TrimEnd(Path.DirectorySeparatorChar));

                int unit_ID_str_index = unitFolderName.IndexOf("- ");
                string unit_ID_str = string.Empty;
                if (unit_ID_str_index >= 0)
                    unit_ID_str = unitFolderName.Substring(unit_ID_str_index + 2, unitFolderName.Length - unit_ID_str_index - 2);

                uint unit_ID = Convert.ToUInt32(unit_ID_str);
                Unit_Files_List unit_Files = unit_Files_List.FirstOrDefault(x => x.Unit_ID == unit_ID);

                if(unit_ID >= 0x13880 && unit_ID <= 0x13a00)
                {
                    if (unit_Files != null && unit_Files.MBONAdded) // Bosses
                    {
                        // Sound Effects
                        string BossFolder = allbossunitsoundeffectsfolder.FirstOrDefault(s => s.Contains(unit_ID.ToString()));
                        string SEFolder = Directory.GetDirectories(BossFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Sound Effect"));
                        string SE = SEFolder + @"\001-MBON\002.nus3bank";

                        if (!File.Exists(SE))
                            throw new Exception();

                        string reimportSEFolder = unitFolder + @"\Extracted MBON\Sound Effects";

                        Directory.CreateDirectory(reimportSEFolder);

                        File.Copy(SE, unitFolder + @"\Extracted MBON\Sound Effect.nus3bank", true);

                        convertNUS3toWav(SE, reimportSEFolder);

                        // Local Voice Files

                        List<string> nus3AudioVoiceFiles = Directory.GetFiles(unitFolder + @"\Extracted MBON", "*.nus3audio", SearchOption.TopDirectoryOnly).ToList();
                        string LocalVoiceFile = nus3AudioVoiceFiles.FirstOrDefault(s => Path.GetFileName(s).Contains("Local Voice Files"));

                        if (!File.Exists(LocalVoiceFile))
                            throw new Exception();

                        string reimportLVFFolder = unitFolder + @"\Extracted MBON\Local Voice Files";

                        Directory.CreateDirectory(reimportLVFFolder);

                        convertNUS3toWav(LocalVoiceFile, reimportLVFFolder);


                        // Global Voice Files

                        string GlobalVoiceFile = nus3AudioVoiceFiles.FirstOrDefault(s => Path.GetFileName(s).Contains("Global Voice Files"));

                        if (!File.Exists(GlobalVoiceFile))
                            throw new Exception();

                        string reimportGVFFolder = unitFolder + @"\Extracted MBON\Global Voice Files";

                        Directory.CreateDirectory(reimportGVFFolder);

                        convertNUS3toWav(GlobalVoiceFile, reimportGVFFolder);
                    }
                }
            }
        }

        public void extractScript()
        {
            List<string> Files_002 = Directory.GetFiles(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Export", "002.bin", SearchOption.AllDirectories).ToList();
            foreach(var script_Check in Files_002)
            {
                if (script_Check.Contains("Script 1"))
                {
                    FileStream fs = File.OpenRead(script_Check);
                    string gundamName = Path.GetFileName(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(script_Check).FullName).FullName).FullName).FullName);
                    string binaryCopyFolder = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Script\Binaries";
                    string outputScriptFolder = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Script\Script";
                    string funcPointerTxt = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Script\Script func pointers";

                    FileStream ofs = File.Create(binaryCopyFolder + @"\" + gundamName + ".bin");
                    fs.CopyTo(ofs);
                    fs.Close();
                    ofs.Close();

                    string mscdecexeSource = Path.Combine(Directory.GetCurrentDirectory(), @"3rd Party\mscdec\mscdec.exe");

                    string args = @"""" + script_Check + @"""" + " -o " + @"""" + outputScriptFolder + @"\" + gundamName + ".c" + @"""";

                    using (Process mscdec = new Process())
                    {
                        mscdec.StartInfo.FileName = mscdecexeSource;
                        mscdec.StartInfo.UseShellExecute = false;
                        mscdec.StartInfo.RedirectStandardOutput = true;
                        mscdec.StartInfo.CreateNoWindow = true;
                        mscdec.StartInfo.Arguments = args;
                        mscdec.Start();
                        string logOutput = mscdec.StandardOutput.ReadToEnd();
                        StreamWriter otxt = File.CreateText(funcPointerTxt + @"\" + gundamName + ".txt");
                        otxt.Write(logOutput);
                        otxt.Close();
                        mscdec.WaitForExit();
                    }
                }
            }

        }

        public void extractBin()
        {
            List<string> binaryFiles = Directory.GetFiles(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Export", "*.bin", SearchOption.AllDirectories).ToList();

            foreach (var binary in binaryFiles)
            {
                string gundamFolder = Directory.GetParent(Directory.GetParent(binary).FullName).FullName;
                string extractFolder = gundamFolder + @"\Extracted MBON";
                string filePath = binary;

                FileStream stream = File.Open(filePath, FileMode.Open);
                long streamSize = stream.Length;
                stream.Close();

                string baseExtractPath = extractFolder + @"\" + Path.GetFileNameWithoutExtension(filePath);

                new PAC.Extract.ExtractPAC(filePath, stream).extractPAC(0, out long unused, baseExtractPath);
            }

        }


        public void ExportBin()
        {
            UnitIDList unitIDList = load_UnitID();

            FileStream fs = File.OpenRead(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\big_endian_list.bin");

            List<MBONBinaries> BinaryList = new List<MBONBinaries>();

            for (int j = 0; j < 356; j++)
            {
                MBONBinaries mBONBinaries = new MBONBinaries();

                for (int i = 0; i < 15; i++)
                {
                    if (i == 0)
                    {
                        mBONBinaries.unitID = readUIntBigEndian(fs);
                    }
                    else
                    {
                        mBONBinaries.binaryHashes.Add(readUIntBigEndian(fs));
                    }
                }

                BinaryList.Add(mBONBinaries);
            }

            fs.Close();

            for (int i = 0; i < BinaryList.Count; i++)
            {
                MBONBinaries binary = BinaryList[i];
                UnitID unit = unitIDList.Unit_ID.FirstOrDefault(x => x.id == binary.unitID);
                string unitName = "";
                if (unit != null)
                {
                    unitName = unit.name_english + " - " + unit.id.ToString();
                }
                else
                {
                    unitName = "Unknown - " + binary.unitID.ToString();
                }
                string unitFolder = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Export\" + (i + 1).ToString() + ". " + unitName;

                Directory.CreateDirectory(unitFolder);
                Directory.CreateDirectory(unitFolder + @"\Original MBON");
                Directory.CreateDirectory(unitFolder + @"\Extracted MBON");

                List<string> junkBin = Directory.GetFiles(@"D:\MBON\Image0\archives\", binary.binaryHashes[0].ToString("X8") + ".bin", SearchOption.AllDirectories).ToList();

                if (junkBin.Count == 0 || junkBin.Count > 1)
                    throw new Exception();

                copyBinarywithout0x10000(junkBin[0], unitFolder + @"\Original MBON\" + "Junk - " + binary.binaryHashes[0].ToString("X8") + ".bin");

                List<string> dataBin = Directory.GetFiles(@"D:\MBON\Image0\archives\", binary.binaryHashes[1].ToString("X8") + ".bin", SearchOption.AllDirectories).ToList();

                if (dataBin.Count == 0 || dataBin.Count > 1)
                    throw new Exception();

                copyBinarywithout0x10000(dataBin[0], unitFolder + @"\Original MBON\" + "Data - " + binary.binaryHashes[1].ToString("X8") + ".bin");

                List<string> script_1Bin = Directory.GetFiles(@"D:\MBON\Image0\archives\", binary.binaryHashes[2].ToString("X8") + ".bin", SearchOption.AllDirectories).ToList();

                if (script_1Bin.Count == 0 || script_1Bin.Count > 1)
                    throw new Exception();

                copyBinarywithout0x10000(script_1Bin[0], unitFolder + @"\Original MBON\" + "Script 1 - " + binary.binaryHashes[2].ToString("X8") + ".bin");

                List<string> script_2Bin = Directory.GetFiles(@"D:\MBON\Image0\archives\", binary.binaryHashes[3].ToString("X8") + ".bin", SearchOption.AllDirectories).ToList();

                if (script_2Bin.Count == 0 || script_2Bin.Count > 1)
                    throw new Exception();

                copyBinarywithout0x10000(script_2Bin[0], unitFolder + @"\Original MBON\" + "Script 2 - " + binary.binaryHashes[3].ToString("X8") + ".bin");

                List<string> modelBin = Directory.GetFiles(@"D:\MBON\Image0\archives\", binary.binaryHashes[4].ToString("X8") + ".bin", SearchOption.AllDirectories).ToList();

                if (modelBin.Count == 0 || modelBin.Count > 1)
                    throw new Exception();

                copyBinarywithout0x10000(modelBin[0], unitFolder + @"\Original MBON\" + "Model and Texture - " + binary.binaryHashes[4].ToString("X8") + ".bin");

                List<string> OMOBin = Directory.GetFiles(@"D:\MBON\Image0\archives\", binary.binaryHashes[5].ToString("X8") + ".bin", SearchOption.AllDirectories).ToList();

                if (OMOBin.Count == 0 || OMOBin.Count > 1)
                    throw new Exception();

                copyBinarywithout0x10000(OMOBin[0], unitFolder + @"\Original MBON\" + "OMO - " + binary.binaryHashes[5].ToString("X8") + ".bin");

                List<string> EIDXBin = Directory.GetFiles(@"D:\MBON\Image0\archives\", binary.binaryHashes[6].ToString("X8") + ".bin", SearchOption.AllDirectories).ToList();

                if (EIDXBin.Count == 0 || EIDXBin.Count > 1)
                    throw new Exception();

                copyBinarywithout0x10000(EIDXBin[0], unitFolder + @"\Original MBON\" + "EIDX - " + binary.binaryHashes[6].ToString("X8") + ".bin");

                if (binary.binaryHashes[7] != 0xffffffff || binary.binaryHashes[8] != 0xffffffff)
                    throw new Exception();

                List<string> DNSOBin = Directory.GetFiles(@"D:\MBON\Image0\archives\", binary.binaryHashes[9].ToString("X8") + ".bin", SearchOption.AllDirectories).ToList();

                if (DNSOBin.Count > 1)
                    throw new Exception();

                if (DNSOBin.Count != 0)
                    copyBinarywithout0x10000(DNSOBin[0], unitFolder + @"\Original MBON\" + "DNSO - " + binary.binaryHashes[9].ToString("X8") + ".bin");

                List<string> unkBin = Directory.GetFiles(@"D:\MBON\Image0\archives\", binary.binaryHashes[10].ToString("X8") + ".bin", SearchOption.AllDirectories).ToList();

                if (unkBin.Count > 0)
                    throw new Exception();

                List<string> KPKPBin = Directory.GetFiles(@"D:\MBON\Image0\archives\", binary.binaryHashes[11].ToString("X8") + ".bin", SearchOption.AllDirectories).ToList();

                if (KPKPBin.Count > 1)
                    throw new Exception();

                if (KPKPBin.Count != 0)
                    copyBinarywithout0x10000(KPKPBin[0], unitFolder + @"\Original MBON\" + "KPKP - " + binary.binaryHashes[11].ToString("X8") + ".bin");

                if (binary.binaryHashes[12] != 0xffffffff || binary.binaryHashes[13] != 0xffffffff)
                    throw new Exception();
            }
        }

        private void copyBinarywithout0x10000(string filePath, string fileName)
        {
            FileStream fs = File.OpenRead(filePath);

            FileStream ofs = File.Create(fileName);
            fs.Seek(0x10000, SeekOrigin.Begin);
            fs.CopyTo(ofs);
            fs.Close();
            ofs.Close();
        }
    }
}
