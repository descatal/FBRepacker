using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    class Voice_Line_Logic
    {
        public uint schemaVersion { get; set; }
        public uint magic { get; set; }
        public uint unit_ID { get; set; }
        public List<Voice_Line_Logic_Set_Data> voice_Line_Logic_Set_Datas { get; set; }

        public Voice_Line_Logic()
        {
            voice_Line_Logic_Set_Datas = new List<Voice_Line_Logic_Set_Data>();
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
