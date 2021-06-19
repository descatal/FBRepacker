using FBRepacker.Data.DataTypes;
using FBRepacker.PAC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.MBON_Parse
{
    class Parse_Reload : Internals
    {
        public Parse_Reload()
        {

        }

        public void parse_Reload()
        {
            if(Properties.Settings.Default.ReloadBinaryInputGameVer == 2)
            {
                parse_Reload_FB();
            }
            else
            {
                parse_Reload_MBON();
            }
        }

        public void parse_Reload_FB()
        {
            FileStream fs = File.OpenRead(Properties.Settings.Default.ReloadBinaryFilePath);

            Reload reload = new Reload();

            List<Reload_FB> reload_FBs = new List<Reload_FB>();

            reload.game_Ver = Reload.game_ver.FB;

            uint magic_hash = readUIntBigEndian(fs);
            uint unk_0x4 = readUIntBigEndian(fs); // Always 0x22 for some reason
            fs.Seek(0x8, SeekOrigin.Current);

            reload.magic_hash = magic_hash;
            reload.unit_ID = 0;

            uint total_ammo_count = readUIntBigEndian(fs); // Always 3 more than normal ammo, with the last 3 constantly the same.

            uint info_pos = (uint)fs.Position + (total_ammo_count * 0x4);

            for (int i = 0; i < total_ammo_count; i++)
            {
                Reload_FB reload_FB = new Reload_FB();

                reload_FB.hash = readUIntBigEndian(fs);

                uint return_pos = (uint)fs.Position;
                fs.Seek(info_pos + (i * 0x84), SeekOrigin.Begin);

                uint ammo_type = readUIntBigEndian(fs);
                bool isAmmoType = Enum.IsDefined(typeof(ammo_type_enum), (int)ammo_type);

                //if (!isAmmoType && i > 50)
                    //throw new Exception("Unidentified Ammo Type!");

                reload_FB.ammo_type = (ammo_type_enum)ammo_type;
                reload_FB.max_ammo = readUIntBigEndian(fs);
                reload_FB.initial_ammo = readUIntBigEndian(fs);
                reload_FB.timed_duration_frame = readUIntBigEndian(fs);
                reload_FB.unk_0x10 = readUIntBigEndian(fs);

                uint reload_type = readUIntBigEndian(fs);
                bool isReloadType = Enum.IsDefined(typeof(reload_type_enum), (int)reload_type);

                //if (!isReloadType && i > 50)
                    //throw new Exception("Unidentified Reload Type!");

                reload_FB.reload_type = (reload_type_enum)reload_type;
                reload_FB.cooldown_duration_frame = readUIntBigEndian(fs);
                reload_FB.reload_duration_frame = readUIntBigEndian(fs);
                reload_FB.assault_burst_reload_duration_frame = readUIntBigEndian(fs);
                reload_FB.blast_burst_reload_duration_frame = readUIntBigEndian(fs);

                uint unk_0x28 = readUIntBigEndian(fs);
                //if (unk_0x28 != reload_FB.assault_burst_reload_duration_frame)
                    //throw new Exception("unk_0x28 not the same as blast burst frame duration!");
                reload_FB.unk_0x28 = unk_0x28;

                uint unk_0x2C = readUIntBigEndian(fs);
                //if (unk_0x2C != reload_FB.blast_burst_reload_duration_frame)
                    //throw new Exception("unk_0x2C not the same as blast burst frame duration!");
                reload_FB.unk_0x2C = unk_0x2C;

                uint unk_0x30 = readUIntBigEndian(fs);
                //if (unk_0x30 != 0 && i > 50)
                    //throw new Exception("unk_0x30 not 0!");
                reload_FB.inactive_unk_0x30 = unk_0x30;

                uint inactive_cooldown_duration_frame = readUIntBigEndian(fs);
                //if (cooldown_duration_frame_copy != reload_FB.cooldown_duration_frame)
                    //throw new Exception("cooldown_duration_frame_copy not same!");
                reload_FB.inactive_cooldown_duration_frame = inactive_cooldown_duration_frame;

                uint inactive_reload_duration_frame = readUIntBigEndian(fs);
                //if (reload_duration_frame_copy != reload_FB.reload_duration_frame && i > 50)
                    //throw new Exception("reload_duration_frame_copy not same!");
                reload_FB.inactive_reload_duration_frame = inactive_reload_duration_frame;

                uint inactive_assault_burst_reload_duration_frame = readUIntBigEndian(fs);
                //if (assault_burst_reload_duration_frame_copy != reload_FB.assault_burst_reload_duration_frame)
                    //throw new Exception("assault_burst_reload_duration_frame_copy not same!");
                reload_FB.inactive_assault_burst_reload_duration_frame = inactive_assault_burst_reload_duration_frame;

                uint inactive_blast_burst_reload_duration_fram = readUIntBigEndian(fs);
                //if (blast_burst_reload_duration_frame_copy != reload_FB.blast_burst_reload_duration_frame)
                    //throw new Exception("blast_burst_reload_duration_frame_copy not same!");
                reload_FB.inactive_blast_burst_reload_duration_frame = inactive_blast_burst_reload_duration_fram;

                uint inactive_unk_0x44 = readUIntBigEndian(fs);
                //if (unk_0x44 != reload_FB.unk_0x28)
                    //throw new Exception("unk_0x44 not same!");
                reload_FB.inactive_unk_0x44 = inactive_unk_0x44;

                uint inactive_unk_0x48 = readUIntBigEndian(fs);
                //if (unk_0x48 != reload_FB.unk_0x2C)
                    //throw new Exception("unk_0x48 not same!");
                reload_FB.inactive_unk_0x48 = inactive_unk_0x48;

                reload_FB.burst_replenish = readUIntBigEndian(fs);
                reload_FB.unk_0x50 = readUIntBigEndian(fs);
                reload_FB.unk_0x54 = readUIntBigEndian(fs);
                reload_FB.unk_0x58 = readUIntBigEndian(fs);

                uint charge_input = readUIntBigEndian(fs);

                //if (charge_input > 0x4)
                    //throw new Exception("Unidentified Charge Type!");

                reload_FB.charge_input = (charge_input_enum)charge_input;
                reload_FB.charge_duration_frame = readUIntBigEndian(fs);
                reload_FB.assault_burst_charge_duration_frame = readUIntBigEndian(fs);
                reload_FB.blast_burst_charge_duration_frame = readUIntBigEndian(fs);

                uint unk_0x6C = readUIntBigEndian(fs);
                //if (unk_0x6C != reload_FB.blast_burst_charge_duration_frame)
                    //throw new Exception("unk_0x6C not same!");
                reload_FB.unk_0x6C = unk_0x6C;

                uint unk_0x70 = readUIntBigEndian(fs);
                //if (unk_0x70 != reload_FB.blast_burst_charge_duration_frame)
                    //throw new Exception("unk_0x70 not same!");
                reload_FB.unk_0x70 = unk_0x70;

                reload_FB.release_charge_duration_frame = readUIntBigEndian(fs);
                reload_FB.max_charge_level = readUIntBigEndian(fs);
                reload_FB.unk_0x7C = readUIntBigEndian(fs);
                reload_FB.unk_0x80 = readUIntBigEndian(fs);

                reload_FBs.Add(reload_FB);

                fs.Seek(return_pos, SeekOrigin.Begin);
            }

            reload.reload_FB = reload_FBs;

            // Save JSON
            string JSON = JsonConvert.SerializeObject(reload, Formatting.Indented);

            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ReloadBinaryFilePath);
            string outputPath = Properties.Settings.Default.outputReloadJSONFolderPath + @"\" + fileName + @"_Reload.JSON";

            StreamWriter fsJSON = File.CreateText(outputPath);
            fsJSON.Write(JSON);
            fsJSON.Close();
        }

        public void parse_Reload_MBON()
        {
            FileStream fs = File.OpenRead(Properties.Settings.Default.ReloadBinaryFilePath);

            List<uint> hash_list = new List<uint>();
            Reload reload = new Reload();

            List<Reload_MBON> reload_MBONs = new List<Reload_MBON>();

            reload.game_Ver = Reload.game_ver.MBON;

            uint magic_hash = readUIntBigEndian(fs);
            uint unk_0x4 = readUIntBigEndian(fs); // Always 0x22 for some reason
            uint unit_ID = readUIntBigEndian(fs);

            reload.magic_hash = magic_hash;
            reload.unit_ID = unit_ID;

            uint total_ammo_count = readUIntBigEndian(fs); // Always 3 more than normal ammo, with the last 3 constantly the same.
            uint real_ammo_count = readUIntBigEndian(fs); // The real ammo count that we care.

            if (real_ammo_count > 16)
                throw new Exception("real ammo count > 16!");

            for(int i = 0; i < 3; i++)
            {
                uint is0 = readUIntBigEndian(fs);
                if (is0 != 0)
                    throw new Exception("Not 0!");
            }

            uint hash_pos = (uint)fs.Position;

            for(int i = 0; i < real_ammo_count; i++)
            {
                uint hash = readUIntBigEndian(fs);
                hash_list.Add(hash);
            }

            fs.Seek(hash_pos + 0x40, SeekOrigin.Begin);
            uint info_pos = (uint)fs.Position + (total_ammo_count * 0x4);

            for(int i = 0; i < total_ammo_count; i++)
            {
                Reload_MBON reload_MBON = new Reload_MBON();

                reload_MBON.hash = readUIntBigEndian(fs);

                uint return_pos = (uint)fs.Position;
                fs.Seek(info_pos + (i * 0x84), SeekOrigin.Begin);

                uint ammo_type = readUIntBigEndian(fs);
                bool isAmmoType = Enum.IsDefined(typeof(ammo_type_enum), (int)ammo_type);

                if (!isAmmoType && (i < total_ammo_count - 3))
                    throw new Exception("Unidentified Ammo Type!");

                reload_MBON.ammo_type = (ammo_type_enum)ammo_type;
                reload_MBON.max_ammo = readUIntBigEndian(fs);
                reload_MBON.initial_ammo = readUIntBigEndian(fs);
                reload_MBON.timed_duration_frame = readUIntBigEndian(fs);
                reload_MBON.unk_0x10 = readUIntBigEndian(fs);

                uint reload_type = readUIntBigEndian(fs);
                bool isReloadType = Enum.IsDefined(typeof(reload_type_enum), (int)reload_type);

                if (!isReloadType && (i < total_ammo_count - 3))
                    throw new Exception("Unidentified Reload Type!");

                reload_MBON.reload_type = (reload_type_enum)reload_type;
                reload_MBON.cooldown_duration_frame = readUIntBigEndian(fs);
                reload_MBON.reload_duration_frame = readUIntBigEndian(fs);
                reload_MBON.burst_reload_duration_frame = readUIntBigEndian(fs);
                reload_MBON.inactive_unk_0x24 = readUIntBigEndian(fs);
                reload_MBON.inactive_cooldown_duration_frame = readUIntBigEndian(fs);
                reload_MBON.inactive_reload_duration_frame = readUIntBigEndian(fs);
                reload_MBON.inactive_burst_reload_duration_frame = readUIntBigEndian(fs);

                reload_MBON.burst_replenish = readUIntBigEndian(fs);

                reload_MBON.unk_0x38 = readFloat(fs, true);

                //if (reload_MBON.unk_0x38 != 3 && (i < total_ammo_count - 3))
                    //throw new Exception("0x38 not 3!");

                reload_MBON.unk_0x3C = readFloat(fs, true);

                if (reload_MBON.unk_0x3C != 1 && (i < total_ammo_count - 3))
                    throw new Exception("0x3C not 1!");

                reload_MBON.unk_0x40 = readFloat(fs, true);

                //if (reload_MBON.unk_0x40 != 1 && (i < total_ammo_count - 3))
                    //throw new Exception("0x40 not 1!");

                reload_MBON.unk_0x44 = readFloat(fs, true);

                if (reload_MBON.unk_0x44 != 1 && (i < total_ammo_count - 3))
                    throw new Exception("0x44 not 1!");

                reload_MBON.unk_0x48 = readUIntBigEndian(fs);
                reload_MBON.unk_0x4C = readUIntBigEndian(fs);
                reload_MBON.unk_0x50 = readUIntBigEndian(fs);
                reload_MBON.unk_0x54 = readUIntBigEndian(fs);

                uint charge_input = readUIntBigEndian(fs);
                bool isChargeInput = Enum.IsDefined(typeof(charge_input_enum), (int)charge_input);

                if (!isChargeInput && (i < total_ammo_count - 3))
                    throw new Exception("Unidentified Charge Type!");

                reload_MBON.charge_input = (charge_input_enum)charge_input;
                reload_MBON.charge_duration_frame = readUIntBigEndian(fs);
                reload_MBON.burst_charge_duration_frame = readUIntBigEndian(fs);
                reload_MBON.release_charge_duration_frame = readUIntBigEndian(fs);

                reload_MBON.unk_0x68 = readFloat(fs, true);
                if ((reload_MBON.unk_0x68 != 2 && reload_MBON.unk_0x68 != 0) && (i < total_ammo_count - 3))
                    throw new Exception("Unidentified 0x68");

                reload_MBON.unk_0x6C = readFloat(fs, true);
                if ((reload_MBON.unk_0x6C != 1 && reload_MBON.unk_0x6C != 0) && (i < total_ammo_count - 3))
                    throw new Exception("Unidentified 0x6C");

                reload_MBON.unk_0x70 = readFloat(fs, true);
                if ((reload_MBON.unk_0x70 != 2 && reload_MBON.unk_0x70 != 0) && (i < total_ammo_count - 3))
                    throw new Exception("Unidentified 0x70");

                reload_MBON.unk_0x74 = readFloat(fs, true);
                if ((reload_MBON.unk_0x74 != 1 && reload_MBON.unk_0x74 != 0) && (i < total_ammo_count - 3))
                    throw new Exception("Unidentified 0x74");

                reload_MBON.max_charge_level = readUIntBigEndian(fs);

                reload_MBON.unk_0x7C = readUIntBigEndian(fs);
                reload_MBON.unk_0x80 = readUIntBigEndian(fs);

                reload_MBONs.Add(reload_MBON);

                fs.Seek(return_pos, SeekOrigin.Begin);
            }

            reload.reload_MBON = reload_MBONs;

            if (Properties.Settings.Default.convertMBONReload)
                reload = convertMBONtoFBReload(reload);

            // Save JSON
            string JSON = JsonConvert.SerializeObject(reload, Formatting.Indented);

            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ReloadBinaryFilePath);
            string outputPath = Properties.Settings.Default.outputReloadJSONFolderPath + @"\" + fileName + @"_Reload.JSON";

            StreamWriter fsJSON = File.CreateText(outputPath);
            fsJSON.Write(JSON);
            fsJSON.Close();
        }

        public Reload convertMBONtoFBReload(Reload reload)
        {
            if (reload.game_Ver == Reload.game_ver.FB)
                throw new Exception("This reload JSON is already in FB format!");

            List<Reload_FB> reload_FBs = new List<Reload_FB>();
            List<Reload_MBON> reload_MBONs = reload.reload_MBON;

            for(int i = 0; i < reload_MBONs.Count(); i++)
            {
                Reload_MBON reload_MBON = reload_MBONs[i];
                Reload_FB reload_FB = new Reload_FB();

                reload_FB.hash = reload_MBON.hash;
                reload_FB.ammo_type = reload_MBON.ammo_type;
                reload_FB.max_ammo = reload_MBON.max_ammo;
                reload_FB.initial_ammo = reload_MBON.initial_ammo;
                reload_FB.timed_duration_frame = reload_MBON.timed_duration_frame;
                reload_FB.unk_0x10 = reload_MBON.unk_0x10;

                reload_FB.reload_type = reload_MBON.reload_type;
                reload_FB.cooldown_duration_frame = reload_MBON.cooldown_duration_frame;
                reload_FB.reload_duration_frame = reload_MBON.reload_duration_frame;
                reload_FB.assault_burst_reload_duration_frame = reload_MBON.burst_reload_duration_frame;

                uint ori_burst_reload_duration_frame = reload_MBON.burst_reload_duration_frame;

                // S burst is usually burst duration frame / 3.
                uint S_burst_reload_duration_frame = (ori_burst_reload_duration_frame / 3);
                // Assign the / 3 value to blast burst reload.
                reload_FB.blast_burst_reload_duration_frame = S_burst_reload_duration_frame;

                reload_FB.unk_0x28 = S_burst_reload_duration_frame;
                reload_FB.unk_0x2C = S_burst_reload_duration_frame;

                reload_FB.inactive_unk_0x30 = reload_MBON.inactive_unk_0x24;

                reload_FB.inactive_cooldown_duration_frame = reload_MBON.inactive_cooldown_duration_frame;
                reload_FB.inactive_reload_duration_frame = reload_MBON.inactive_reload_duration_frame;
                reload_FB.inactive_assault_burst_reload_duration_frame = reload_MBON.inactive_burst_reload_duration_frame;
                reload_FB.inactive_blast_burst_reload_duration_frame = S_burst_reload_duration_frame;
                reload_FB.inactive_unk_0x44 = S_burst_reload_duration_frame;
                reload_FB.inactive_unk_0x48 = S_burst_reload_duration_frame;

                reload_FB.burst_replenish = reload_MBON.burst_replenish;
                reload_FB.unk_0x50 = reload_MBON.unk_0x48;
                reload_FB.unk_0x54 = reload_MBON.unk_0x4C;
                reload_FB.unk_0x58 = reload_MBON.unk_0x50;

                reload_FB.charge_input = reload_MBON.charge_input;
                reload_FB.charge_duration_frame = reload_MBON.charge_duration_frame;
                reload_FB.assault_burst_charge_duration_frame = reload_MBON.burst_charge_duration_frame;

                uint ori_burst_charge_duration_frame = reload_MBON.burst_charge_duration_frame;

                // S burst is usually burst_charge_duration_frame / 2.
                uint S_burst_charge_duration_frame = (ori_burst_charge_duration_frame / 2);
                // Assign the / 2 value to blast burst reload.
                reload_FB.blast_burst_charge_duration_frame = S_burst_charge_duration_frame;

                reload_FB.unk_0x6C = S_burst_charge_duration_frame;
                reload_FB.unk_0x70 = S_burst_charge_duration_frame;

                reload_FB.release_charge_duration_frame = reload_MBON.release_charge_duration_frame;
                reload_FB.max_charge_level = reload_MBON.max_charge_level;

                reload_FB.unk_0x7C = reload_MBON.unk_0x7C;
                reload_FB.unk_0x80 = reload_MBON.unk_0x80;

                reload_FBs.Add(reload_FB);
            }

            reload.game_Ver = Reload.game_ver.FB;
            reload.reload_MBON = null;
            reload.reload_FB = reload_FBs;

            return reload;
        }

        public void writeReloadBinary(Reload reload)
        {
            /*
            StreamReader fs = File.OpenText(Properties.Settings.Default.ReloadJSONFilePath);
            string JSON = fs.ReadToEnd();

            Reload reload = JsonConvert.DeserializeObject<Reload>(JSON);
            */

            if (reload.game_Ver == Reload.game_ver.FB)
            {
                writeReloadBinary_FB(reload);
            }
            else
            {
                throw new Exception("Rewriting MBON Binary not supported yet!");
                //writeReloadBinary_MBON(reload);
            }
        }

        public void writeReloadBinary_FB(Reload reload)
        {
            if (reload.game_Ver != Reload.game_ver.FB)
                throw new Exception("This reload JSON is not in FB format!");

            if (reload.reload_FB == null)
                throw new Exception("This reload JSON is not in FB format!");

            List<Reload_FB> reload_FBs = reload.reload_FB;

            MemoryStream reloadBin = new MemoryStream();
            MemoryStream hashList = new MemoryStream();
            MemoryStream info = new MemoryStream();

            appendUIntMemoryStream(reloadBin, reload.magic_hash, true);
            appendUIntMemoryStream(reloadBin, 0x22, true);
            appendZeroMemoryStream(reloadBin, 8);

            appendUIntMemoryStream(reloadBin, (uint)reload_FBs.Count(), true);
            for(int i = 0; i < reload_FBs.Count(); i++)
            {
                Reload_FB reload_FB = reload_FBs[i];
                appendUIntMemoryStream(hashList, reload_FB.hash, true);

                appendUIntMemoryStream(info, (uint)reload_FB.ammo_type, true);
                appendUIntMemoryStream(info, reload_FB.max_ammo, true);
                appendUIntMemoryStream(info, reload_FB.initial_ammo, true);
                appendUIntMemoryStream(info, reload_FB.timed_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.unk_0x10, true);
                appendUIntMemoryStream(info, (uint)reload_FB.reload_type, true);
                appendUIntMemoryStream(info, reload_FB.cooldown_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.reload_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.assault_burst_reload_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.blast_burst_reload_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.unk_0x28, true);
                appendUIntMemoryStream(info, reload_FB.unk_0x2C, true);
                appendUIntMemoryStream(info, reload_FB.inactive_unk_0x30, true);
                appendUIntMemoryStream(info, reload_FB.inactive_cooldown_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.inactive_reload_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.inactive_assault_burst_reload_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.inactive_blast_burst_reload_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.inactive_unk_0x44, true);
                appendUIntMemoryStream(info, reload_FB.inactive_unk_0x48, true);
                appendUIntMemoryStream(info, reload_FB.burst_replenish, true);
                appendUIntMemoryStream(info, reload_FB.unk_0x50, true);
                appendUIntMemoryStream(info, reload_FB.unk_0x54, true);
                appendUIntMemoryStream(info, reload_FB.unk_0x58, true);
                appendUIntMemoryStream(info, (uint)reload_FB.charge_input, true);
                appendUIntMemoryStream(info, reload_FB.charge_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.assault_burst_charge_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.blast_burst_charge_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.unk_0x6C, true);
                appendUIntMemoryStream(info, reload_FB.unk_0x70, true);
                appendUIntMemoryStream(info, reload_FB.release_charge_duration_frame, true);
                appendUIntMemoryStream(info, reload_FB.max_charge_level, true);
                appendUIntMemoryStream(info, reload_FB.unk_0x7C, true);
                appendUIntMemoryStream(info, reload_FB.unk_0x80, true);
            }

            hashList.Seek(0, SeekOrigin.Begin);
            info.Seek(0, SeekOrigin.Begin);

            hashList.CopyTo(reloadBin);
            info.CopyTo(reloadBin);

            string oPath = Properties.Settings.Default.outputReloadBinFolderPath + @"\" + Path.GetFileNameWithoutExtension(Properties.Settings.Default.ReloadJSONFilePath) + ".bin";

            FileStream ofs = File.Create(oPath);
            reloadBin.Seek(0, SeekOrigin.Begin);
            reloadBin.CopyTo(ofs);

            ofs.Close();
        }

        public void writeReloadBinary_MBON(Reload reload)
        {

        }

        public Reload parseReloadJSON()
        {
            StreamReader fs = File.OpenText(Properties.Settings.Default.ReloadJSONFilePath);
            string JSON = fs.ReadToEnd();
            Reload reload = JsonConvert.DeserializeObject<Reload>(JSON);

            return reload;
        }
    }
}
