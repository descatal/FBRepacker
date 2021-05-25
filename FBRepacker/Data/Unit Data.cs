using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using FBRepacker.Data.DataTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data
{
    class Unit_Data : Internals
    {
        uint FB_Magic, FB_UnitHash, FB_unkFlag;
        List<uint> FB_ammoSlotHashes = new List<uint>();
        List<uint> FB_unkEnums = new List<uint>();

        public Unit_Data()
        {
            /* 
            using()
            {

            }
             */
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
        }

        private Dictionary<uint, Unit_Varaibles> readVariables(string path, bool isFB)
        {
            FileStream fs = File.OpenRead(path);
            changeStreamFile(fs);

            Stream.Seek(0, SeekOrigin.Begin);
            uint Magic = readUIntBigEndian(Stream.Position);
            uint unitHash = readUIntBigEndian(Stream.Position);
            uint unkFlags = readUIntBigEndian(Stream.Position);

            if (isFB)
            {
                FB_Magic = Magic;
                FB_UnitHash = unitHash;
                FB_unkFlag = unkFlags;
            }

            Stream.Seek(0x4, SeekOrigin.Current);
            uint p1 = readUIntBigEndian(Stream.Position);
            uint p2 = readUIntBigEndian(Stream.Position);
            uint p3 = readUIntBigEndian(Stream.Position);
            uint p4 = readUIntBigEndian(Stream.Position);

            Stream.Seek(p1, SeekOrigin.Begin);
            int ammoSlotCount = readIntBigEndian(Stream.Position);

            /*
            if (ammoSlotCount >= 5)
                throw new Exception("ammo Slot Count > 5!");
             */

            List<uint> ammoSlotHashes = new List<uint>();
            for (int i = 0; i < ammoSlotCount; i++)
            {
                ammoSlotHashes.Add(readUIntBigEndian(Stream.Position));
            }

            if (isFB)
                FB_ammoSlotHashes = ammoSlotHashes;

            Stream.Seek(p2, SeekOrigin.Begin);
            int dataHashCount = readIntBigEndian(Stream.Position);

            List<uint> dataHashes = new List<uint>();
            for (int i = 0; i < dataHashCount; i++)
            {
                dataHashes.Add(readUIntBigEndian(Stream.Position));
            }

            Dictionary<uint, Unit_Varaibles> Variables = new Dictionary<uint, Unit_Varaibles>();
            foreach (uint hash in dataHashes)
            {
                Unit_Varaibles var = new Unit_Varaibles();
                var.unkEnum = readUIntBigEndian(Stream.Position);
                Variables[hash] = var;
            }

            List<uint> unkEnums = new List<uint>();
            Stream.Seek(p3, SeekOrigin.Begin);
            uint count = ((p4 - p3) / 4);
            for(int i = 0; i < count; i++)
            {
                unkEnums.Add(readUIntBigEndian(Stream.Position));
            }

            if (isFB)
                FB_unkEnums = unkEnums;

            Stream.Seek(p4, SeekOrigin.Begin);

            foreach (var variables in Variables)
            {
                variables.Value.Data1 = readUIntBigEndian(Stream.Position);
            }

            foreach (var variables in Variables)
            {
                variables.Value.Data2 = readUIntBigEndian(Stream.Position);
            }

            fs.Close();

            return Variables;
        }
    }
}
