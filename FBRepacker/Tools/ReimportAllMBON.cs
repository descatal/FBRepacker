using FBRepacker.Data;
using FBRepacker.Data.DataTypes;
using FBRepacker.Data.FB_Parse.DataTypes;
using FBRepacker.Data.MBON_Parse;
using FBRepacker.PAC;
using FBRepacker.PAC.Repack;
using FBRepacker.Psarc.V2;
using Newtonsoft.Json;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static FBRepacker.Data.MBON_Parse.nus3AudioNameHash;

namespace FBRepacker.Tools
{
    internal class ReimportAllMBON : Internals
    {
        //new Parse_Unit_Data().readVariables();
        string totalMBONExportFolder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Export";
        string totalMBONScriptFolder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Script";
        string totalMBONReimportFolder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Units";
        string totalMBONCommonReimportFolder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Common";
        string totalMBONCombinedPsarcFolder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Combined Psarc";
        string repackTemplates = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Repack Templates";

        public ReimportAllMBON() 
        {
            /*
            StreamReader PATCHjsonSR = File.OpenText(totalMBONCombinedPsarcFolder + @"\PATCH.json");
            string patch_06_00_OriginalJson = PATCHjsonSR.ReadToEnd();
            PATCHjsonSR.Close();

            TOCFileInfo tocFileInfo = JsonConvert.DeserializeObject<TOCFileInfo>(patch_06_00_OriginalJson);


            tocFileInfo = addNullSPRXTBL(tocFileInfo, patch_06_00_OriginalJson);

            Properties.Settings.Default.inputPsarcJSON = totalMBONCombinedPsarcFolder + @"\PATCH.json";
            Properties.Settings.Default.outputPsarcTBLBinaryNameasPatch = true;
            Properties.Settings.Default.outputPsarcTBLBinary = totalMBONCombinedPsarcFolder;

            new RepackPsarcV2().exportToc(tocFileInfo);
            */

            //reimportAllFB();
            reimportImages();
        }

        public TOCFileInfo addNullSPRXTBL(TOCFileInfo tocFileInfo, string patch_06_00_OriginalJson)
        {
            List<PACFileInfoV2> fileInfos = tocFileInfo.allFiles;

            string[] sprxfilenames = Directory.GetFiles(@"D:\Games\PS3\EXVSFB JPN\Pkg research\PKG\1.09\JP0700-NPJB00512_00-FULLBOOST000100A\USRDIR\patch_05_00\prx");

            foreach (var file in sprxfilenames)
            {
                PACFileInfoV2 data_file_info = new PACFileInfoV2();

                string sprxfilenamewithoutextension = Path.GetFileNameWithoutExtension(file);

                Match ifalreadyexist = Regex.Match(patch_06_00_OriginalJson, sprxfilenamewithoutextension);

                if(sprxfilenamewithoutextension != "TTGONLY" && !ifalreadyexist.Success)
                {
                    uint sprxfilenameuint = Convert.ToUInt32(sprxfilenamewithoutextension, 16);

                    int index = searchPACHash(sprxfilenameuint);
                    if (index != -1)
                    {
                        data_file_info.fileFlags = PACFileInfoV2.fileFlagsEnum.hasFileName | PACFileInfoV2.fileFlagsEnum.hasFileInfo;
                        data_file_info.patchNo = PACFileInfoV2.patchNoEnum.PATCH_6;
                        data_file_info.namePrefix = PACFileInfoV2.prefixEnum.NONE;
                        data_file_info.unk04 = 262144;
                        data_file_info.Size1 = 0;
                        data_file_info.Size2 = 0;
                        data_file_info.Size3 = 0;
                        data_file_info.unk00 = 0;
                        data_file_info.nameHash = sprxfilenameuint;
                        data_file_info.relativePatchPath = "patch_06_00/prx/" + sprxfilenamewithoutextension + ".sprx";
                        data_file_info.hasRelativePatchSubPath = true;
                        data_file_info.filePath = "";
                        data_file_info.fileInfoIndex = index;
                    }

                    fileInfos.Add(data_file_info);
                }
            }

            return tocFileInfo;
        }

        public void copyArcadeSelectSprites()
        {
            string json = File.OpenText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Common").ReadToEnd();
            List<Unit_Info_List> unit_Info_Lists = JsonConvert.DeserializeObject<List<Unit_Info_List>>(json);

            List<Unit_Info_List> new_units = unit_Info_Lists.Where(s => s.series_index == 31).ToList();
            
        }

        public void reimportImages()
        {
            List<string> allUnitFolders = Directory.GetDirectories(totalMBONExportFolder, "*", SearchOption.TopDirectoryOnly).ToList();

            StreamReader alreadyPackedSR = File.OpenText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\temp_unit_list.json");
            string alreadyPackedJSON = alreadyPackedSR.ReadToEnd();
            alreadyPackedSR.Close();
            List<uint> already_repacked = JsonConvert.DeserializeObject<List<uint>>(alreadyPackedJSON);

            string json = File.OpenText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Unit List FB.json").ReadToEnd();
            List<Unit_Info_List> unit_Info_List = JsonConvert.DeserializeObject<List<Unit_Info_List>>(json);

            string filelistjson = File.OpenText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(filelistjson);

            List<string> allPilotImageFolder = Directory.GetDirectories(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\All Pilot Image", "*", SearchOption.TopDirectoryOnly).ToList();
            List<string> allUnitImageFolder = Directory.GetDirectories(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\All Playable Unit Image & Sound Effects", "*", SearchOption.TopDirectoryOnly).ToList();
            List<string> allBossUnitImageFolder = Directory.GetDirectories(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\All Boss Unit Image & Sound Effects", "*", SearchOption.TopDirectoryOnly).ToList();

            UnitIDList unit_Names = load_UnitID();

            StringBuilder MBONlog = new StringBuilder();
            MBONlog.AppendLine("MBON Sprites PAC Hashes");

            allUnitFolders = allUnitFolders.OrderBy(x => uint.Parse(Path.GetFileNameWithoutExtension(x.Split('.')[0]))).ToList();

            uint last_unit_index = (uint)unit_Info_List.Count;
            uint last_series_index = unit_Info_List.Max(r => r.series_index);
            uint last_arcade_small_sprite_index = unit_Info_List.Max(r => r.arcade_small_sprite_index);
            uint last_arcade_unit_name_sprite = unit_Info_List.Max(r => r.arcade_small_sprite_index);
            uint last_figurine_sprite_index = unit_Info_List.Max(r => r.figurine_sprite_index);
            uint last_unk_0x7C = unit_Info_List.Max(x => x.unk_0x7C);
            uint last_unk_0x80 = unit_Info_List.Max(y => y.unk_0x80);

            foreach (string unitFolder in allUnitFolders)
            {
                Match unitNoMatch = Regex.Match(unitFolder, @"([0-9]{1,100}). ");
                string unitNoStr = unitNoMatch.Groups[0].Captures[0].Value;
                uint.TryParse(unitNoStr, out uint unitNo);

                string unitFolderName = Path.GetFileName(unitFolder.TrimEnd(Path.DirectorySeparatorChar));

                int unit_ID_str_index = unitFolderName.IndexOf("- ");
                string unit_ID_str = string.Empty;
                if (unit_ID_str_index >= 0)
                    unit_ID_str = unitFolderName.Substring(unit_ID_str_index + 2, unitFolderName.Length - unit_ID_str_index - 2);

                uint unit_ID = Convert.ToUInt32(unit_ID_str);
                Unit_Info_List unit_Infos = unit_Info_List.FirstOrDefault(x => x.unit_ID == unit_ID);
                Unit_Files_List unit_Files = unit_Files_List.FirstOrDefault(x => x.Unit_ID == unit_ID);

                if (unit_ID < 59900 && unit_Files != null && !already_repacked.Contains(unit_ID))
                {
                    string unitName = unit_Names.Unit_ID.FirstOrDefault(s => s.id == unit_Files.Unit_ID).name_english.Replace(" ", "_");

                    string arcade_selection_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\Arcade_Selection_Sprite_Costume_1";
                    string loading_ally_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\Loading_Ally_Sprite_Costume_1";
                    string loading_enemy_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\Loading_Enemy_Sprite_Costume_1";
                    string free_battle_selection_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\Free_Battle_Selection_Sprite_Costume_1";
                    string loading_enemy_target_unit_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Unit_Sprite_Costume_1";
                    string loading_enemy_target_pilot_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Pilot_Sprite_Costume_1";
                    string in_game_sortie_and_awakening_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\In_Game_Sortie_and_Awakening_Sprite_Costume_1";
                    string result_small_sprite_folder = unitFolder + @"\Extracted MBON\Result_Small_Sprite";
                    string figurine_sprite_folder = unitFolder + @"\Extracted MBON\Figurine_Sprite";
                    string target_small_sprite_folder = unitFolder + @"\Extracted MBON\Target_Small_Sprite";

                    Directory.CreateDirectory(arcade_selection_sprite_costume_1_folder);
                    Directory.CreateDirectory(loading_ally_sprite_costume_1_folder);
                    Directory.CreateDirectory(loading_enemy_sprite_costume_1_folder);
                    Directory.CreateDirectory(free_battle_selection_sprite_costume_1_folder);
                    Directory.CreateDirectory(loading_enemy_target_unit_sprite_costume_1_folder);
                    Directory.CreateDirectory(loading_enemy_target_pilot_sprite_costume_1_folder);
                    Directory.CreateDirectory(in_game_sortie_and_awakening_sprite_costume_1_folder);
                    Directory.CreateDirectory(result_small_sprite_folder);
                    Directory.CreateDirectory(figurine_sprite_folder);
                    Directory.CreateDirectory(target_small_sprite_folder);

                    string reimportFolder = totalMBONReimportFolder + @"\" + unitFolderName;
                    string reimportConvertedfromMBONFolder = totalMBONReimportFolder + @"\" + unitFolderName + @"\" + "Converted from MBON";
                    string reimportFilestoRepack = totalMBONReimportFolder + @"\" + unitFolderName + @"\" + "Files to Repack";
                    string reimportRepackedFiles = totalMBONReimportFolder + @"\" + unitFolderName + @"\" + "Repacked Files";

                    Directory.CreateDirectory(reimportFolder);
                    Directory.CreateDirectory(reimportConvertedfromMBONFolder);
                    Directory.CreateDirectory(reimportFilestoRepack);
                    Directory.CreateDirectory(reimportRepackedFiles);


                    string unitImageFolder = allUnitImageFolder.FirstOrDefault(s => s.Contains(unit_ID.ToString()));
                    string pilotImageFolder = allUnitImageFolder.FirstOrDefault(s => s.Contains(unit_ID.ToString()));


                    
                    if (unit_Files.MBONAdded)
                    {
                        MBONlog.AppendLine(@"//----------------------- " + unitName + @"-----------------------//");
                        // Manually add own made hashes to newly added MBON units
                        Crc32 crc32 = new Crc32();
                        string arcade_selection_str = unitName + "_arcade_selection_sprite_costume_1";
                        uint arcade_selection_hash = crc32.Get(Encoding.UTF8.GetBytes(arcade_selection_str.ToLower()));
                        MBONlog.AppendLine(arcade_selection_str + " - 0x" + arcade_selection_hash.ToString("X8"));

                        string loading_ally_sprite_costume_1_str = unitName + "_loading_ally_sprite_costume_1";
                        uint loading_ally_sprite_costume_1_hash = crc32.Get(Encoding.UTF8.GetBytes(loading_ally_sprite_costume_1_str.ToLower()));
                        MBONlog.AppendLine(loading_ally_sprite_costume_1_str + " - 0x" + loading_ally_sprite_costume_1_hash.ToString("X8"));

                        string loading_enemy_sprite_costume_1_str = unitName + "_loading_enemy_sprite_costume_1";
                        uint loading_enemy_sprite_costume_1_hash = crc32.Get(Encoding.UTF8.GetBytes(loading_enemy_sprite_costume_1_str.ToLower()));
                        MBONlog.AppendLine(loading_enemy_sprite_costume_1_str + " - 0x" + loading_enemy_sprite_costume_1_hash.ToString("X8"));

                        string free_battle_selection_sprite_costume_1_str = unitName + "_arcade_selection_sprite_costume_1";
                        uint free_battle_selection_sprite_costume_1_hash = crc32.Get(Encoding.UTF8.GetBytes(free_battle_selection_sprite_costume_1_str.ToLower()));
                        MBONlog.AppendLine(free_battle_selection_sprite_costume_1_str + " - 0x" + free_battle_selection_sprite_costume_1_hash.ToString("X8"));

                        string loading_enemy_target_unit_sprite_costume_1_str = unitName + "_arcade_selection_sprite_costume_1";
                        uint loading_enemy_target_unit_sprite_costume_1_hash = crc32.Get(Encoding.UTF8.GetBytes(loading_enemy_target_unit_sprite_costume_1_str.ToLower()));
                        MBONlog.AppendLine(loading_enemy_target_unit_sprite_costume_1_str + " - 0x" + loading_enemy_target_unit_sprite_costume_1_hash.ToString("X8"));

                        string loading_enemy_target_pilot_sprite_costume_1_str = unitName + "_arcade_selection_sprite_costume_1";
                        uint loading_enemy_target_pilot_sprite_costume_1_hash = crc32.Get(Encoding.UTF8.GetBytes(loading_enemy_target_pilot_sprite_costume_1_str.ToLower()));
                        MBONlog.AppendLine(loading_enemy_target_pilot_sprite_costume_1_str + " - 0x" + loading_enemy_target_pilot_sprite_costume_1_hash.ToString("X8"));

                        uint in_game_sortie_and_awakening_sprite_costume_1_hash = unit_Files.sortie_and_awakening_sprites_PAC_hash;

                        uint KPKP_hash = unit_Files.sortie_mouth_anim_enum_KPKP_PAC_hash;

                        string result_small_sprite_str = unitName + "_result_small_sprite";
                        uint result_small_sprite_hash = crc32.Get(Encoding.UTF8.GetBytes(result_small_sprite_str.ToLower()));
                        MBONlog.AppendLine(result_small_sprite_str + " - 0x" + result_small_sprite_hash.ToString("X8"));

                        string figurine_sprite_str = unitName + "_figurine_sprite";
                        uint figurine_sprite_hash = crc32.Get(Encoding.UTF8.GetBytes(figurine_sprite_str.ToLower()));
                        MBONlog.AppendLine(figurine_sprite_str + " - 0x" + figurine_sprite_hash.ToString("X8"));

                        string target_small_sprite_str = unitName + "_target_small_sprite";
                        uint target_small_sprite_hash = crc32.Get(Encoding.UTF8.GetBytes(target_small_sprite_str.ToLower()));
                        MBONlog.AppendLine(target_small_sprite_str + " - 0x" + target_small_sprite_hash.ToString("X8"));

                        Unit_Info_List new_Unit_Files = new Unit_Info_List();

                        new_Unit_Files.unit_index = (byte)last_unit_index;
                        new_Unit_Files.series_index = (byte)(last_series_index + 1);
                        new_Unit_Files.unit_ID = unit_ID;
                        new_Unit_Files.release_string = "リリース";
                        new_Unit_Files.F_string = "F" + unit_ID;
                        new_Unit_Files.F_out_string = "F_OUT_" + unit_ID;
                        new_Unit_Files.P_string = "P" + unit_ID;

                        List<Unit_Info_List> units_In_Series = unit_Info_List.Where(x => x.series_index == (last_series_index + 1)).ToList();
                        new_Unit_Files.internal_index = (byte)units_In_Series.Count();

                        new_Unit_Files.arcade_small_sprite_index = (byte)last_arcade_small_sprite_index;
                        new_Unit_Files.arcade_unit_name_sprite = (byte)last_arcade_unit_name_sprite;
                        new_Unit_Files.arcade_selection_sprite_costume_1_hash = arcade_selection_hash;
                        new_Unit_Files.loading_ally_sprite_costume_1_hash = arcade_selection_hash;
                        new_Unit_Files.loading_enemy_sprite_costume_1_hash = loading_enemy_sprite_costume_1_hash;
                        new_Unit_Files.free_battle_selection_sprite_costume_1_hash = free_battle_selection_sprite_costume_1_hash;
                        new_Unit_Files.loading_enemy_target_unit_sprite_costume_1_hash = loading_enemy_target_unit_sprite_costume_1_hash;
                        new_Unit_Files.loading_enemy_target_pilot_sprite_costume_1_hash = loading_enemy_target_pilot_sprite_costume_1_hash;
                        new_Unit_Files.in_game_sortie_and_awakening_sprite_costume_1_hash = in_game_sortie_and_awakening_sprite_costume_1_hash;
                        new_Unit_Files.KPKP_hash = KPKP_hash;
                        new_Unit_Files.result_small_sprite_hash = result_small_sprite_hash;
                        new_Unit_Files.figurine_sprite_index = (byte)last_figurine_sprite_index;
                        new_Unit_Files.figurine_sprite_hash = figurine_sprite_hash;
                        new_Unit_Files.target_small_sprite_hash = target_small_sprite_hash;
                        new_Unit_Files.unk_0x7C = last_unk_0x7C;
                        new_Unit_Files.unk_0x80 = last_unk_0x80;
                        new_Unit_Files.IS_Costume_costume_2_string = "0.0";
                        new_Unit_Files.IS_Costume_T_costume_2_string = "0.0";
                        new_Unit_Files.IS_Costume_costume_3_string = "0.0";
                        new_Unit_Files.IS_Costume_T_costume_3_string = "0.0";

                        last_unit_index++;
                        last_arcade_small_sprite_index++;
                        last_arcade_unit_name_sprite++;
                        last_figurine_sprite_index++;
                        last_unk_0x7C++;
                        last_unk_0x80++;

                        unit_Info_List.Add(new_Unit_Files);
                    }
                    
                }
            }

            foreach (string unitFolder in allUnitFolders)
            {
                Match unitNoMatch = Regex.Match(unitFolder, @"([0-9]{1,100}). ");
                string unitNoStr = unitNoMatch.Groups[0].Captures[0].Value;
                uint.TryParse(unitNoStr, out uint unitNo);

                string unitFolderName = Path.GetFileName(unitFolder.TrimEnd(Path.DirectorySeparatorChar));

                int unit_ID_str_index = unitFolderName.IndexOf("- ");
                string unit_ID_str = string.Empty;
                if (unit_ID_str_index >= 0)
                    unit_ID_str = unitFolderName.Substring(unit_ID_str_index + 2, unitFolderName.Length - unit_ID_str_index - 2);

                uint unit_ID = Convert.ToUInt32(unit_ID_str);
                Unit_Info_List unit_Infos = unit_Info_List.FirstOrDefault(x => x.unit_ID == unit_ID);
                Unit_Files_List unit_Files = unit_Files_List.FirstOrDefault(x => x.Unit_ID == unit_ID);

                if (unit_ID > 59900 && unit_Files != null && !already_repacked.Contains(unit_ID))
                {
                    string unitName = unit_Names.Unit_ID.FirstOrDefault(s => s.id == unit_Files.Unit_ID).name_english.Replace(" ", "_");

                    string arcade_selection_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\Arcade_Selection_Sprite_Costume_1";
                    string loading_ally_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\Loading_Ally_Sprite_Costume_1";
                    string loading_enemy_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\Loading_Enemy_Sprite_Costume_1";
                    string free_battle_selection_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\Free_Battle_Selection_Sprite_Costume_1";
                    string loading_enemy_target_unit_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Unit_Sprite_Costume_1";
                    string loading_enemy_target_pilot_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Pilot_Sprite_Costume_1";
                    string in_game_sortie_and_awakening_sprite_costume_1_folder = unitFolder + @"\Extracted MBON\In_Game_Sortie_and_Awakening_Sprite_Costume_1";
                    string result_small_sprite_folder = unitFolder + @"\Extracted MBON\Result_Small_Sprite";
                    string figurine_sprite_folder = unitFolder + @"\Extracted MBON\Figurine_Sprite";
                    string target_small_sprite_folder = unitFolder + @"\Extracted MBON\Target_Small_Sprite";

                    Directory.CreateDirectory(arcade_selection_sprite_costume_1_folder);
                    Directory.CreateDirectory(loading_ally_sprite_costume_1_folder);
                    Directory.CreateDirectory(loading_enemy_sprite_costume_1_folder);
                    Directory.CreateDirectory(free_battle_selection_sprite_costume_1_folder);
                    Directory.CreateDirectory(loading_enemy_target_unit_sprite_costume_1_folder);
                    Directory.CreateDirectory(loading_enemy_target_pilot_sprite_costume_1_folder);
                    Directory.CreateDirectory(in_game_sortie_and_awakening_sprite_costume_1_folder);
                    Directory.CreateDirectory(result_small_sprite_folder);
                    Directory.CreateDirectory(figurine_sprite_folder);
                    Directory.CreateDirectory(target_small_sprite_folder);

                    string reimportFolder = totalMBONReimportFolder + @"\" + unitFolderName;
                    string reimportConvertedfromMBONFolder = totalMBONReimportFolder + @"\" + unitFolderName + @"\" + "Converted from MBON";
                    string reimportFilestoRepack = totalMBONReimportFolder + @"\" + unitFolderName + @"\" + "Files to Repack";
                    string reimportRepackedFiles = totalMBONReimportFolder + @"\" + unitFolderName + @"\" + "Repacked Files";

                    Directory.CreateDirectory(reimportFolder);
                    Directory.CreateDirectory(reimportConvertedfromMBONFolder);
                    Directory.CreateDirectory(reimportFilestoRepack);
                    Directory.CreateDirectory(reimportRepackedFiles);

                    string unitImageFolder = allUnitImageFolder.FirstOrDefault(s => s.Contains(unit_ID.ToString()));
                    string pilotImageFolder = allUnitImageFolder.FirstOrDefault(s => s.Contains(unit_ID.ToString()));

                    if (unit_Files.MBONAdded)
                    {
                        MBONlog.AppendLine(@"//----------------------- " + unitName + @"-----------------------//");
                        // Manually add own made hashes to newly added MBON units
                        Crc32 crc32 = new Crc32();
                        string arcade_selection_str = unitName + "_arcade_selection_sprite_costume_1";
                        uint arcade_selection_hash = crc32.Get(Encoding.UTF8.GetBytes(arcade_selection_str.ToLower()));
                        MBONlog.AppendLine(arcade_selection_str + " - 0x" + arcade_selection_hash.ToString("X8"));

                        string loading_ally_sprite_costume_1_str = unitName + "_loading_ally_sprite_costume_1";
                        uint loading_ally_sprite_costume_1_hash = crc32.Get(Encoding.UTF8.GetBytes(loading_ally_sprite_costume_1_str.ToLower()));
                        MBONlog.AppendLine(loading_ally_sprite_costume_1_str + " - 0x" + loading_ally_sprite_costume_1_hash.ToString("X8"));

                        string loading_enemy_sprite_costume_1_str = unitName + "_loading_enemy_sprite_costume_1";
                        uint loading_enemy_sprite_costume_1_hash = crc32.Get(Encoding.UTF8.GetBytes(loading_enemy_sprite_costume_1_str.ToLower()));
                        MBONlog.AppendLine(loading_enemy_sprite_costume_1_str + " - 0x" + loading_enemy_sprite_costume_1_hash.ToString("X8"));

                        string free_battle_selection_sprite_costume_1_str = unitName + "_arcade_selection_sprite_costume_1";
                        uint free_battle_selection_sprite_costume_1_hash = crc32.Get(Encoding.UTF8.GetBytes(free_battle_selection_sprite_costume_1_str.ToLower()));
                        MBONlog.AppendLine(free_battle_selection_sprite_costume_1_str + " - 0x" + free_battle_selection_sprite_costume_1_hash.ToString("X8"));

                        string loading_enemy_target_unit_sprite_costume_1_str = unitName + "_arcade_selection_sprite_costume_1";
                        uint loading_enemy_target_unit_sprite_costume_1_hash = crc32.Get(Encoding.UTF8.GetBytes(loading_enemy_target_unit_sprite_costume_1_str.ToLower()));
                        MBONlog.AppendLine(loading_enemy_target_unit_sprite_costume_1_str + " - 0x" + loading_enemy_target_unit_sprite_costume_1_hash.ToString("X8"));

                        string loading_enemy_target_pilot_sprite_costume_1_str = unitName + "_arcade_selection_sprite_costume_1";
                        uint loading_enemy_target_pilot_sprite_costume_1_hash = crc32.Get(Encoding.UTF8.GetBytes(loading_enemy_target_pilot_sprite_costume_1_str.ToLower()));
                        MBONlog.AppendLine(loading_enemy_target_pilot_sprite_costume_1_str + " - 0x" + loading_enemy_target_pilot_sprite_costume_1_hash.ToString("X8"));

                        uint in_game_sortie_and_awakening_sprite_costume_1_hash = unit_Files.sortie_and_awakening_sprites_PAC_hash;

                        uint KPKP_hash = unit_Files.sortie_mouth_anim_enum_KPKP_PAC_hash;

                        string result_small_sprite_str = unitName + "_result_small_sprite";
                        uint result_small_sprite_hash = crc32.Get(Encoding.UTF8.GetBytes(result_small_sprite_str.ToLower()));
                        MBONlog.AppendLine(result_small_sprite_str + " - 0x" + result_small_sprite_hash.ToString("X8"));

                        string figurine_sprite_str = unitName + "_figurine_sprite";
                        uint figurine_sprite_hash = crc32.Get(Encoding.UTF8.GetBytes(figurine_sprite_str.ToLower()));
                        MBONlog.AppendLine(figurine_sprite_str + " - 0x" + figurine_sprite_hash.ToString("X8"));

                        string target_small_sprite_str = unitName + "_target_small_sprite";
                        uint target_small_sprite_hash = crc32.Get(Encoding.UTF8.GetBytes(target_small_sprite_str.ToLower()));
                        MBONlog.AppendLine(target_small_sprite_str + " - 0x" + target_small_sprite_hash.ToString("X8"));

                        Unit_Info_List new_Unit_Files = new Unit_Info_List();

                        new_Unit_Files.unit_index = (byte)last_unit_index;
                        new_Unit_Files.series_index = (byte)(last_series_index + 1);
                        new_Unit_Files.unit_ID = unit_ID;
                        new_Unit_Files.release_string = "リリース";
                        new_Unit_Files.F_string = "F" + unit_ID;
                        new_Unit_Files.F_out_string = "F_OUT_" + unit_ID;
                        new_Unit_Files.P_string = "P" + unit_ID;

                        List<Unit_Info_List> units_In_Series = unit_Info_List.Where(x => x.series_index == (last_series_index + 1)).ToList();
                        new_Unit_Files.internal_index = (byte)units_In_Series.Count();

                        new_Unit_Files.arcade_small_sprite_index = (byte)last_arcade_small_sprite_index;
                        new_Unit_Files.arcade_unit_name_sprite = (byte)last_arcade_unit_name_sprite;
                        new_Unit_Files.arcade_selection_sprite_costume_1_hash = arcade_selection_hash;
                        new_Unit_Files.loading_ally_sprite_costume_1_hash = arcade_selection_hash;
                        new_Unit_Files.loading_enemy_sprite_costume_1_hash = loading_enemy_sprite_costume_1_hash;
                        new_Unit_Files.free_battle_selection_sprite_costume_1_hash = free_battle_selection_sprite_costume_1_hash;
                        new_Unit_Files.loading_enemy_target_unit_sprite_costume_1_hash = loading_enemy_target_unit_sprite_costume_1_hash;
                        new_Unit_Files.loading_enemy_target_pilot_sprite_costume_1_hash = loading_enemy_target_pilot_sprite_costume_1_hash;
                        new_Unit_Files.in_game_sortie_and_awakening_sprite_costume_1_hash = in_game_sortie_and_awakening_sprite_costume_1_hash;
                        new_Unit_Files.KPKP_hash = KPKP_hash;
                        new_Unit_Files.result_small_sprite_hash = result_small_sprite_hash;
                        new_Unit_Files.figurine_sprite_index = (byte)last_figurine_sprite_index;
                        new_Unit_Files.figurine_sprite_hash = figurine_sprite_hash;
                        new_Unit_Files.target_small_sprite_hash = target_small_sprite_hash;
                        new_Unit_Files.unk_0x7C = last_unk_0x7C;
                        new_Unit_Files.unk_0x80 = last_unk_0x80;
                        new_Unit_Files.IS_Costume_costume_2_string = "0.0";
                        new_Unit_Files.IS_Costume_T_costume_2_string = "0.0";
                        new_Unit_Files.IS_Costume_costume_3_string = "0.0";
                        new_Unit_Files.IS_Costume_T_costume_3_string = "0.0";

                        last_unit_index++;
                        last_arcade_small_sprite_index++;
                        last_arcade_unit_name_sprite++;
                        last_figurine_sprite_index++;
                        last_unk_0x7C++;
                        last_unk_0x80++;

                        unit_Info_List.Add(new_Unit_Files);
                    }
                }
            }

            string unit_Info_List_MBON = JsonConvert.SerializeObject(unit_Info_List, Formatting.Indented);
            StreamWriter SW = File.CreateText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Unit List MBON.json");
            SW.Write(unit_Info_List_MBON);
            SW.Close();

            StreamWriter streamWriter = File.CreateText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\GeneratedMBONSpriteHashes.txt");
            streamWriter.Write(MBONlog.ToString());

            streamWriter.Close();
        }
        
        public void reimportAll_Projectile_Hit_Reload()
        {
            List<string> allUnitFolders = Directory.GetDirectories(totalMBONExportFolder, "*", SearchOption.TopDirectoryOnly).ToList();

            StreamReader alreadyPackedSR = File.OpenText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\temp_unit_list.json");
            string alreadyPackedJSON = alreadyPackedSR.ReadToEnd();
            alreadyPackedSR.Close();
            List<uint> already_repacked = JsonConvert.DeserializeObject<List<uint>>(alreadyPackedJSON);

            string json = File.OpenText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(json);

            StreamReader PATCHjsonSR = File.OpenText(totalMBONCombinedPsarcFolder + @"\PATCH.json");
            string patch_06_00_OriginalJson = PATCHjsonSR.ReadToEnd();
            TOCFileInfo tocFileInfo = JsonConvert.DeserializeObject<TOCFileInfo>(patch_06_00_OriginalJson);
            PATCHjsonSR.Close();


            string commonHitReimportFolder = totalMBONCommonReimportFolder + @"\Hit_Properties - DF3B4191";
            string commonProjectileReimportFolder = totalMBONCommonReimportFolder + @"\Projectile_Properties - AEB4F916";
            string commonReloadReimportFolder = totalMBONCommonReimportFolder + @"\Reload_Properties - 3DD6DC78";

            Directory.CreateDirectory(commonHitReimportFolder);
            Directory.CreateDirectory(commonProjectileReimportFolder);
            Directory.CreateDirectory(commonReloadReimportFolder);

            // Template
            string repack_Template_Hit = repackTemplates + @"\Hit_Properties";
            DirectoryCopy(repack_Template_Hit, commonHitReimportFolder, true);

            string repack_Template_Projectiles = repackTemplates + @"\Projectile_Properties";
            DirectoryCopy(repack_Template_Projectiles, commonProjectileReimportFolder, true);

            string repack_Template_Reload = repackTemplates + @"\Reload";
            DirectoryCopy(repack_Template_Reload, commonReloadReimportFolder, true);

            uint commonHitFileCount = 2;
            uint commonProjectileFileCount = 2;

            StringBuilder hitPACSB = new StringBuilder();
            StringBuilder projectilePACSB = new StringBuilder();

            allUnitFolders = allUnitFolders.OrderBy(x => uint.Parse(Path.GetFileNameWithoutExtension(x.Split('.')[0]))).ToList();

            foreach (string unitFolder in allUnitFolders)
            {
                Match unitNoMatch = Regex.Match(unitFolder, @"([0-9]{1,100}). ");
                string unitNoStr = unitNoMatch.Groups[0].Captures[0].Value;
                uint.TryParse(unitNoStr, out uint unitNo);

                string unitFolderName = Path.GetFileName(unitFolder.TrimEnd(Path.DirectorySeparatorChar));

                int unit_ID_str_index = unitFolderName.IndexOf("- ");
                string unit_ID_str = string.Empty;
                if (unit_ID_str_index >= 0)
                    unit_ID_str = unitFolderName.Substring(unit_ID_str_index + 2, unitFolderName.Length - unit_ID_str_index - 2);

                uint unit_ID = Convert.ToUInt32(unit_ID_str);
                Unit_Files_List unit_Files = unit_Files_List.FirstOrDefault(x => x.Unit_ID == unit_ID);

                if (unit_Files != null)
                {
                    UnitIDList unit_Infos = load_UnitID();
                    string unitName = unit_Infos.Unit_ID.FirstOrDefault(s => s.id == unit_Files.Unit_ID).name_english.Replace(" ", "_");

                    string reimportConvertedfromMBONFolder = totalMBONReimportFolder + @"\" + unitFolderName + @"\" + "Converted from MBON";

                    string extractMBONFolder = unitFolder + @"\Extracted MBON";
                    string originalMBONFolder = unitFolder + @"\Original MBON";
                    List<string> dataFolder = Directory.GetDirectories(extractMBONFolder, "*", SearchOption.TopDirectoryOnly).ToList();
                    dataFolder = dataFolder.Where(x => x.Contains("Data")).ToList();
                    if (dataFolder.Count() == 0 || dataFolder.Count() > 0x1)
                        throw new Exception();

                    string data = dataFolder[0];

                    Directory.CreateDirectory(reimportConvertedfromMBONFolder + @"\Hit_Properties");
                    Directory.CreateDirectory(reimportConvertedfromMBONFolder + @"\Projectile_Properties");
                    Directory.CreateDirectory(reimportConvertedfromMBONFolder + @"\Reload");

                    
                    // -------------------------------------------- Hit Properties Parse --------------------------------------------
                    // For some reason old NPCs has a centralized hit properties binary.
                    // For those NPC the hit properties binary will be 0x18 long, and we exclude that.
                    // We will manually import the combined hit binary as the first file.
                    if (commonHitFileCount == 2)
                    {
                        File.Copy(repackTemplates + @"\old_NPC_Hit_Binary_Combined_E4A7DBD8.bin", commonHitReimportFolder + @"\001-FHM\002_old_NPC_Combined.bin", true);

                        hitPACSB.AppendLine("--" + commonHitFileCount.ToString() + "--");
                        hitPACSB.AppendLine("FHM Offset: 0");
                        hitPACSB.AppendLine("Size: 0");
                        hitPACSB.AppendLine("FHMAssetLoadEnum: 0");
                        hitPACSB.AppendLine("FHMunkEnum: 0");
                        hitPACSB.AppendLine("FHMFileNo: 1");
                        hitPACSB.AppendLine("Header: bin");
                        hitPACSB.AppendLine("fileName: 002_old_NPC_Combined.bin");
                        hitPACSB.AppendLine();
                        hitPACSB.AppendLine();
                        hitPACSB.AppendLine("//");

                        commonHitFileCount++;  
                    }

                    FileStream hit_fs = File.OpenRead(data + @"\001-FHM\008.bin");
                    long hit_file_size = hit_fs.Length;
                    hit_fs.Close();
                    if (hit_file_size != 0x18)
                    {
                        /// Export the JSON for future edit
                        Properties.Settings.Default.HitBinaryFilePath = data + @"\001-FHM\008.bin";
                        Properties.Settings.Default.outputHitJSONFolderPath = reimportConvertedfromMBONFolder + @"\Hit_Properties";

                        new Parse_Hit().parse_Hit();

                        /// For repack, just normal copy will do
                        File.Copy(data + @"\001-FHM\008.bin", commonHitReimportFolder + @"\001-FHM\" + commonHitFileCount.ToString("000") + "_" + unitName + ".bin", true);

                        hitPACSB.AppendLine("--" + commonHitFileCount.ToString() + "--");
                        hitPACSB.AppendLine("FHM Offset: 0");
                        hitPACSB.AppendLine("Size: 0");
                        hitPACSB.AppendLine("FHMAssetLoadEnum: 0");
                        hitPACSB.AppendLine("FHMunkEnum: 0");
                        hitPACSB.AppendLine("FHMFileNo: 1");
                        hitPACSB.AppendLine("Header: bin");
                        hitPACSB.AppendLine("fileName: " + commonHitFileCount.ToString("000") + "_" + unitName + ".bin");
                        hitPACSB.AppendLine();
                        hitPACSB.AppendLine();
                        hitPACSB.AppendLine("//");

                        commonHitFileCount++;
                    }
                    
                    /*
                    // -------------------------------------------- Projectile Properties Parse --------------------------------------------
                    /// Generate JSON from Binary
                    Properties.Settings.Default.ProjecitleBinaryFilePath = data + @"\001-FHM\009.bin";
                    Properties.Settings.Default.outputProjectileJSONFolderPath = reimportConvertedfromMBONFolder + @"\Projectile_Properties";
                    Properties.Settings.Default.convertMBONProjecitle = true;
                    Properties.Settings.Default.truncateProjectileType = true;

                    if (unit_Files.MBONAdded
                        // DLC units does not need to have projectile types truncated
                        || unit_ID == 0x3AF3 // Banshee Norn
                        || unit_ID == 0x5281 // Strike Rouge
                        || unit_ID == 0x564B // Gold Frame Amatsu Mina
                        || unit_ID == 0x6DBB // Altron Gundam
                        || unit_ID == 0x4287 // Re-Gz
                        || unit_ID == 0x0439 // Char's Zaku II
                        || unit_ID == 0x371F // Avalanche Exia
                        || unit_ID == 0x3AE9 // FAUC
                        || unit_ID == 0x4683 // Nobel
                        || unit_ID == 0x4e7b // Blitz
                        || unit_ID == 0x69b5 // Harute
                        || unit_ID == 0x6dc5 // Sandrock
                        || unit_ID == 0x792D // Penelope
                        || unit_ID == 0x046B // Perfect Gundam
                        || unit_ID == 0x3729 // 007S
                        || unit_ID == 0x5641 // Red Dragon
                        || unit_ID == 0x6DE3 // Tallgeese II
                        )
                    {
                        Properties.Settings.Default.truncateProjectileType = false;
                    }

                    new ParseProjectileProperties().convertProjectileBintoJSON();

                    /// Generate Binary from Json
                    Properties.Settings.Default.ProjecitleJSONFilePath = reimportConvertedfromMBONFolder + @"\Projectile_Properties\009_Projectile.JSON";
                    Properties.Settings.Default.ProjectileBinaryInputGameVer = 1;
                    Properties.Settings.Default.outputProjectileBinFolderPath = reimportConvertedfromMBONFolder + @"\Projectile_Properties";

                    // write Binary
                    ParseProjectileProperties parseProjectileProperties = new ParseProjectileProperties();
                    Projectile_Properties projectile_Properties = parseProjectileProperties.parseProjectileJSON();
                    parseProjectileProperties.writeProjectileBinary(projectile_Properties);

                    // Save JSON
                    string JSON = JsonConvert.SerializeObject(projectile_Properties, Formatting.Indented);

                    // Create a backup copy of old JSON.
                    string oriJSONFileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ProjecitleJSONFilePath);
                    string oriJSONFilePath = Path.GetDirectoryName(Properties.Settings.Default.ProjecitleJSONFilePath);

                    FileStream fs = File.OpenRead(Properties.Settings.Default.ProjecitleJSONFilePath);
                    FileStream ofs = File.Create(oriJSONFilePath + @"\" + oriJSONFileName + "_backup.JSON");

                    fs.CopyTo(ofs);
                    fs.Close();
                    ofs.Close();

                    StreamWriter fsJSON = WaitForTextWrite(Properties.Settings.Default.ProjecitleJSONFilePath);
                    fsJSON.Write(JSON);
                    fsJSON.Close();

                    File.Copy(reimportConvertedfromMBONFolder + @"\Projectile_Properties\009_Projectile.bin", commonProjectileReimportFolder + @"\001-FHM\" + commonProjectileFileCount.ToString("000") + "_" + unitName + ".bin", true);

                    projectilePACSB.AppendLine("--" + commonProjectileFileCount.ToString() + "--");
                    projectilePACSB.AppendLine("FHM Offset: 0");
                    projectilePACSB.AppendLine("Size: 0");
                    projectilePACSB.AppendLine("FHMAssetLoadEnum: 0");
                    projectilePACSB.AppendLine("FHMunkEnum: 0");
                    projectilePACSB.AppendLine("FHMFileNo: 1");
                    projectilePACSB.AppendLine("Header: bin");
                    projectilePACSB.AppendLine("fileName: " + commonProjectileFileCount.ToString("000") + "_" + unitName + ".bin");
                    projectilePACSB.AppendLine();
                    projectilePACSB.AppendLine();
                    projectilePACSB.AppendLine("//");

                    commonProjectileFileCount++;
                    */

                    /*
                    // -------------------------------------------- Reload Properties Parse --------------------------------------------
                    Properties.Settings.Default.ReloadBinaryInputGameVer = 1;
                    Properties.Settings.Default.ReloadBinaryFilePath = data + @"\001-FHM\010.bin";
                    Properties.Settings.Default.outputReloadJSONFolderPath = reimportConvertedfromMBONFolder + @"\Reload";

                    new Parse_Reload().parse_Reload();

                    Properties.Settings.Default.ReloadJSONFilePath = totalMBONCommonReimportFolder + @"\Reload.JSON";

                    Parse_Reload parseReload = new Parse_Reload();

                    Reload reload = parseReload.parseReloadJSON();
                    List<Reload_FB> reload_FBs = reload.reload_FB;

                    string appendReloadJSONPath = reimportConvertedfromMBONFolder + @"\Reload\010_Reload.json";

                    if (File.Exists(appendReloadJSONPath))
                    {
                        StreamReader appendReloadJSONFS = File.OpenText(appendReloadJSONPath);
                        string appendJSON = appendReloadJSONFS.ReadToEnd();

                        Reload new_reload = JsonConvert.DeserializeObject<Reload>(appendJSON);

                        if (new_reload.game_Ver != Reload.game_ver.FB)
                            throw new Exception("Game version not FB!");

                        List<Reload_FB> new_reload_FBs = new_reload.reload_FB;

                        for (int i = 0; i < new_reload_FBs.Count; i++)
                        {
                            Reload_FB reload_FB = reload_FBs[i];
                            Reload_FB new_reload_FB = new_reload_FBs[i];

                            int hash_exist = reload_FBs.FindIndex(x => x.hash.Equals(new_reload_FB.hash));

                            if (hash_exist != -1)
                            {
                                reload_FBs[hash_exist] = new_reload_FB;
                            }
                            else
                            {
                                reload_FBs.Add(new_reload_FB);
                            }
                        }

                        appendReloadJSONFS.Close();
                    }

                    Properties.Settings.Default.outputReloadBinFolderPath = totalMBONCommonReimportFolder;
                    parseReload.writeReloadBinary(reload);

                    // Save JSON
                    string ReloadJSON = JsonConvert.SerializeObject(reload, Formatting.Indented);

                    // Create a backup copy of old JSON.
                    string oriReloadJSONFileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ReloadJSONFilePath);
                    string oriReloadJSONFilePath = Path.GetDirectoryName(Properties.Settings.Default.ReloadJSONFilePath);

                    StreamReader sr = File.OpenText(Properties.Settings.Default.ReloadJSONFilePath);
                    StreamWriter sw = File.CreateText(oriReloadJSONFilePath + @"\" + oriReloadJSONFileName + "_backup.JSON");
                    sw.Write(sr.ReadToEnd());
                    sr.Close();
                    sw.Close();

                    string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ReloadJSONFilePath);
                    string outputPath = totalMBONCommonReimportFolder + @"\Reload.json";

                    //WaitForFile(outputPath);
                    StreamWriter fsReloadJSON = File.CreateText(outputPath);
                    fsReloadJSON.Write(ReloadJSON);
                    fsReloadJSON.Close();
                    */
                }
            }

            // -------------------------------------------- PAC Info --------------------------------------------
            /// Hit_Properties
            StreamWriter Hit_PAC_Info = File.CreateText(commonHitReimportFolder + @"\PAC.info");
            Hit_PAC_Info.WriteLine("--1--");
            Hit_PAC_Info.WriteLine("FHMOffset: 0");
            Hit_PAC_Info.WriteLine("Header: fhm");
            Hit_PAC_Info.WriteLine("--FHM--");
            Hit_PAC_Info.WriteLine("Total file size: 0");
            Hit_PAC_Info.WriteLine("Number of files: " + (commonHitFileCount - 2).ToString());
            Hit_PAC_Info.WriteLine("FHM chunk size: 2656");
            Hit_PAC_Info.WriteLine("fileName: 001.fhm");
            Hit_PAC_Info.WriteLine("Additional info flag: 0");
            Hit_PAC_Info.WriteLine();
            Hit_PAC_Info.WriteLine();
            Hit_PAC_Info.WriteLine("//");

            Hit_PAC_Info.Write(hitPACSB.ToString());

            Hit_PAC_Info.WriteLine("--" + (commonHitFileCount).ToString() + "--");
            Hit_PAC_Info.WriteLine("Header: endfile");
            Hit_PAC_Info.WriteLine("End File Offset: 767844");
            Hit_PAC_Info.WriteLine("End File Size: 0");
            Hit_PAC_Info.WriteLine("fileName: endfile.endfile");
            Hit_PAC_Info.WriteLine();
            Hit_PAC_Info.WriteLine("//");

            Hit_PAC_Info.Close();


            StreamWriter Projectile_PAC_Info = File.CreateText(commonProjectileReimportFolder + @"\PAC.info");
            Projectile_PAC_Info.WriteLine("--1--");
            Projectile_PAC_Info.WriteLine("FHMOffset: 0");
            Projectile_PAC_Info.WriteLine("Header: fhm");
            Projectile_PAC_Info.WriteLine("--FHM--");
            Projectile_PAC_Info.WriteLine("Total file size: 0");
            Projectile_PAC_Info.WriteLine("Number of files: " + (commonProjectileFileCount - 2).ToString());
            Projectile_PAC_Info.WriteLine("FHM chunk size: 2656");
            Projectile_PAC_Info.WriteLine("fileName: 001.fhm");
            Projectile_PAC_Info.WriteLine("Additional info flag: 0");
            Projectile_PAC_Info.WriteLine();
            Projectile_PAC_Info.WriteLine();
            Projectile_PAC_Info.WriteLine("//");

            Projectile_PAC_Info.Write(projectilePACSB.ToString());

            Projectile_PAC_Info.WriteLine("--" + (commonProjectileFileCount).ToString() + "--");
            Projectile_PAC_Info.WriteLine("Header: endfile");
            Projectile_PAC_Info.WriteLine("End File Offset: 767844");
            Projectile_PAC_Info.WriteLine("End File Size: 0");
            Projectile_PAC_Info.WriteLine("fileName: endfile.endfile");
            Projectile_PAC_Info.WriteLine();
            Projectile_PAC_Info.WriteLine("//");

            Projectile_PAC_Info.Close();
        }

        public void reimportAllFB()
        {
            List<string> allUnitFolders = Directory.GetDirectories(totalMBONExportFolder, "*", SearchOption.TopDirectoryOnly).ToList();

            StreamReader alreadyPackedSR = File.OpenText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\temp_unit_list.json");
            string alreadyPackedJSON = alreadyPackedSR.ReadToEnd();
            alreadyPackedSR.Close();
            List<uint> already_repacked = JsonConvert.DeserializeObject<List<uint>>(alreadyPackedJSON);

            string json = File.OpenText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(json);

            StreamReader PATCHjsonSR = File.OpenText(totalMBONCombinedPsarcFolder + @"\PATCH.json");
            string patch_06_00_OriginalJson = PATCHjsonSR.ReadToEnd();
            TOCFileInfo tocFileInfo = JsonConvert.DeserializeObject<TOCFileInfo>(patch_06_00_OriginalJson);
            PATCHjsonSR.Close();

            foreach (string unitFolder in allUnitFolders)
            {
                Match unitNoMatch = Regex.Match(unitFolder, @"([0-9]{1,100}). ");
                string unitNoStr = unitNoMatch.Groups[0].Captures[0].Value;
                uint.TryParse(unitNoStr, out uint unitNo);

                string unitFolderName = Path.GetFileName(unitFolder.TrimEnd(Path.DirectorySeparatorChar));

                int unit_ID_str_index = unitFolderName.IndexOf("- ");
                string unit_ID_str = string.Empty;
                if (unit_ID_str_index >= 0)
                    unit_ID_str = unitFolderName.Substring(unit_ID_str_index + 2, unitFolderName.Length - unit_ID_str_index - 2);

                uint unit_ID = Convert.ToUInt32(unit_ID_str);
                Unit_Files_List unit_Files = unit_Files_List.FirstOrDefault(x => x.Unit_ID == unit_ID);

                if (unit_ID >= 0x13921 && unit_ID <= 0x139fd && unit_ID != 0x13949 && unit_Files != null && unit_Files.MBONAdded && !already_repacked.Contains(unit_ID)) 
                {
                    string reimportFolder = totalMBONReimportFolder + @"\" + unitFolderName;
                    string reimportConvertedfromMBONFolder = totalMBONReimportFolder + @"\" + unitFolderName + @"\" + "Converted from MBON";
                    string reimportFilestoRepack = totalMBONReimportFolder + @"\" + unitFolderName + @"\" + "Files to Repack";
                    string reimportRepackedFiles = totalMBONReimportFolder + @"\" + unitFolderName + @"\" + "Repacked Files";

                    Directory.CreateDirectory(reimportFolder);
                    Directory.CreateDirectory(reimportConvertedfromMBONFolder);
                    Directory.CreateDirectory(reimportFilestoRepack);
                    Directory.CreateDirectory(reimportRepackedFiles);

                    string extractMBONFolder = unitFolder + @"\Extracted MBON";
                    string originalMBONFolder = unitFolder + @"\Original MBON";
                    List<string> dataFolder = Directory.GetDirectories(extractMBONFolder, "*", SearchOption.TopDirectoryOnly).ToList();
                    dataFolder = dataFolder.Where(x => x.Contains("Data")).ToList();
                    if (dataFolder.Count() == 0 || dataFolder.Count() > 0x1)
                        throw new Exception();

                    List<string> script1Folder = Directory.GetDirectories(extractMBONFolder, "*", SearchOption.TopDirectoryOnly).ToList();
                    script1Folder = script1Folder.Where(x => x.Contains("Script 1")).ToList();
                    if (script1Folder.Count() == 0 || script1Folder.Count() > 0x1)
                        throw new Exception();

                    List<string> EIDXFolder = Directory.GetDirectories(extractMBONFolder, "*", SearchOption.TopDirectoryOnly).ToList();
                    EIDXFolder = EIDXFolder.Where(x => x.Contains("EIDX")).ToList();
                    if (EIDXFolder.Count() == 0 || EIDXFolder.Count() > 0x1)
                        throw new Exception();

                    List<string> SEFolder = Directory.GetDirectories(extractMBONFolder, "*", SearchOption.TopDirectoryOnly).ToList();
                    SEFolder = SEFolder.Where(x => x.Contains("Sound Effects")).ToList();
                    if (SEFolder.Count() == 0 || SEFolder.Count() > 0x1)
                        throw new Exception();

                    List<string> DNSOFolder = Directory.GetDirectories(extractMBONFolder, "*", SearchOption.TopDirectoryOnly).ToList();
                    DNSOFolder = DNSOFolder.Where(x => x.Contains("DNSO")).ToList();
                    if (DNSOFolder.Count() == 0 || DNSOFolder.Count() > 0x1)
                        throw new Exception();

                    string script1 = script1Folder[0];

                    string data = dataFolder[0];

                    string EIDX = EIDXFolder[0];

                    string SE = SEFolder[0];

                    string DNSO = DNSOFolder[0];

                    // -------------------------------------------- Unit Data Parse --------------------------------------------
                    /// Variables
                    Properties.Settings.Default.inputUnitDataBinary = data + @"\001-FHM\002.bin";
                    Properties.Settings.Default.inputUnitDataReloadBinary = data + @"\001-FHM\010.bin";

                    Directory.CreateDirectory(reimportConvertedfromMBONFolder + @"\Unit Variables");
                    Properties.Settings.Default.outputUnitDataJSONPath = reimportConvertedfromMBONFolder + @"\Unit Variables";

                    new Parse_Unit_Data().readVariables();

                    Properties.Settings.Default.inputUnitDataJSON = reimportConvertedfromMBONFolder + @"\Unit Variables" + @"\UnitData.JSON";

                    Properties.Settings.Default.outputUnitDataBinaryPath = reimportConvertedfromMBONFolder + @"\Unit Variables";

                    new Parse_Unit_Data().writeVariables();

                    /// Script
                    Directory.CreateDirectory(reimportConvertedfromMBONFolder + @"\Script");
                    Properties.Settings.Default.outputScriptFolderPath = reimportConvertedfromMBONFolder + @"\Script";

                    Properties.Settings.Default.B4ACFilePath = data + @"\001-FHM\011.bin";

                    new Generate_Script_B4AC();

                    Properties.Settings.Default.inputMeleeVarBinaryPath = data + @"\001-FHM\007.bin";

                    new Parse_Melee_Variables();


                    // -------------------------------------------- Script Refactor --------------------------------------------
                    Properties.Settings.Default.BABBFilePath = script1 + @"\001-FHM\002.bin";
                    Properties.Settings.Default.outputScriptFolderPath = totalMBONScriptFolder + @"\Refactored Script";

                    Properties.Settings.Default.scriptBigEndian = false;
                    Properties.Settings.Default.CScriptFilePath = totalMBONScriptFolder + @"\Script\" + unitFolderName + @".c";
                    Properties.Settings.Default.MinScriptPointer = 100000;

                    Properties.Settings.Default.scriptModifyLink = true;
                    Properties.Settings.Default.scriptModifyRefactor = true;
                    Properties.Settings.Default.inputScriptRefactorTxtFolder = reimportConvertedfromMBONFolder + @"\Script";

                    new ModifyUnitScript();

                    compileMSCwithFix(unitFolderName);

                    
                    // -------------------------------------------- Audio Files -----------------------------------------------------

                    /// Logic 
                    Directory.CreateDirectory(reimportConvertedfromMBONFolder + @"\Voice Data");

                    Properties.Settings.Default.inputVoiceLogicBinary = data + @"\001-FHM\006.bin";
                    Properties.Settings.Default.outputVoiceLogicJSONFolder = reimportConvertedfromMBONFolder + @"\Voice Data";

                    new Parse_Voice_Line_Logic().deserializeVoiceLogicBinary();

                    Properties.Settings.Default.inputVoiceLogicJSON = reimportConvertedfromMBONFolder + @"\Voice Data\006.JSON";
                    Properties.Settings.Default.outputVoiceLogicBinaryFolder = reimportConvertedfromMBONFolder + @"\Voice Data";

                    new Parse_Voice_Line_Logic().serializeVoiceLogicBinary();


                    // Sound Effects
                    Directory.CreateDirectory(reimportConvertedfromMBONFolder + @"\Sound Effects");

                    Properties.Settings.Default.Nus3SoundHashFormat = (int)audioFormatEnum.VAG;
                    Properties.Settings.Default.soundHashMainTitle = ""; // SE does not care about this

                    Properties.Settings.Default.inputNus3File = extractMBONFolder + @"\Sound Effects.nus3bank";
                    Properties.Settings.Default.outputNameandHashFolder = reimportConvertedfromMBONFolder + @"\Sound Effects";

                    // For the soundhashes
                    new nus3AudioNameHash((audioFormatEnum)Properties.Settings.Default.Nus3SoundHashFormat, Properties.Settings.Default.soundHashMainTitle);

                    // Generate file Infos
                    Properties.Settings.Default.inputAudioPACInfoFolder = extractMBONFolder + @"\Sound Effects";
                    Properties.Settings.Default.audioPACInfoSTREAMName = "003.STREAM";
                    Properties.Settings.Default.outputAudioPACInfoFolder = reimportConvertedfromMBONFolder + @"\Sound Effects";

                    Properties.Settings.Default.audioPACInfoNus3SoundHashFormat = (int)audioFormatEnum.VAG;

                    new GenerateAudioPACInfo((audioFormatEnum)Properties.Settings.Default.audioPACInfoNus3SoundHashFormat);


                    // -------------------------------------------- Image Files -----------------------------------------------------




                    // -------------------------------------------- EIDX ------------------------------------------------------------

                    Directory.CreateDirectory(reimportConvertedfromMBONFolder + @"\EIDX");

                    Properties.Settings.Default.ALEOFolderPath = EIDX + @"\001-FHM\002-FHM\";
                    Properties.Settings.Default.outputALEOFolderPath = reimportConvertedfromMBONFolder + @"\EIDX";

                    new ParseALEO();
                    
                    
                    // -------------------------------------------- DNSO ------------------------------------------------------------

                    FileStream dnsoFS = File.OpenRead(DNSO + @"\001-FHM\002-FHM\003.bin");

                    dnsoFS.Seek(4, SeekOrigin.Begin);

                    uint ammoCount = readUIntBigEndian(dnsoFS);

                    // This will cause crash
                    if (ammoCount > 12)
                        throw new Exception(); 

                    dnsoFS.Close();
                    

                    // -------------------------------------------- Prepare Repack Files --------------------------------------------

                    repackFiles(
                        reimportRepackedFiles,
                        reimportFilestoRepack,
                        unitFolderName,
                        data,
                        reimportConvertedfromMBONFolder,
                        EIDX,
                        SE,
                        unit_Files
                        );

                    // Get unit's english name
                    UnitIDList unit_Infos = load_UnitID();
                    string unitName = unit_Infos.Unit_ID.FirstOrDefault(s => s.id == unit_Files.Unit_ID).name_english.Replace(" ", "_");
                    unitName = unitName.Replace(".", "_");
                    unitName = unitName.Replace("∀", "Turn_A");

                    string basePsarcRepackFolder = totalMBONCombinedPsarcFolder + @"\Units\FB_Units\" + unitName;
                    string[] allRepackedPACs = Directory.GetFiles(reimportRepackedFiles, "*", SearchOption.TopDirectoryOnly);


                    string Data_Path = basePsarcRepackFolder + @"\Data\PATCH" + unit_Files.data_and_script_PAC_hash.ToString("X8") + ".PAC";

                    FileStream dataFS = File.OpenRead(allRepackedPACs.FirstOrDefault(s => s.Contains("Data")));

                    dataFS.Seek(0, SeekOrigin.Begin);

                    FileStream newDataFS = File.Create(Data_Path);

                    dataFS.CopyTo(newDataFS);

                    dataFS.Close();

                    newDataFS.Close();

                    
                    string EIDX_Path = basePsarcRepackFolder + @"\EIDX\PATCH" + unit_Files.effects_EIDX_PAC_hash.ToString("X8") + ".PAC";

                    FileStream EIDXFS = File.OpenRead(allRepackedPACs.FirstOrDefault(s => s.Contains("EIDX")));

                    EIDXFS.Seek(0, SeekOrigin.Begin);

                    FileStream newEIDXFS = File.Create(EIDX_Path);

                    EIDXFS.CopyTo(newEIDXFS);

                    EIDXFS.Close();

                    newEIDXFS.Close();
                    

                    // Write new PATCH.TBL
                    tocFileInfo = rewritePsarcTBL(tocFileInfo, reimportRepackedFiles, originalMBONFolder, unit_Files);

                    Properties.Settings.Default.inputPsarcJSON = totalMBONCombinedPsarcFolder + @"\PATCH.json";
                    Properties.Settings.Default.outputPsarcTBLBinaryNameasPatch = true;
                    Properties.Settings.Default.outputPsarcTBLBinary = totalMBONCombinedPsarcFolder;

                    new RepackPsarcV2().exportToc(tocFileInfo);

                    already_repacked.Add(unit_ID);
                    string updateJSON = JsonConvert.SerializeObject(already_repacked, Formatting.Indented);
                    StreamWriter sw = File.CreateText(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\temp_unit_list.json");
                    sw.Write(updateJSON);
                    sw.Close();
                }
            }
        }

        public TOCFileInfo rewritePsarcTBL(TOCFileInfo tocFileInfo, string reimportRepackedFiles, string originalMBONFolder, Unit_Files_List unit_Files)
        {
            List<PACFileInfoV2> fileInfos = tocFileInfo.allFiles;

            // Get unit's english name
            UnitIDList unit_Infos = load_UnitID();
            string unitName = unit_Infos.Unit_ID.FirstOrDefault(s => s.id == unit_Files.Unit_ID).name_english.Replace(" ", "_");
            unitName = unitName.Replace(".", "_");
            unitName = unitName.Replace("∀", "Turn_A");

            Directory.CreateDirectory(totalMBONCombinedPsarcFolder + @"\Units\");
            string basePsarcRepackFolder = totalMBONCombinedPsarcFolder + @"\Units\" + unitName;
            Directory.CreateDirectory(basePsarcRepackFolder);

            Directory.CreateDirectory(basePsarcRepackFolder + @"\Data");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\DNSO");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\EIDX");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Global_Pilot_Voices");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\KPKP");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Local_Pilot_Voices");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Model_and_Texture");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\OMO");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Sound_Effects");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Sprites");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Sprites" + @"\Arcade_Sprites");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Sprites" + @"\Free_Battle_Sprites");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Sprites" + @"\Loading_Ally_Sprite");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Sprites" + @"\Loading_Enemy_Sprite");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Sprites" + @"\Loading_Enemy_Target_Pilot_Sprite");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Sprites" + @"\Loading_Enemy_Target_Unit_Sprite");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Sprites" + @"\Result_Small_Sprite");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Sprites" + @"\Sortie_and_Awakening_Sprites");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Sprites" + @"\Target_Small_Sprite");
            Directory.CreateDirectory(basePsarcRepackFolder + @"\Voice_File_List");

            List<uint> newPACs = new List<uint>();

            string DNSO_Path = basePsarcRepackFolder + @"\DNSO\PATCH" + unit_Files.weapon_image_DNSO_PAC_hash.ToString("X8") + ".PAC";
            string KPKP_Path = basePsarcRepackFolder + @"\KPKP\PATCH" + unit_Files.sortie_mouth_anim_enum_KPKP_PAC_hash.ToString("X8") + ".PAC";
            string Model_and_Texture_Path = basePsarcRepackFolder + @"\Model_and_Texture\PATCH" + unit_Files.model_and_texture_PAC_hash.ToString("X8") + ".PAC";
            string OMO_Path = basePsarcRepackFolder + @"\OMO\PATCH" + unit_Files.animation_OMO_PAC_hash.ToString("X8") + ".PAC";
            string Data_Path = basePsarcRepackFolder + @"\Data\PATCH" + unit_Files.data_and_script_PAC_hash.ToString("X8") + ".PAC";
            string EIDX_Path = basePsarcRepackFolder + @"\EIDX\PATCH" + unit_Files.effects_EIDX_PAC_hash.ToString("X8") + ".PAC";
            string SE_Path = basePsarcRepackFolder + @"\Sound_Effects\PATCH" + unit_Files.sound_effect_PAC_hash.ToString("X8") + ".PAC";

            newPACs.Add(unit_Files.weapon_image_DNSO_PAC_hash);
            newPACs.Add(unit_Files.sortie_mouth_anim_enum_KPKP_PAC_hash);
            newPACs.Add(unit_Files.model_and_texture_PAC_hash);
            newPACs.Add(unit_Files.animation_OMO_PAC_hash);
            newPACs.Add(unit_Files.data_and_script_PAC_hash);
            newPACs.Add(unit_Files.effects_EIDX_PAC_hash);
            newPACs.Add(unit_Files.sound_effect_PAC_hash);

            string[] allOriginalPACs = Directory.GetFiles(originalMBONFolder, "*", SearchOption.TopDirectoryOnly);

            FileStream DNSOFS = File.OpenRead(allOriginalPACs.FirstOrDefault(s => s.Contains("DNSO")));
            FileStream KPKPFS = File.OpenRead(allOriginalPACs.FirstOrDefault(s => s.Contains("KPKP")));
            FileStream Model_and_Texture_FS = File.OpenRead(allOriginalPACs.FirstOrDefault(s => s.Contains("Model and Texture")));
            FileStream OMOFS = File.OpenRead(allOriginalPACs.FirstOrDefault(s => s.Contains("OMO")));

            DNSOFS.Seek(0, SeekOrigin.Begin);
            KPKPFS.Seek(0, SeekOrigin.Begin);
            Model_and_Texture_FS.Seek(0, SeekOrigin.Begin);
            OMOFS.Seek(0, SeekOrigin.Begin);

            FileStream newDNSOFS = File.Create(DNSO_Path);
            DNSOFS.CopyTo(newDNSOFS);

            FileStream newKPKPFS = File.Create(KPKP_Path);
            KPKPFS.CopyTo(newKPKPFS);

            FileStream newModel_and_Texture_FS = File.Create(Model_and_Texture_Path);
            Model_and_Texture_FS.CopyTo(newModel_and_Texture_FS);

            FileStream newOMOFS = File.Create(OMO_Path);
            OMOFS.CopyTo(newOMOFS);

            DNSOFS.Close();
            KPKPFS.Close();
            Model_and_Texture_FS.Close();
            OMOFS.Close();

            newDNSOFS.Close();
            newKPKPFS.Close();
            newModel_and_Texture_FS.Close();
            newOMOFS.Close();

            string[] allRepackedPACs = Directory.GetFiles(reimportRepackedFiles, "*", SearchOption.TopDirectoryOnly);

            FileStream dataFS = File.OpenRead(allRepackedPACs.FirstOrDefault(s => s.Contains("Data")));
            FileStream EIDXFS = File.OpenRead(allRepackedPACs.FirstOrDefault(s => s.Contains("EIDX")));
            FileStream SEFS = File.OpenRead(allRepackedPACs.FirstOrDefault(s => s.Contains("Sound Effects")));

            dataFS.Seek(0, SeekOrigin.Begin);
            EIDXFS.Seek(0, SeekOrigin.Begin);
            SEFS.Seek(0, SeekOrigin.Begin);

            FileStream newDataFS = File.Create(Data_Path);
            FileStream newEIDXFS = File.Create(EIDX_Path);
            FileStream newSEFS = File.Create(SE_Path);

            dataFS.CopyTo(newDataFS);
            EIDXFS.CopyTo(newEIDXFS);
            SEFS.CopyTo(newSEFS);

            dataFS.Close();
            EIDXFS.Close();
            SEFS.Close();

            newDataFS.Close();
            newEIDXFS.Close();
            newSEFS.Close();

            string[] allPsarcRepackPACFiles = Directory.GetFiles(basePsarcRepackFolder, "*.PAC", SearchOption.AllDirectories);

            foreach(var file in newPACs)
            {
                PACFileInfoV2 data_file_info = new PACFileInfoV2();

                string relativePathPACFolder = fetchRelativePathFolderName(unit_Files, file);
                string absoluteFilePath = allPsarcRepackPACFiles.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s) == "PATCH" + file.ToString("X8"));

                int index = searchPACHash(file);
                if(index != -1)
                {
                    data_file_info.fileFlags = PACFileInfoV2.fileFlagsEnum.hasFilePath | PACFileInfoV2.fileFlagsEnum.hasFileName | PACFileInfoV2.fileFlagsEnum.hasFileInfo;
                    data_file_info.patchNo = PACFileInfoV2.patchNoEnum.PATCH_6;
                    data_file_info.namePrefix = PACFileInfoV2.prefixEnum.PATCH;
                    data_file_info.unk04 = 262144;
                    data_file_info.Size1 = 0;
                    data_file_info.Size2 = 0;
                    data_file_info.Size3 = 0;
                    data_file_info.unk00 = 0;
                    data_file_info.nameHash = file;
                    data_file_info.relativePatchPath = "patch_06_00/Units/" + unitName + @"/" + relativePathPACFolder + @"/PATCH" + file.ToString("X8") + ".PAC";
                    data_file_info.hasRelativePatchSubPath = true;
                    data_file_info.filePath = absoluteFilePath;
                    data_file_info.fileInfoIndex = index;
                }

                fileInfos.Add(data_file_info);
            }

            return tocFileInfo;
        }

        public string fetchRelativePathFolderName(Unit_Files_List unit_Files, uint target)
        {
            if(target == unit_Files.data_and_script_PAC_hash)
                return "Data";

            if (target == unit_Files.model_and_texture_PAC_hash)
                return "Model_and_Texture";

            if (target == unit_Files.animation_OMO_PAC_hash)
                return "OMO";

            if (target == unit_Files.effects_EIDX_PAC_hash)
                return "EIDX";

            if (target == unit_Files.sound_effect_PAC_hash)
                return "Sound_Effects";

            if (target == unit_Files.global_pilot_voices_PAC_hash)
                return "Global_Pilot_Voices";

            if (target == unit_Files.weapon_image_DNSO_PAC_hash)
                return "DNSO";

            /*
            if (target == unit_Files.sortie_and_awakening_sprites_PAC_hash)
                return "asd";
            */ 

            if (target == unit_Files.sortie_mouth_anim_enum_KPKP_PAC_hash)
                return "KPKP";

            if (target == unit_Files.voice_file_list_PAC_hash)
                return "Voice_File_List";

            if (target == unit_Files.local_pilot_voices_STREAM_PAC_hash)
                return "Local_Pilot_Voices";

            return string.Empty;
        }

        public int searchPACHash(uint nameHash)
        {
            List<byte[]> DATATBLs = new List<byte[]> {
                Properties.Resources.DATA,
                Properties.Resources._01_PATCH,
                Properties.Resources._02_PATCH,
                Properties.Resources._03_PATCH,
                Properties.Resources._04_PATCH,
                Properties.Resources._05_PATCH,
                Properties.Resources._06_PATCH,
            };

            foreach (byte[] DATATBL in DATATBLs)
            {
                int index = searchTBLIndex(DATATBL, nameHash);
                if (index != -1)
                {
                    return index;
                }
            }

            return -1;
        }

        private int searchTBLIndex(byte[] TBL, uint nameHash)
        {
            MemoryStream TBLS = new MemoryStream(TBL);
            byte[] nameHashBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(nameHash));
            int nameHashPosition = Search(TBL, nameHashBytes);

            if (nameHashPosition != -1)
            {
                int fileInfoPosition = nameHashPosition - 0x1C;
                byte[] fileInfoPositionBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(fileInfoPosition));
                int indexPosition = Search(TBL, fileInfoPositionBytes);

                if (indexPosition == -1)
                    return -1;

                byte[] temp = new byte[4];
                TBLS.Seek(0x8, SeekOrigin.Begin);
                TBLS.Read(temp, 0, 4);
                int skipRange = BinaryPrimitives.ReadInt32BigEndian(temp) * 4;

                int startingPoint = skipRange + 0x10;

                int index = (indexPosition - startingPoint) / 4;

                return index;
            }
            else
            {
                return -1;
            }
        }

        public void repackFiles(
            string reimportRepackedFiles, 
            string reimportFilestoRepack, 
            string unitFolderName, 
            string data, 
            string reimportConvertedfromMBONFolder, 
            string ExtractedMBONEIDXFolder,
            string ExtractedMBONSoundEffectsFolder,
            Unit_Files_List unit_Files
            )
        {
            RepackPAC repackInstance = new RepackPAC(Properties.Settings.Default.OutputRepackPAC);
            Properties.Settings.Default.OutputRepackPAC = reimportRepackedFiles;

            string data_folder_path = reimportFilestoRepack + @"\Data - " + unit_Files.data_and_script_PAC_hash.ToString("X8");
            string EIDX_folder_path = reimportFilestoRepack + @"\EIDX - " + unit_Files.effects_EIDX_PAC_hash.ToString("X8");
            string SE_folder_path = reimportFilestoRepack + @"\Sound Effects - " + unit_Files.sound_effect_PAC_hash.ToString("X8");
            string global_pilot_voices_folder_path = reimportFilestoRepack + @"\Global Pilot Voices - " + unit_Files.global_pilot_voices_PAC_hash.ToString("X8");
            string voice_file_list_folder_path = reimportFilestoRepack + @"\Voice File List - " + unit_Files.voice_file_list_PAC_hash.ToString("X8");
            string local_pilot_voices_folder_path = reimportFilestoRepack + @"\Local Pilot Voices - " + unit_Files.local_pilot_voices_STREAM_PAC_hash.ToString("X8");

            Directory.CreateDirectory(data_folder_path);
            Directory.CreateDirectory(EIDX_folder_path);
            Directory.CreateDirectory(SE_folder_path);
            Directory.CreateDirectory(global_pilot_voices_folder_path);
            Directory.CreateDirectory(voice_file_list_folder_path);
            Directory.CreateDirectory(local_pilot_voices_folder_path);

            
            /// Repack Data Folder
            DirectoryCopy(repackTemplates + @"\Data", data_folder_path, true);

            string data_001FHM_path = data_folder_path + @"\001-FHM\";

            FileStream fs002 = File.Create(data_001FHM_path + @"\002.bin");
            FileStream dataFS = File.OpenRead(reimportConvertedfromMBONFolder + @"\Unit Variables\UnitData.bin");
            dataFS.Seek(0, SeekOrigin.Begin);
            dataFS.CopyTo(fs002);
            fs002.Close();
            dataFS.Close();

            FileStream fs003 = File.Create(data_001FHM_path + @"\003.bin");
            FileStream MBON003FS = File.OpenRead(data + @"\001-FHM\003.bin");
            MBON003FS.Seek(0, SeekOrigin.Begin);
            MBON003FS.CopyTo(fs003);
            fs003.Close();
            MBON003FS.Close();

            FileStream fs005 = File.Create(data_001FHM_path + @"\005.bin");
            FileStream MBON005FS = File.OpenRead(data + @"\001-FHM\005.bin");
            MBON005FS.Seek(0, SeekOrigin.Begin);
            MBON005FS.CopyTo(fs005);
            fs005.Close();
            MBON005FS.Close();

            FileStream fs006 = File.Create(data_001FHM_path + @"\006.bin");
            FileStream mscFS = File.OpenRead(totalMBONScriptFolder + @"\Compiled Refactored Script\" + unitFolderName + ".mscsb");
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
            

            
            // EIDX folder
            DirectoryCopy(ExtractedMBONEIDXFolder, EIDX_folder_path, true);

            DirectoryCopy(reimportConvertedfromMBONFolder + @"\EIDX", EIDX_folder_path + @"\001-FHM\002-FHM\", true);

            Properties.Settings.Default.OpenRepackPath = EIDX_folder_path;

            repackInstance = new RepackPAC(Properties.Settings.Default.OutputRepackPAC);
            repackInstance.initializePACInfoFileRepack();
            repackInstance.parseInfo();
            repackInstance.repackPAC();
            

            
            // Sound Effect folder
            DirectoryCopy(repackTemplates + @"\Sound Effects", SE_folder_path, true);

            DirectoryCopy(ExtractedMBONSoundEffectsFolder, SE_folder_path + @"\001-FHM\002-FHM\", true);

            // - Edit PAC Info File
            StreamReader SEPACInfoSR = File.OpenText(reimportConvertedfromMBONFolder + @"\Sound Effects\Sound Effects PACInfo.txt");
            string SEPACInfo = SEPACInfoSR.ReadToEnd();
            SEPACInfoSR.Close();

            StreamReader RepackSEPACInfoSR = File.OpenText(SE_folder_path + @"\PAC.info");
            string RepackSEPACInfo = RepackSEPACInfoSR.ReadToEnd();
            RepackSEPACInfoSR.Close();

            StreamWriter RepackSEPACInfoSW = File.CreateText(SE_folder_path + @"\PAC.info");
            
            string modifiedSEPACInfo = Regex.Replace(RepackSEPACInfo, 
                @"(Number of audio files: [0-9]{1,100}(\r\n|\r|\n)+fileName: 003.STREAM(\r\n|\r|\n)+#Sound: [\s\S]*?(?=[/][/]))",
                SEPACInfo);

            RepackSEPACInfoSW.Write(modifiedSEPACInfo);
            RepackSEPACInfoSW.Close();

            // - Replace the soundhash files
            FileStream oriSESoundHash = File.OpenRead(reimportConvertedfromMBONFolder + @"\Sound Effects\Sound Effects.soundhash");
            FileStream tarSESoundHash = File.Create(SE_folder_path + @"\001-FHM\002-FHM\004.soundhashes");
            oriSESoundHash.Seek(0, SeekOrigin.Begin);
            oriSESoundHash.CopyTo(tarSESoundHash);
            oriSESoundHash.Close();
            tarSESoundHash.Close();
            
            //File.Move(reimportConvertedfromMBONFolder + @"\Sound Effects\Sound Effects.soundhash", SE_folder_path + @"\001-FHM\002-FHM\004.soundhashes");

            Properties.Settings.Default.OpenRepackPath = SE_folder_path;

            repackInstance = new RepackPAC(Properties.Settings.Default.OutputRepackPAC);
            repackInstance.initializePACInfoFileRepack();
            repackInstance.parseInfo();
            repackInstance.repackPAC();
            
        }

        public void fixsys_2badref()
        {
            string totalMBONScriptOri = totalMBONScriptFolder + @"\Script";
            string totalMBONScriptFuncPointers = totalMBONScriptFolder + @"\Script func pointers";

            List<string> allScripts = Directory.GetFiles(totalMBONScriptOri, "*", SearchOption.AllDirectories).ToList();
            foreach (var scriptpath in allScripts)
            {
                StreamReader sr = File.OpenText(scriptpath);
                string script = sr.ReadToEnd();
                sr.Close();

                string scriptName = Path.GetFileNameWithoutExtension(scriptpath);
                int unit_ID_str_index = scriptName.IndexOf("- ");
                string unit_ID_str = string.Empty;
                if (unit_ID_str_index >= 0)
                    unit_ID_str = scriptName.Substring(unit_ID_str_index + 2, scriptName.Length - unit_ID_str_index - 2);

                List<string> allFuncPointers = Directory.GetFiles(totalMBONScriptFuncPointers, "*", SearchOption.AllDirectories).ToList();
                string funcPointerPath = allFuncPointers.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(scriptName));

                Dictionary<uint, uint> funcPointers = new Dictionary<uint, uint>();
                StreamReader funcPointerSR = File.OpenText(funcPointerPath);

                funcPointerSR.ReadLine();
                while (!funcPointerSR.EndOfStream)
                {
                    string funcStr = funcPointerSR.ReadLine();
                    uint.TryParse(funcStr, out uint func);
                    string pointerStr = funcPointerSR.ReadLine();
                    uint.TryParse(pointerStr, out uint pointer);

                    funcPointers[func] = pointer;
                }

                funcPointerSR.Close();

                //Match incorrectRef = Regex.Match(script, @"sys_2[(]0x2, global[0-9]{1,100}, func_([0-9]{1,100})[)]");
                //Match incorrectRef = Regex.Match(script, @"sys_1[(]func_([0-9]{1,100}), ([a-fA-Fx0-9]{1,100})[)];");
                //Match incorrectRef = Regex.Match(script, @"sys_1[(]func_([0-9]{1,100}), ([a-fA-Fx0-9]{1,100}), ([\s\S]*?)[)];");
                Match incorrectRef = Regex.Match(script, @"sys_8[(]global212, ([\S]*?(?=,)), ([\S]*?(?=,)), ([\S]*?(?=,)), func_([0-9]{1,100})[)];");
                while (incorrectRef.Success)
                {
                    /*
                    uint.TryParse(incorrectRef.Groups[1].Value, out uint incorrectRefFunc);
                    uint correctPointer = funcPointers[incorrectRefFunc];
                    correctPointer -= 0x30;
                    script = Regex.Replace(script, @"sys_2[(]0x2, global[0-9]{1,100}, func_" + incorrectRef.Groups[1].Value + @"[)]", @"sys_2(0x2, global212, 0x" + correctPointer.ToString("X").ToLower() + ")");
                    incorrectRef = Regex.Match(script, @"sys_2[(]0x2, global[0-9]{1,100}, func_([0-9]{1,100})[)]");
                    */

                    /*
                    uint.TryParse(incorrectRef.Groups[1].Value, out uint incorrectRefFunc);
                    uint correctPointer = funcPointers[incorrectRefFunc];
                    correctPointer -= 0x30;

                    if(correctPointer == 0x3000A)
                    {
                        script = Regex.Replace(script, @"sys_1[(]func_([0-9]{1,100}), " + incorrectRef.Groups[2].Value + @"[)];", @"sys_1(0x3000a, " + incorrectRef.Groups[2].Value + @");");
                        incorrectRef = Regex.Match(script, @"sys_1[(]func_([0-9]{1,100}), ([a-fA-Fx0-9]{1,100})[)];");
                    }

                    if (correctPointer == 0x30011)
                    {
                        script = Regex.Replace(script, @"sys_1[(]func_([0-9]{1,100}), " + incorrectRef.Groups[2].Value + @"[)];", @"sys_1(0x30011, " + incorrectRef.Groups[2].Value + @");");
                        incorrectRef = Regex.Match(script, @"sys_1[(]func_([0-9]{1,100}), ([a-fA-Fx0-9]{1,100})[)];");
                    }
                    */

                    /*

                    uint.TryParse(incorrectRef.Groups[1].Value, out uint incorrectRefFunc);
                    uint correctPointer = funcPointers[incorrectRefFunc];
                    correctPointer -= 0x30;

                    if(correctPointer == 0x30011)
                    {
                        script = Regex.Replace(script, @"sys_1[(]func_([0-9]{1,100}), " + incorrectRef.Groups[2].Value + @", " + incorrectRef.Groups[3].Value + @"[)];", @"sys_1(0x30011, " + incorrectRef.Groups[2].Value + @", " + incorrectRef.Groups[3].Value + @");");
                        incorrectRef = Regex.Match(script, @"sys_1[(]func_([0-9]{1,100}), ([a-fA-Fx0-9]{1,100}), ([\s\S]*?)[)];");
                    }
                    else
                    {

                    }

                    */

                    uint.TryParse(incorrectRef.Groups[4].Value, out uint incorrectRefFunc);
                    uint correctPointer = funcPointers[incorrectRefFunc];
                    correctPointer -= 0x30;

                    script = Regex.Replace(script, 
                        @"sys_8[(]global212, " + incorrectRef.Groups[1].Value + ", " + incorrectRef.Groups[2].Value + ", " + incorrectRef.Groups[3].Value + ", func_" + incorrectRef.Groups[4].Value + "[)];",
                        @"sys_8(global212, " + incorrectRef.Groups[1].Value + ", " + incorrectRef.Groups[2].Value + ", " + incorrectRef.Groups[3].Value + ", 0x" + correctPointer.ToString("x") + ");");
                    incorrectRef = Regex.Match(script, @"sys_8[(]global212, ([\S]*?(?=,)), ([\S]*?(?=,)), ([\S]*?(?=,)), func_([0-9]{1,100})[)];");
                    
                }

                StreamWriter sw = File.CreateText(totalMBONScriptFolder + @"\fixed\" + scriptName + ".c");
                sw.Write(script);
                sw.Close();
            }
        }

        public void compileMSCwithFix(string unitFolderName)
        {
            string CS = File.ReadAllText(Properties.Settings.Default.CScriptFilePath);
            int reset_0x2E_Count = 0;

            if (CS.Contains(@"sys_2D(0x3, 0xd, var1, func_"))
                reset_0x2E_Count++;

            if (CS.Contains(@"sys_2D(0x3, 0xe, var1, func_"))
                reset_0x2E_Count++;

            if (CS.Contains(@"sys_2D(0x3, 0xf, var1, func_"))
                reset_0x2E_Count++;

            string inputCFilePath = totalMBONScriptFolder + @"\Refactored Script\" + unitFolderName + ".c";
            string outputCFilePath = totalMBONScriptFolder + @"\Compiled Refactored Script\" + unitFolderName + ".mscsb";

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



        /*
        public void getAllSelectSprites()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(@"E:\MBON\Image0\archives\");
            List<FileInfo> allBin =  directoryInfo.GetFiles("*", SearchOption.AllDirectories).ToList();
            //List<string> allBin = Directory.GetFiles(@"E:\MBON\Image0\archives\", "*", SearchOption.AllDirectories).ToList();
            List<FileInfo> binaryFiles = allBin.Where(x => x.Length == 0x7F9060).ToList();

            DirectoryInfo allUnitsDI = new DirectoryInfo(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\All Playable Unit Image & Sound Effects");
            List<FileSystemInfo> allExistingUnit = allUnitsDI.GetFileSystemInfos("*", SearchOption.AllDirectories).ToList();
            List<FileSystemInfo> allExistingUnitSelect = allExistingUnit.Where(x => x.Attributes == FileAttributes.Directory).Where(s => Path.GetFileNameWithoutExtension(s.FullName).Contains("Arcade Sprite")).ToList();

            List<string> allSelectHashes = new List<string>();

            foreach(var select in allExistingUnitSelect)
            {
                string filePath = Path.GetFileNameWithoutExtension(select.FullName);
                int binaryHash = filePath.IndexOf("- ");
                string binaryHash_str = string.Empty;
                if (binaryHash >= 0)
                    binaryHash_str = filePath.Substring(binaryHash + 2, filePath.Length - binaryHash - 2);

                allSelectHashes.Add(binaryHash_str);
            }

            foreach (var binary in binaryFiles)
            {
                string filePath = binary.FullName;
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                if (!allSelectHashes.Contains(fileName))
                {
                    FileStream stream = File.Open(filePath, FileMode.Open);
                    long streamSize = stream.Length;
                    stream.Close();

                    string baseExtractPath = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\All Select Sprites\" + fileName + @"\";

                    new PAC.Extract.ExtractPAC(filePath, stream).extractPAC(0, out long unused, baseExtractPath);
                }
            }
        }
        */
        
    }
}
