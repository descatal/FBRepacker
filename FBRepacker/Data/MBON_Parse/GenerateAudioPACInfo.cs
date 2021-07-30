using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.MBON_Parse
{
    class GenerateAudioPACInfo : Internals
    {
        string streamName = "003.STREAM";

        // IS14
        /*
        string codec = "7";
        string Subheader_Size = "48";
        string Loop_Start = "0";
        string Loop_Length = "0";
        string Loop_Flag = "0";
        string Loop_Float = "4";
        string Loop_Float_2 = "-99";
        string var_0x50 = "1";
        string var_0x54 = "10";
        string var_0x60 = "128";
        string var_0x6C = "-100";
        string var_0x70 = "1000";
        string var_0x9C = "0";
        string var_0xAC = "0";
        string Format = "BNSF/is14";
        string Subheader_Count = "12";
        */

        // AT3
        
        string codec = "3";
        string Subheader_Size = "64";
        string Loop_Start = "0";
        string Loop_Length = "0";
        string Loop_Flag = "0";
        string Loop_Float = "4";
        string Loop_Float_2 = "-99";
        string var_0x50 = "1";
        string var_0x54 = "10";
        string var_0x60 = "128";
        string var_0x6C = "-100";
        string var_0x70 = "1000";
        string var_0x9C = "0";
        string var_0xAC = "0";
        string Format = "AT3";
        string Subheader_Count = "16";
        

        // vag (non-loop)
        /*
        string codec = "2";
        string Subheader_Size = "4";
        string Loop_Start = "0";
        string Loop_Length = "0";
        string Loop_Flag = "0";
        string Loop_Float = "0";
        string Loop_Float_2 = "-99";
        string var_0x50 = "0";
        string var_0x54 = "4";
        string var_0x60 = "23";
        string var_0x6C = "0";
        string var_0x70 = "1200";
        string var_0x9C = "0";
        string var_0xAC = "1";
        string Format = "VAG";
        string Subheader_Count = "1";
        

        // vag (loop)
        /*
        string codec = "2";
        string Subheader_Size = "4";
        string Loop_Start = "0";
        string Loop_Length = "0";
        string Loop_Flag = "0";
        string Loop_Float = "0";
        string Loop_Float_2 = "-10";
        string var_0x50 = "0";
        string var_0x54 = "4";
        string var_0x60 = "13";
        string var_0x6C = "0";
        string var_0x70 = "1200";
        string var_0x9C = "0";
        string var_0xAC = "1";
        string Format = "VAG";
        string Subheader_Count = "1";
        */

        public GenerateAudioPACInfo()
        {
            StringBuilder info = new StringBuilder();
            List<string> AudioFiles = Directory.GetFiles(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Bound Doc\Converted from MBON\Local Voice Lines").ToList();

            StreamWriter txt = File.CreateText(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Bound Doc\Converted from MBON\Local Voice Lines Info.txt");

            info.AppendLine("Number of audio files: " + AudioFiles.Count());
            info.AppendLine("fileName: " + streamName);
            for(int i = 0; i < AudioFiles.Count(); i++)
            {
                info.AppendLine("#Sound: " + (i + 1));
                info.AppendLine("Codec: " + codec);
                info.AppendLine("Subheader Size: " + Subheader_Size);
                info.AppendLine("Loop Start: " + Loop_Start);
                info.AppendLine("Loop Length: " + Loop_Length);
                info.AppendLine("Loop Flag: " + Loop_Flag);
                info.AppendLine("Loop Float: " + Loop_Float);
                info.AppendLine("Loop Float 2: " + Loop_Float_2);
                info.AppendLine("var_0x50: " + var_0x50);
                info.AppendLine("var_0x54: " + var_0x54);
                info.AppendLine("var_0x60: " + var_0x60);
                info.AppendLine("var_0x6C: " + var_0x6C);
                info.AppendLine("var_0x70: " + var_0x70);
                info.AppendLine("var_0x9C: " + var_0x9C);
                info.AppendLine("var_0xAC: " + var_0xAC);
                info.AppendLine("Format: " + Format);
                info.AppendLine("Subheader Count: " + Subheader_Count);

                string audioName = Path.GetFileName(AudioFiles[i]);
                info.AppendLine("fileName: " + audioName);
            }

            info.AppendLine("//");

            txt.Write(info);
            txt.Close();
        }
    }
}
