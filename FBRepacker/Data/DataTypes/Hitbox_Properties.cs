using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    internal class Hitbox_Properties
    {
        public uint version { get; set; }
        public uint ID_Hash { get; set; }
        public uint unit_ID { get; set; }
        public List<melee_Hitbox_Properties> melee_Hitbox_Data { get; set; }
        public List<unk_Hitbox_Properties> unk_Hitbox_Data { get; set; }
        public List<shield_Hitbox_Properties> shield_Hitbox_Data { get; set; }

        public Hitbox_Properties()
        {
            melee_Hitbox_Data = new List<melee_Hitbox_Properties>();
            unk_Hitbox_Data = new List<unk_Hitbox_Properties>();
            shield_Hitbox_Data = new List<shield_Hitbox_Properties>();
        }
    }

    class melee_Hitbox_Properties
    {
        public uint hitbox_Hash { get; set; }

        public uint hitbox_Hit_Hash { get; set; }

        public all_Hitbox_Types all_Hitbox_Types { get; set; }
    }

    class unk_Hitbox_Properties
    {
        public uint hitbox_Hash { get; set; }

        public all_Hitbox_Types all_Hitbox_Types { get; set; }
    }

    class shield_Hitbox_Properties
    {
        public uint hitbox_Hash { get; set; }

        public all_Hitbox_Types all_Hitbox_Types { get; set; }
    }

    class all_Hitbox_Types
    {
        public type_1_Hitbox_Data type_1_Hitboxes { get; set; }
        public type_2_Hitbox_Data type_2_Hitboxes { get; set; }
        // After investigation there's no type 3 to 5 data, just a constant 0x10 0 padding
        /*
        public type_3_Hitbox_Data type_3_Hitboxes { get; set; }
        public type_4_Hitbox_Data type_4_Hitboxes { get; set; }
        public type_5_Hitbox_Data type_5_Hitboxes { get; set; }
        */
    }

    class type_1_Hitbox_Data
    {
        public List<hitbox_Data> hitbox_Datas { get; set; }

        public type_1_Hitbox_Data()
        {
            hitbox_Datas = new List<hitbox_Data>();
        }
    }

    class type_2_Hitbox_Data
    {
        public List<hitbox_Data> hitbox_Datas { get; set; }

        public type_2_Hitbox_Data()
        {
            hitbox_Datas = new List<hitbox_Data>();
        }
    }

    // After investigation there's no type 3 to 5 data, just a constant 0x10 0 padding
    /*
    class type_3_Hitbox_Data
    {
        public List<hitbox_Data> hitbox_Datas { get; set; }

        public type_3_Hitbox_Data()
        {
            hitbox_Datas = new List<hitbox_Data>();
        }
    }

    class type_4_Hitbox_Data
    {
        public List<hitbox_Data> hitbox_Datas { get; set; }

        public type_4_Hitbox_Data()
        {
            hitbox_Datas = new List<hitbox_Data>();
        }
    }

    class type_5_Hitbox_Data
    {
        public List<hitbox_Data> hitbox_Datas { get; set; }

        public type_5_Hitbox_Data()
        {
            hitbox_Datas = new List<hitbox_Data>();
        }
    }
    */

    class hitbox_Data // should be fixed 0x40 length
    {
        public hitbox_Data_Type hitbox_Type { get; set; }
        public uint model_Hash { get; set; }
        public uint unk_0x8 { get; set; } // some kind of enum
        public float size { get; set; } // is it multiplier or actual size?
        public float unk_0x10 { get; set; }
        public float unk_0x14 { get; set; }
        public uint unk_0x18 { get; set; }
        public uint unk_0x1c { get; set; }
        public uint unk_0x20 { get; set; }
        public uint unk_0x24 { get; set; }
        public uint unk_0x28 { get; set; }
        public uint unk_0x2c { get; set; }
        public uint unk_0x30 { get; set; }
        public float unk_0x34 { get; set; }
        public float unk_0x38 { get; set; } // not sure what this does, but am sure is a float
        public float direction { get; set; } // not sure how it does this, but it will be negative on backwards hitboxes (e.g. shining's 2N)
    }

    enum hitbox_Data_Type
    {
        unk_0 = 0, // Found on Xi's shield hitbox, but no actual usage
        radius = 1, // normal melee radius size with multiplier
        narrow = 2, // seen on FAZZ's Ult, only front?
        shield = 3  // seen on temporary shield such as Wing EW's CSa activation
    }
}
