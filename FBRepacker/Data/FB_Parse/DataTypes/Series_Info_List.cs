using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.FB_Parse.DataTypes
{
    class Series_Info_List
    {
        public byte series_index { get; set; } // Might need to increase to short
        public byte series_index_2 { get; set; } // Weird index, idk what it does, but add 1 should be fine.
        public byte unk_0x3 { get; set; } // Always 0x80 unless the last unknown series
        public byte unk_0x4 { get; set; } // Always 0xFF
        public string release_string { get; set; } // should be constant E3 83 AA E3 83 AA E3 83 BC E3 82 B9
        public byte series_placement_order { get; set; }
        public byte series_sprite_index { get; set; } // the nth image to be used in the combined sprite PAC.
        public byte series_sprite_index_2 { get; set; } // should be the same as above
        public byte unk_0xB { get; set; } // Always 0xFF
        public uint series_movie_hash { get; set; } // the PAC file hash for the MP4 file that contains the launch movie.

        public Series_Info_List()
        {

        }
    }
}
