using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    public class Reload
    {
        public enum game_ver
        {
            FB,
            MBON
        }

        public game_ver game_Ver { get; set; }
        public uint unit_ID { get; set; }
        public uint magic_hash { get; set; }
        public List<Reload_FB> reload_FB { get; set; }
        public List<Reload_MBON> reload_MBON { get; set; }

        public Reload()
        {

        }
    }

    public enum ammo_type_enum
    {
        normal = 0,
        special = 1, // Like Alex's Shield or Deathscythe EW's Shield
        timed = 2,
        special_timed = 3 // Like Cherudim's Shield or Nu's Shield
    }

    public enum reload_type_enum
    {
        constant = 0,
        depleted = 1,
        one_time = 2,
        manual = 3, // Unicorn BR
        // this is only found in 3 cases:
        // GP02's CSb Charge, 2263099163
        // Tallgeese III's not used ammo, 3944396171
        // Zakrello's idk what, 3285651340
        special = 4
    }

    [Flags]
    public enum charge_input_enum
    {
        no_charge = 0,
        shoot = 1,
        melee = 2,
        jump = 4
    }

    public class Reload_FB
    {
        public uint hash { get; set; }
        public ammo_type_enum ammo_type { get; set; }
        public uint max_ammo { get; set; }
        public uint initial_ammo { get; set; }
        public uint timed_duration_frame { get; set; }
        public uint unk_0x10 { get; set; }
        public reload_type_enum reload_type { get; set; }
        public uint cooldown_duration_frame { get; set; }
        public uint reload_duration_frame { get; set; }
        public uint assault_burst_reload_duration_frame { get; set; }
        public uint blast_burst_reload_duration_frame { get; set; }
        public uint unk_0x28 { get; set; }
        public uint unk_0x2C { get; set; }
        public uint inactive_unk_0x30 { get; set; }
        public uint inactive_cooldown_duration_frame { get; set; }
        public uint inactive_reload_duration_frame { get; set; }
        public uint inactive_assault_burst_reload_duration_frame { get; set; }
        public uint inactive_blast_burst_reload_duration_frame { get; set; }
        public uint inactive_unk_0x44 { get; set; }
        public uint inactive_unk_0x48 { get; set; }
        public uint burst_replenish { get; set; }
        public uint unk_0x50 { get; set; }
        public uint unk_0x54 { get; set; }
        public uint unk_0x58 { get; set; }
        public charge_input_enum charge_input { get; set; }
        public uint charge_duration_frame { get; set; }
        public uint assault_burst_charge_duration_frame { get; set; }
        public uint blast_burst_charge_duration_frame { get; set; }
        public uint unk_0x6C { get; set; }
        public uint unk_0x70 { get; set; }
        public uint release_charge_duration_frame { get; set; }
        public uint max_charge_level { get; set; }
        public uint unk_0x7C { get; set; }
        public uint unk_0x80 { get; set; }
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public Reload_FB()
        {

        }
    }

    public class Reload_MBON
    {
        public uint hash { get; set; }
        public ammo_type_enum ammo_type { get; set; }
        public uint max_ammo { get; set; }
        public uint initial_ammo { get; set; }
        public uint timed_duration_frame { get; set; }
        public uint unk_0x10 { get; set; }
        public reload_type_enum reload_type { get; set; }
        public uint cooldown_duration_frame { get; set; }
        public uint reload_duration_frame { get; set; }
        public uint burst_reload_duration_frame { get; set; }
        public uint inactive_unk_0x24 { get; set; }
        public uint inactive_cooldown_duration_frame { get; set; }
        public uint inactive_reload_duration_frame { get; set; }
        public uint inactive_burst_reload_duration_frame { get; set; }
        public uint burst_replenish { get; set; }
        public float s_burst_reload_rate_multiplier { get; set; }
        public float unk_0x3C { get; set; }
        public float unk_0x40 { get; set; }
        public float unk_0x44 { get; set; }

        public uint unk_0x48 { get; set; } // FB 0x50
        public uint unk_0x4C { get; set; } // FB 0x54
        public uint unk_0x50 { get; set; } // FB 0x58

        public uint unk_0x54 { get; set; } // not found in FB

        public charge_input_enum charge_input { get; set; }
        public uint charge_duration_frame { get; set; }
        public uint burst_charge_duration_frame { get; set; }
        public uint release_charge_duration_frame { get; set; }
        public float unk_0x68 { get; set; }
        public float unk_0x6C { get; set; }
        public float unk_0x70 { get; set; }
        public float unk_0x74 { get; set; }
        public uint max_charge_level { get; set; }
        public uint unk_0x7C { get; set; }
        public uint unk_0x80 { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public Reload_MBON()
        {

        }
    }
}
