using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Psarc.V2
{
    public class PACFileInfoV2
    {
        public enum patchNoEnum
        {
            PATCH_1 = 0100,
            PATCH_2 = 0200,
            PATCH_3 = 0300,
            PATCH_4 = 0400,
            PATCH_5 = 0500,
            PATCH_6 = 0600
        }
        public enum fileFlagsEnum
        {
            hasFileInfo = 1,
            hasFileName = 2,
            hasFilePath = 4
        }
        public enum prefixEnum
        {
            NONE = 1,
            PATCH = 2,
            STREAM = 4
        }

        public fileFlagsEnum fileFlags { get; set; }
        public patchNoEnum patchNo { get; set; }
        public prefixEnum namePrefix { get; set; }
        //public uint patchNo { get; set; }
        //public uint relativePathIndex { get; set; } // removed in favor of just using data's existing position in JSON 
        public uint unk04 { get; set; }
        public uint Size1 { get; set; }
        public uint Size2 { get; set; }
        public uint Size3 { get; set; }
        public uint unk00 { get; set; }
        public uint nameHash { get; set; }
        public string relativePatchPath { get; set; }
        public bool hasRelativePatchSubPath { get; set; }
        public string filePath { get; set; }
        public int fileInfoIndex { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public PACFileInfoV2()
        {
            hasRelativePatchSubPath = false;
            relativePatchPath = "patch_01_00/00000000.PAC";
        }
    }
}
