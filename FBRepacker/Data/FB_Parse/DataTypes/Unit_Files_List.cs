using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.FB_Parse.DataTypes
{
    internal class Unit_Files_List
    {
        public uint Unit_ID { get; set; }
        public uint dummy_PAC_hash { get; set; }
        public uint data_and_script_PAC_hash { get; set; }
        public uint model_and_texture_PAC_hash { get; set; }
        public uint animation_OMO_PAC_hash { get; set; }
        public uint effects_EIDX_PAC_hash { get; set; }
        public uint sound_effect_PAC_hash { get; set; }
        public uint global_pilot_voices_PAC_hash { get; set; }
        public uint weapon_image_DNSO_PAC_hash { get; set; }
        public uint sortie_and_awakening_sprites_PAC_hash { get; set; }
        public uint sortie_mouth_anim_enum_KPKP_PAC_hash { get; set; }
        public uint voice_file_list_PAC_hash { get; set; }
        public uint local_pilot_voices_STREAM_PAC_hash { get; set; }
        public bool MBONAdded { get; set; }

        public Unit_Files_List()
        {

        }
    }
}
