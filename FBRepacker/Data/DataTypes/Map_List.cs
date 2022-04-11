using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    internal class Map_List
    {
        public uint version { get; set; }
        public string SStageListString { get; set; }
        public List<Map_List_Properties> map_list_properties { get; set; }

        public Map_List()
        {
            map_list_properties = new List<Map_List_Properties>();
        }
    }

    class Map_List_Properties
    {
        public byte index { get; set; }
        public byte series_index { get; set; }
        public string release_string { get; set; }
        public string stage_string { get; set; }
        public uint map_hash { get; set; }
        public map_select_Flag map_select_Flags { get; set; }
        public uint map_sprite_hash { get; set; }
        public uint select_order { get; set; }
        public byte image_sprite_index { get; set; }
        public uint unk_0x20 { get; set; }
    }

    enum map_select_Flag
    {
        NO_SELECT = 0,
        FREE_BATTLE = 0x20,
        ONLINE_BATTLE = 0x40,
        INCLUDE_IN_RANDOM = 0x80, // if there's no random flag in all maps, it will choose random from other non 0 flag.
    }
}
