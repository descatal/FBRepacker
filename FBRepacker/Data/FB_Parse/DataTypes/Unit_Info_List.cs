using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.FB_Parse.DataTypes
{
    class Unit_Info_List
    {
        /*
        public string start_string { get; set; } // First unit will point to "SCharacterList.", second unit onward is 0.0. after first unit.
        public ushort number_of_selectable_units { get; set; } // 0 for non first unit.
        public ushort unk_0x6 { get; set; } 
        */

        public byte unit_index { get; set; } // Might need to increase to short
        public byte series_index { get; set; }
        public ushort unk_0x2 { get; set; } // Always 0xFFFF
        public uint unit_ID { get; set; } // Unit ID
        public string release_string { get; set; } // Always after SCharacterList. , which is Release in Japanese リリース
        public string F_string { get; set; } // F + UnitID in dec. e.g. F1011 for 0x3F3 unit ID.
        public string F_out_string { get; set; } // F + _OUT_ + UnitID in dec. e.g. F_OUT_1011 for 0x3F3 unit ID.
        public string P_string { get; set; } // P + UnitID in dec. e.g. P1011 for 0x3F3 unit ID.
        public byte internal_index { get; set; } // Placement of unit in its series, starts from 00.
        public byte arcade_small_sprite_index { get; set; } // To use the nth image in the arcade small sprite PAC. 
        public byte arcade_unit_name_sprite { get; set; }
        public byte unk_0x1B { get; set; } // Always FF, reserved?
        public uint arcade_selection_sprite_costume_1_hash { get; set; } // Unit and Pilot's Image.
        public uint arcade_selection_sprite_costume_2_hash { get; set; } // Unit and Pilot's Image.
        public uint arcade_selection_sprite_costume_3_hash { get; set; } // Unit and Pilot's Image.
        public uint loading_ally_sprite_costume_1_hash { get; set; } // Unit and Pilot's Image.
        public uint loading_ally_sprite_costume_2_hash { get; set; } // Unit and Pilot's Image.
        public uint loading_ally_sprite_costume_3_hash { get; set; } // Unit and Pilot's Image.
        public uint loading_enemy_sprite_costume_1_hash { get; set; } // Unit and Pilot's Image.
        public uint loading_enemy_sprite_costume_2_hash { get; set; } // Unit and Pilot's Image.
        public uint loading_enemy_sprite_costume_3_hash { get; set; } // Unit and Pilot's Image.
        public uint free_battle_selection_sprite_costume_1_hash { get; set; } // Also applies to FB Mission, Unit and Pilot's Image.
        public uint free_battle_selection_sprite_costume_2_hash { get; set; } // Also applies to FB Mission, Unit and Pilot's Image.
        public uint free_battle_selection_sprite_costume_3_hash { get; set; } // Also applies to FB Mission, Unit and Pilot's Image.
        public uint loading_enemy_target_unit_sprite_costume_1_hash { get; set; } // Target's mecha sprite (enemy)
        public uint loading_enemy_target_pilot_sprite_costume_1_hash { get; set; } // Target's pilot sprite (enemy)
        public uint loading_enemy_target_pilot_sprite_costume_2_hash { get; set; } // Not sure why? This is same as above for unit with 2 costume.
        public uint loading_enemy_target_pilot_sprite_costume_3_hash { get; set; }
        public uint in_game_sortie_and_awakening_sprite_costume_1_hash { get; set; } // Also can be found in EBOOT, not sure which one is dominant.
        public uint in_game_sortie_and_awakening_sprite_costume_2_hash { get; set; } // Also can be found in EBOOT, not sure which one is dominant.
        public uint in_game_sortie_and_awakening_sprite_costume_3_hash { get; set; } // Also can be found in EBOOT, not sure which one is dominant.
        public uint KPKP_hash { get; set; } // Also can be found in EBOOT, not sure which one is dominant.
        public uint result_small_sprite_hash { get; set; } // The small result sprite that can be seen after game concludes. Similiar to arcade small sprite, but seperated for each unit with each PAC.
        public byte unk_0x70 { get; set; } // Not sure what this is, always 0.
        public byte figurine_sprite_index { get; set; } // To use the nth image in the trophy sprite combined PAC, PATCHDC38B066.PAC. 
        public ushort unk_0x72 { get; set; } // Always 0xFFFF.
        public uint figurine_sprite_hash { get; set; } // Unused, these figurine images are seperated.
        public uint target_small_sprite_hash { get; set; } // target small sprite hash
        public uint unk_0x7C { get; set; } // Not sure what this is
        public uint unk_0x80 { get; set; } // Not sure what this is
        public uint catalog_pilot_costume_2_sprite_hash { get; set; } // will be 0 if the unit does not have alt costume.
        public string IS_Costume_T_costume_2_string { get; set; } // Description for the alt costumes
        public string IS_Costume_costume_2_string { get; set; }
        public uint catalog_pilot_costume_3_sprite_hash { get; set; }
        public string IS_Costume_T_costume_3_string { get; set; } // Always the same with start, except for RX-78 Gundam.
        public string IS_Costume_costume_3_string { get; set; }
        public uint unk_0x9C { get; set; } // not sure what this is, sometimes is 10000. Char's Zaku.


        public Unit_Info_List()
        {
            unk_0x2 = 0xFFFF;
            unk_0x1B = 0xFF;

            arcade_selection_sprite_costume_2_hash = 0;
            arcade_selection_sprite_costume_3_hash = 0;
            loading_ally_sprite_costume_2_hash = 0;
            loading_ally_sprite_costume_3_hash = 0;
            loading_enemy_sprite_costume_2_hash = 0;
            loading_enemy_sprite_costume_3_hash = 0;
            free_battle_selection_sprite_costume_2_hash = 0;
            free_battle_selection_sprite_costume_3_hash = 0;
            in_game_sortie_and_awakening_sprite_costume_2_hash = 0;
            in_game_sortie_and_awakening_sprite_costume_3_hash = 0;

            target_small_sprite_hash = 0;
            unk_0x70 = 0;
            unk_0x72 = 0xFFFF;
            unk_0x7C = 0;
            unk_0x80 = 0;
            catalog_pilot_costume_2_sprite_hash = 0;
            catalog_pilot_costume_3_sprite_hash = 0;
            unk_0x9C = 0;
        }
    }
}
