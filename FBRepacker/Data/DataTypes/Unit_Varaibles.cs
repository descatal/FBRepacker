using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    public enum data_Types
    {
        Float = 0,
        Int = 3,
        Unk = 6
    }

    class Unit_Varaibles
    {
        public uint schemaVersion { get; set; }
        public uint magic { get; set; }
        public uint setCount { get; set; }
        public uint Unit_ID { get; set; }
        public uint unk_Hash { get; set; }
        public uint reloadHashIndex_Slot_1 { get; set; } // used to be unk_0x20
        public uint reloadHashIndex_Slot_2 { get; set; } // used to be unk_0x24
        public uint reloadHashIndex_Slot_3 { get; set; } // used to be unk_0x28
        public uint reloadHashIndex_Slot_4 { get; set; } // used to be unk_0x2c
        public List<Ammo_Data> ammo_Datas { get; set; }
        
        // Section between the data type and data values
        // This index function similiarly to reload Hash Index, where it assigns the nth set to the nth mode of a unit.
        public List<int> set_Data_Assignment_Index { get; set; }
        public List<Unit_Data> datas { get; set; }

        public Unit_Varaibles()
        {
            ammo_Datas = new List<Ammo_Data>();
            datas = new List<Unit_Data>();
            set_Data_Assignment_Index = new List<int>();
        }
    }

    [Serializable]
    class Unit_Data
    {
        public data_Types Data_Type_Enum { get; set; }
        public Data_Hash Data_Hash { get; set; }
        public List<dynamic> Data_Value { get; set; }
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public Unit_Data()
        {
            Data_Hash = new Data_Hash();
            Data_Value = new List<dynamic>();
        }
    }

    public static class Extensions
    {
        public static T DeepClone<T>(this T obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                stream.Position = 0;

                return (T)formatter.Deserialize(stream);
            }
        }
    }

    class Ammo_Data
    {
        public uint ammo_Hash { get; set; }
    }

    class Data_Hash_Schema
    {
        public uint schemaVersion { get; set; }
        public List<Data_Hash> Data_Hashes { get; set; }

        public Data_Hash_Schema()
        {
            Data_Hashes = new List<Data_Hash>();
        }
    }

    [Serializable]
    class Data_Hash
    {
        public uint Hash { get; set; }
        public string description { get; set; }
        public Data_Hash()
        {
            description = "unidentified Hash";
        }
    }
}
