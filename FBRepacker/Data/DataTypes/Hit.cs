using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    class Hit
    {
        public uint magic_hash { get; set; }
        public List<uint> hitPropertiesType { get; set; }
        public List<HitProperties> hitProperties { get; set; }
        public Hit()
        {
            /*
            hashDic = new Dictionary<uint, object>
            {
                { 0x33EF2CCB, typeof(uint) },
                { 0xA964CCA4, typeof(uint) },
                { 0x07431A19, typeof(uint) },
                { 0x0EDBFE57, typeof(uint) },
                { 0xD196FC95, typeof(uint) },
                { 0x54058D5D, typeof(uint) },
                { 0x1A107AB7, typeof(uint) },
                { 0xFAE45595, typeof(uint) },
                { 0xBEDC2392, typeof(uint) },
                { 0xEE43A562, typeof(uint) },
                { 0x38BEA931, typeof(uint) },
                { 0xE392B8D6, typeof(uint) },
                { 0xA7C78487, typeof(uint) },
                { 0x7BE01C98, typeof(uint) },
                { 0x0408DD77, typeof(uint) },
                { 0x29941888, typeof(uint) },
                { 0xC47A5D38, typeof(uint) },
                { 0x502E9BAF, typeof(uint) },
                { 0x46CED294, typeof(uint) },
                { 0xBF000953, typeof(uint) },
                { 0x8B954576, typeof(uint) },
                { 0x57B2DD69, typeof(uint) },
                { 0xE252D228, typeof(uint) },
                { 0xC0EB5412, typeof(float) },
                { 0xDDCB9D74, typeof(uint) },
                { 0x8823E502, typeof(uint) },
                { 0x4F1F46C1, typeof(uint) },
                { 0xB3419082, typeof(uint) },
            };
            */
        }
    }

    public class HitProperties
    {
        public uint hash { get; set; }
        public uint hit_type { get; set; }
        public uint damage { get; set; }
        public uint unk_0x8 { get; set; }
        public uint down_value { get; set; }
        public uint yoruke_value { get; set; }
        public uint unk_MBON { get; set; }
        public uint unk_type_0x14 { get; set; }
        public float damage_correction { get; set; }
        public uint special_effect { get; set; }
        public uint hit_effect { get; set; }
        public uint fly_direction_1 { get; set; }
        public uint fly_direction_2 { get; set; }
        public uint fly_direction_3 { get; set; }
        public uint enemy_camera_shake_multiplier { get; set; }
        public uint player_camera_shake_multiplier { get; set; }
        public uint unk_0x38 { get; set; }
        public uint knock_up_angle { get; set; }
        public uint knock_up_range { get; set; }
        public uint unk_0x44 { get; set; }
        public uint multiple_hit_interval_frame { get; set; }
        public uint multiple_hit_count { get; set; }
        public uint enemy_stun_duration { get; set; }
        public uint player_stun_duration { get; set; }
        public uint hit_visual_effect { get; set; }
        public float hit_visual_effect_size_multiplier { get; set; }
        public uint hit_sound_effect_hash { get; set; }
        public uint unk_0x64 { get; set; }
        public uint friendly_damage_flag { get; set; }
        public uint unk_0x6C { get; set; }
    }

    public class HitProperties2
    {
        public uint hash { get; set; }
        public uint hit_type { get; set; }
        public uint damage { get; set; }
        public uint unk_0x8 { get; set; }
        public uint down_value { get; set; }
        public uint yoruke_value { get; set; }
        public uint unk_MBON { get; set; }
        public uint unk_type_0x14 { get; set; }
        public float damage_correction { get; set; }
        public uint special_effect { get; set; }
        public uint hit_effect { get; set; }
        public uint fly_direction_1 { get; set; }
        public uint fly_direction_2 { get; set; }
        public uint fly_direction_3 { get; set; }
        public uint enemy_camera_shake_multiplier { get; set; }
        public uint player_camera_shake_multiplier { get; set; }
        public uint unk_0x38 { get; set; }
        public uint knock_up_angle { get; set; }
        public uint knock_up_range { get; set; }
        public uint unk_0x44 { get; set; }
        public uint multiple_hit_interval_frame { get; set; }
        public uint multiple_hit_count { get; set; }
        public uint enemy_stun_duration { get; set; }
        public uint player_stun_duration { get; set; }
        public uint hit_visual_effect { get; set; }
        public float hit_visual_effect_size_multiplier { get; set; }
        public uint hit_sound_effect_hash { get; set; }
        public uint unk_0x64 { get; set; }
        public uint friendly_damage_flag { get; set; }
        public uint unk_0x6C { get; set; }
    }
    /*
    public class HitTemplate
    {
        public uint hash { get; set; }

        public Dictionary<uint, string> hashDic { get; set; }

        public HitTemplate()
        {
            hashDic = new Dictionary<uint, string>
            {
                { 0x33EF2CCB, "hit_type" },
                { 0xA964CCA4, "damage" },
                { 0x07431A19, "unk_0x8" },
                { 0x0EDBFE57, "down_value" },
                { 0xD196FC95, "yoruke_value" },
                { 0x54058D5D, "unk_type_0x14" },
                { 0x1A107AB7, "damage_correction" },
                { 0xFAE45595, "special_effect" },
                { 0xBEDC2392, "hit_effect" },
                { 0xEE43A562, "fly_direction_1" },
                { 0x38BEA931, "fly_direction_2" },
                { 0xE392B8D6, "fly_direction_3" },
                { 0xA7C78487, "enemy_camera_shake_multiplier" },
                { 0x7BE01C98, "player_camera_shake_multiplier" },
                { 0x0408DD77, "unk_0x38" },
                { 0x29941888, "knock_up_angle" },
                { 0xC47A5D38, "knock_up_range" },
                { 0x502E9BAF, "unk_0x44" },
                { 0x46CED294, "multiple_hit_interval_frame" },
                { 0xBF000953, "multiple_hit_count" },
                { 0x8B954576, "enemy_stun_duration" },
                { 0x57B2DD69, "player_stun_duration" },
                { 0xE252D228, "hit_visual_effect" },
                { 0xC0EB5412, "hit_visual_effect_size_multiplier" },
                { 0xDDCB9D74, "hit_sound_effect_hash" },
                { 0x8823E502, "unk_0x64" },
                { 0x4F1F46C1, "friendly_damage_flag" },
                { 0xB3419082, "unk_0x6C" },
            };
        }
    }
    */
}
