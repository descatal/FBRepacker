using FBRepacker.Data.DataTypes;
using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FBRepacker.Data.MBON_Parse
{
    class ParseUnitIDGroup : Internals
    {
        public ParseUnitIDGroup()
        {
            parseIDGroup(@"G:\Games\PS4\MBON\Unit ID Group List Big Endian (Voice Logic).bin");
        }

        private void parseIDGroup(string path)
        {
            FileStream fs = File.OpenRead(path);
            changeStreamFile(fs);

            Stream.Seek(0x4, SeekOrigin.Begin); // Skip magic, should be 0x238ABFF0

            uint groupCount = readUIntBigEndian();
            Stream.Seek(0x8, SeekOrigin.Current);

            List<SoundLogicUnitIDGroup> soundLogicUnitIDGroups = new List<SoundLogicUnitIDGroup>();
            
            for (int i = 0; i < groupCount; i++)
            {
                SoundLogicUnitIDGroup properties = new SoundLogicUnitIDGroup();
                int groupID = (int)readUIntBigEndian();
                properties.groupID = groupID;

                uint groupPointer = readUIntBigEndian();
                properties.groupPointer = groupPointer;
                uint returnAddress = (uint)Stream.Position;
                Stream.Seek(groupPointer, SeekOrigin.Begin);

                uint IDcount = readUIntBigEndian();
                List<uint> unitIDs = new List<uint>();
                for(int j = 0; j < IDcount; j++)
                {
                    uint unitID = readUIntBigEndian();
                    unitIDs.Add(unitID);
                }

                properties.unitIDs = unitIDs;
                soundLogicUnitIDGroups.Add(properties);
                Stream.Seek(returnAddress, SeekOrigin.Begin);
            }

            SoundLogicUnitIDGroupList soundLogicUnitIDGroupList = new SoundLogicUnitIDGroupList();

            soundLogicUnitIDGroupList.soundLogicUnitIDGroupList = soundLogicUnitIDGroups;

            string jsonString = JsonSerializer.Serialize(soundLogicUnitIDGroupList, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(@"G:\Games\PS4\MBON\GroupList.json", jsonString);

            fs.Close();
        }
    }
}
