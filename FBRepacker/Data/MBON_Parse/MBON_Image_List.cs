using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.MBON_Parse
{
    class MBON_Image_List : Internals
    {
        public MBON_Image_List()
        {
            //@"G:\Games\PS4\MBON\All Pilot Image List.bin";
            //@"G:\Games\PS4\MBON\All Playable Unit Image List.bin";
            //@"G:\Games\PS4\MBON\All Boss Unit Image List.bin";
            //@"G:\Games\PS4\MBON\All Local Sound List.bin";
            string MBONImageListPath = @"G:\Games\PS4\MBON\All Pilot Image List.bin";
            read_Pilot_Images(MBONImageListPath);
            //read_Playable_Unit_Images(MBONImageListPath);
            //read_Boss_Unit_Image_List(MBONImageListPath);
            //read_Local_Sound_List(MBONImageListPath);
        }

        private void read_Pilot_Images(string path)
        {
            FileStream fs = File.OpenRead(path);
            changeStreamFile(fs);

            string[] MBONDir = Directory.GetFileSystemEntries(@"G:\Games\PS4\MBON\Extracted\MBON\Image0\archives", "*", SearchOption.AllDirectories);
            string newDir = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Input\MBON\v2\All Pilot Image";

            for (int i = 0; i < 220; i++)
            {
                Stream.Seek(0xC, SeekOrigin.Current);
                string AwakenCostume2 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string RightPilotCostume3 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x8, SeekOrigin.Current);
                string PilotEyeCostume3 = readUIntSmallEndian().ToString("X8");
                string PilotEyeCostume2 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string SortieCostume1 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string RightPilotCostume2 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x8, SeekOrigin.Current);
                string LeftPilotCostume1 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string AwakenCostume3 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x20, SeekOrigin.Current);
                string SortieCostume2 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string PilotEyeCostume1 = readUIntSmallEndian().ToString("X8");
                string GalleryCostume2 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string GalleryCostume1 = readUIntSmallEndian().ToString("X8");
                string LeftPilotCostume3 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string LeftPilotCostume2 = readUIntSmallEndian().ToString("X8");
                string RightPilotCostume1 = readUIntSmallEndian().ToString("X8");
                string GalleryCostume3 = readUIntSmallEndian().ToString("X8");
                string SortieCostume3 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x10, SeekOrigin.Current);
                string unitID = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x8, SeekOrigin.Current);
                string AwakenCostume1 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x4, SeekOrigin.Current);

                string AwakenCostume2_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(AwakenCostume2));
                string RightPilotCostume3_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(RightPilotCostume3));
                string PilotEyeCostume3_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(PilotEyeCostume3));
                string PilotEyeCostume2_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(PilotEyeCostume2));
                string SortieCostume1_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(SortieCostume1));
                string RightPilotCostume2_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(RightPilotCostume2));
                string LeftPilotCostume1_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(LeftPilotCostume1));
                string AwakenCostume3_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(AwakenCostume3));
                string SortieCostume2_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(SortieCostume2));
                string PilotEyeCostume1_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(PilotEyeCostume1));
                string GalleryCostume2_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(GalleryCostume2));
                string GalleryCostume1_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(GalleryCostume1));
                string LeftPilotCostume3_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(LeftPilotCostume3));
                string LeftPilotCostume2_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(LeftPilotCostume2));
                string RightPilotCostume1_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(RightPilotCostume1));
                string GalleryCostume3_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(GalleryCostume3));
                string SortieCostume3_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(SortieCostume3));
                string AwakenCostume1_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(AwakenCostume1));

                string dir = newDir + @"\" + (i + 1) + @"\";
                Directory.CreateDirectory(dir);

                if (AwakenCostume2_Path != null)
                    File.Copy(AwakenCostume2_Path, dir + "Awakening Cut In Costume 2 Sprite - " + Path.GetFileName(AwakenCostume2) + ".bin", true);
                if (RightPilotCostume3_Path != null)
                    File.Copy(RightPilotCostume3_Path, dir + "Right Pilot Costume 3 Sprite - " + Path.GetFileName(RightPilotCostume3) + ".bin", true);
                if (PilotEyeCostume3_Path != null)
                    File.Copy(PilotEyeCostume3_Path, dir + "Pilot Eye Costume 3 Sprite - " + Path.GetFileName(PilotEyeCostume3) + ".bin", true);
                if (PilotEyeCostume2_Path != null)
                    File.Copy(PilotEyeCostume2_Path, dir + "Pilot Eye Costume 2 Sprite - " + Path.GetFileName(PilotEyeCostume2) + ".bin", true);
                if (SortieCostume1_Path != null)
                    File.Copy(SortieCostume1_Path, dir + "Sortie Cut In Costume 1 Sprite - " + Path.GetFileName(SortieCostume1) + ".bin", true);
                if (RightPilotCostume2_Path != null)
                    File.Copy(RightPilotCostume2_Path, dir + "Right Pilot Costume 2 Sprite - " + Path.GetFileName(RightPilotCostume2) + ".bin", true);
                if (LeftPilotCostume1_Path != null)
                    File.Copy(LeftPilotCostume1_Path, dir + "Left Pilot Costume 1 Sprite - " + Path.GetFileName(LeftPilotCostume1) + ".bin", true);
                if (AwakenCostume3_Path != null)
                    File.Copy(AwakenCostume3_Path, dir + "Awakening Cut In Costume 3 Sprite - " + Path.GetFileName(AwakenCostume3) + ".bin", true);
                if (SortieCostume2_Path != null)
                    File.Copy(SortieCostume2_Path, dir + "Sortie Cut In Costume 2 Sprite - " + Path.GetFileName(SortieCostume2) + ".bin", true);
                if (PilotEyeCostume1_Path != null)
                    File.Copy(PilotEyeCostume1_Path, dir + "Pilot Eye Costume 1 Sprite - " + Path.GetFileName(PilotEyeCostume1) + ".bin", true);
                if (GalleryCostume2_Path != null)
                    File.Copy(GalleryCostume2_Path, dir + "Gallery Costume 2 Sprite - " + Path.GetFileName(GalleryCostume2) + ".bin", true);
                if (GalleryCostume1_Path != null)
                    File.Copy(GalleryCostume1_Path, dir + "Gallery Costume 1 Sprite - " + Path.GetFileName(GalleryCostume1) + ".bin", true);
                if (LeftPilotCostume3_Path != null)
                    File.Copy(LeftPilotCostume3_Path, dir + "Left Pilot Costume 3 Sprite - " + Path.GetFileName(LeftPilotCostume3) + ".bin", true);
                if (LeftPilotCostume2_Path != null)
                    File.Copy(LeftPilotCostume2_Path, dir + "Left Pilot Costume 2  Sprite - " + Path.GetFileName(LeftPilotCostume2) + ".bin", true);
                if (RightPilotCostume1_Path != null)
                    File.Copy(RightPilotCostume1_Path, dir + "Right Pilot Costume 1 Sprite - " + Path.GetFileName(RightPilotCostume1) + ".bin", true);
                if (GalleryCostume3_Path != null)
                    File.Copy(GalleryCostume3_Path, dir + "Gallery Costume 3 Sprite - " + Path.GetFileName(GalleryCostume3) + ".bin", true);
                if (SortieCostume3_Path != null)
                    File.Copy(SortieCostume3_Path, dir + "Sortie Cut In Costume 3 Sprite - " + Path.GetFileName(SortieCostume3) + ".bin", true);
                if (AwakenCostume1_Path != null)
                    File.Copy(AwakenCostume1_Path, dir + "Awakening Cut In Costume 1 Sprite - " + Path.GetFileName(AwakenCostume1) + ".bin", true);
            }

            /*
            StringBuilder B4ACScript = new StringBuilder();

            B4ACScript.AppendLine("All Images file name Hashes: ");

            for (int i = 0; i < 220; i++)
            {
                B4ACScript.AppendLine("-----------------------------------------");
                B4ACScript.AppendLine("Unit: " + (i + 1).ToString());
                Stream.Seek(0xC, SeekOrigin.Current);
                string AwakenCostume2 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string RightPilotCostume3 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x8, SeekOrigin.Current);
                string PilotEyeCostume3 = readUIntSmallEndian().ToString("X8");
                string PilotEyeCostume2 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string SortieCostume1 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string RightPilotCostume2 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x8, SeekOrigin.Current);
                string LeftPilotCostume1 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string AwakenCostume3 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x20, SeekOrigin.Current);
                string SortieCostume2 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string PilotEyeCostume1 = readUIntSmallEndian().ToString("X8");
                string GalleryCostume2 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string GalleryCostume1 = readUIntSmallEndian().ToString("X8");
                string LeftPilotCostume3 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string LeftPilotCostume2 = readUIntSmallEndian().ToString("X8");
                string RightPilotCostume1 = readUIntSmallEndian().ToString("X8");
                string GalleryCostume3 = readUIntSmallEndian().ToString("X8");
                string SortieCostume3 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x10, SeekOrigin.Current);
                string unitID = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x8, SeekOrigin.Current);
                string AwakenCostume1 = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x4, SeekOrigin.Current);

                B4ACScript.AppendLine("Unit ID: " + unitID);
                B4ACScript.AppendLine("Awakening Cut In Sprite Costume 1: " + AwakenCostume1);
                B4ACScript.AppendLine("Awakening Cut In Sprite Costume 2: " + AwakenCostume2);
                B4ACScript.AppendLine("Awakening Cut In Sprite Costume 3: " + AwakenCostume3);
                B4ACScript.AppendLine("Gallery Sprite Costume 1: " + GalleryCostume1);
                B4ACScript.AppendLine("Gallery Sprite Costume 2: " + GalleryCostume2);
                B4ACScript.AppendLine("Gallery Sprite Costume 3: " + GalleryCostume3);
                B4ACScript.AppendLine("Left Pilot Sprite Costume 1: " + LeftPilotCostume1);
                B4ACScript.AppendLine("Left Pilot Sprite Costume 2: " + LeftPilotCostume2);
                B4ACScript.AppendLine("Left Pilot Sprite Costume 3: " + LeftPilotCostume3);
                B4ACScript.AppendLine("Right Pilot Sprite Costume 1: " + RightPilotCostume1);
                B4ACScript.AppendLine("Right Pilot Sprite Costume 2: " + RightPilotCostume2);
                B4ACScript.AppendLine("Right Pilot Sprite Costume 3: " + RightPilotCostume3);
                B4ACScript.AppendLine("Pilot Eye Sprite Costume 1: " + PilotEyeCostume1);
                B4ACScript.AppendLine("Pilot Eye Sprite Costume 2: " + PilotEyeCostume2);
                B4ACScript.AppendLine("Pilot Eye Sprite Costume 3: " + PilotEyeCostume3);
                B4ACScript.AppendLine("Sortie Cut In Sprite Costume 1: " + SortieCostume1);
                B4ACScript.AppendLine("Sortie Cut In Sprite Costume 2: " + SortieCostume2);
                B4ACScript.AppendLine("Sortie Cut In Sprite Costume 3: " + SortieCostume3);
                B4ACScript.AppendLine(Environment.NewLine);
            }

            StreamWriter txt = File.CreateText(Path.GetDirectoryName(path) + @"\Image_List.txt");
            txt.Write(B4ACScript);

            txt.Close();
            */
            fs.Close();
        }

        private void read_Playable_Unit_Images(string path)
        {
            FileStream fs = File.OpenRead(path);
            changeStreamFile(fs);

            string[] MBONDir = Directory.GetFileSystemEntries(@"G:\Games\PS4\MBON\Extracted\MBON\Image0\archives", "*", SearchOption.AllDirectories);
            string newDir = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Input\MBON\v2\All Playable Unit Image";

            List<uint> unitIDs = new List<uint>();
            for (int i = 0; i < 186; i++)
            {
                uint unitID = readUIntSmallEndian();
                unitIDs.Add(unitID);
            }

            for (int i = 0; i < 186; i++)
            {
                Stream.Seek(0xC, SeekOrigin.Current);
                string Small_Intermission_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x40, SeekOrigin.Current);
                string Arcade_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x24, SeekOrigin.Current);
                string Left_Sortie_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string Trophy_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x40, SeekOrigin.Current);
                string Sound_Effect = readUIntSmallEndian().ToString("X8");
                string Small_Right_Sortie_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x1C, SeekOrigin.Current);
                string Right_Sortie_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x18, SeekOrigin.Current);
                string Select_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x68, SeekOrigin.Current);

                string Small_Intermission_Sprite_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Small_Intermission_Sprite));
                string Arcade_Sprite_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Arcade_Sprite));
                string Left_Sortie_Sprite_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Left_Sortie_Sprite));
                string Trophy_Sprite_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Trophy_Sprite));
                string Sound_Effect_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Sound_Effect));
                string Small_Right_Sortie_Sprite_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Small_Right_Sortie_Sprite));
                string Right_Sortie_Sprite_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Right_Sortie_Sprite));
                string Select_Sprite_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Select_Sprite));

                string dir = newDir + @"\" + (i + 1) + @"\";
                Directory.CreateDirectory(dir);

                if (Small_Intermission_Sprite_Path != null)
                    File.Copy(Small_Intermission_Sprite_Path, dir + "Small Intermission Sprite - " + Path.GetFileName(Small_Intermission_Sprite) + ".bin", true);
                if (Arcade_Sprite_Path != null)
                    File.Copy(Arcade_Sprite_Path, dir + "Arcade Sprite - " + Path.GetFileName(Arcade_Sprite) + ".bin", true);
                if (Left_Sortie_Sprite_Path != null)
                    File.Copy(Left_Sortie_Sprite_Path, dir + "Left Sortie Sprite - " + Path.GetFileName(Left_Sortie_Sprite) + ".bin", true);
                if (Trophy_Sprite_Path != null)
                    File.Copy(Trophy_Sprite_Path, dir + "Trophy Sprite - " + Path.GetFileName(Trophy_Sprite) + ".bin", true);
                if (Sound_Effect_Path != null)
                    File.Copy(Sound_Effect_Path, dir + "Sound Effect - " + Path.GetFileName(Sound_Effect) + ".bin", true);
                if (Small_Right_Sortie_Sprite_Path != null)
                    File.Copy(Small_Right_Sortie_Sprite_Path, dir + "Small Right Sortie Sprite - " + Path.GetFileName(Small_Right_Sortie_Sprite) + ".bin", true);
                if (Right_Sortie_Sprite_Path != null)
                    File.Copy(Right_Sortie_Sprite_Path, dir + "Right Sortie Sprite - " + Path.GetFileName(Right_Sortie_Sprite) + ".bin", true);
                if (Select_Sprite_Path != null)
                    File.Copy(Select_Sprite_Path, dir + "Select Sprite - " + Path.GetFileName(Select_Sprite) + ".bin", true);
            }

            /*
            StringBuilder B4ACScript = new StringBuilder();

            B4ACScript.AppendLine("All Images file name Hashes: ");
            List<uint> unitIDs = new List<uint>();

            for (int i = 0; i < 186; i++)
            {
                uint unitID = readUIntSmallEndian();
                unitIDs.Add(unitID);
            }

            for (int i = 0; i < 186; i++)
            {
                B4ACScript.AppendLine("-----------------------------------------");
                B4ACScript.AppendLine("Unit: " + (i + 1).ToString());

                B4ACScript.AppendLine("Unit ID: " + unitIDs[i].ToString("X8"));
                Stream.Seek(0xC, SeekOrigin.Current);
                string Small_Intermission_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x40, SeekOrigin.Current);
                string Arcade_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x24, SeekOrigin.Current);
                string Left_Sortie_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string Trophy_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x40, SeekOrigin.Current);
                string Sound_Effect = readUIntSmallEndian().ToString("X8");
                string Small_Right_Sortie_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x1C, SeekOrigin.Current);
                string Right_Sortie_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x18, SeekOrigin.Current);
                string Select_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x68, SeekOrigin.Current);

                B4ACScript.AppendLine("Small Intermission Sprite: " + Small_Intermission_Sprite);
                B4ACScript.AppendLine("Arcade Sprite: " + Arcade_Sprite);
                B4ACScript.AppendLine("Left Sortie Sprite: " + Left_Sortie_Sprite);
                B4ACScript.AppendLine("Right Sortie Sprite: " + Right_Sortie_Sprite);
                B4ACScript.AppendLine("Trophy Sprite: " + Trophy_Sprite);
                B4ACScript.AppendLine("Sound Effect: " + Sound_Effect);
                B4ACScript.AppendLine("Select Sprite: " + Select_Sprite);
                B4ACScript.AppendLine("Small Right Sortie Sprite: " + Small_Right_Sortie_Sprite);
            }

            StreamWriter txt = File.CreateText(Path.GetDirectoryName(path) + @"\Playable_Unit_Image_List.txt");
            txt.Write(B4ACScript);

            txt.Close();
            */
            fs.Close();
        }

        private void read_Boss_Unit_Image_List(string path)
        {
            FileStream fs = File.OpenRead(path);
            changeStreamFile(fs);

            string[] MBONDir = Directory.GetFileSystemEntries(@"G:\Games\PS4\MBON\Extracted\MBON\Image0\archives", "*", SearchOption.AllDirectories);
            string newDir = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Input\MBON\v2\All Boss Unit Image";

            List<uint> unitIDs = new List<uint>();
            for (int i = 0; i < 36; i++)
            {
                uint unitID = readUIntSmallEndian();
                unitIDs.Add(unitID);
            }

            for (int i = 0; i < 36; i++)
            {
                string Small_Intermission_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x28, SeekOrigin.Current);
                string Trophy_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string Sound_Effect = readUIntSmallEndian().ToString("X8");
                string Small_Right_Sortie_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x8, SeekOrigin.Current);
                string Right_Sortie_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x30, SeekOrigin.Current);

                string Small_Intermission_Sprite_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Small_Intermission_Sprite));
                string Trophy_Sprite_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Trophy_Sprite));
                string Sound_Effect_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Sound_Effect));
                string Small_Right_Sortie_Sprite_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Small_Right_Sortie_Sprite));
                string Right_Sortie_Sprite_Path = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(Right_Sortie_Sprite));

                string dir = newDir + @"\" + (i + 1) + @"\";
                Directory.CreateDirectory(dir);

                if (Small_Intermission_Sprite_Path != null)
                    File.Copy(Small_Intermission_Sprite_Path, dir + "Small Intermission Sprite - " + Path.GetFileName(Small_Intermission_Sprite) + ".bin", true);
                if (Trophy_Sprite_Path != null)
                    File.Copy(Trophy_Sprite_Path, dir + "Trophy Sprite - " + Path.GetFileName(Trophy_Sprite) + ".bin", true);
                if (Sound_Effect_Path != null)
                    File.Copy(Sound_Effect_Path, dir + "Sound Effect - " + Path.GetFileName(Sound_Effect) + ".bin", true);
                if (Small_Right_Sortie_Sprite_Path != null)
                    File.Copy(Small_Right_Sortie_Sprite_Path, dir + "Small Right Sortie Sprite - " + Path.GetFileName(Small_Right_Sortie_Sprite) + ".bin", true);
                if (Right_Sortie_Sprite_Path != null)
                    File.Copy(Right_Sortie_Sprite_Path, dir + "Right Sortie Sprite - " + Path.GetFileName(Right_Sortie_Sprite) + ".bin", true);
            }

            /*
            StringBuilder B4ACScript = new StringBuilder();

            B4ACScript.AppendLine("All Boss Images file name Hashes: ");
            List<uint> unitIDs = new List<uint>();

            for (int i = 0; i < 36; i++)
            {
                uint unitID = readUIntSmallEndian();
                unitIDs.Add(unitID);
            }

            for (int i = 0; i < 36; i++)
            {
                B4ACScript.AppendLine("-----------------------------------------");
                B4ACScript.AppendLine("Unit: " + (i + 1).ToString());

                B4ACScript.AppendLine("Unit ID: " + unitIDs[i].ToString("X8"));
                string Small_Intermission_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x28, SeekOrigin.Current);
                string Trophy_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0xC, SeekOrigin.Current);
                string Sound_Effect = readUIntSmallEndian().ToString("X8");
                string Small_Right_Sortie_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x8, SeekOrigin.Current);
                string Right_Sortie_Sprite = readUIntSmallEndian().ToString("X8");
                Stream.Seek(0x30, SeekOrigin.Current);

                B4ACScript.AppendLine("Small Intermission Sprite: " + Small_Intermission_Sprite);
                B4ACScript.AppendLine("Right Sortie Sprite: " + Right_Sortie_Sprite);
                B4ACScript.AppendLine("Trophy Sprite: " + Trophy_Sprite);
                B4ACScript.AppendLine("Sound Effect: " + Sound_Effect);
                B4ACScript.AppendLine("Small Right Sortie Sprite: " + Small_Right_Sortie_Sprite);
            }

            StreamWriter txt = File.CreateText(Path.GetDirectoryName(path) + @"\Boss_Unit_Image_List.txt");
            txt.Write(B4ACScript);

            txt.Close();

            */

            fs.Close();
        }

        private void read_Local_Sound_List(string path)
        {
            FileStream fs = File.OpenRead(path);
            changeStreamFile(fs);

            string[] MBONDir = Directory.GetFileSystemEntries(@"G:\Games\PS4\MBON\Extracted\MBON\Image0\archives", "*", SearchOption.AllDirectories);
            string newDir = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Input\MBON\v2\All Local Sound";

            for (int i = 0; i < 220; i++)
            {
                uint ID = readUIntSmallEndian();
                string titleImageHash = readUIntSmallEndian().ToString("X8");
                string voiceLogicHash = readUIntSmallEndian().ToString("X8");
                string localVoiceHash = readUIntSmallEndian().ToString("X8");
                string localSpecialVoiceHash = readUIntSmallEndian().ToString("X8");
                string unkHash = readUIntSmallEndian().ToString("X8");
                string cantFountHash = readUIntSmallEndian().ToString("X8");

                string titleImagePath = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(titleImageHash));
                string voiceLogicPath = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(voiceLogicHash));
                string localVoicePath = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(localVoiceHash));
                string localSpecialVoicePath = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(localSpecialVoiceHash));
                string unkPath = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(unkHash));
                string cantFountPath = MBONDir.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).Equals(cantFountHash));

                string dir = newDir + @"\" + (i + 1) + @"\";
                Directory.CreateDirectory(dir);

                /*
                if (titleImagePath != null)
                    File.Copy(titleImageHash, dir + Path.GetFileName(titleImageHash) + ".bin", true);
                if (voiceLogicPath != null)
                    File.Copy(voiceLogicPath, dir + Path.GetFileName(voiceLogicHash) + ".bin", true);
                 */
                if (localVoicePath != null)
                    File.Copy(localVoicePath, dir + Path.GetFileName(localVoiceHash) + ".bin", true);
                /*
                if (localSpecialVoicePath != null)
                    File.Copy(localSpecialVoicePath, dir + Path.GetFileName(localSpecialVoiceHash) + ".bin", true);
                if (unkPath != null)
                    File.Copy(unkPath, dir + Path.GetFileName(unkPath) + ".bin", true);
                if (cantFountPath != null)
                    File.Copy(cantFountPath, dir + Path.GetFileName(cantFountPath) + ".bin", true);
                 */
            }

            fs.Close();
        }
    }
}
