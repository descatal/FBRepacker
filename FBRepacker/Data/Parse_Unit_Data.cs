using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using FBRepacker.Data.DataTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FBRepacker.Data
{
    class Parse_Unit_Data : Internals
    {
        public uint schemaVersion = 1;

        public Parse_Unit_Data()
        {
            /* 
            using()
            {

            }
             */
            /*
            string[] MBONDataFiles = Directory.GetFiles(Properties.Settings.Default.MBONDataFolderPath);
            Dictionary<uint, Unit_Varaibles> MBON_Variables = readVariables(MBONDataFiles[1], false);

            string[] FBDataFiles = Directory.GetFiles(Properties.Settings.Default.FBDataFolderPath);
            Dictionary<uint, Unit_Varaibles> FB_Variables = readVariables(FBDataFiles[1], true);

            Dictionary<uint, Unit_Varaibles> n_Variables = new Dictionary<uint, Unit_Varaibles>(FB_Variables);

            foreach (var vari in MBON_Variables)
            {
                if (!FB_Variables.ContainsKey(vari.Key))
                {
                    n_Variables[vari.Key] = vari.Value;
                }
            }

            //FB_Variables = n_Variables;

            List<uint> matchedHash = new List<uint>();

            if (MBON_Variables.Count > FB_Variables.Count)
            {
                matchedHash = MBON_Variables.Keys.Intersect(FB_Variables.Keys).ToList();
            }
            else
            {
                matchedHash = FB_Variables.Keys.Intersect(MBON_Variables.Keys).ToList();
            }
            
            Dictionary<uint, Unit_Varaibles> new_Variables = FB_Variables;
            foreach (uint hash in matchedHash)
            {
                new_Variables[hash] = MBON_Variables[hash];
            }

            string oDataPath = Properties.Settings.Default.outputDataFolderPath + @"\002.bin";
            MemoryStream oDataHeader = new MemoryStream();

            appendUIntMemoryStream(oDataHeader, FB_Magic, true);
            appendUIntMemoryStream(oDataHeader, FB_UnitHash, true);
            appendUIntMemoryStream(oDataHeader, FB_unkFlag, true);
            appendZeroMemoryStream(oDataHeader, 0x4);

            // Assume header will always be 0x30 in size.
            uint pointer = 0x30;
            appendUIntMemoryStream(oDataHeader, pointer, true);

            MemoryStream oDataWeaponSlot = new MemoryStream();
            appendIntMemoryStream(oDataWeaponSlot, FB_ammoSlotHashes.Count, true);
            foreach(var ammoSlotHashes in FB_ammoSlotHashes)
            {
                appendUIntMemoryStream(oDataWeaponSlot, ammoSlotHashes, true);
            }

            pointer += (uint)oDataWeaponSlot.Length;
            appendUIntMemoryStream(oDataHeader, pointer, true);

            MemoryStream oDataVariables = new MemoryStream();
            appendIntMemoryStream(oDataVariables, new_Variables.Count, true);
            foreach (var variables in new_Variables)
            {
                appendUIntMemoryStream(oDataVariables, variables.Key, true);
            }
            foreach (var variables in new_Variables)
            {
                appendUIntMemoryStream(oDataVariables, variables.Value.unkEnum, true);
            }

            pointer += (uint)oDataVariables.Length;
            appendUIntMemoryStream(oDataHeader, pointer, true);

            foreach (var unkEnum in FB_unkEnums)
            {
                appendUIntMemoryStream(oDataVariables, unkEnum, true);
            }

            pointer += (uint)(FB_unkEnums.Count * 0x04);
            appendUIntMemoryStream(oDataHeader, pointer, true);

            foreach (var variables in new_Variables)
            {
                appendUIntMemoryStream(oDataVariables, variables.Value.Data1, true);
            }
            /*
            foreach (var variables in new_Variables)
            {
                appendUIntMemoryStream(oDataVariables, variables.Value.Data2, true);
            }
            */
            /*
            appendUIntMemoryStream(oDataHeader, 0, true);
            appendUIntMemoryStream(oDataHeader, 1, true);
            appendUIntMemoryStream(oDataHeader, 2, true);
            appendUIntMemoryStream(oDataHeader, 3, true);

            FileStream fs = File.Create(oDataPath);
            MemoryStream VariableMS = new MemoryStream();
            VariableMS.Write(oDataHeader.ToArray(), 0, (int)oDataHeader.Length);
            VariableMS.Write(oDataWeaponSlot.ToArray(), 0, (int)oDataWeaponSlot.Length);
            VariableMS.Write(oDataVariables.ToArray(), 0, (int)oDataVariables.Length);

            fs.Write(VariableMS.ToArray(), 0, (int)VariableMS.Length);
            VariableMS.Flush();
            fs.Flush();
            fs.Close();
            */
        }

        public Data_Hash_Schema parseDataHashSchema(string path)
        {
            if (!File.Exists(path))
                throw new Exception("Invalid Unit Data Schema JSON file!");
            string JSONPath = path;
            StreamReader sR = File.OpenText(JSONPath);
            string JSON = sR.ReadToEnd();
            sR.Close();
            Data_Hash_Schema data_Hash_Schema = JsonConvert.DeserializeObject<Data_Hash_Schema>(JSON);
            return data_Hash_Schema;
        }

        public void readVariables()
        {
            FileStream fs = File.OpenRead(Properties.Settings.Default.inputUnitDataBinary);
            FileStream reloadfs = File.OpenRead(Properties.Settings.Default.inputUnitDataReloadBinary);

            reloadfs.Seek(0x10, SeekOrigin.Begin);
            uint reloadHashCount = readUIntBigEndian(reloadfs);
            reloadfs.Seek(0x60, SeekOrigin.Begin);

            List<uint> reloadHashes = new List<uint>();
            for(int i = 0; i < reloadHashCount; i++)
            {
                uint reloadHash = readUIntBigEndian(reloadfs);
                reloadHashes.Add(reloadHash);
            }

            reloadfs.Seek(0x20, SeekOrigin.Begin);

            List<uint> reloadHashIndex = new List<uint>();
            for(int i = 0; i < 5; i++)
            {
                uint reloadHashInitialIndex = readUIntBigEndian(reloadfs);
                if(reloadHashes.Contains(reloadHashInitialIndex))
                {
                    uint index = (uint)reloadHashes.IndexOf(reloadHashInitialIndex);
                    reloadHashIndex.Add(index);
                }
                else
                {
                    reloadHashIndex.Add(0xFFFFFFFF);
                }

            }


            Data_Hash_Schema data_Hash_Schema = parseDataHashSchema(Properties.Settings.Default.inputUnitDataHashSchemaJSON);
            List<Data_Hash> data_Hashes = data_Hash_Schema.Data_Hashes; 
            Unit_Varaibles unit_Varaibles = new Unit_Varaibles();
            unit_Varaibles.schemaVersion = schemaVersion;

            uint Magic = readUIntBigEndian(fs);
            unit_Varaibles.magic = Magic;

            uint Unit_ID = readUIntBigEndian(fs);
            unit_Varaibles.Unit_ID = Unit_ID;

            uint unk_Hash = readUIntBigEndian(fs);
            unit_Varaibles.unk_Hash = unk_Hash;

            uint unk_0xC_enum = readUIntBigEndian(fs);
            if (unk_0xC_enum != 0)
                throw new Exception("unk_0xC is not 0!");

            uint ammoHashChunkPointer = readUIntBigEndian(fs);
            uint unitDataHashChunk = readUIntBigEndian(fs);
            uint unitDataSetIndexChunk = readUIntBigEndian(fs);
            uint unitDataValueChunk = readUIntBigEndian(fs);

            uint reloadHashIndex_1 = readUIntBigEndian(fs); // depreceated stuff, since they don't store used reload hash here anymore
            uint reloadHashIndex_2 = readUIntBigEndian(fs); // depreceated stuff, since they don't store used reload hash here anymore
            uint reloadHashIndex_3 = readUIntBigEndian(fs); // depreceated stuff, since they don't store used reload hash here anymore
            uint reloadHashIndex_4 = readUIntBigEndian(fs); // depreceated stuff, since they don't store used reload hash here anymore

            unit_Varaibles.reloadHashIndex_Slot_1 = reloadHashIndex[0]; // reloadHashIndex_1;
            unit_Varaibles.reloadHashIndex_Slot_2 = reloadHashIndex[1]; // reloadHashIndex_2;
            unit_Varaibles.reloadHashIndex_Slot_3 = reloadHashIndex[2]; // reloadHashIndex_3;
            unit_Varaibles.reloadHashIndex_Slot_4 = reloadHashIndex[3]; // reloadHashIndex_4;

            // TODO: add 5th ammo?

            fs.Seek(ammoHashChunkPointer, SeekOrigin.Begin);
            uint ammoSlotCount = readUIntBigEndian(fs);

            List<Ammo_Data> ammoSlotHashes = unit_Varaibles.ammo_Datas;

            if (ammoSlotCount != 0x4)
                throw new Exception("ammoSlot is not depreceated!");

            /*
            for (int i = 0; i < ammoSlotCount; i++)
            {
                Ammo_Data ammo_Data = new Ammo_Data();
                uint ammo_Hash = readUIntBigEndian(fs); // probably nothing here.
                ammo_Data.ammo_Hash = ammo_Hash;
                ammoSlotHashes.Add(ammo_Data);
            }
            */

            foreach(uint reloadHash in reloadHashes)
            {
                Ammo_Data ammo_Data = new Ammo_Data();
                ammo_Data.ammo_Hash = reloadHash;
                ammoSlotHashes.Add(ammo_Data);
            }

            fs.Seek(unitDataSetIndexChunk, SeekOrigin.Begin);
            uint setCount = readUIntBigEndian(fs);
            unit_Varaibles.setCount = setCount;

            for(int i = 0; i < setCount; i++)
            {
                int index = (int)readUIntBigEndian(fs);

                unit_Varaibles.set_Data_Assignment_Index.Add(index);

                if (index != i)
                {

                }
                    //throw new Exception("index mismatch!");
            }

            fs.Seek(unitDataHashChunk, SeekOrigin.Begin);
            uint dataCount = readUIntBigEndian(fs);

            List<Unit_Data> dataList = unit_Varaibles.datas;
            for (int i = 0; i < dataCount; i++)
            {
                Unit_Data unit_Data = new Unit_Data();

                uint data_Hash_Read = readUIntBigEndian(fs);
                Data_Hash data_Hash = data_Hashes.FirstOrDefault(x => x.Hash == data_Hash_Read);
                if (data_Hash == null)
                    throw new Exception("Unidentified hash: 0x" + data_Hash.Hash.ToString("X8"));

                unit_Data.Data_Hash = data_Hash;
                long hashReturnPos = fs.Position;
                fs.Seek(unitDataHashChunk + dataCount * 0x4 + i * 0x4 + 0x4, SeekOrigin.Begin);

                uint data_Hash_Type = readUIntBigEndian(fs);
                data_Types dataType = (data_Types)data_Hash_Type;
                unit_Data.Data_Type_Enum = dataType;

                for(int j = 0; j < setCount; j++)
                {
                    fs.Seek(unitDataValueChunk + (j * dataCount * 4) + (i * 0x4), SeekOrigin.Begin);
                    dynamic value;
                    switch (dataType)
                    {
                        case data_Types.Float:
                            value = readFloat(fs, true);
                            break;
                        case data_Types.Int: // I am not sure if this is int or uint
                            value = readUIntBigEndian(fs);
                            break;
                        case data_Types.Unk:
                            value = readUIntBigEndian(fs);
                            break;
                        default:
                            value = readUIntBigEndian(fs);
                            break;
                    }

                    unit_Data.Data_Value.Add(value);
                }

                fs.Seek(hashReturnPos, SeekOrigin.Begin);
                dataList.Add(unit_Data);
            }

            /*
            string CS = File.ReadAllText(Properties.Settings.Default.CScriptFilePath);
            List<uint> asd = data_Hashes.Select(x => x.Hash).ToList();

            AhoCorasick.Trie trie = new AhoCorasick.Trie();

            for (int i = 0; i < asd.Count; i++)
            {
                string funcPointerHex = "0x" + asd[i].ToString("X");
                trie.Add(funcPointerHex.ToLower());
            }
            trie.Build();

            List<string> addedWord = new List<string>();
            foreach (string word in trie.Find(CS))
            {
                if(!addedWord.Contains(word))
                    addedWord.Add(word);
            }

            string JSONasd = JsonConvert.SerializeObject(addedWord, Formatting.Indented);
            StreamWriter sWasd = File.CreateText(Properties.Settings.Default.outputUnitDataJSONPath + @"\asd.JSON");
            sWasd.Write(JSONasd);
            sWasd.Close();
             */

            Dictionary<uint, uint> conversionHash = new Dictionary<uint, uint>
            {
                { 0xCCE10B14, 0x83A09DD3 },
                { 0x5FAE7052, 0x10EFE695 },
                { 0x66A5F155, 0xBFD7B8DB }, // skip 0xB8AA5D4E, we take melee damage and apply it directly.
                { 0x3312894D, 0x86302075 },
                { 0x51AC65A2, 0xE48ECC9A },
                { 0x644EE109, 0x197DF49 },
                { 0x83B425AD, 0xB2AC12DA },

                { 0xFB54ED00, 0xA88DCE10 },
                { 0x681B9646, 0x3BC2B556 },
                { 0x25A32C27, 0x86AF159B }, // skip 0xFBAC803C, we take shooting damage and apply it directly.
                { 0x8E1DDA67, 0xC291056D },
                { 0xECA33688, 0xA02FE982 },
                { 0x86305C88, 0x38EF7209 },
                { 0x14FCA1E8, 0xEAB2BBF2 },

                { 0x906BF615, 0x10E5CA5B }, // skip 0x4E645A0E, should be 1 anyways.
            };

            if (Properties.Settings.Default.convertMBONUnitData)
            {
                Unit_Data MBON_Damage_Dealt_EX_Gauge_Increase_Multiplier = new Unit_Data();
                Unit_Data MBON_Damage_Taken_EX_Gauge_Increase_Multiplier = new Unit_Data();
                Unit_Data Awakening_unk_0x8 = new Unit_Data();
                Unit_Data Awakening_unk_0xC = new Unit_Data();
                Unit_Data Awakening_unk_0x10 = new Unit_Data();
                Unit_Data Awakening_unk_0x14 = new Unit_Data();
                Unit_Data Awakening_unk_0x18 = new Unit_Data();

                List<Unit_Data> newUnitData = dataList.ToList();
                for (int i = 0; i < dataList.Count; i++)
                {
                    Unit_Data data = dataList[i];
                    
                    switch (data.Data_Hash.Hash)
                    {
                        case 0x211CF6DD: // Unknown Enum, not called by script, seen values of 0 or 1 - MBON Exclusive (removed in FB conversion)
                            newUnitData.Remove(data);
                            break;
                        case 0x1F2CEDD: //"Unknown Enum, not called by script, seen values of 3 - MBON Exclusive (removed in FB conversion)"
                            newUnitData.Remove(data);
                            break;
                        case 0x7291BF3B: // "Unknown Value, not called by script, always the same value as value of hash 0xA449D488, seen values of 0x578 (1400) - MBON Exclusive (removed in FB conversion)"
                            newUnitData.Remove(data);
                            break;
                        case 0xB8AA5D4E:
                            newUnitData.Remove(data);
                            break;
                        case 0xFBAC803C:
                            newUnitData.Remove(data);
                            break;
                        case 0x4E645A0E:
                            newUnitData.Remove(data);
                            break;
                        case 0xE1C391B7:
                            MBON_Damage_Dealt_EX_Gauge_Increase_Multiplier = data;
                            newUnitData.Remove(data);
                            break;
                        case 0xD9A9D2BF:
                            MBON_Damage_Taken_EX_Gauge_Increase_Multiplier = data;
                            newUnitData.Remove(data);
                            break;
                        case 0xF445B45A:
                            Awakening_unk_0x8 = data;
                            newUnitData.Remove(data);
                            break;
                        case 0xF83C13D:
                            Awakening_unk_0xC = data;
                            newUnitData.Remove(data);
                            break;
                        case 0x80815199:
                            Awakening_unk_0x10 = data;
                            newUnitData.Remove(data);
                            break;
                        case 0xD7F9E2AA:
                            Awakening_unk_0x14 = data;
                            newUnitData.Remove(data);
                            break;
                        case 0x794C406A:
                            Awakening_unk_0x18 = data;
                            newUnitData.Remove(data);
                            break;
                        default:
                            if (conversionHash.ContainsKey(data.Data_Hash.Hash))
                            {
                                uint equivalentFBHash = conversionHash[data.Data_Hash.Hash];
                                Data_Hash FB_data_Hash = data_Hashes.FirstOrDefault(x => x.Hash == equivalentFBHash);
                                if (FB_data_Hash == null)
                                    throw new Exception("Unidentified hash: 0x" + FB_data_Hash.Hash.ToString("X8"));

                                data.Data_Hash.Hash = FB_data_Hash.Hash;
                                data.Data_Hash.description = FB_data_Hash.description;
                            }
                            break;
                    }
                }

                unit_Varaibles.datas = newUnitData.ToList();
                dataList = unit_Varaibles.datas;

                // Insert additional awakening data
                // Assault Burst:
                // Find the index for Boost Consumption Multiplier

                int ABBCIndex = dataList.FindIndex(x => x.Data_Hash.Hash == 0xB2AC12DA);

                // For the 0 value with correct number of sets.
                List<dynamic> unk_0_Values = new List<dynamic>();
                for (int j = 0; j < setCount; j++)
                {
                    unk_0_Values.Add(0);
                }

                Unit_Data unk_1C = new Unit_Data();
                unk_1C.Data_Hash.Hash = 0x801145CC;
                unk_1C.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_1C.Data_Hash.Hash).description;
                unk_1C.Data_Type_Enum = data_Types.Int;
                unk_1C.Data_Value = unk_0_Values; // Always 0

                dataList.Insert(ABBCIndex + 1, unk_1C);

                Unit_Data unk_20 = new Unit_Data();
                unk_20.Data_Hash.Hash = 0xCE8EA6A0;
                unk_20.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_20.Data_Hash.Hash).description;
                unk_20.Data_Type_Enum = data_Types.Int;
                unk_20.Data_Value = unk_0_Values; // Always 0

                dataList.Insert(ABBCIndex + 2, unk_20);

                Unit_Data Damage_Dealt_EX_Gauge_Increase_Multiplier = MBON_Damage_Dealt_EX_Gauge_Increase_Multiplier;
                Damage_Dealt_EX_Gauge_Increase_Multiplier.Data_Hash.Hash = 0xF73F72CF;
                Damage_Dealt_EX_Gauge_Increase_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == Damage_Dealt_EX_Gauge_Increase_Multiplier.Data_Hash.Hash).description;

                dataList.Insert(ABBCIndex + 3, Damage_Dealt_EX_Gauge_Increase_Multiplier);

                Unit_Data Damage_Taken_EX_Gauge_Increase_Multiplier = MBON_Damage_Taken_EX_Gauge_Increase_Multiplier;
                Damage_Taken_EX_Gauge_Increase_Multiplier.Data_Hash.Hash = 0x85B796C1;
                Damage_Taken_EX_Gauge_Increase_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == Damage_Taken_EX_Gauge_Increase_Multiplier.Data_Hash.Hash).description;

                dataList.Insert(ABBCIndex + 4, Damage_Taken_EX_Gauge_Increase_Multiplier);

                Unit_Data unk_0x8 = Awakening_unk_0x8;
                unk_0x8.Data_Hash.Hash = 0xF23C5041;
                unk_0x8.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_0x8.Data_Hash.Hash).description;

                dataList.Insert(ABBCIndex + 5, unk_0x8);

                Unit_Data unk_0xC = Awakening_unk_0xC;
                unk_0xC.Data_Hash.Hash = 0x9FA2526;
                unk_0xC.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_0xC.Data_Hash.Hash).description;

                dataList.Insert(ABBCIndex + 6, unk_0xC);

                Unit_Data unk_0x10 = Awakening_unk_0x10;
                unk_0x10.Data_Hash.Hash = 0x6B108DAF;
                unk_0x10.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_0x10.Data_Hash.Hash).description;

                dataList.Insert(ABBCIndex + 7, unk_0x10);

                Unit_Data unk_0x14 = Awakening_unk_0x14;
                unk_0x14.Data_Hash.Hash = 0xD18006B1;
                unk_0x14.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_0x14.Data_Hash.Hash).description;

                dataList.Insert(ABBCIndex + 8, unk_0x14);

                Unit_Data unk_0x18 = Awakening_unk_0x18;
                unk_0x18.Data_Hash.Hash = 0x92DD9C5C;
                unk_0x18.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_0x18.Data_Hash.Hash).description;

                dataList.Insert(ABBCIndex + 9, unk_0x18);


                int BBBCIndex = dataList.FindIndex(x => x.Data_Hash.Hash == 0xEAB2BBF2);

                Unit_Data BBunk_1C = (Unit_Data)unk_1C.DeepClone();
                BBunk_1C.Data_Hash.Hash = 0x7246A834;
                BBunk_1C.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == BBunk_1C.Data_Hash.Hash).description;

                dataList.Insert(BBBCIndex + 1, BBunk_1C);

                Unit_Data BBunk_20 = (Unit_Data)unk_20.DeepClone();
                BBunk_20.Data_Hash.Hash = 0x96900F88;
                BBunk_20.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == BBunk_20.Data_Hash.Hash).description;

                dataList.Insert(BBBCIndex + 2, BBunk_20);

                Unit_Data BBDamage_Dealt_EX_Gauge_Increase_Multiplier = (Unit_Data)Damage_Dealt_EX_Gauge_Increase_Multiplier.DeepClone();
                BBDamage_Dealt_EX_Gauge_Increase_Multiplier.Data_Hash.Hash = 0x93DF0931;
                BBDamage_Dealt_EX_Gauge_Increase_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == BBDamage_Dealt_EX_Gauge_Increase_Multiplier.Data_Hash.Hash).description;

                dataList.Insert(BBBCIndex + 3, BBDamage_Dealt_EX_Gauge_Increase_Multiplier);

                Unit_Data BBDamage_Taken_EX_Gauge_Increase_Multiplier = (Unit_Data)Damage_Taken_EX_Gauge_Increase_Multiplier.DeepClone();
                BBDamage_Taken_EX_Gauge_Increase_Multiplier.Data_Hash.Hash = 0xBD8C1DB2;
                BBDamage_Taken_EX_Gauge_Increase_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == BBDamage_Taken_EX_Gauge_Increase_Multiplier.Data_Hash.Hash).description;

                dataList.Insert(BBBCIndex + 4, BBDamage_Taken_EX_Gauge_Increase_Multiplier);

                Unit_Data BBunk_0x8 = (Unit_Data)unk_0x8.DeepClone();
                BBunk_0x8.Data_Hash.Hash = 0xCB44FD01;
                BBunk_0x8.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == BBunk_0x8.Data_Hash.Hash).description;

                dataList.Insert(BBBCIndex + 5, BBunk_0x8);

                Unit_Data BBunk_0xC = (Unit_Data)unk_0xC.DeepClone();
                BBunk_0xC.Data_Hash.Hash = 0x30828866;
                BBunk_0xC.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == BBunk_0xC.Data_Hash.Hash).description;

                dataList.Insert(BBBCIndex + 6, BBunk_0xC);

                Unit_Data BBunk_0x10 = (Unit_Data)unk_0x10.DeepClone();
                BBunk_0x10.Data_Hash.Hash = 0xC4B9C065;
                BBunk_0x10.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == BBunk_0x10.Data_Hash.Hash).description;

                dataList.Insert(BBBCIndex + 7, BBunk_0x10);

                Unit_Data BBunk_0x14 = (Unit_Data)unk_0x14.DeepClone();
                BBunk_0x14.Data_Hash.Hash = 0xE8F8ABF1;
                BBunk_0x14.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == BBunk_0x14.Data_Hash.Hash).description;

                dataList.Insert(BBBCIndex + 8, BBunk_0x14);

                Unit_Data BBunk_0x18 = (Unit_Data)unk_0x18.DeepClone();
                BBunk_0x18.Data_Hash.Hash = 0x3D74D196;
                BBunk_0x18.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == BBunk_0x18.Data_Hash.Hash).description;

                dataList.Insert(BBBCIndex + 9, BBunk_0x18);

                // For unknown Burst we need to add from the top, just reuse E burst's data for now.
                // Start from the end of Blast Burst's unk_0x30
                int EndOfBlastBurstIndex = dataList.FindIndex(x => x.Data_Hash.Hash == 0x3D74D196);

                Unit_Data E_Red_Lock_Melee = dataList.FirstOrDefault(x => x.Data_Hash.Hash == 0x9872A871);
                Unit_Data unk_Burst_Red_Lock_Melee = new Unit_Data();
                unk_Burst_Red_Lock_Melee.Data_Hash.Hash = 0xB196FF51;
                unk_Burst_Red_Lock_Melee.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_Burst_Red_Lock_Melee.Data_Hash.Hash).description;
                unk_Burst_Red_Lock_Melee.Data_Type_Enum = E_Red_Lock_Melee.Data_Type_Enum;
                unk_Burst_Red_Lock_Melee.Data_Value = E_Red_Lock_Melee.Data_Value;

                dataList.Insert(EndOfBlastBurstIndex + 1, unk_Burst_Red_Lock_Melee);

                Unit_Data E_Red_Lock = dataList.FirstOrDefault(x => x.Data_Hash.Hash == 0xF9867B09);
                Unit_Data unk_Burst_Red_Lock = new Unit_Data();
                unk_Burst_Red_Lock.Data_Hash.Hash = 0x22D98417;
                unk_Burst_Red_Lock.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_Burst_Red_Lock.Data_Hash.Hash).description;
                unk_Burst_Red_Lock.Data_Type_Enum = E_Red_Lock.Data_Type_Enum;
                unk_Burst_Red_Lock.Data_Value = E_Red_Lock.Data_Value;

                dataList.Insert(EndOfBlastBurstIndex + 2, unk_Burst_Red_Lock);

                Unit_Data E_Damage_Multiplier = dataList.FirstOrDefault(x => x.Data_Hash.Hash == 0x10E5CA5B);
                Unit_Data unk_Burst_Damage_Multiplier = new Unit_Data();
                unk_Burst_Damage_Multiplier.Data_Hash.Hash = 0x9187715B;
                unk_Burst_Damage_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_Burst_Damage_Multiplier.Data_Hash.Hash).description;
                unk_Burst_Damage_Multiplier.Data_Type_Enum = E_Damage_Multiplier.Data_Type_Enum;
                unk_Burst_Damage_Multiplier.Data_Value = E_Damage_Multiplier.Data_Value;

                dataList.Insert(EndOfBlastBurstIndex + 3, unk_Burst_Damage_Multiplier);

                Unit_Data E_Damage_Taken_Multiplier = dataList.FirstOrDefault(x => x.Data_Hash.Hash == 0x1539CA4C);
                Unit_Data unk_Burst_Damage_Taken_Multiplier = new Unit_Data();
                unk_Burst_Damage_Taken_Multiplier.Data_Hash.Hash = 0xFEF1E665;
                unk_Burst_Damage_Taken_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_Burst_Damage_Taken_Multiplier.Data_Hash.Hash).description;
                unk_Burst_Damage_Taken_Multiplier.Data_Type_Enum = E_Damage_Taken_Multiplier.Data_Type_Enum;
                unk_Burst_Damage_Taken_Multiplier.Data_Value = E_Damage_Taken_Multiplier.Data_Value;

                dataList.Insert(EndOfBlastBurstIndex + 4, unk_Burst_Damage_Taken_Multiplier);

                Unit_Data E_Mobility_Multiplier = dataList.FirstOrDefault(x => x.Data_Hash.Hash == 0x778726A3);
                Unit_Data unk_Burst_Mobility_Multiplier = new Unit_Data();
                unk_Burst_Mobility_Multiplier.Data_Hash.Hash = 0x9C4F0A8A;
                unk_Burst_Mobility_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_Burst_Mobility_Multiplier.Data_Hash.Hash).description;
                unk_Burst_Mobility_Multiplier.Data_Type_Enum = E_Mobility_Multiplier.Data_Type_Enum;
                unk_Burst_Mobility_Multiplier.Data_Value = E_Mobility_Multiplier.Data_Value;

                dataList.Insert(EndOfBlastBurstIndex + 5, unk_Burst_Mobility_Multiplier);

                Unit_Data E_Down_Value_Multiplier = dataList.FirstOrDefault(x => x.Data_Hash.Hash == 0xAEA5ADC9);
                Unit_Data unk_Burst_Down_Value_Multiplier = new Unit_Data();
                unk_Burst_Down_Value_Multiplier.Data_Hash.Hash = 0x2FC716C9;
                unk_Burst_Down_Value_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_Burst_Down_Value_Multiplier.Data_Hash.Hash).description;
                unk_Burst_Down_Value_Multiplier.Data_Type_Enum = E_Down_Value_Multiplier.Data_Type_Enum;
                unk_Burst_Down_Value_Multiplier.Data_Value = E_Down_Value_Multiplier.Data_Value;

                dataList.Insert(EndOfBlastBurstIndex + 6, unk_Burst_Down_Value_Multiplier);

                Unit_Data E_Boost_Consumption_Multiplier = dataList.FirstOrDefault(x => x.Data_Hash.Hash == 0x372435B3);
                Unit_Data unk_Burst_Boost_Consumption_Multiplier = new Unit_Data();
                unk_Burst_Boost_Consumption_Multiplier.Data_Hash.Hash = 0x6B97DED5;
                unk_Burst_Boost_Consumption_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unk_Burst_Boost_Consumption_Multiplier.Data_Hash.Hash).description;
                unk_Burst_Boost_Consumption_Multiplier.Data_Type_Enum = E_Boost_Consumption_Multiplier.Data_Type_Enum;
                unk_Burst_Boost_Consumption_Multiplier.Data_Value = E_Boost_Consumption_Multiplier.Data_Value;

                dataList.Insert(EndOfBlastBurstIndex + 7, unk_Burst_Boost_Consumption_Multiplier);

                Unit_Data unkBunk_1C = (Unit_Data)unk_1C.DeepClone();
                unkBunk_1C.Data_Hash.Hash = 0x955B0EA3;
                unkBunk_1C.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkBunk_1C.Data_Hash.Hash).description;

                dataList.Insert(EndOfBlastBurstIndex + 8, unkBunk_1C);

                Unit_Data unkBunk_20 = (Unit_Data)unk_20.DeepClone();
                unkBunk_20.Data_Hash.Hash = 0x17B56AAF;
                unkBunk_20.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkBunk_20.Data_Hash.Hash).description;

                dataList.Insert(EndOfBlastBurstIndex + 9, unkBunk_20);

                Unit_Data unkBDamage_Dealt_EX_Gauge_Increase_Multiplier = (Unit_Data)Damage_Dealt_EX_Gauge_Increase_Multiplier.DeepClone();
                unkBDamage_Dealt_EX_Gauge_Increase_Multiplier.Data_Hash.Hash = 0x6AFDDA4;
                unkBDamage_Dealt_EX_Gauge_Increase_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkBDamage_Dealt_EX_Gauge_Increase_Multiplier.Data_Hash.Hash).description;

                dataList.Insert(EndOfBlastBurstIndex + 10, unkBDamage_Dealt_EX_Gauge_Increase_Multiplier);

                Unit_Data unkBDamage_Taken_EX_Gauge_Increase_Multiplier = (Unit_Data)Damage_Taken_EX_Gauge_Increase_Multiplier.DeepClone();
                unkBDamage_Taken_EX_Gauge_Increase_Multiplier.Data_Hash.Hash = 0x1CB5995C;
                unkBDamage_Taken_EX_Gauge_Increase_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkBDamage_Taken_EX_Gauge_Increase_Multiplier.Data_Hash.Hash).description;

                dataList.Insert(EndOfBlastBurstIndex + 11, unkBDamage_Taken_EX_Gauge_Increase_Multiplier);

                Unit_Data unkBunk_0x8 = (Unit_Data)unk_0x8.DeepClone();
                unkBunk_0x8.Data_Hash.Hash = 0xDC6C99C1;
                unkBunk_0x8.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkBunk_0x8.Data_Hash.Hash).description;

                dataList.Insert(EndOfBlastBurstIndex + 12, unkBunk_0x8);

                Unit_Data unkBunk_0xC = (Unit_Data)unk_0xC.DeepClone();
                unkBunk_0xC.Data_Hash.Hash = 0x27AAECA6;
                unkBunk_0xC.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkBunk_0xC.Data_Hash.Hash).description;

                dataList.Insert(EndOfBlastBurstIndex + 13, unkBunk_0xC);

                Unit_Data unkBunk_0x10 = (Unit_Data)unk_0x10.DeepClone();
                unkBunk_0x10.Data_Hash.Hash = 0xA1DEFB23;
                unkBunk_0x10.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkBunk_0x10.Data_Hash.Hash).description;

                dataList.Insert(EndOfBlastBurstIndex + 14, unkBunk_0x10);

                Unit_Data unkBunk_0x14 = (Unit_Data)unk_0x14.DeepClone();
                unkBunk_0x14.Data_Hash.Hash = 0xFFD0CF31;
                unkBunk_0x14.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkBunk_0x14.Data_Hash.Hash).description;

                dataList.Insert(EndOfBlastBurstIndex + 15, unkBunk_0x14);

                Unit_Data unkBunk_0x18 = (Unit_Data)unk_0x18.DeepClone();
                unkBunk_0x18.Data_Hash.Hash = 0x5813EAD0;
                unkBunk_0x18.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkBunk_0x18.Data_Hash.Hash).description;

                dataList.Insert(EndOfBlastBurstIndex + 16, unkBunk_0x18);


                int Unk2BCIndex = dataList.FindIndex(x => x.Data_Hash.Hash == 0x372435B3);

                Unit_Data unkB2unk_1C = (Unit_Data)unk_1C.DeepClone();
                unkB2unk_1C.Data_Hash.Hash = 0x9124A5C1;
                unkB2unk_1C.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkB2unk_1C.Data_Hash.Hash).description;

                dataList.Insert(Unk2BCIndex + 1, unkB2unk_1C);

                Unit_Data unkB2unk_20 = (Unit_Data)unk_20.DeepClone();
                unkB2unk_20.Data_Hash.Hash = 0x4B0681C9;
                unkB2unk_20.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkB2unk_20.Data_Hash.Hash).description;

                dataList.Insert(Unk2BCIndex + 2, unkB2unk_20);

                Unit_Data unkB2Damage_Dealt_EX_Gauge_Increase_Multiplier = (Unit_Data)Damage_Dealt_EX_Gauge_Increase_Multiplier.DeepClone();
                unkB2Damage_Dealt_EX_Gauge_Increase_Multiplier.Data_Hash.Hash = 0xAECA6071;
                unkB2Damage_Dealt_EX_Gauge_Increase_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkB2Damage_Dealt_EX_Gauge_Increase_Multiplier.Data_Hash.Hash).description;

                dataList.Insert(Unk2BCIndex + 3, unkB2Damage_Dealt_EX_Gauge_Increase_Multiplier);

                Unit_Data unkB2Damage_Taken_EX_Gauge_Increase_Multiplier = (Unit_Data)Damage_Taken_EX_Gauge_Increase_Multiplier.DeepClone();
                unkB2Damage_Taken_EX_Gauge_Increase_Multiplier.Data_Hash.Hash = 0x6D82D38C;
                unkB2Damage_Taken_EX_Gauge_Increase_Multiplier.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkB2Damage_Taken_EX_Gauge_Increase_Multiplier.Data_Hash.Hash).description;

                dataList.Insert(Unk2BCIndex + 4, unkB2Damage_Taken_EX_Gauge_Increase_Multiplier);

                Unit_Data unkB2unk_0x8 = (Unit_Data)unk_0x8.DeepClone();
                unkB2unk_0x8.Data_Hash.Hash = 0xA540F511;
                unkB2unk_0x8.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkB2unk_0x8.Data_Hash.Hash).description;

                dataList.Insert(Unk2BCIndex + 5, unkB2unk_0x8);

                Unit_Data unkB2unk_0xC = (Unit_Data)unk_0xC.DeepClone();
                unkB2unk_0xC.Data_Hash.Hash = 0x5E868076;
                unkB2unk_0xC.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkB2unk_0xC.Data_Hash.Hash).description;

                dataList.Insert(Unk2BCIndex + 6, unkB2unk_0xC);

                Unit_Data unkB2unk_0x10 = (Unit_Data)unk_0x10.DeepClone();
                unkB2unk_0x10.Data_Hash.Hash = 0x7A39B6B3;
                unkB2unk_0x10.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkB2unk_0x10.Data_Hash.Hash).description;

                dataList.Insert(Unk2BCIndex + 7, unkB2unk_0x10);

                Unit_Data unkB2unk_0x14 = (Unit_Data)unk_0x14.DeepClone();
                unkB2unk_0x14.Data_Hash.Hash = 0x86FCA3E1;
                unkB2unk_0x14.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkB2unk_0x14.Data_Hash.Hash).description;

                dataList.Insert(Unk2BCIndex + 8, unkB2unk_0x14);

                Unit_Data unkB2unk_0x18 = (Unit_Data)unk_0x18.DeepClone();
                unkB2unk_0x18.Data_Hash.Hash = 0x83F4A740;
                unkB2unk_0x18.Data_Hash.description = data_Hashes.FirstOrDefault(x => x.Hash == unkB2unk_0x18.Data_Hash.Hash).description;

                dataList.Insert(Unk2BCIndex + 9, unkB2unk_0x18);
            }

            fs.Close();
            string JSON = JsonConvert.SerializeObject(unit_Varaibles, Formatting.Indented);
            StreamWriter sW = File.CreateText(Properties.Settings.Default.outputUnitDataJSONPath + @"\UnitData.JSON");
            sW.Write(JSON);
            sW.Close();
        }

        public void writeVariables()
        {
            StreamReader sR = File.OpenText(Properties.Settings.Default.inputUnitDataJSON);
            string JSON = sR.ReadToEnd();
            sR.Close();

            Unit_Varaibles unit_Varaibles = JsonConvert.DeserializeObject<Unit_Varaibles>(JSON);

            if (unit_Varaibles.schemaVersion != schemaVersion)
                throw new Exception("Unsupported Schema Version!");

            List<Ammo_Data> ammoDataList = unit_Varaibles.ammo_Datas;
            
            MemoryStream ammo_Hash_Chunk_MS = new MemoryStream();
            appendUIntMemoryStream(ammo_Hash_Chunk_MS, (uint)ammoDataList.Count, true); 

            for (int i = 0; i < ammoDataList.Count; i++)
            {
                appendUIntMemoryStream(ammo_Hash_Chunk_MS, ammoDataList[i].ammo_Hash, true);
            }

            MemoryStream data_Set_Index_MS = new MemoryStream();
            List<int> set_Data_Assignment_Index = unit_Varaibles.set_Data_Assignment_Index;
            appendUIntMemoryStream(data_Set_Index_MS, unit_Varaibles.setCount, true);
            for (int i = 0; i < unit_Varaibles.setCount; i++)
            {
                appendUIntMemoryStream(data_Set_Index_MS, (uint)set_Data_Assignment_Index[i], true);
            }

            MemoryStream data_Hash_MS = new MemoryStream();
            MemoryStream data_Type_MS = new MemoryStream();
            MemoryStream data_Value_MS = new MemoryStream();

            List<Unit_Data> dataList = unit_Varaibles.datas;
            appendUIntMemoryStream(data_Hash_MS, (uint)dataList.Count, true);
            List<MemoryStream> data_MS_List = new List<MemoryStream>();
            uint setCount = unit_Varaibles.setCount;
            for (int i = 0; i < setCount; i++)
            {
                data_MS_List.Add(new MemoryStream());
            }
            for (int i = 0; i < dataList.Count; i++)
            {
                Data_Hash data_Hash = dataList[i].Data_Hash;
                List<dynamic> Data_Value = dataList[i].Data_Value;
                data_Types data_Type = dataList[i].Data_Type_Enum;

                appendUIntMemoryStream(data_Hash_MS, data_Hash.Hash, true);
                appendUIntMemoryStream(data_Type_MS, (uint)data_Type, true);

                if (Data_Value.Count != unit_Varaibles.setCount)
                    throw new Exception("Set count mismatch for hash: 0x" + data_Hash.Hash.ToString("X8"));

                for (int j = 0; j < Data_Value.Count; j++)
                {
                    switch (data_Type)
                    {
                        case data_Types.Float:
                            appendFloatMemoryStream(data_MS_List[j], (float)Data_Value[j], true);
                            break;
                        case data_Types.Int:
                            appendUIntMemoryStream(data_MS_List[j], (uint)Data_Value[j], true);
                            break;
                        default:
                            appendUIntMemoryStream(data_MS_List[j], (uint)Data_Value[j], true);
                            break;
                    }
                }
            }

            for (int i = 0; i < setCount; i++)
            {
                MemoryStream memoryStream = data_MS_List[i];
                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.CopyTo(data_Value_MS);
            }

            ammo_Hash_Chunk_MS.Seek(0, SeekOrigin.Begin);
            data_Set_Index_MS.Seek(0, SeekOrigin.Begin);
            data_Hash_MS.Seek(0, SeekOrigin.Begin);
            data_Type_MS.Seek(0, SeekOrigin.Begin);
            data_Value_MS.Seek(0, SeekOrigin.Begin);

            MemoryStream Unit_Variable_MS = new MemoryStream();

            appendUIntMemoryStream(Unit_Variable_MS, unit_Varaibles.magic, true);
            appendUIntMemoryStream(Unit_Variable_MS, unit_Varaibles.Unit_ID, true);
            appendUIntMemoryStream(Unit_Variable_MS, unit_Varaibles.unk_Hash, true);
            appendUIntMemoryStream(Unit_Variable_MS, 0, true);

            appendUIntMemoryStream(Unit_Variable_MS, 0x30, true);
            appendUIntMemoryStream(Unit_Variable_MS, 0x30 + (uint)ammo_Hash_Chunk_MS.Length, true);
            appendUIntMemoryStream(Unit_Variable_MS, 0x30 + (uint)ammo_Hash_Chunk_MS.Length + (uint)data_Hash_MS.Length + (uint)data_Type_MS.Length, true);
            appendUIntMemoryStream(Unit_Variable_MS, 0x30 + (uint)ammo_Hash_Chunk_MS.Length + (uint)data_Hash_MS.Length + (uint)data_Type_MS.Length + (uint)data_Set_Index_MS.Length, true);

            appendUIntMemoryStream(Unit_Variable_MS, unit_Varaibles.reloadHashIndex_Slot_1, true);
            appendUIntMemoryStream(Unit_Variable_MS, unit_Varaibles.reloadHashIndex_Slot_2, true);
            appendUIntMemoryStream(Unit_Variable_MS, unit_Varaibles.reloadHashIndex_Slot_3, true);
            appendUIntMemoryStream(Unit_Variable_MS, unit_Varaibles.reloadHashIndex_Slot_4, true);

            ammo_Hash_Chunk_MS.CopyTo(Unit_Variable_MS);
            data_Hash_MS.CopyTo(Unit_Variable_MS);
            data_Type_MS.CopyTo(Unit_Variable_MS);
            data_Set_Index_MS.CopyTo(Unit_Variable_MS);
            data_Value_MS.CopyTo(Unit_Variable_MS);

            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.inputUnitDataJSON);
            FileStream ofs = File.Create(Properties.Settings.Default.outputUnitDataBinaryPath + @"\" + fileName + ".bin");
            Unit_Variable_MS.Seek(0, SeekOrigin.Begin);
            Unit_Variable_MS.CopyTo(ofs);
            ofs.Close();
        }


        public void combineDataHashSchema()
        {
            Data_Hash_Schema FBSchema = parseDataHashSchema(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB JSON\FBUnitDataSchema.JSON");
            Data_Hash_Schema MBONSchema = parseDataHashSchema(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB JSON\MBONUnitDataSchema.JSON");

            List<uint> FB_Hashes = FBSchema.Data_Hashes.Select(x => x.Hash).ToList();
            List<uint> MBON_Hashes = MBONSchema.Data_Hashes.Select(x => x.Hash).ToList();

            List<uint> intersect = FB_Hashes.Except(MBON_Hashes).ToList();
            List<uint> nonIntersect = MBON_Hashes.Except(FB_Hashes).ToList();
            for (int i = 0; i < nonIntersect.Count; i++)
            {
                uint asd = nonIntersect[i];
                if (FB_Hashes.Contains(nonIntersect[i]))
                    throw new Exception();

                Data_Hash data_Hash = MBONSchema.Data_Hashes.FirstOrDefault(x => x.Hash == nonIntersect[i]);
                if (data_Hash == null)
                    throw new Exception();

                data_Hash.description += " - MBON Exclusive";

                FBSchema.Data_Hashes.Add(data_Hash);
            }

            for(int i = 0; i < intersect.Count; i++)
            {
                Data_Hash data_Hash = FBSchema.Data_Hashes.FirstOrDefault(x => x.Hash == intersect[i]);
                data_Hash.description += " - FB Exclusive";
            }

            string JSON = JsonConvert.SerializeObject(FBSchema);
            StreamWriter sW = File.CreateText(Properties.Settings.Default.outputUnitDataHashSchemaJSONPath + @"\UnitDataSchema.JSON");
            sW.Write(JSON);
            sW.Close();
        }

        // Probably a one time export of the hashes
        public void writeDataHashSchema()
        {
            FileStream fs = File.OpenRead(Properties.Settings.Default.inputUnitDataHashSchemaBinary);

            Data_Hash_Schema data_Hash_Schema = new Data_Hash_Schema();
            data_Hash_Schema.schemaVersion = 1;
            List<Data_Hash> data_Hashes = data_Hash_Schema.Data_Hashes;

            uint Magic = readUIntBigEndian(fs);
            uint Unit_ID = readUIntBigEndian(fs);
            uint unk_Hash = readUIntBigEndian(fs);
            uint unk_0xC = readUIntBigEndian(fs);
            if (unk_0xC != 0)
                throw new Exception("unk_0xC is not 0!");
            uint ammoHashChunkPointer = readUIntBigEndian(fs);
            uint unitDataHashChunk = readUIntBigEndian(fs);

            fs.Seek(unitDataHashChunk, SeekOrigin.Begin);
            uint dataCount = readUIntBigEndian(fs);

            for (int i = 0; i < dataCount; i++)
            {
                uint data_Hash_Read = readUIntBigEndian(fs);
                Data_Hash data_Hash = new Data_Hash();
                data_Hash.Hash = data_Hash_Read;
                data_Hash.description = "to be filled in";
                data_Hashes.Add(data_Hash);
            }

            fs.Close();

            string JSON = JsonConvert.SerializeObject(data_Hash_Schema);
            StreamWriter sW = File.CreateText(Properties.Settings.Default.outputUnitDataHashSchemaJSONPath + @"\UnitDataSchema.JSON");
            sW.Write(JSON);
            sW.Close();
        }
    }
}
