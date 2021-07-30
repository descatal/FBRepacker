using FBRepacker.Data.DataTypes;
using FBRepacker.PAC;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace FBRepacker.Data
{
    class Voice_Line_Logic : Internals
    {
        uint unitID;

        public Voice_Line_Logic()
        {

        }

        public void deserializeVoiceLogicBinary()
        {
            UnitIDList unitIDList = load_UnitID();
            SoundLogicUnitIDGroupList soundLogicUnitIDGroupList = load_GroupList();
            List<Voice_Line_Logic_Set_Data> data = parse_Voice_Line_Logic(Properties.Settings.Default.inputVoiceLogicBinary, unitIDList, soundLogicUnitIDGroupList);
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Wing Zero EW\Extract MBON\Data - 4A5DEE5F\001-MBON\002-FHM\007.bin", unitIDList, soundLogicUnitIDGroupList);

            string outputName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.inputVoiceLogicBinary);

            write_Voice_Line_Data_Json(data, Properties.Settings.Default.outputVoiceLogicJSONFolder + @"\" + outputName + ".JSON");
                
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Wing Zero EW\Converted from MBON\WingZeroEWVoiceLogic.json");
        }

        public void serializeVoiceLogicBinary()
        {
            StreamReader JSON = File.OpenText(Properties.Settings.Default.inputVoiceLogicJSON);
            string JSONStr = JSON.ReadToEnd();

            List<Voice_Line_Logic_Set_Data> data = JsonConvert.DeserializeObject<List<Voice_Line_Logic_Set_Data>>(JSONStr);

            string outputName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.inputVoiceLogicJSON);

            write_FB_Voice_Line_Logic(data, Properties.Settings.Default.outputVoiceLogicJSONFolder + @"\" + outputName + ".bin");
                
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Wing Zero EW\Converted from MBON\Voice Logic.bin");
        }


        public UnitIDList load_UnitID()
        {
            string jsonString = Properties.Resources.Unit_IDs;
            UnitIDList unit_ID = System.Text.Json.JsonSerializer.Deserialize<UnitIDList>(jsonString);
            return unit_ID;
        }

        public SoundLogicUnitIDGroupList load_GroupList()
        {
            // Group list in MBON
            string jsonString = Properties.Resources.Voice_Unit_ID_Group;
            SoundLogicUnitIDGroupList unit_ID_Group = System.Text.Json.JsonSerializer.Deserialize<SoundLogicUnitIDGroupList>(jsonString);
            return unit_ID_Group;
        }

        public void write_Voice_Line_Data_Json(List<Voice_Line_Logic_Set_Data> data, string outputPath)
        {
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = System.Text.Json.JsonSerializer.Serialize<List<Voice_Line_Logic_Set_Data>>(data, options);
            StreamWriter fs = File.CreateText(outputPath);
            fs.Write(jsonString);
            fs.Close();
        }

        public List<Voice_Line_Logic_Set_Data> parse_Voice_Line_Logic(string path, UnitIDList unitIDList, SoundLogicUnitIDGroupList soundLogicUnitIDGroupList)
        {
            FileStream fs = File.OpenRead(path);
            changeStreamFile(fs);

            List<Voice_Line_Logic_Set_Data> set_Data = new List<Voice_Line_Logic_Set_Data>();

            Stream.Seek(0x4, SeekOrigin.Begin);
            unitID = readUIntBigEndian();

            uint pointer1 = readUIntBigEndian();
            uint pointer2 = readUIntBigEndian();
            uint pointer3 = readUIntBigEndian();

            // Parse Individual Voice Line Sets
            Stream.Seek(pointer1, SeekOrigin.Begin);
            uint IndividualSetCount = readUIntBigEndian();

            Stream.Seek(0xC, SeekOrigin.Current);

            for (int i = 0; i < IndividualSetCount; i++)
            {
                uint prop1 = readUIntBigEndian();
                uint index = readUIntBigEndian();
                uint prop2 = readUIntBigEndian();
                uint prop3 = readUIntBigEndian();
                uint prop4 = readUIntBigEndian();
                uint prop5 = readUIntBigEndian();
                uint prop6 = readUIntBigEndian();
                uint prop7 = readUIntBigEndian();
                uint prop8 = readUIntBigEndian();
                uint prop9 = readUIntBigEndian();
                uint prop10 = readUIntBigEndian();

                Voice_Line_Logic_Set_Data voice_Line_Logic_Set_Data = new Voice_Line_Logic_Set_Data();
                Voice_Line_Logic_Set_Data_Individual_Properties voice_Line_Logic_Set_Data_Properties = new Voice_Line_Logic_Set_Data_Individual_Properties();
                voice_Line_Logic_Set_Data_Properties.prop1 = prop1;
                voice_Line_Logic_Set_Data_Properties.prop2 = prop2;
                voice_Line_Logic_Set_Data_Properties.prop3 = prop3;
                voice_Line_Logic_Set_Data_Properties.prop4 = prop4;
                voice_Line_Logic_Set_Data_Properties.prop5 = prop5;
                voice_Line_Logic_Set_Data_Properties.prop6 = prop6;
                voice_Line_Logic_Set_Data_Properties.prop7 = prop7;
                voice_Line_Logic_Set_Data_Properties.prop8 = prop8;
                voice_Line_Logic_Set_Data_Properties.prop9 = prop9;
                voice_Line_Logic_Set_Data_Properties.prop10 = prop10;

                uint voiceCount = readUIntBigEndian();
                uint setPointer = readUIntBigEndian();

                voice_Line_Logic_Set_Data.properties = voice_Line_Logic_Set_Data_Properties;
                voice_Line_Logic_Set_Data.voiceCount = voiceCount;
                voice_Line_Logic_Set_Data.setPointer = setPointer;
                voice_Line_Logic_Set_Data.voiceType = voiceType.Individual;
                voice_Line_Logic_Set_Data.index = index;

                uint returnAddress = (uint)Stream.Position;
                List<uint> voiceHashes = new List<uint>();

                Stream.Seek(setPointer, SeekOrigin.Begin);
                for (int j = 0; j < voiceCount; j++)
                {
                    uint voiceHash = readUIntBigEndian();
                    voiceHashes.Add(voiceHash);
                }

                voice_Line_Logic_Set_Data.voiceHashes = voiceHashes;

                Stream.Seek(returnAddress, SeekOrigin.Begin);
                set_Data.Add(voice_Line_Logic_Set_Data);
            }

            // Parse Triggered Voice Line Sets
            Stream.Seek(pointer2, SeekOrigin.Begin);
            uint TriggeredSetCount = readUIntBigEndian();

            Stream.Seek(0xC, SeekOrigin.Current);

            for (int i = 0; i < TriggeredSetCount; i++)
            {
                uint triggerCondition = readUIntBigEndian();
                uint index = readUIntBigEndian();
                uint groupID = readUIntBigEndian();
                uint voiceCount = readUIntBigEndian();
                uint setPointer = readUIntBigEndian();

                Voice_Line_Logic_Set_Data voice_Line_Logic_Set_Data = new Voice_Line_Logic_Set_Data();
                voice_Line_Logic_Set_Data.voiceCount = voiceCount;
                voice_Line_Logic_Set_Data.setPointer = setPointer;
                voice_Line_Logic_Set_Data.triggerCondition = triggerCondition;
                voice_Line_Logic_Set_Data.index = index;
                
                uint returnAddress = (uint)Stream.Position;
                List<uint> voiceHashes = new List<uint>();

                Stream.Seek(setPointer, SeekOrigin.Begin);
                for (int j = 0; j < voiceCount; j++)
                {
                    uint voiceHash = readUIntBigEndian();
                    voiceHashes.Add(voiceHash);
                }

                List<uint> triggerUnitIDs = soundLogicUnitIDGroupList.soundLogicUnitIDGroupList.FirstOrDefault(s => s.groupID.Equals(groupID)).unitIDs;

                voice_Line_Logic_Set_Data.voiceHashes = voiceHashes;
                voice_Line_Logic_Set_Data.triggerUnitID = triggerUnitIDs;
                voice_Line_Logic_Set_Data.voiceType = voiceType.Triggered;

                Stream.Seek(returnAddress, SeekOrigin.Begin);
                set_Data.Add(voice_Line_Logic_Set_Data);
            }

            // Parse Paired Voice Line Sets (Banters)
            Stream.Seek(pointer3, SeekOrigin.Begin);
            uint PairedSetCount = readUIntBigEndian();

            Stream.Seek(0xC, SeekOrigin.Current);

            for (int i = 0; i < PairedSetCount; i++)
            {
                uint triggerCondition = readUIntBigEndian();
                uint index = readUIntBigEndian();
                uint groupID = readUIntBigEndian();
                uint voiceCount = 2; // Paired is always two
                uint setPointer = 0; // There's no pointer
                uint self_VoiceHash = readUIntBigEndian();
                uint target_VoiceHash = readUIntBigEndian();
                uint unk_Zero = readUIntBigEndian(); // Not sure what

                List<uint> voiceHashes = new List<uint>();
                voiceHashes.Add(self_VoiceHash);
                voiceHashes.Add(target_VoiceHash);

                List<uint> triggerUnitIDs = soundLogicUnitIDGroupList.soundLogicUnitIDGroupList.FirstOrDefault(s => s.groupID.Equals(groupID)).unitIDs;

                Voice_Line_Logic_Set_Data voice_Line_Logic_Set_Data = new Voice_Line_Logic_Set_Data();
                voice_Line_Logic_Set_Data.voiceCount = voiceCount;
                voice_Line_Logic_Set_Data.setPointer = setPointer;
                voice_Line_Logic_Set_Data.triggerCondition = triggerCondition;
                voice_Line_Logic_Set_Data.index = index;
                voice_Line_Logic_Set_Data.voiceHashes = voiceHashes;
                voice_Line_Logic_Set_Data.triggerUnitID = triggerUnitIDs;
                voice_Line_Logic_Set_Data.voiceType = voiceType.Pair_Triggered;

                set_Data.Add(voice_Line_Logic_Set_Data);
            }

            fs.Close();

            return set_Data;
        }

        private void write_FB_Voice_Line_Logic(List<Voice_Line_Logic_Set_Data> data, string path)
        {
            MemoryStream Voice_Line_Logic = new MemoryStream();
            MemoryStream header = new MemoryStream();
            MemoryStream Individual_Voice_Lines = new MemoryStream();
            MemoryStream Triggered_Voice_Lines = new MemoryStream();
            MemoryStream Paired_Voice_Lines = new MemoryStream();

            List<Voice_Line_Logic_Set_Data> IndividualUnsorted = data.Where(s => s.voiceType.Equals(voiceType.Individual)).ToList();
            List<Voice_Line_Logic_Set_Data> Individual = IndividualUnsorted.OrderBy(o => o.properties.prop1).ToList();
            List<Voice_Line_Logic_Set_Data> Triggered = data.Where(s => s.voiceType.Equals(voiceType.Triggered)).ToList();
            List<Voice_Line_Logic_Set_Data> Paired = data.Where(s => s.voiceType.Equals(voiceType.Pair_Triggered)).ToList();

            appendUIntMemoryStream(header, 0x1964B447, true); // No idea, the magic seems to be changing everytime. 
            appendUIntMemoryStream(header, unitID, true);
            appendUIntMemoryStream(header, (uint)Individual.Count, true);

            // Individual Voice Lines
            MemoryStream prop1 = new MemoryStream();
            MemoryStream prop2 = new MemoryStream();
            MemoryStream prop3 = new MemoryStream();
            MemoryStream prop4 = new MemoryStream();
            MemoryStream prop5 = new MemoryStream();
            MemoryStream prop6 = new MemoryStream();
            MemoryStream prop7 = new MemoryStream();
            MemoryStream prop8 = new MemoryStream();
            MemoryStream prop9 = new MemoryStream();
            MemoryStream prop10 = new MemoryStream();
            MemoryStream properties = new MemoryStream();
            MemoryStream Individual_Pointers = new MemoryStream();
            MemoryStream Individual_Voice_Hash = new MemoryStream();
            List<uint> Individual_Relative_Offset = new List<uint>();

            for (int i = 0; i < Individual.Count; i++)
            {
                appendUIntMemoryStream(prop1, Individual[i].properties.prop1, true);
                appendUIntMemoryStream(prop2, Individual[i].properties.prop2, true);
                appendUIntMemoryStream(prop3, Individual[i].properties.prop3, true);
                appendUIntMemoryStream(prop4, Individual[i].properties.prop4, true);
                appendUIntMemoryStream(prop5, Individual[i].properties.prop5, true);
                appendUIntMemoryStream(prop6, Individual[i].properties.prop6, true);
                appendUIntMemoryStream(prop7, Individual[i].properties.prop7, true);
                appendUIntMemoryStream(prop8, Individual[i].properties.prop8, true);
                appendUIntMemoryStream(prop9, Individual[i].properties.prop9, true);
                appendUIntMemoryStream(prop10, Individual[i].properties.prop10, true);

                Individual_Relative_Offset.Add((uint)Individual_Voice_Hash.Position);
                uint voiceCount = Individual[i].voiceCount;
                appendUIntMemoryStream(Individual_Voice_Hash, voiceCount, true);
                for (int j = 0; j < voiceCount; j++)
                {
                    appendUIntMemoryStream(Individual_Voice_Hash, Individual[i].voiceHashes[j], true);
                }
            }

            prop1.Position = 0;
            prop2.Position = 0;
            prop3.Position = 0;
            prop4.Position = 0;
            prop5.Position = 0;
            prop6.Position = 0;
            prop7.Position = 0;
            prop8.Position = 0;
            prop9.Position = 0;
            prop10.Position = 0;

            prop1.CopyTo(properties);
            prop2.CopyTo(properties);
            prop3.CopyTo(properties);
            prop4.CopyTo(properties);
            prop5.CopyTo(properties);
            prop6.CopyTo(properties);
            prop7.CopyTo(properties);
            prop8.CopyTo(properties);
            prop9.CopyTo(properties);
            prop10.CopyTo(properties);

            properties.Position = 0;
            properties.CopyTo(Individual_Voice_Lines);

            for (int i = 0; i < Individual.Count; i++)
            {
                uint Individual_Voice_Hash_Start_Offset = 0x20 + (uint)properties.Length + (uint)(Individual.Count * 0x04); // header size + properties section size + pointer section size
                uint Individual_Offset = Individual_Voice_Hash_Start_Offset + Individual_Relative_Offset[i];
                appendUIntMemoryStream(Individual_Pointers, Individual_Offset, true);
            }

            Individual_Pointers.Position = 0;
            Individual_Voice_Hash.Position = 0;
            Individual_Pointers.CopyTo(Individual_Voice_Lines);
            Individual_Voice_Hash.CopyTo(Individual_Voice_Lines);

            // Triggered Voice Lines
            MemoryStream Triggered_Pointers = new MemoryStream();
            MemoryStream Triggered_Voice_Hash = new MemoryStream();
            List<uint> Triggered_Relative_Offset = new List<uint>();
            
            appendUIntMemoryStream(Triggered_Voice_Lines, (uint)Triggered.Count, true);
            appendZeroMemoryStream(Triggered_Voice_Lines, 0xC);

            for (int i = 0; i < Triggered.Count; i++)
            {
                Triggered_Relative_Offset.Add((uint)Triggered_Voice_Hash.Position);

                appendUIntMemoryStream(Triggered_Voice_Hash, 0, true); // The unk flag stuff? Hopefully does not cause anything weird by setting it to 0.

                uint triggerCondition = Triggered[i].triggerCondition;
                appendUIntMemoryStream(Triggered_Voice_Hash, triggerCondition, true);

                uint voiceCount = Triggered[i].voiceCount;
                appendUIntMemoryStream(Triggered_Voice_Hash, voiceCount, true);

                uint triggerUnitIDCount = (uint)Triggered[i].triggerUnitID.Count;
                appendUIntMemoryStream(Triggered_Voice_Hash, triggerUnitIDCount, true);

                for (int j = 0; j < voiceCount; j++)
                {
                    appendUIntMemoryStream(Triggered_Voice_Hash, Triggered[i].voiceHashes[j], true);
                }

                for (int j = 0; j < triggerUnitIDCount; j++)
                {
                    appendUIntMemoryStream(Triggered_Voice_Hash, Triggered[i].triggerUnitID[j], true);
                }
            }

            uint Triggered_Voice_Hash_Start_Offset = 0x20 + (uint)Individual_Voice_Lines.Length + 0x10 + (uint)(Triggered.Count * 0x04); // header size +　Individual section size + Triggered set count section size + Triggered Pointer section size

            for (int i = 0; i < Triggered.Count; i++)
            {
                uint Triggered_Offset = Triggered_Voice_Hash_Start_Offset + Triggered_Relative_Offset[i];
                appendUIntMemoryStream(Triggered_Pointers, Triggered_Offset, true);
            }

            Triggered_Pointers.Position = 0;
            Triggered_Voice_Hash.Position = 0;
            Triggered_Pointers.CopyTo(Triggered_Voice_Lines);
            Triggered_Voice_Hash.CopyTo(Triggered_Voice_Lines);

            // Paired Voice Lines
            MemoryStream Paired_Pointers = new MemoryStream();
            MemoryStream Paired_Voice_Hash = new MemoryStream();
            List<uint> Paired_Relative_Offset = new List<uint>();

            appendUIntMemoryStream(Paired_Voice_Lines, (uint)Paired.Count, true);
            appendZeroMemoryStream(Paired_Voice_Lines, 0xC);

            for (int i = 0; i < Paired.Count; i++)
            {
                Paired_Relative_Offset.Add((uint)Paired_Voice_Hash.Position);

                appendUIntMemoryStream(Paired_Voice_Hash, 0, true); // The unk flag stuff? Hopefully does not cause anything weird by setting it to 0.

                uint pairedCondition = Paired[i].triggerCondition;
                appendUIntMemoryStream(Paired_Voice_Hash, pairedCondition, true);

                uint voiceCount = Paired[i].voiceCount;

                if (voiceCount != 2)
                    throw new System.Exception("Paired hash count != 2!");

                for (int j = 0; j < voiceCount; j++)
                {
                    appendUIntMemoryStream(Paired_Voice_Hash, Paired[i].voiceHashes[j], true);
                }

                appendUIntMemoryStream(Paired_Voice_Hash, 0, true); // Not sure what this is

                uint pairedUnitIDCount = (uint)Paired[i].triggerUnitID.Count;

                if (pairedUnitIDCount != 1)
                    throw new System.Exception("Paired unit ID count != 1!");

                appendUIntMemoryStream(Paired_Voice_Hash, pairedUnitIDCount, true);
                appendUIntMemoryStream(Paired_Voice_Hash, (uint)Paired[i].triggerUnitID[0], true);
            }

            uint Paired_Voice_Hash_Start_Offset = 0x20 + (uint)Individual_Voice_Lines.Length + (uint)Triggered_Voice_Lines.Length + 0x10 + (uint)(Paired.Count * 0x04); // header size +　Individual section size + Triggered set count section size + Triggered Pointer section size

            for (int i = 0; i < Paired.Count; i++)
            {
                uint Paired_Offset = Paired_Voice_Hash_Start_Offset + Paired_Relative_Offset[i];
                appendUIntMemoryStream(Paired_Pointers, Paired_Offset, true);
            }

            Paired_Pointers.Position = 0;
            Paired_Voice_Hash.Position = 0;
            Paired_Pointers.CopyTo(Paired_Voice_Lines);
            Paired_Voice_Hash.CopyTo(Paired_Voice_Lines);

            // Header and pointers.
            uint sectionOffset = 0x20 + (uint)properties.Length;
            appendUIntMemoryStream(header, sectionOffset, true);

            sectionOffset = 0x20 + (uint)Individual_Voice_Lines.Length;
            appendUIntMemoryStream(header, sectionOffset, true);

            sectionOffset += (uint)Triggered_Voice_Lines.Length;
            appendUIntMemoryStream(header, sectionOffset, true);

            appendZeroMemoryStream(header, 0x8);

            header.Position = 0;
            Individual_Voice_Lines.Position = 0;
            Triggered_Voice_Lines.Position = 0;
            Paired_Voice_Lines.Position = 0;

            header.CopyTo(Voice_Line_Logic);
            Individual_Voice_Lines.CopyTo(Voice_Line_Logic);
            Triggered_Voice_Lines.CopyTo(Voice_Line_Logic);
            Paired_Voice_Lines.CopyTo(Voice_Line_Logic);

            addPaddingStream(Voice_Line_Logic);
            appendZeroMemoryStream(Voice_Line_Logic, 0x1); // I have no idea why it adds one 0 after padding.

            FileStream fs = File.Create(path);
            fs.Write(Voice_Line_Logic.ToArray(), 0, (int)Voice_Line_Logic.Length);

            fs.Close();
        }
    }

    public enum voiceType
    {
        Individual,
        Triggered, // Triggered by unit ID
        Pair_Triggered, // Banters
    }

    class Voice_Line_Logic_Set_Data
    {
        public voiceType voiceType { get; set; }
        public Voice_Line_Logic_Set_Data_Individual_Properties properties { get; set; }
        public uint setPointer { get; set; }
        public uint voiceCount { get; set; }
        public List<uint> voiceHashes { get; set; }
        public uint index { get; set; }
        public uint triggerCondition { get; set; }
        public List<uint> triggerUnitID { get; set; }

        public Voice_Line_Logic_Set_Data() 
        {
            this.triggerCondition = 0;
            this.triggerUnitID = new List<uint>();
        }
    }

    class Voice_Line_Logic_Set_Data_Individual_Properties
    {
        public uint prop1 { get; set; }
        public uint prop2 { get; set; }
        public uint prop3 { get; set; }
        public uint prop4 { get; set; }
        public uint prop5 { get; set; }
        public uint prop6 { get; set; }
        public uint prop7 { get; set; }
        public uint prop8 { get; set; }
        public uint prop9 { get; set; }
        public uint prop10 { get; set; }
    }
}
