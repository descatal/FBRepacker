using FBRepacker.Data.DataTypes;
using FBRepacker.Data.FB_Parse;
using FBRepacker.Data.FB_Parse.DataTypes;
using FBRepacker.Data.MBON_Parse;
using FBRepacker.PAC;
using FBRepacker.Psarc.V2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            //extractNPCImages();
            //extractUnitImages();
            //extractNPCSounds();
            //extractUnitImages();
            //generateMBONUnitListCSourceCode();
            //filter();
            //rename();
            //unitlistmacro();
            //commonspritemacro();
            //combine_patch_05_00();
            //checkmodeleffectsmacro();
            //checkmodelloadenumsmacro();
            //checkhitboxpropertiesmacro();
            //search_sprites();
            search_stages();
        }

        public void combine_patch_05_00()
        {
            List<string> newer = Directory.GetFiles(@"I:\Full Boost\MBON Reimport Project\Total MBON Combined Psarc", "*.PAC", SearchOption.AllDirectories).ToList();
            List<string> older = Directory.GetFiles(@"I:\Full Boost\MBON Reimport Project\1.09_Ori", "*.PAC", SearchOption.AllDirectories).ToList();
        
            Directory.CreateDirectory(@"I:\Full Boost\MBON Reimport Project\Total MBON Combined Psarc\patch_05_00_Ori\");


            StreamReader newJSONsr = File.OpenText(@"I:\Full Boost\MBON Reimport Project\Total MBON Combined Psarc\PATCH.json");
            string newJSON = newJSONsr.ReadToEnd();
            newJSONsr.Close();

            TOCFileInfo newPACInfo = JsonConvert.DeserializeObject<TOCFileInfo>(newJSON);


            StreamReader oldJSONsr = File.OpenText(@"I:\Full Boost\MBON Reimport Project\1.09_Ori\PATCH.json");
            string oldJSON = oldJSONsr.ReadToEnd();
            oldJSONsr.Close();

            TOCFileInfo oldPACInfo = JsonConvert.DeserializeObject<TOCFileInfo>(oldJSON);

            for (int i = 0; i < older.Count; i++)
            {
                string old = Path.GetFileNameWithoutExtension(older[i]);
                if(!newer.Exists(s => Path.GetFileNameWithoutExtension(s) == old))
                {
                    string oldPath = older.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s) == old);
                    string PACName = Path.GetFileNameWithoutExtension(oldPath);
                    string newPath = @"I:\Full Boost\MBON Reimport Project\Total MBON Combined Psarc\patch_05_00_Ori\" + PACName + @".PAC";

                    File.Copy(oldPath, newPath, true);

                    PACFileInfoV2 oldPAC = oldPACInfo.allFiles.FirstOrDefault(s => PACName.Contains(s.nameHash.ToString("X8")));

                    oldPAC.filePath = newPath;
                    oldPAC.relativePatchPath = "patch_05_00/patch_05_00_Ori/" + PACName + ".PAC";

                    newPACInfo.allFiles.Add(oldPAC);
                }
            }

            StreamWriter newJSONsw = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON Combined Psarc\PATCH.json");
            string combinedJSON = JsonConvert.SerializeObject(newPACInfo, Formatting.Indented);
            newJSONsw.Write(combinedJSON);
            newJSONsw.Close();
        }

        public void checkmodeleffectsmacro()
        {
            string JSONIPath = Properties.Settings.Default.inputFBUnitInfoListJSON;
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB JSON\Unit List.json";
            StreamReader ww = File.OpenText(JSONIPath);
            string JSON = ww.ReadToEnd();
            ww.Close();
            List<Unit_Info_List> unit_Info_Lists = JsonConvert.DeserializeObject<List<Unit_Info_List>>(JSON);

            string json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(json);

            json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\Unit List MBON.json").ReadToEnd();
            List<Unit_Info_List> unit_Info_List = JsonConvert.DeserializeObject<List<Unit_Info_List>>(json);

            List<string> allUnitFolders = Directory.GetDirectories(@"I:\Full Boost\MBON Reimport Project\Total MBON Export", "*", SearchOption.TopDirectoryOnly).ToList();

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
                    string extractMBONFolder = unitFolder + @"\Extracted MBON";

                    List<string> ModelandTextureFolder = Directory.GetDirectories(extractMBONFolder, "*", SearchOption.TopDirectoryOnly).ToList();
                    ModelandTextureFolder = ModelandTextureFolder.Where(x => x.Contains("Model and Texture")).ToList();
                    if (ModelandTextureFolder.Count() == 0 || ModelandTextureFolder.Count() > 0x1)
                        throw new Exception();

                    string modelandtexturefolder = ModelandTextureFolder[0];

                    string modeleffectbinary = Directory.GetFiles(modelandtexturefolder + @"\001-FHM\", "*.bin", SearchOption.TopDirectoryOnly).ToList().Last();

                    // -------------------------------------------- Model Effects --------------------------------------------
                    Model_Effects model_Effects = new Parse_Model_Effects().parse_Model_Effects_Data(modeleffectbinary);

                    foreach (var model_eff in model_Effects.model_hashes_and_data)
                    {
                        List<Model_Bone_Effect_Dataset> model_Bone_Effect_Datasets = model_eff.Value;
                        for(int k = 0; k < model_Bone_Effect_Datasets.Count; k++)
                        {
                            Model_Bone_Effect_Dataset model_Bone_Effect_Dataset = model_Bone_Effect_Datasets[k];
                            if(model_Bone_Effect_Dataset.unk_bone_dataset_enum == 0x1 && model_Bone_Effect_Dataset.dataset.Count >= 0x10)
                            {

                            }
                        }
                    }
                }
            }
        }

        public void checkmodelloadenumsmacro()
        {
            string JSONIPath = Properties.Settings.Default.inputFBUnitInfoListJSON;
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB JSON\Unit List.json";
            StreamReader ww = File.OpenText(JSONIPath);
            string JSON = ww.ReadToEnd();
            ww.Close();
            List<Unit_Info_List> unit_Info_Lists = JsonConvert.DeserializeObject<List<Unit_Info_List>>(JSON);

            string json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(json);

            json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\Unit List MBON.json").ReadToEnd();
            List<Unit_Info_List> unit_Info_List = JsonConvert.DeserializeObject<List<Unit_Info_List>>(json);

            List<string> allUnitFolders = Directory.GetDirectories(@"I:\Full Boost\MBON Reimport Project\Total MBON Export", "*", SearchOption.TopDirectoryOnly).ToList();

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

                if (unit_Files != null && unit_ID < 59900)
                {
                    string extractMBONFolder = unitFolder + @"\Extracted MBON";

                    List<string> ModelandTextureFolder = Directory.GetDirectories(extractMBONFolder, "*", SearchOption.TopDirectoryOnly).ToList();
                    ModelandTextureFolder = ModelandTextureFolder.Where(x => x.Contains("Model and Texture")).ToList();
                    if (ModelandTextureFolder.Count() == 0 || ModelandTextureFolder.Count() > 0x1)
                        throw new Exception();

                    string modelandtexturefolder = ModelandTextureFolder[0];

                    string modellistbinary = Directory.GetFiles(modelandtexturefolder + @"\001-FHM\", "002.bin", SearchOption.TopDirectoryOnly).ToList().First();

                    Model_List model_List = new Parse_Model_List().parse_model_list(modellistbinary);

                    foreach (var model in model_List.model_Hash_Info_List)
                    {
                        if(model.load_enum >= 8)
                        {

                        }
                    }
                }
            }
        }

        public void checkhitboxpropertiesmacro()
        {
            string JSONIPath = Properties.Settings.Default.inputFBUnitInfoListJSON;
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB JSON\Unit List.json";
            StreamReader ww = File.OpenText(JSONIPath);
            string JSON = ww.ReadToEnd();
            ww.Close();
            List<Unit_Info_List> unit_Info_Lists = JsonConvert.DeserializeObject<List<Unit_Info_List>>(JSON);

            string json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(json);

            json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\Unit List MBON.json").ReadToEnd();
            List<Unit_Info_List> unit_Info_List = JsonConvert.DeserializeObject<List<Unit_Info_List>>(json);

            List<string> allUnitFolders = Directory.GetDirectories(@"I:\Full Boost\MBON Reimport Project\Total MBON Export", "*", SearchOption.TopDirectoryOnly).ToList();

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

                if (unit_Files != null && unit_ID < 59900)
                {
                    string extractMBONFolder = unitFolder + @"\Extracted MBON";

                    List<string> dataFolder = Directory.GetDirectories(extractMBONFolder, "*", SearchOption.TopDirectoryOnly).ToList();
                    dataFolder = dataFolder.Where(x => x.Contains("Data")).ToList();
                    if (dataFolder.Count() == 0 || dataFolder.Count() > 0x1)
                        throw new Exception();

                    string data_Folder = dataFolder[0];

                    string hitboxbinary = Directory.GetFiles(data_Folder + @"\001-FHM\", "003.bin", SearchOption.TopDirectoryOnly).ToList().First();

                    Hitbox_Properties hitbox_Properties = new Parse_Hitbox_Properties().parse_Hitbox_Properties(hitboxbinary);

                    foreach (var melee_hitbox in hitbox_Properties.melee_Hitbox_Data)
                    {
                        List<hitbox_Data> first = melee_hitbox.all_Hitbox_Types.type_1_Hitboxes.hitbox_Datas;
                        checkHitboxMacro(first, data_Folder);
                        List<hitbox_Data> second = melee_hitbox.all_Hitbox_Types.type_2_Hitboxes.hitbox_Datas;
                        checkHitboxMacro(second, data_Folder);

                        // After investigation there's no type 3 to 5 data, just a constant 0x10 0 padding
                        /*
                        List<hitbox_Data> third = melee_hitbox.all_Hitbox_Types.type_3_Hitboxes.hitbox_Datas;

                        if(third.Count() != 0) // Always 0
                            throw new Exception();

                        checkHitboxMacro(third, data_Folder);

                        List<hitbox_Data> fourth = melee_hitbox.all_Hitbox_Types.type_4_Hitboxes.hitbox_Datas;

                        if (fourth.Count() != 0) // Always 0
                            throw new Exception();

                        checkHitboxMacro(fourth, data_Folder);

                        List<hitbox_Data> fifth = melee_hitbox.all_Hitbox_Types.type_5_Hitboxes.hitbox_Datas;

                        if (fifth.Count() != 0) // Always 0
                            throw new Exception();
                        
                        checkHitboxMacro(fifth, data_Folder);
                        */
                    }

                    if (hitbox_Properties.unk_Hitbox_Data.Count() == 0)
                        throw new Exception();

                    foreach (var unk_hitbox in hitbox_Properties.unk_Hitbox_Data)
                    {
                        List<hitbox_Data> first = unk_hitbox.all_Hitbox_Types.type_1_Hitboxes.hitbox_Datas;
                        checkHitboxMacro(first, data_Folder);
                        List<hitbox_Data> second = unk_hitbox.all_Hitbox_Types.type_2_Hitboxes.hitbox_Datas;
                        checkHitboxMacro(second, data_Folder);
                    }

                    foreach (var shield_hitbox in hitbox_Properties.shield_Hitbox_Data)
                    {
                        List<hitbox_Data> first = shield_hitbox.all_Hitbox_Types.type_1_Hitboxes.hitbox_Datas;
                        checkHitboxMacro(first, data_Folder);
                        List<hitbox_Data> second = shield_hitbox.all_Hitbox_Types.type_2_Hitboxes.hitbox_Datas;
                        checkHitboxMacro(second, data_Folder);
                    }
                }
            }
        }

        public void checkHitboxMacro(List<hitbox_Data> data_set, string data_Folder)
        {
            foreach (var f in data_set)
            {
                if (f.size == 0)
                {
                    // only found on tallgeese III and Epyon, whip?
                }

                if (f.unk_0x8 == 0)
                {
                    // found on astray red frame's punch hasei first hit
                }

                if (f.unk_0x10 != 0) // Float
                {
                    if(f.hitbox_Type != hitbox_Data_Type.narrow)
                    {
                        // it must be narrow to have this data populated
                    }
                    // Found on Reborns's EX N Melee special hasei final hit (where he does a full circle swing)
                    // Found on Reborns's transformed 2 N Melee where he does a full circle swing
                    // Found on Turn A's 4/6 N Second Slash where he does the big sword swing 
                    // Found on Xenon's CSa (three stages)
                }

                if (f.unk_0x14 != 0) // Float
                {
                    if (f.hitbox_Type != hitbox_Data_Type.narrow)
                    {
                        // it must be narrow to have this data populated
                    }
                    // Found on Age-2's Up Down swing shoulder sword
                    // Found on Epyon's up whip
                    // Found on Red Frame Custom's N BC (bfs swipe up)
                }


                if (f.unk_0x18 != 0)
                {
                    // Always 0
                }

                if (f.unk_0x1c != 0)
                {
                    // Always 0
                }

                if (f.unk_0x20 != 0)
                {
                    // Always 0
                }

                if (f.unk_0x24 != 0)
                {
                    // Always 0
                }

                if (f.unk_0x28 != 0)
                {
                    // Always 0
                }

                if (f.unk_0x2c != 0)
                {
                    // Always 0
                }

                if (f.unk_0x30 != 0)
                {
                    // Always 0
                }
            }
        }

        public void commonspritemacro()
        {
            string JSONIPath = Properties.Settings.Default.inputFBUnitInfoListJSON;
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB JSON\Unit List.json";
            StreamReader ww = File.OpenText(JSONIPath);
            string JSON = ww.ReadToEnd();
            ww.Close();
            List<Unit_Info_List> unit_Info_Lists = JsonConvert.DeserializeObject<List<Unit_Info_List>>(JSON);

            List<string> totalMBONExport = Directory.GetDirectories(@"I:\Full Boost\MBON Reimport Project\Total MBON Export", "*", SearchOption.TopDirectoryOnly).ToList();

            StringBuilder sb = new StringBuilder();

            uint count = 0;

            for (int i = 0; i < unit_Info_Lists.Count; i++)
            {
                Unit_Info_List unit_Info = unit_Info_Lists[i];

                if(unit_Info.unk_0x72 >= 216)
                {
                    uint unit_ID = unit_Info.unit_ID;
                    string exportfolder = totalMBONExport.FirstOrDefault(unitFolderName =>
                    {
                        int unit_ID_str_index = unitFolderName.IndexOf("- ");
                        string unit_ID_str = string.Empty;
                        if (unit_ID_str_index >= 0)
                            unit_ID_str = unitFolderName.Substring(unit_ID_str_index + 2, unitFolderName.Length - unit_ID_str_index - 2);

                        uint unit_IDa = Convert.ToUInt32(unit_ID_str);
                        return unit_IDa == unit_ID;
                    });

                    exportfolder = exportfolder + @"\Extracted MBON\Sprites\";
                    string res_small_sprite = Directory.GetDirectories(exportfolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => s.Contains("Result_Small_Sprite"));
                    string smallsprite = res_small_sprite + @"\free_selection_sprite.dds";

                    File.Copy(smallsprite, @"I:\Full Boost\MBON Reimport Project\Total MBON Common\test\" + (218 + count) + ".dds", true);
                    count++;
                }
            }

            GenerateSpritePACInfo generateSpritePACInfo = new GenerateSpritePACInfo();

            string PACInfo = generateSpritePACInfo.writeSpritePACInfo(@"I:\Full Boost\MBON Reimport Project\Total MBON Common\test\", 218, 1);

            sb.Append(PACInfo);

            StreamWriter awe = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON Common\test\aww.txt");
            awe.Write(sb.ToString());
            awe.Close();
        }

        public void unitlistmacro()
        {
            string JSONIPath = Properties.Settings.Default.inputFBUnitInfoListJSON;
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB JSON\Unit List.json";
            StreamReader ww = File.OpenText(JSONIPath);
            string JSON = ww.ReadToEnd();
            ww.Close();
            List<Unit_Info_List> unit_Info_Lists = JsonConvert.DeserializeObject<List<Unit_Info_List>>(JSON);

            int is255yet = 0;
            bool is255 = false;

            for(int i = 0; i < unit_Info_Lists.Count; i++)
            {
                Unit_Info_List unit_Info = unit_Info_Lists[i];

                if(unit_Info.unit_ID >= 80011)
                {
                    unit_Info.arcade_small_sprite_index = 255;
                }

                if (is255yet != 0)
                {
                    int diff = i - is255yet;
                    unit_Info.unk_0x72 = (ushort)(255 + diff);
                }
                else
                {
                    unit_Info.unk_0x72 = unit_Info.arcade_small_sprite_index;
                }

                if (unit_Info.arcade_small_sprite_index == 255 && is255 == false)
                {
                    is255 = true;
                    is255yet = i;
                }

                unit_Info.arcade_small_sprite_index = 0;
                unit_Info.arcade_unit_name_sprite = 0;
                unit_Info.figurine_sprite_index = 0;
            }

            string outputJSON = JsonConvert.SerializeObject(unit_Info_Lists);
            StreamWriter aww = File.CreateText(JSONIPath);
            aww.Write(outputJSON);
            aww.Close();
        }

        public void filter()
        {
            List<string> oriFiles = Directory.GetFiles(@"E:\MBON\Image0\archives\", "*", SearchOption.AllDirectories).ToList();
            List<string> oriFilesSized = oriFiles.Where(s =>
            {
                FileInfo fileInfo = new FileInfo(s);
                return fileInfo.Length == (long)0x237460;
            }).ToList();

            List<string> allRightSortieFiles = Directory.GetDirectories(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\All Playable Unit Image & Sound Effects", "*", SearchOption.AllDirectories).ToList();
            allRightSortieFiles = allRightSortieFiles.Where(s => s.Contains("Right Sortie Sprite") || s.Contains("Left Sortie Sprite")).ToList();
            List<string> allRightSortieHashes = new List<string>();
            for(int i = 0; i < allRightSortieFiles.Count; i++)
            {
                string rightSortieFile = Path.GetFileName(allRightSortieFiles[i]);
                int hash_str_index = rightSortieFile.IndexOf("- ");
                string hash_str = string.Empty;
                if (hash_str_index >= 0)
                    hash_str = rightSortieFile.Substring(hash_str_index + 2, rightSortieFile.Length - hash_str_index - 2);
                allRightSortieHashes.Add(hash_str);
            }

            List<string> filtered = new List<string>();
            for (int i = 0; i < oriFilesSized.Count; i++)
            { 
                string oriFile = Path.GetFileNameWithoutExtension(oriFilesSized[i]);
                if (!allRightSortieHashes.Contains(oriFile))
                {
                    filtered.Add(oriFile);
                }
            }

            for(int i = 0; i < oriFiles.Count; i++)
            {
                string filename = Path.GetFileNameWithoutExtension(oriFiles[i]);
                if(filtered.Contains(filename))
                {
                    File.Copy(oriFiles[i], @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Input\MBON\v2\All NPC Right Sortie Sprites\" + filename + ".bin", true);
                }
            }
        }

        public void search_sprites()
        {
            List<string> oriFiles = Directory.GetFiles(@"E:\MBON\Image0\archives\", "*", SearchOption.AllDirectories).ToList();
            List<string> oriFilesSized = oriFiles.Where(s =>
            {
                FileInfo fileInfo = new FileInfo(s);
                return fileInfo.Length >= (long)0x7d0;
            }).ToList();

            Dictionary<ushort, ushort> width_height = new Dictionary<ushort, ushort>();

            string extract_folder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\test_sprites";

            for (int i = 0; i < oriFilesSized.Count; i++)
            {
                FileStream fs = File.OpenRead(oriFilesSized[i]);
                fs.Seek(0x10000, SeekOrigin.Begin);

                bool isNTP3 = readUIntBigEndian(fs) == 0x4E545033;

                if (isNTP3)
                {
                    fs.Seek(0x20, SeekOrigin.Current);
                    ushort width = readUShort(fs, true);
                    ushort height = readUShort(fs, true);

                    fs.Close();

                    if (!width_height.ContainsKey(width) && !width_height.ContainsValue(height))
                    {
                        width_height[width] = height;

                        string filePath = oriFilesSized[i];

                        FileStream stream = File.Open(filePath, FileMode.Open);
                        long streamSize = stream.Length;
                        stream.Close();

                        string baseExtractPath = extract_folder + @"\" + Path.GetFileNameWithoutExtension(filePath);

                        new PAC.Extract.ExtractPAC(filePath, stream).extractPAC(0, out long unused, baseExtractPath);
                    }
                }
                else
                {
                    fs.Close();
                }
            }
        }

        public void search_stages()
        {
            List<string> oriFiles = Directory.GetFiles(@"E:\MBON\Image0\archives\", "*", SearchOption.AllDirectories).ToList();
            List<string> oriFilesSized = oriFiles.Where(s =>
            {
                FileInfo fileInfo = new FileInfo(s);
                return fileInfo.Length >= (long)0x2DC6C0;
            }).ToList();

            Dictionary<ushort, ushort> width_height = new Dictionary<ushort, ushort>();

            string extract_folder = @"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\test_maps";

            StreamWriter sw = File.CreateText(extract_folder + @"\asd.txt");
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < oriFilesSized.Count; i++)
            {
                FileStream fs = File.OpenRead(oriFilesSized[i]);
                fs.Seek(0x10000, SeekOrigin.Begin);

                bool ismodel_NDP3 = false;
                bool isFHM = readUIntBigEndian(fs) == 0x46484D20;
                if(isFHM)
                {
                    fs.Seek(0xc, SeekOrigin.Current);
                    uint numberoffiles = readUIntBigEndian(fs);
                    if(numberoffiles > 0)
                    {
                        uint firstFileOffset = readUIntBigEndian(fs);

                        fs.Seek(firstFileOffset + 0x10000, SeekOrigin.Begin);
                        isFHM = readUIntBigEndian(fs) == 0x46484D20;

                        if (isFHM)
                        {
                            fs.Seek(0xc, SeekOrigin.Current);
                            numberoffiles = readUIntBigEndian(fs);

                            if(numberoffiles > 0)
                            {
                                uint offset = firstFileOffset;
                                firstFileOffset = readUIntBigEndian(fs);
                                fs.Seek(offset + firstFileOffset + 0x10000, SeekOrigin.Begin);

                                string sky_nud = readString(fs, 0x7);
                                if (sky_nud == "sky_nud")
                                    ismodel_NDP3 = true;
                            }
                        }
                    }
                }

                fs.Close();

                if (ismodel_NDP3)
                {
                    string filePath = oriFilesSized[i];

                    /*
                    FileStream stream = File.Open(filePath, FileMode.Open);
                    long streamSize = stream.Length;
                    stream.Close();

                    string baseExtractPath = extract_folder + @"\" + Path.GetFileNameWithoutExtension(filePath);

                    new PAC.Extract.ExtractPAC(filePath, stream).extractPAC(0, out long unused, baseExtractPath);
                    */

                    string name = Path.GetFileNameWithoutExtension(filePath);
                    sb.AppendLine(name);
                }
            }

            sw.WriteLine(sb.ToString());
            sw.Close();
        }

        public void rename()
        {
            List<string> files = Directory.GetFiles(@"I:\Full Boost\MBON Reimport Project\Total MBON Export", "*.nus3bank", SearchOption.AllDirectories).ToList();
            foreach(string file in files)
            {
                if(Path.GetFileName(file) == "Sound Effect.nus3bank")
                {
                    string path = Path.GetDirectoryName(file);
                    File.Copy(file, path + @"\Sound Effects.nus3bank");
                    File.Delete(file);
                }
            }    
        }

        public void generateMBONUnitListCSourceCode()
        {
            resizeMBONLMB lmbrefactor = new resizeMBONLMB();

            string totalMBONExportFolder = @"I:\Full Boost\MBON Reimport Project\Total MBON Export";
            List<string> allUnitFolders = Directory.GetDirectories(totalMBONExportFolder, "*", SearchOption.TopDirectoryOnly).ToList();

            string json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(json);

            json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\Unit List MBON.json").ReadToEnd();
            List<Unit_Info_List> unit_Info_List = JsonConvert.DeserializeObject<List<Unit_Info_List>>(json);

            StreamReader alreadyPackedSR = File.OpenText(@"I:\Full Boost\MBON Reimport Project\temp_unit_list.json");
            string alreadyPackedJSON = alreadyPackedSR.ReadToEnd();
            alreadyPackedSR.Close();
            List<uint> already_repacked = JsonConvert.DeserializeObject<List<uint>>(alreadyPackedJSON);

            List<string> convertedFromMBONFolders = Directory.GetDirectories(@"I:\Full Boost\MBON Reimport Project\Total MBON Units", "*", SearchOption.TopDirectoryOnly).ToList();

            uint voiceFileListStartIndex = 0x115;
            uint voiceFileListStartUnkEnumIndex = 0x5A6;
            uint pilotNameIndex = 0x6;

            // Common
            uint Gundam_Voice_File_List_Info_Arr_Index = 214;
            uint Arcade_select_voice_hash_Arr_Index = 270;
            uint Arcade_continue_no_voice_hash_Arr_Index = 270;
            uint Arcade_continue_voice_hash_Arr_Index = 270;
            uint Arcade_continue_yes_voice_hash_Arr_Index = 270;
            uint Bandai_Namco_Games_voice_hash_Arr_Index = 270;
            uint Gundam_Hash_Info_Arr_Index = 213;
            uint Gundam_unk_enum_info_Arr_Index = 213;
            uint Gundam_update_unit_id_list_Arr_Index = 36;
            uint Gundam_update_unit_id_list_2_Arr_Index = 29;
            uint Gundam_string_info_Arr_Index = 275;

            StringBuilder commonCFile1 = new StringBuilder();
            StringBuilder commonCFile2 = new StringBuilder();
            StringBuilder commonCFile3 = new StringBuilder();
            StringBuilder commonCFile4 = new StringBuilder();
            StringBuilder commonCFile5 = new StringBuilder();
            StringBuilder commonCFile6 = new StringBuilder();
            StringBuilder commonCFile7 = new StringBuilder();
            StringBuilder commonCFile8 = new StringBuilder();
            StringBuilder commonCFile9 = new StringBuilder();
            StringBuilder commonCFile10 = new StringBuilder();
            StringBuilder commonCFile11 = new StringBuilder();
            StringBuilder commonCFile12 = new StringBuilder();

            StringBuilder unit_IDs = new StringBuilder();
            uint count = 0;


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

                if (unit_Files != null)
                {
                    // Get unit's english name
                    UnitIDList unit_Names = load_UnitID();
                    string unitName = unit_Names.Unit_ID.FirstOrDefault(s => s.id == unit_Files.Unit_ID).name_english.Replace(" ", "_");
                    unitName = unitName.Replace(".", "_");
                    unitName = unitName.Replace("∀", "Turn_A");
                    unitName = unitName.Replace("(", "");
                    unitName = unitName.Replace(")", "");
                    unitName = unitName.Replace("-", "_");
                    unitName = unitName.Replace("&", "and");
                    unitName = unitName.Replace("[", "");
                    unitName = unitName.Replace("]", "");
                    unitName = unitName.Replace("00_", "");

                    string reimportFolder = convertedFromMBONFolders.FirstOrDefault(s => {
                        unit_ID_str_index = s.IndexOf("- ");
                        unit_ID_str = string.Empty;
                        if (unit_ID_str_index >= 0)
                            unit_ID_str = s.Substring(unit_ID_str_index + 2, s.Length - unit_ID_str_index - 2);

                        uint unit_folder_ID = Convert.ToUInt32(unit_ID_str);

                        return unit_ID == unit_folder_ID;
                    }
                    );

                    if (unit_Files.MBONAdded && (unit_ID < 0xea6b || unit_ID >= 0x1388b))
                    {
                        unit_IDs.AppendLine("MBON_Added_Unit_ID[" + count + "]" + " = 0x" + unit_ID.ToString("X") + ";");
                        count++;

                        string localVoiceFilesFolder = @"\\?\" + reimportFolder + @"\Converted From MBON\Local Voice Files\";
                        List<string> localVoiceFilesSoundHashesList = Directory.GetFiles(localVoiceFilesFolder, "*", SearchOption.AllDirectories).ToList();
                        string localVoiceFilesSoundHashes = localVoiceFilesSoundHashesList.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Contains("Local Voice Files - ") && Path.GetExtension(s) == ".txt");
                        if (localVoiceFilesSoundHashes == null)
                            throw new Exception();

                        List<string> soundHashes = File.ReadAllLines(localVoiceFilesSoundHashes).ToList();
                        Dictionary<string, string> soundHashesPairs = new Dictionary<string, string>();
                        for (int i = 0; i < soundHashes.Count; i++)
                        {
                            string soundHash = soundHashes[i];
                            int semicolon_str_index = soundHash.IndexOf(": ");
                            string nameandhash = string.Empty;
                            if (semicolon_str_index >= 0)
                                nameandhash = soundHash.Substring(semicolon_str_index + 2, soundHash.Length - semicolon_str_index - 2);

                            if(nameandhash != "")
                            {
                                string name = nameandhash.Split('-')[0];
                                string hash = nameandhash.Split('-')[1];

                                name = name.Replace(" ", "");
                                hash = hash.Replace(" ", "");

                                soundHashesPairs[name] = hash;
                            }
                        }

                        // Voice Lines Filters
                        // Chara Select
                        List<string> chara_select_voice_lines = soundHashesPairs.Keys.Where(s => s.ToLower().Contains("CHARA_SELECT".ToLower())).ToList();

                        StringBuilder chara_select_voice_lines_sb = new StringBuilder();
                        for (int i = 0; i < 10; i++)
                        {
                            if (i < chara_select_voice_lines.Count)
                            {
                                string key = chara_select_voice_lines[i];
                                chara_select_voice_lines_sb.AppendLine(unitName + @"_ArcadeSelectHash.hash_" + (i + 1) + @" = 0x" + soundHashesPairs[key] + @"; // " + key);
                            }
                            else
                            {
                                chara_select_voice_lines_sb.AppendLine(unitName + @"_ArcadeSelectHash.hash_" + (i + 1) + @" = 0;");
                            }
                        }

                        // Arcade Continue No Hash
                        List<string> arcade_cont_no_voice_lines = soundHashesPairs.Keys.Where(s => s.ToLower().Contains("GAME_OVER".ToLower())).ToList();

                        StringBuilder arcade_cont_no_voice_lines_sb = new StringBuilder();
                        for (int i = 0; i < 5; i++)
                        {
                            if (i < arcade_cont_no_voice_lines.Count)
                            {
                                string key = arcade_cont_no_voice_lines[i];
                                arcade_cont_no_voice_lines_sb.AppendLine(unitName + @"_ArcadeContinueNoHash.hash_" + (i + 1) + @" = 0x" + soundHashesPairs[key] + @"; // " + key);
                            }
                            else
                            {
                                arcade_cont_no_voice_lines_sb.AppendLine(unitName + @"_ArcadeContinueNoHash.hash_" + (i + 1) + @" = 0;");
                            }
                        }

                        // Arcade Continue Prompt Hash
                        List<string> arcade_cont_voice_lines = soundHashesPairs.Keys.Where(s => s.ToLower().Contains("CONTINUE_DEC".ToLower())).ToList();

                        StringBuilder arcade_cont_voice_lines_sb = new StringBuilder();
                        for (int i = 0; i < 5; i++)
                        {
                            if (i < arcade_cont_voice_lines.Count)
                            {
                                string key = arcade_cont_voice_lines[i];
                                arcade_cont_voice_lines_sb.AppendLine(unitName + @"_ArcadeContinueHash.hash_" + (i + 1) + @" = 0x" + soundHashesPairs[key] + @"; // " + key);
                            }
                            else
                            {
                                arcade_cont_voice_lines_sb.AppendLine(unitName + @"_ArcadeContinueHash.hash_" + (i + 1) + @" = 0;");
                            }
                        }

                        // Arcade Continue Yes Hash
                        List<string> arcade_cont_yes_voice_lines = soundHashesPairs.Keys.Where(s => s.ToLower().Contains("CONTINUE".ToLower())).ToList();

                        StringBuilder arcade_cont_yes_voice_lines_sb = new StringBuilder();
                        for (int i = 0; i < 10; i++)
                        {
                            if (i < arcade_cont_yes_voice_lines.Count)
                            {
                                string key = arcade_cont_yes_voice_lines[i];
                                arcade_cont_yes_voice_lines_sb.AppendLine(unitName + @"_ArcadeContinueYesHash.hash_" + (i + 1) + @" = 0x" + soundHashesPairs[key] + @"; // " + key);
                            }
                            else
                            {
                                arcade_cont_yes_voice_lines_sb.AppendLine(unitName + @"_ArcadeContinueYesHash.hash_" + (i + 1) + @" = 0;");
                            }
                        }

                        // Arcade Continue Yes Hash
                        List<string> bandai_namco_games_voice_lines = soundHashesPairs.Keys.Where(s => s.ToLower().Contains("bng_logo".ToLower())).ToList();

                        StringBuilder bandai_namco_games_voice_lines_sb = new StringBuilder();
                        for (int i = 0; i < 2; i++)
                        {
                            if (i < bandai_namco_games_voice_lines.Count)
                            {
                                string key = bandai_namco_games_voice_lines[i];
                                bandai_namco_games_voice_lines_sb.AppendLine(unitName + @"_BandaiNamcoGamesHash.hash_" + (i + 1) + @" = 0x" + soundHashesPairs[key] + @"; // " + key);
                            }
                            else
                            {
                                bandai_namco_games_voice_lines_sb.AppendLine(unitName + @"_BandaiNamcoGamesHash.hash_" + (i + 1) + @" = 0;");
                            }
                        }

                        StringBuilder unitCFile = new StringBuilder();

                        string unitTemplate = @"

#include """ + unitName + @".h""
#include ""../unit_list_structs.h""
#include ""../unit_list_common.h""

unit_voice_file_list_info " + unitName + @"_inject_unit_voice_file_list_info()
{
    unit_voice_file_list_info " + unitName + @"_VoiceFileList;
    " + unitName + @"_VoiceFileList.UnitID = 0x" + unit_ID.ToString("X8") + @";
    " + unitName + @"_VoiceFileList.index = 0x" + voiceFileListStartIndex.ToString("X8") + @";
    " + unitName + @"_VoiceFileList.voice_file_list_hash = 0x" + unit_Files.voice_file_list_PAC_hash.ToString("X8") + @";
    " + unitName + @"_VoiceFileList.unk_enum_0x8 = 0x" + voiceFileListStartUnkEnumIndex.ToString("X8") + @";
    return " + unitName + @"_VoiceFileList;
}

unit_voice_hash_list_0x28 " + unitName + @"_inject_arcade_select_hash()
{
    unit_voice_hash_list_0x28 " + unitName + @"_ArcadeSelectHash;
    " + chara_select_voice_lines_sb.ToString() + @"
    return " + unitName + @"_ArcadeSelectHash;
}

unit_voice_hash_list_0x14 " + unitName + @"_inject_arcade_continue_no_hash()
{
    unit_voice_hash_list_0x14 " + unitName + @"_ArcadeContinueNoHash;
    " + arcade_cont_no_voice_lines_sb.ToString() + @"
    return " + unitName + @"_ArcadeContinueNoHash;
}

unit_voice_hash_list_0x18 " + unitName + @"_inject_arcade_continue_prompt_hash()
{
    unit_voice_hash_list_0x18 " + unitName + @"_ArcadeContinueHash;
    " + arcade_cont_voice_lines_sb.ToString() + @"
    return " + unitName + @"_ArcadeContinueHash;
}

unit_voice_hash_list_0x28 " + unitName + @"_inject_arcade_continue_yes_hash()
{
    unit_voice_hash_list_0x28 " + unitName + @"_ArcadeContinueYesHash;
    " + arcade_cont_yes_voice_lines_sb.ToString() + @"
    return " + unitName + @"_ArcadeContinueYesHash;
}

unit_voice_hash_list_0x8 " + unitName + @"_inject_bandai_namco_games_serifu_hash()
{
    unit_voice_hash_list_0x8 " + unitName + @"_BandaiNamcoGamesHash;
    " + bandai_namco_games_voice_lines_sb.ToString() + @"
    return " + unitName + @"_BandaiNamcoGamesHash;
}

unit_hash_info " + unitName + @"_inject_unit_hash_info()
{
    unit_hash_info " + unitName + @";
    " + unitName + @".UnitID = 0x" + unit_ID.ToString("X8") + @";
    " + unitName + @".unk0 = 0xDDC3CBD6;
    " + unitName + @".DataScript = 0x" + unit_Files.data_and_script_PAC_hash.ToString("X8") + @";
    " + unitName + @".ModelText = 0x" + unit_Files.model_and_texture_PAC_hash.ToString("X8") + @";
    " + unitName + @".OMO = 0x" + unit_Files.animation_OMO_PAC_hash.ToString("X8") + @";
    " + unitName + @".EIDX = 0x" + unit_Files.effects_EIDX_PAC_hash.ToString("X8") + @";
    " + unitName + @".Sound = 0x" + unit_Files.sound_effect_PAC_hash.ToString("X8") + @";
    " + unitName + @".GlobalPilotVoice = 0x" + unit_Files.global_pilot_voices_PAC_hash.ToString("X8") + @";
    " + unitName + @".WeaponImage = 0x" + unit_Files.weapon_image_DNSO_PAC_hash.ToString("X8") + @";
    " + unitName + @".IngameImage = 0x" + unit_Files.weapon_image_DNSO_PAC_hash.ToString("X8") + @";
    " + unitName + @".KPKP = 0x" + unit_Files.sortie_mouth_anim_enum_KPKP_PAC_hash.ToString("X8") + @";
    " + unitName + @".VoiceFileList = 0x" + unit_Files.voice_file_list_PAC_hash.ToString("X8") + @";
    " + unitName + @".Stream = 0x" + unit_Files.local_pilot_voices_STREAM_PAC_hash.ToString("X8") + @";

    return " + unitName + @";
}

unit_unk_enum_info " + unitName + @"_inject_unk_enum()
{
    unit_unk_enum_info " + unitName + @"_unk_Enum;
    " + unitName + @"_unk_Enum.UnitID = 0x" + unit_ID.ToString("X8") + @";
    " + unitName + @"_unk_Enum.unk_enum = 0x1;
    return " + unitName + @"_unk_Enum;
}

unit_update_unit_id_list " + unitName + @"_inject_gundam_update_unit_ID_list()
{
    unit_update_unit_id_list " + unitName + @"_Update_Unit_ID_List;
    " + unitName + @"_Update_Unit_ID_List.UnitID = 0x" + unit_ID.ToString("X8") + @";
    return " + unitName + @"_Update_Unit_ID_List;
}

unit_update_unit_id_list " + unitName + @"_inject_update_unit_ID_list_2()
{
    unit_update_unit_id_list " + unitName + @"_Unit_ID_List_2;
    " + unitName + @"_Unit_ID_List_2.UnitID = 0x" + unit_ID.ToString("X8") + @";
    return " + unitName + @"_Unit_ID_List_2;
}

unit_string_info " + unitName + @"_inject_string()
{
    add_long_pilot_name_string_ID(""F" + unit_ID.ToString() + @""", " + pilotNameIndex + @");
    add_short_pilot_name_string_ID(""S" + unit_ID.ToString() + @""", " + pilotNameIndex + @");
    add_long_unit_name_string_ID(""P" + unit_ID.ToString() + @""", " + pilotNameIndex + @");
    add_short_unit_name_string_ID(""Q" + unit_ID.ToString() + @""", " + pilotNameIndex + @");

    unit_string_info " + unitName + @"_Gundam_String;
    " + unitName + @"_Gundam_String.UnitID = 0x" + unit_ID.ToString("X8") + @";
    " + unitName + @"_Gundam_String.unk_enum = 0xFFFFFFFF;
    " + unitName + @"_Gundam_String.long_unit_name_str = (int)&Added_Gundam_string_Arr[" + pilotNameIndex + @"].long_pilot_name_str;
    " + unitName + @"_Gundam_String.short_unit_name_str = (int)&Added_Gundam_string_Arr[" + pilotNameIndex + @"].short_pilot_name_str;
    " + unitName + @"_Gundam_String.long_pilot_name_str = (int)&Added_Gundam_string_Arr[" + pilotNameIndex + @"].long_unit_name_str;
    " + unitName + @"_Gundam_String.short_pilot_name_str = (int)&Added_Gundam_string_Arr[" + pilotNameIndex + @"].short_unit_name_str;

    return " + unitName + @"_Gundam_String;
}
                        ";

                        unitCFile.Append(unitTemplate);

                        StringBuilder unitHFile = new StringBuilder();

                        string unitHTemplate = @"
#include ""../unit_list_structs.h""

unit_voice_file_list_info " + unitName + @"_inject_unit_voice_file_list_info();
unit_voice_hash_list_0x28 " + unitName + @"_inject_arcade_select_hash();
unit_voice_hash_list_0x14 " + unitName + @"_inject_arcade_continue_no_hash();
unit_voice_hash_list_0x18 " + unitName + @"_inject_arcade_continue_prompt_hash();
unit_voice_hash_list_0x28 " + unitName + @"_inject_arcade_continue_yes_hash();
unit_voice_hash_list_0x8 " + unitName + @"_inject_bandai_namco_games_serifu_hash();
unit_hash_info " + unitName + @"_inject_unit_hash_info();
unit_unk_enum_info " + unitName + @"_inject_unk_enum();
unit_update_unit_id_list " + unitName + @"_inject_gundam_update_unit_ID_list();
unit_update_unit_id_list " + unitName + @"_inject_update_unit_ID_list_2();
unit_string_info " + unitName + @"_inject_string();

                        ";

                        unitHFile.Append(unitHTemplate);

                        StreamWriter ctxt = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\" + unitName + @".cpp");
                        StreamWriter htxt = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\" + unitName + @".h");

                        ctxt.Write(unitCFile.ToString());
                        htxt.Write(unitHFile.ToString());

                        ctxt.Close();
                        htxt.Close();

                        /*
                        already_repacked.Add(unit_ID);
                        string updateJSON = JsonConvert.SerializeObject(already_repacked, Formatting.Indented);
                        StreamWriter sw = File.CreateText(@"I:\Full Boost\MBON Reimport Project\temp_unit_list.json");
                        sw.Write(updateJSON);
                        sw.Close();
                        */

                        voiceFileListStartIndex++;
                        voiceFileListStartUnkEnumIndex++;
                        pilotNameIndex++;

                        commonCFile1.AppendLine(@"Gundam_Voice_File_List_Info_Arr[" + Gundam_Voice_File_List_Info_Arr_Index + "] = " + unitName + @"_inject_unit_voice_file_list_info();");
                        commonCFile2.AppendLine(@"Arcade_select_voice_hash_Arr[" + Arcade_select_voice_hash_Arr_Index + "] = " + unitName + @"_inject_arcade_select_hash();");
                        commonCFile3.AppendLine(@"Arcade_continue_no_voice_hash_Arr[" + Arcade_continue_no_voice_hash_Arr_Index + "] = " + unitName + @"_inject_arcade_continue_no_hash();");
                        commonCFile4.AppendLine(@"Arcade_continue_voice_hash_Arr[" + Arcade_continue_voice_hash_Arr_Index + "] = " + unitName + @"_inject_arcade_continue_prompt_hash();");
                        commonCFile5.AppendLine(@"Arcade_continue_yes_voice_hash_Arr[" + Arcade_continue_yes_voice_hash_Arr_Index + "] = " + unitName + @"_inject_arcade_continue_yes_hash();");
                        commonCFile6.AppendLine(@"Bandai_Namco_Games_voice_hash_Arr[" + Bandai_Namco_Games_voice_hash_Arr_Index + "] = " + unitName + @"_inject_bandai_namco_games_serifu_hash();");

                        commonCFile7.AppendLine(@"Gundam_Hash_Info_Arr[" + Gundam_Hash_Info_Arr_Index + "] = " + unitName + @"_inject_unit_hash_info();");
                        commonCFile8.AppendLine(@"Gundam_unk_enum_info_Arr[" + Gundam_unk_enum_info_Arr_Index + "] = " + unitName + @"_inject_unk_enum();");
                        commonCFile9.AppendLine(@"Gundam_update_unit_id_list_Arr[" + Gundam_update_unit_id_list_Arr_Index + "] = " + unitName + @"_inject_gundam_update_unit_ID_list();");
                        commonCFile10.AppendLine(@"Gundam_update_unit_id_list_2_Arr[" + Gundam_update_unit_id_list_2_Arr_Index + "] = " + unitName + @"_inject_update_unit_ID_list_2();");
                        commonCFile11.AppendLine(@"Gundam_string_info_Arr[" + Gundam_string_info_Arr_Index + "] = " + unitName + @"_inject_string();");

                        // Common
                        Gundam_Voice_File_List_Info_Arr_Index++;
                        Arcade_select_voice_hash_Arr_Index++;
                        Arcade_continue_no_voice_hash_Arr_Index++;
                        Arcade_continue_voice_hash_Arr_Index++;
                        Arcade_continue_yes_voice_hash_Arr_Index++;
                        Bandai_Namco_Games_voice_hash_Arr_Index++;
                        Gundam_Hash_Info_Arr_Index++;
                        Gundam_unk_enum_info_Arr_Index++;
                        Gundam_update_unit_id_list_Arr_Index++;
                        Gundam_update_unit_id_list_2_Arr_Index++;
                        Gundam_string_info_Arr_Index++;

                        commonCFile12.AppendLine(@"#include ""new_units/" + unitName + @".h""");
                    }
                }
            }

            StreamWriter commonC1 = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\commonC1.txt");
            StreamWriter commonC2 = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\commonC2.txt");
            StreamWriter commonC3 = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\commonC3.txt");
            StreamWriter commonC4 = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\commonC4.txt");
            StreamWriter commonC5 = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\commonC5.txt");
            StreamWriter commonC6 = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\commonC6.txt");
            StreamWriter commonC7 = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\commonC7.txt");
            StreamWriter commonC8 = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\commonC8.txt");
            StreamWriter commonC9 = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\commonC9.txt");
            StreamWriter commonC10 = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\commonC10.txt");
            StreamWriter commonC11 = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\commonC11.txt");
            StreamWriter commonC12 = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\commonC12.txt");
            StreamWriter MBONUnitIDSW = File.CreateText(@"I:\Full Boost\MBON Reimport Project\Total MBON New Unit List\MBONUnitIDs.txt");

            commonC1.Write(commonCFile1.ToString());
            commonC2.Write(commonCFile2.ToString());
            commonC3.Write(commonCFile3.ToString());
            commonC4.Write(commonCFile4.ToString());
            commonC5.Write(commonCFile5.ToString());
            commonC6.Write(commonCFile6.ToString());
            commonC7.Write(commonCFile7.ToString());
            commonC8.Write(commonCFile8.ToString());
            commonC9.Write(commonCFile9.ToString());
            commonC10.Write(commonCFile10.ToString());
            commonC11.Write(commonCFile11.ToString());
            commonC12.Write(commonCFile12.ToString());
            MBONUnitIDSW.Write(unit_IDs.ToString());

            commonC1.Close();
            commonC2.Close();
            commonC3.Close();
            commonC4.Close();
            commonC5.Close();
            commonC6.Close();
            commonC7.Close();
            commonC8.Close();
            commonC9.Close();
            commonC10.Close();
            commonC11.Close();
            commonC12.Close();
            MBONUnitIDSW.Close();
        }

        public void extractUnitImages()
        {
            resizeMBONLMB lmbrefactor = new resizeMBONLMB();

            string totalMBONExportFolder = @"I:\Full Boost\MBON Reimport Project\Total MBON Export";
            List<string> allUnitFolders = Directory.GetDirectories(totalMBONExportFolder, "*", SearchOption.TopDirectoryOnly).ToList();

            string json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(json);

            json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\Unit List MBON.json").ReadToEnd();
            List<Unit_Info_List> unit_Info_List = JsonConvert.DeserializeObject<List<Unit_Info_List>>(json);

            List<string> allunitimagesfolder = Directory.GetDirectories(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\All Playable Unit Image & Sound Effects", "*", SearchOption.TopDirectoryOnly).ToList();

            List<string> allpilotimagesfolder = Directory.GetDirectories(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\All Pilot Image", "*", SearchOption.TopDirectoryOnly).ToList();

            StreamReader alreadyPackedSR = File.OpenText(@"I:\Full Boost\MBON Reimport Project\temp_unit_list.json");
            string alreadyPackedJSON = alreadyPackedSR.ReadToEnd();
            alreadyPackedSR.Close();
            List<uint> already_repacked = JsonConvert.DeserializeObject<List<uint>>(alreadyPackedJSON);

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

                if (unit_Files != null && !already_repacked.Contains(unit_ID))
                {
                    if (unit_ID < 60011)
                    {
                        // Sound Effects
                        string UnitFolder = allunitimagesfolder.FirstOrDefault(s => {
                                unit_ID_str_index = s.IndexOf("- ");
                                unit_ID_str = string.Empty;
                                if (unit_ID_str_index >= 0)
                                    unit_ID_str = s.Substring(unit_ID_str_index + 2, s.Length - unit_ID_str_index - 2);

                                uint unit_folder_ID = Convert.ToUInt32(unit_ID_str);
                                
                                return unit_ID == unit_folder_ID;
                            }
                        );

                        string PilotFolder = allpilotimagesfolder.FirstOrDefault(s => {
                                unit_ID_str_index = s.IndexOf("- ");
                                unit_ID_str = string.Empty;
                                if (unit_ID_str_index >= 0)
                                    unit_ID_str = s.Substring(unit_ID_str_index + 2, s.Length - unit_ID_str_index - 2);

                                uint unit_folder_ID = Convert.ToUInt32(unit_ID_str);

                                return unit_ID == unit_folder_ID;
                            }
                        );

                        string arcadeUnitSpriteFolder = Directory.GetDirectories(UnitFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Arcade Sprite"));
                        string arcadeUnitSprite = arcadeUnitSpriteFolder + @"\001-MBON\002.dds";

                        string rightUnitSpriteFolder = Directory.GetDirectories(UnitFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Right Sortie Sprite"));
                        string rightUnitSprite = rightUnitSpriteFolder + @"\001-MBON\002.dds";

                        string rightPilotSpriteFolder = Directory.GetDirectories(PilotFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Right Pilot Costume 1 Sprite"));
                        string rightPilotSprite = rightPilotSpriteFolder + @"\001-MBON\002.dds";

                        string targetSpriteFolder = Directory.GetDirectories(UnitFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Small Intermission Sprite"));
                        string targetSprite = targetSpriteFolder + @"\001-MBON\002.dds";

                        string trophySpriteFolder = Directory.GetDirectories(UnitFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Trophy Sprite"));
                        string trophySprite = trophySpriteFolder + @"\001-MBON\002.dds";

                        string pilotEyeFolder = Directory.GetDirectories(PilotFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Pilot Eye Costume 1 Sprite"));
                        string pilotEyeSprite = pilotEyeFolder + @"\001-MBON\002.dds";

                        string awakeningSpriteFolder = Directory.GetDirectories(PilotFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Awakening Cut In Costume 1 Sprite"));
                        List<string> awakeningSprites = Directory.GetFiles(awakeningSpriteFolder, "*", SearchOption.AllDirectories).Where(s => s.Contains(".dds")).ToList();
                        string awakeningLMB = Directory.GetFiles(awakeningSpriteFolder, "*", SearchOption.AllDirectories).FirstOrDefault(s => s.Contains(".LMB"));

                        string sortieSpriteFolder = Directory.GetDirectories(PilotFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Sortie Cut In Costume 1 Sprite"));
                        List<string> sortieSprites = Directory.GetFiles(sortieSpriteFolder, "*", SearchOption.AllDirectories).Where(s => s.Contains(".dds")).ToList();
                        string sortieLMB = Directory.GetFiles(sortieSpriteFolder, "*", SearchOption.AllDirectories).FirstOrDefault(s => s.Contains(".LMB"));

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Arcade_Selection_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Arcade_Selection_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Figurine_Sprite"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Figurine_Sprite");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Free_Battle_Selection_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Free_Battle_Selection_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\In_Game_Sortie_and_Awakening_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\In_Game_Sortie_and_Awakening_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Loading_Ally_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Loading_Ally_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Loading_Enemy_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Loading_Enemy_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Pilot_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Pilot_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Unit_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Unit_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Result_Small_Sprite"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Result_Small_Sprite");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Target_Small_Sprite"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Target_Small_Sprite");

                        string extractedExportArcadeFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Arcade_Selection_Sprite_Costume_1 - " + unit_Infos.arcade_selection_sprite_costume_1_hash;
                        string extractedExportFreeBattleSelectionFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Free_Battle_Selection_Sprite_Costume_1 - " + unit_Infos.free_battle_selection_sprite_costume_1_hash;
                        string extractedExportInGameSortieandAwakeningFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\In_Game_Sortie_and_Awakening_Sprite_Costume_1 - " + unit_Infos.in_game_sortie_and_awakening_sprite_costume_1_hash;
                        string extractedExportLoadingAllyFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Loading_Ally_Sprite_Costume_1 - " + unit_Infos.loading_ally_sprite_costume_1_hash;
                        string extractedExportLoadingEnemyFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Loading_Enemy_Sprite_Costume_1 - " + unit_Infos.loading_enemy_sprite_costume_1_hash;
                        string extractedExportLoadingEnemyTargetPilotFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Loading_Enemy_Target_Pilot_Sprite_Costume_1 - " + unit_Infos.loading_enemy_target_pilot_sprite_costume_1_hash;
                        string extractedExportLoadingEnemyTargetUnitFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Loading_Enemy_Target_Unit_Sprite_Costume_1 - " + unit_Infos.loading_enemy_target_unit_sprite_costume_1_hash;
                        string extractedExportFigurineSpriteFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Figurine_Sprite - " + unit_Infos.figurine_sprite_hash;
                        string extractedExportResultSmallSpriteFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Result_Small_Sprite - " + unit_Infos.result_small_sprite_hash;
                        string extractedExportTargetSmallSpriteFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Target_Small_Sprite - " + unit_Infos.figurine_sprite_hash;

                        Directory.CreateDirectory(extractedExportArcadeFolder);
                        Directory.CreateDirectory(extractedExportFreeBattleSelectionFolder);
                        Directory.CreateDirectory(extractedExportInGameSortieandAwakeningFolder);
                        Directory.CreateDirectory(extractedExportLoadingAllyFolder);
                        Directory.CreateDirectory(extractedExportLoadingEnemyFolder);
                        Directory.CreateDirectory(extractedExportLoadingEnemyTargetPilotFolder);
                        Directory.CreateDirectory(extractedExportLoadingEnemyTargetUnitFolder);
                        Directory.CreateDirectory(extractedExportFigurineSpriteFolder);
                        Directory.CreateDirectory(extractedExportResultSmallSpriteFolder);
                        Directory.CreateDirectory(extractedExportTargetSmallSpriteFolder);

                        scale_dds_precise(arcadeUnitSprite, extractedExportArcadeFolder + @"\arcade_unit.dds", 0, false, 1280, 720);

                        flip_dds(rightPilotSprite, extractedExportArcadeFolder + @"\arcade_pilot.dds", 0, false);
                        scale_dds_precise(extractedExportArcadeFolder + @"\arcade_pilot.dds", extractedExportArcadeFolder + @"\arcade_pilot.dds", 0, false, 440, 280);
                        resize_dds_canvas(extractedExportArcadeFolder + @"\arcade_pilot.dds", extractedExportArcadeFolder + @"\arcade_pilot.dds", 0, false, 280, 280, -80, 0);

                        flip_dds(rightUnitSprite, extractedExportFreeBattleSelectionFolder + @"\free_battle_unit.dds", 3, true);
                        scale_dds_precise(extractedExportFreeBattleSelectionFolder + @"\free_battle_unit.dds", extractedExportFreeBattleSelectionFolder + @"\free_battle_unit.dds", 3, true, 560, 448);

                        flip_dds(rightPilotSprite, extractedExportFreeBattleSelectionFolder + @"\free_battle_pilot.dds", 0, false);
                        scale_dds_precise(extractedExportFreeBattleSelectionFolder + @"\free_battle_pilot.dds", extractedExportFreeBattleSelectionFolder + @"\free_battle_pilot.dds", 0, false, 204, 130);
                        resize_dds_canvas(extractedExportFreeBattleSelectionFolder + @"\free_battle_pilot.dds", extractedExportFreeBattleSelectionFolder + @"\free_battle_pilot.dds", 0, false, 152, 104, -30, 0);

                        flip_dds(rightUnitSprite, extractedExportLoadingAllyFolder + @"\loading_ally_unit.dds", 3, true);
                        scale_dds_precise(extractedExportLoadingAllyFolder + @"\loading_ally_unit.dds", extractedExportLoadingAllyFolder + @"\loading_ally_unit.dds", 3, true, 560, 448);

                        flip_dds(rightPilotSprite, extractedExportLoadingAllyFolder + @"\loading_ally_pilot.dds", 0, false);
                        scale_dds_precise(extractedExportLoadingAllyFolder + @"\loading_ally_pilot.dds", extractedExportLoadingAllyFolder + @"\loading_ally_pilot.dds", 0, false, 352, 224);

                        scale_dds_precise(rightUnitSprite, extractedExportLoadingEnemyFolder + @"\loading_enemy_unit.dds", 3, true, 560, 448);

                        scale_dds_precise(rightPilotSprite, extractedExportLoadingEnemyFolder + @"\loading_enemy_pilot.dds", 0, false, 352, 224);

                        scale_dds_precise(rightUnitSprite, extractedExportLoadingEnemyTargetUnitFolder + @"\loading_enemy_target_unit.dds", 3, true, 560, 448);

                        scale_dds_precise(rightPilotSprite, extractedExportLoadingEnemyTargetPilotFolder + @"\loading_enemy_target_pilot.dds", 0, false, 280, 180);

                        scale_dds_precise(rightUnitSprite, extractedExportResultSmallSpriteFolder + @"\temp_copy_unit.dds", 0, false, 140, 112);
                        selection_sprite_macro(@"I:\Full Boost\MBON Reimport Project\Sprite Templates\select_sprite_template_working.xcf", extractedExportResultSmallSpriteFolder + @"\temp_copy_unit.dds", extractedExportResultSmallSpriteFolder + @"\free_selection_sprite.dds", 0, false);
                        scale_dds_precise(extractedExportResultSmallSpriteFolder + @"\free_selection_sprite.dds", extractedExportResultSmallSpriteFolder + @"\free_selection_sprite.dds", 0, false, 136, 68);
                        resize_dds_canvas(extractedExportResultSmallSpriteFolder + @"\free_selection_sprite.dds", extractedExportResultSmallSpriteFolder + @"\free_selection_sprite.dds", 0, false, 144, 80, 4, 6);

                        scale_dds_precise(targetSprite, extractedExportTargetSmallSpriteFolder + @"\target_small_sprite.dds", 0, false, 96, 32);

                        scale_dds_precise(trophySprite, extractedExportFigurineSpriteFolder + @"\figurine_sprites.dds", 0, false, 168, 168);

                        // sortie and awakening
                        Directory.CreateDirectory(extractedExportInGameSortieandAwakeningFolder + @"\awakening");
                        Directory.CreateDirectory(extractedExportInGameSortieandAwakeningFolder + @"\sortie");
                        Directory.CreateDirectory(extractedExportInGameSortieandAwakeningFolder + @"\eye");

                        //File.Copy(awakeningLMB, extractedExportInGameSortieandAwakeningFolder + @"\awakening\awakening.LMB");

                        // We will do refactor later
                        File.Copy(awakeningLMB, extractedExportInGameSortieandAwakeningFolder + @"\awakening\awakening.LMB", true);
                        File.Copy(sortieLMB, extractedExportInGameSortieandAwakeningFolder + @"\sortie\sortie.LMB", true);
                        //lmbrefactor.resizeLMB(awakeningLMB, extractedExportInGameSortieandAwakeningFolder + @"\awakening\awakening.LMB", (float)0.6667);
                        //lmbrefactor.resizeLMB(sortieLMB, extractedExportInGameSortieandAwakeningFolder + @"\sortie\sortie.LMB", (float)0.6667);

                        for (int i = 0; i < awakeningSprites.Count; i++)
                        {
                            save_dds(awakeningSprites[i], extractedExportInGameSortieandAwakeningFolder + @"\awakening\awakening_sprite_" + i + @".dds", 3, true);
                        }

                        for (int i = 0; i < sortieSprites.Count; i++)
                        {
                            save_dds(sortieSprites[i], extractedExportInGameSortieandAwakeningFolder + @"\sortie\sortie_sprite_" + i + @".dds", 3, true);
                        }

                        scale_dds_precise(pilotEyeSprite, extractedExportInGameSortieandAwakeningFolder + @"\eye\pilot_eye.dds", 3, true, 272, 96);

                        already_repacked.Add(unit_ID);
                        string updateJSON = JsonConvert.SerializeObject(already_repacked, Formatting.Indented);
                        StreamWriter sw = File.CreateText(@"I:\Full Boost\MBON Reimport Project\temp_unit_list.json");
                        sw.Write(updateJSON);
                        sw.Close();
                    }
                }
            }
        }

        public void extractNPCImages()
        {
            resizeMBONLMB lmbrefactor = new resizeMBONLMB();

            string totalMBONExportFolder = @"I:\Full Boost\MBON Reimport Project\Total MBON Export";
            List<string> allUnitFolders = Directory.GetDirectories(totalMBONExportFolder, "*", SearchOption.TopDirectoryOnly).ToList();

            string json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(json);

            json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\Unit List MBON.json").ReadToEnd();
            List<Unit_Info_List> unit_Info_List = JsonConvert.DeserializeObject<List<Unit_Info_List>>(json);

            List<string> allbossunitimagesfolder = Directory.GetDirectories(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\All Boss Unit Image & Sound Effects", "*", SearchOption.TopDirectoryOnly).ToList();

            List<string> allbosspilotimagesfolder = Directory.GetDirectories(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Output\MBON\v2\All Pilot Image", "*", SearchOption.TopDirectoryOnly).ToList();

            StreamReader alreadyPackedSR = File.OpenText(@"I:\Full Boost\MBON Reimport Project\temp_unit_list.json");
            string alreadyPackedJSON = alreadyPackedSR.ReadToEnd();
            alreadyPackedSR.Close();
            List<uint> already_repacked = JsonConvert.DeserializeObject<List<uint>>(alreadyPackedJSON);

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

                if (unit_ID >= 0x13880 && unit_ID <= 0x13a00 && (unit_ID == 0x13949))
                {
                    if (unit_Files != null) // Bosses
                    {
                        // Sound Effects
                        string BossUnitFolder = allbossunitimagesfolder.FirstOrDefault(s => s.Contains(unit_ID.ToString()));
                        string BossPilotFolder = allbosspilotimagesfolder.FirstOrDefault(s => s.Contains(unit_ID.ToString()));

                        string arcadeUnitSpriteFolder = Directory.GetDirectories(BossUnitFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Arcade Sprite"));
                        string arcadeUnitSprite = arcadeUnitSpriteFolder + @"\001-MBON\002.dds";

                        string rightUnitSpriteFolder = Directory.GetDirectories(BossUnitFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Right Sortie Sprite"));
                        string rightUnitSprite = rightUnitSpriteFolder + @"\001-MBON\002.dds";

                        string rightPilotSpriteFolder = Directory.GetDirectories(BossPilotFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Right Pilot Costume 1 Sprite"));
                        string rightPilotSprite = rightPilotSpriteFolder + @"\001-MBON\002.dds";

                        string targetSpriteFolder = Directory.GetDirectories(BossUnitFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Small Intermission Sprite"));
                        string targetSprite = targetSpriteFolder + @"\001-MBON\002.dds";

                        string trophySpriteFolder = Directory.GetDirectories(BossUnitFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Trophy Sprite"));
                        string trophySprite = trophySpriteFolder + @"\001-MBON\002.dds";

                        string pilotEyeFolder = Directory.GetDirectories(BossPilotFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Pilot Eye Costume 1 Sprite"));
                        string pilotEyeSprite = pilotEyeFolder + @"\001-MBON\002.dds";

                        string awakeningSpriteFolder = Directory.GetDirectories(BossPilotFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Awakening Cut In Costume 1 Sprite"));
                        List<string> awakeningSprites = Directory.GetFiles(awakeningSpriteFolder, "*", SearchOption.AllDirectories).Where(s => s.Contains(".dds")).ToList();
                        string awakeningLMB = Directory.GetFiles(awakeningSpriteFolder, "*", SearchOption.AllDirectories).FirstOrDefault(s => s.Contains(".LMB"));

                        string sortieSpriteFolder = Directory.GetDirectories(BossPilotFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Sortie Cut In Costume 1 Sprite"));
                        List<string> sortieSprites = Directory.GetFiles(sortieSpriteFolder, "*", SearchOption.AllDirectories).Where(s => s.Contains(".dds")).ToList();
                        string sortieLMB = Directory.GetFiles(sortieSpriteFolder, "*", SearchOption.AllDirectories).FirstOrDefault(s => s.Contains(".LMB"));
                        
                        if(Directory.Exists(unitFolder + @"\Extracted MBON\Arcade_Selection_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Arcade_Selection_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Figurine_Sprite"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Figurine_Sprite");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Free_Battle_Selection_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Free_Battle_Selection_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\In_Game_Sortie_and_Awakening_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\In_Game_Sortie_and_Awakening_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Loading_Ally_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Loading_Ally_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Loading_Enemy_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Loading_Enemy_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Pilot_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Pilot_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Unit_Sprite_Costume_1"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Loading_Enemy_Target_Unit_Sprite_Costume_1");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Result_Small_Sprite"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Result_Small_Sprite");

                        if (Directory.Exists(unitFolder + @"\Extracted MBON\Target_Small_Sprite"))
                            Directory.Delete(unitFolder + @"\Extracted MBON\Target_Small_Sprite");

                        string extractedExportArcadeFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Arcade_Selection_Sprite_Costume_1 - " + unit_Infos.arcade_selection_sprite_costume_1_hash;
                        string extractedExportFreeBattleSelectionFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Free_Battle_Selection_Sprite_Costume_1 - " + unit_Infos.free_battle_selection_sprite_costume_1_hash;
                        string extractedExportInGameSortieandAwakeningFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\In_Game_Sortie_and_Awakening_Sprite_Costume_1 - " + unit_Infos.in_game_sortie_and_awakening_sprite_costume_1_hash;
                        string extractedExportLoadingAllyFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Loading_Ally_Sprite_Costume_1 - " + unit_Infos.loading_ally_sprite_costume_1_hash;
                        string extractedExportLoadingEnemyFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Loading_Enemy_Sprite_Costume_1 - " + unit_Infos.loading_enemy_sprite_costume_1_hash;
                        string extractedExportLoadingEnemyTargetPilotFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Loading_Enemy_Target_Pilot_Sprite_Costume_1 - " + unit_Infos.loading_enemy_target_pilot_sprite_costume_1_hash;
                        string extractedExportLoadingEnemyTargetUnitFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Loading_Enemy_Target_Unit_Sprite_Costume_1 - " + unit_Infos.loading_enemy_target_unit_sprite_costume_1_hash;
                        string extractedExportFigurineSpriteFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Figurine_Sprite - " + unit_Infos.figurine_sprite_hash;
                        string extractedExportResultSmallSpriteFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Result_Small_Sprite - " + unit_Infos.result_small_sprite_hash;
                        string extractedExportTargetSmallSpriteFolder = @"\\?\" + unitFolder + @"\Extracted MBON\Sprites\Target_Small_Sprite - " + unit_Infos.figurine_sprite_hash;

                        Directory.CreateDirectory(extractedExportArcadeFolder);
                        Directory.CreateDirectory(extractedExportFreeBattleSelectionFolder);
                        Directory.CreateDirectory(extractedExportInGameSortieandAwakeningFolder);
                        Directory.CreateDirectory(extractedExportLoadingAllyFolder);
                        Directory.CreateDirectory(extractedExportLoadingEnemyFolder);
                        Directory.CreateDirectory(extractedExportLoadingEnemyTargetPilotFolder);
                        Directory.CreateDirectory(extractedExportLoadingEnemyTargetUnitFolder);
                        Directory.CreateDirectory(extractedExportFigurineSpriteFolder);
                        Directory.CreateDirectory(extractedExportResultSmallSpriteFolder);
                        Directory.CreateDirectory(extractedExportTargetSmallSpriteFolder);

                        
                        if(!unitFolder.Contains("(Boss)") && !unitFolder.Contains("Rephaser") && !unitFolder.Contains("Dystopia") && !unitFolder.Contains("AXE") && !unitFolder.Contains("Mystic") && !unitFolder.Contains("Ignith") && !unitFolder.Contains("Carnage") && !unitFolder.Contains("Tachyon"))
                        {
                            scale_dds_precise(arcadeUnitSprite, extractedExportArcadeFolder + @"\arcade_unit.dds", 0, false, 1280, 720);

                            flip_dds(rightPilotSprite, extractedExportArcadeFolder + @"\arcade_pilot.dds", 0, false);
                            scale_dds_precise(extractedExportArcadeFolder + @"\arcade_pilot.dds", extractedExportArcadeFolder + @"\arcade_pilot.dds", 0, false, 440, 280);
                            resize_dds_canvas(extractedExportArcadeFolder + @"\arcade_pilot.dds", extractedExportArcadeFolder + @"\arcade_pilot.dds", 0, false, 280, 280, -80, 0);
                        }
                        
                        flip_dds(rightUnitSprite, extractedExportFreeBattleSelectionFolder + @"\free_battle_unit.dds", 3, true);
                        scale_dds_precise(extractedExportFreeBattleSelectionFolder + @"\free_battle_unit.dds", extractedExportFreeBattleSelectionFolder + @"\free_battle_unit.dds", 3, true, 560, 448);

                        flip_dds(rightPilotSprite, extractedExportFreeBattleSelectionFolder + @"\free_battle_pilot.dds", 0, false);
                        scale_dds_precise(extractedExportFreeBattleSelectionFolder + @"\free_battle_pilot.dds", extractedExportFreeBattleSelectionFolder + @"\free_battle_pilot.dds", 0, false, 204, 130);
                        resize_dds_canvas(extractedExportFreeBattleSelectionFolder + @"\free_battle_pilot.dds", extractedExportFreeBattleSelectionFolder + @"\free_battle_pilot.dds", 0, false, 152, 104, -30, 0);

                        flip_dds(rightUnitSprite, extractedExportLoadingAllyFolder + @"\loading_ally_unit.dds", 3, true);
                        scale_dds_precise(extractedExportLoadingAllyFolder + @"\loading_ally_unit.dds", extractedExportLoadingAllyFolder + @"\loading_ally_unit.dds", 3, true, 560, 448);

                        flip_dds(rightPilotSprite, extractedExportLoadingAllyFolder + @"\loading_ally_pilot.dds", 0, false);
                        scale_dds_precise(extractedExportLoadingAllyFolder + @"\loading_ally_pilot.dds", extractedExportLoadingAllyFolder + @"\loading_ally_pilot.dds", 0, false, 352, 224);

                        scale_dds_precise(rightUnitSprite, extractedExportLoadingEnemyFolder + @"\loading_enemy_unit.dds", 3, true, 560, 448);

                        scale_dds_precise(rightPilotSprite, extractedExportLoadingEnemyFolder + @"\loading_enemy_pilot.dds", 0, false, 352, 224);

                        scale_dds_precise(rightUnitSprite, extractedExportLoadingEnemyTargetUnitFolder + @"\loading_enemy_target_unit.dds", 3, true, 560, 448);

                        scale_dds_precise(rightPilotSprite, extractedExportLoadingEnemyTargetPilotFolder + @"\loading_enemy_target_pilot.dds", 0, false, 280, 180);

                        scale_dds_precise(rightUnitSprite, extractedExportResultSmallSpriteFolder + @"\temp_copy_unit.dds", 0, false, 140, 112);
                        selection_sprite_macro(@"I:\Full Boost\MBON Reimport Project\Sprite Templates\select_sprite_template_working.xcf", extractedExportResultSmallSpriteFolder + @"\temp_copy_unit.dds", extractedExportResultSmallSpriteFolder + @"\free_selection_sprite.dds", 0, false);
                        scale_dds_precise(extractedExportResultSmallSpriteFolder + @"\free_selection_sprite.dds", extractedExportResultSmallSpriteFolder + @"\free_selection_sprite.dds", 0, false, 136, 68);
                        resize_dds_canvas(extractedExportResultSmallSpriteFolder + @"\free_selection_sprite.dds", extractedExportResultSmallSpriteFolder + @"\free_selection_sprite.dds", 0, false, 144, 80, 4, 6);

                        scale_dds_precise(targetSprite, extractedExportTargetSmallSpriteFolder + @"\target_small_sprite.dds", 0, false, 96, 32);

                        scale_dds_precise(trophySprite, extractedExportFigurineSpriteFolder + @"\figurine_sprites.dds", 0, false, 168, 168);

                        // sortie and awakening
                        Directory.CreateDirectory(extractedExportInGameSortieandAwakeningFolder + @"\awakening");
                        Directory.CreateDirectory(extractedExportInGameSortieandAwakeningFolder + @"\sortie");
                        Directory.CreateDirectory(extractedExportInGameSortieandAwakeningFolder + @"\eye");

                        //File.Copy(awakeningLMB, extractedExportInGameSortieandAwakeningFolder + @"\awakening\awakening.LMB");

                        // We will do refactor later
                        File.Copy(awakeningLMB, extractedExportInGameSortieandAwakeningFolder + @"\awakening\awakening.LMB", true);
                        File.Copy(sortieLMB, extractedExportInGameSortieandAwakeningFolder + @"\sortie\sortie.LMB", true);
                        //lmbrefactor.resizeLMB(awakeningLMB, extractedExportInGameSortieandAwakeningFolder + @"\awakening\awakening.LMB", (float)0.6667);
                        //lmbrefactor.resizeLMB(sortieLMB, extractedExportInGameSortieandAwakeningFolder + @"\sortie\sortie.LMB", (float)0.6667);

                        for (int i = 0; i < awakeningSprites.Count; i++)
                        {
                            save_dds(awakeningSprites[i], extractedExportInGameSortieandAwakeningFolder + @"\awakening\awakening_sprite_" + i + @".dds", 3, true);
                        }

                        for (int i = 0; i < sortieSprites.Count; i++)
                        {
                            save_dds(sortieSprites[i], extractedExportInGameSortieandAwakeningFolder + @"\sortie\sortie_sprite_" + i + @".dds", 3, true);
                        }

                        scale_dds_precise(pilotEyeSprite, extractedExportInGameSortieandAwakeningFolder + @"\eye\pilot_eye.dds", 3, true, 272, 96);

                        /*
                        already_repacked.Add(unit_ID);
                        string updateJSON = JsonConvert.SerializeObject(already_repacked, Formatting.Indented);
                        StreamWriter sw = File.CreateText(@"I:\Full Boost\MBON Reimport Project\temp_unit_list.json");
                        sw.Write(updateJSON);
                        sw.Close();
                        */
                    }
                }
            }
        }

        public void extractNPCSounds()
        {
            string totalMBONExportFolder = @"I:\Full Boost\MBON Reimport Project\Total MBON Export";
            List<string> allUnitFolders = Directory.GetDirectories(totalMBONExportFolder, "*", SearchOption.TopDirectoryOnly).ToList();

            string json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
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

                if(unit_ID == 21151)
                {
                    if (unit_Files != null) // Bosses
                    {
                        // Sound Effects
                        //string BossFolder = allbossunitsoundeffectsfolder.FirstOrDefault(s => s.Contains(unit_ID.ToString()));
                        //string SEFolder = Directory.GetDirectories(BossFolder, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(s => Path.GetFileName(s).Contains("Sound Effect"));
                        //string SE = SEFolder + @"\001-MBON\002.nus3bank";

                        //if (!File.Exists(SE))
                            //throw new Exception();

                        //string reimportSEFolder = unitFolder + @"\Extracted MBON\Sound Effects";

                        //Directory.CreateDirectory(reimportSEFolder);

                        //File.Copy(SE, unitFolder + @"\Extracted MBON\Sound Effects.nus3bank", true);

                        //convertNUS3toWav(SE, reimportSEFolder);

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
            List<string> binaryFiles = Directory.GetFiles(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Input\MBON\v2\All NPC Right Sortie Sprites", "*.bin", SearchOption.AllDirectories).ToList();

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
