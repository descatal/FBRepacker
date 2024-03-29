﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    public class Rootobject
    {
        public UnitID[] UnitID { get; set; }
    }

    public class UnitID
    {
        public uint id { get; set; }
        public string name_english { get; set; }
        public string name_japanese { get; set; }
        public string name_chinese { get; set; }
    }

    public class UnitIDList
    {
        public List<UnitID> Unit_ID { get; set; }

        public UnitIDList()
        {
            Unit_ID = new List<UnitID>();  
        }
    }

    class SoundLogicUnitIDGroup
    {
        public int groupID { get; set; }
        public uint groupPointer { get; set; }
        public List<uint> unitIDs { get; set; }

        public SoundLogicUnitIDGroup()
        {
            unitIDs = new List<uint>(); 
        }
    }

    class SoundLogicUnitIDGroupList
    {
        public List<SoundLogicUnitIDGroup> soundLogicUnitIDGroupList { get; set; }

        public SoundLogicUnitIDGroupList()
        {
            soundLogicUnitIDGroupList = new List<SoundLogicUnitIDGroup>();
        }
    }
}
