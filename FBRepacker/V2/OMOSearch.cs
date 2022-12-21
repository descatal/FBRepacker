using FBRepacker.Data.DataTypes;
using FBRepacker.Data.FB_Parse.DataTypes;
using FBRepacker.PAC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.V2
{
    internal class OMOSearch : Internals
    {
        public OMOSearch()
        {
            var allMap = getAllOMOMap();
            var Json = JsonConvert.SerializeObject(allMap);
            var fs = File.CreateText(@"I:\Full Boost\MBON Reimport Project\AllOMO.json");
            fs.Write(Json);
            fs.Close();
        }

        public List<OMOMap> getAllOMOMap()
        {
            string totalMBONCombinedPsarcFolder = @"\\?\" + @"I:\Full Boost\MBON Reimport Project\Total MBON Combined Psarc";
            string totalMBONExportFolder = @"\\?\" + @"I:\Full Boost\MBON Reimport Project\Total MBON Export";

            List<string> allUnitFolders = Directory.GetDirectories(totalMBONExportFolder, "*", SearchOption.TopDirectoryOnly).ToList();

            allUnitFolders = allUnitFolders.OrderBy(x => uint.Parse(Path.GetFileNameWithoutExtension(x.Split('.')[0]))).ToList();

            UnitIDList unit_Infos = load_UnitID();

            string json = File.OpenText(@"I:\Full Boost\MBON Reimport Project\AllUnitsPACHashes.json").ReadToEnd();
            List<Unit_Files_List> unit_Files_List = JsonConvert.DeserializeObject<List<Unit_Files_List>>(json);

            List<OMOMap> omoMapList = new List<OMOMap>();

            foreach (var units in unit_Infos.Unit_ID)
            {
                var unitFolder = allUnitFolders.Where(x => x.Contains(units.id.ToString())).FirstOrDefault();

                // Get unit's english name
                string unitName = unit_Infos.Unit_ID.FirstOrDefault(s => s.id == units.id).name_english.Replace(" ", "_");
                unitName = unitName.Replace(".", "_");
                unitName = unitName.Replace("∀", "Turn_A");
                unitName = unitName.Replace("Ⅱ", "II");

                Unit_Files_List unit_Files = unit_Files_List.FirstOrDefault(x => x.Unit_ID == units.id);

                string basePsarcRepackFolder = totalMBONCombinedPsarcFolder + @"\Units\MBON_Units\" + unitName;

                string extractMBONFolder = unitFolder + @"\Extracted MBON";

                var files = Directory.GetDirectories(extractMBONFolder, "*", SearchOption.TopDirectoryOnly).ToList();

                foreach (var OMOFolder in files)
                {
                    if (OMOFolder.Contains("OMO"))
                    {
                        OMOMap omomap = new OMOMap();
                        omomap.unitName = unitName;

                        var omotopfilesandfolders = Directory.GetFileSystemEntries(OMOFolder + @"\001-FHM\002-FHM\", "*", SearchOption.TopDirectoryOnly).ToList();
                        omotopfilesandfolders.Sort();

                        var omohashmap = omotopfilesandfolders.Where(x => x.Contains("003.bin")).FirstOrDefault();

                        omotopfilesandfolders = omotopfilesandfolders.Skip(2).ToList();

                        FileStream fs = File.OpenRead(omohashmap);

                        List<OMO> omoList = new List<OMO>();

                        var hashcount = readUIntBigEndian(fs);
                        for(int i = 0; i < hashcount; i++)
                        {
                            OMO omo = new OMO();
                            var hash = readUIntBigEndian(fs);
                            omo.OMOHash = hash;
                            omo.OMOHashHex = hash.ToString("X8");
                            var path = omotopfilesandfolders[i].Remove(0, 4).Replace("\\", "/");
                            omo.fileName = path;

                            omoList.Add(omo);
                        }

                        omomap.omo = omoList;

                        omoMapList.Add(omomap);
                    }
                }
            }
            return omoMapList;
        }

        public UnitIDList load_UnitID()
        {
            string jsonString = Properties.Resources.Unit_IDs;
            UnitIDList unit_ID = System.Text.Json.JsonSerializer.Deserialize<UnitIDList>(jsonString);
            return unit_ID;
        }
    }

    class OMOMap
    {
        public String unitName { get; set; }

        public List<OMO> omo = new List<OMO>();

        public OMOMap()
        {
            omo = new List<OMO>();
        }
    }
    class OMO
    {
        public uint OMOHash { get; set; }
        public String OMOHashHex { get; set; }
        public String fileName { get; set; }
    }

}
