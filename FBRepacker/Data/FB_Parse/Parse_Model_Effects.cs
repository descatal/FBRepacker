using FBRepacker.Data.DataTypes;
using FBRepacker.PAC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.FB_Parse
{
    internal class Parse_Model_Effects : Internals
    {
        public Parse_Model_Effects()
        {

        }

        public void serialize_Model_Effects_Data()
        {
            StreamReader JSONSr = File.OpenText(Properties.Settings.Default.inputModelEffectsJSONPath);
            string JSON = JSONSr.ReadToEnd();
            JSONSr.Close();
            Model_Effects model_Effects = JsonConvert.DeserializeObject<Model_Effects>(JSON);

            MemoryStream Model_Effects_MS = write_Model_Effects_Data(model_Effects);

            FileStream ofs = File.Create(Properties.Settings.Default.outputModelEffectsBinaryPath + @"\Model_Effects.bin");
            Model_Effects_MS.Seek(0, SeekOrigin.Begin);
            Model_Effects_MS.CopyTo(ofs);
            ofs.Close();
        }

        public MemoryStream write_Model_Effects_Data(Model_Effects model_Effects)
        {
            MemoryStream model_Effect_MS = new MemoryStream();

            appendUIntMemoryStream(model_Effect_MS, model_Effects.ID_Hash, true);
            appendUIntMemoryStream(model_Effect_MS, model_Effects.unit_ID, true);
            appendUIntMemoryStream(model_Effect_MS, 0, true);
            appendUIntMemoryStream(model_Effect_MS, 0, true);

            // Model Hash Region
            MemoryStream model_Effect_Hashes_MS = new MemoryStream();

            appendUIntMemoryStream(model_Effect_Hashes_MS, (uint)model_Effects.model_hashes_and_data.Count(), true);

            MemoryStream model_Effect_Dataset_MSes = new MemoryStream();
            long model_Effect_Dataset_MS_offset = 0x4 + model_Effects.model_hashes_and_data.Count() * 0x8;

            foreach (var model_Effect in model_Effects.model_hashes_and_data)
            {
                appendUIntMemoryStream(model_Effect_Hashes_MS, model_Effect.Key, true);

                MemoryStream model_Effect_Dataset_MS = new MemoryStream();
                appendUIntMemoryStream(model_Effect_Dataset_MS, (uint)model_Effect.Value.Count(), true);

                MemoryStream model_Effect_Data_MSes = new MemoryStream();
                long model_Effect_Data_MS_offset = 0x4 + model_Effect.Value.Count() * 0x8;
                for (int i = 0; i < model_Effect.Value.Count(); i++)
                {
                    Model_Bone_Effect_Dataset model_Bone_Effect_Dataset = model_Effect.Value[i];
                    appendUIntMemoryStream(model_Effect_Dataset_MS, model_Bone_Effect_Dataset.unk_bone_dataset_enum, true);

                    MemoryStream model_Effect_Data_MS = new MemoryStream();
                    appendUIntMemoryStream(model_Effect_Data_MS, (uint)model_Bone_Effect_Dataset.dataset.Count(), true);
                    for (int j = 0; j < model_Bone_Effect_Dataset.dataset.Count(); j++)
                    {
                        Model_Bone_Effects_Data model_Bone_Effects_Data = model_Bone_Effect_Dataset.dataset[j];
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.bone_index, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, (uint)(0x4 + model_Bone_Effect_Dataset.dataset.Count() * 0x8 + j * 0x50), true); // Pointer
                    }

                    for (int j = 0; j < model_Bone_Effect_Dataset.dataset.Count(); j++)
                    {
                        Model_Bone_Effects_Data model_Bone_Effects_Data = model_Bone_Effect_Dataset.dataset[j];
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.ALEO_Hash_0x0, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x4, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.ALEO_Hash_0x8, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0xc, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x10, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x14, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x18, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x1c, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x20, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x24, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x28, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x2c, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x30, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x34, true);
                        appendFloatMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x38, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x3c, true);
                        appendFloatMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x40, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x44, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x48, true);
                        appendUIntMemoryStream(model_Effect_Data_MS, model_Bone_Effects_Data.unk_0x4c, true);
                    }

                    appendUIntMemoryStream(model_Effect_Dataset_MS, (uint)model_Effect_Data_MS_offset, true);

                    model_Effect_Data_MS_offset += model_Effect_Data_MS.Length;

                    model_Effect_Data_MSes.Write(model_Effect_Data_MS.ToArray(), 0, (int)model_Effect_Data_MS.Length);
                }

                model_Effect_Dataset_MS.Write(model_Effect_Data_MSes.ToArray(), 0, (int)model_Effect_Data_MSes.Length);

                appendUIntMemoryStream(model_Effect_Hashes_MS, (uint)model_Effect_Dataset_MS_offset, true);

                model_Effect_Dataset_MSes.Write(model_Effect_Dataset_MS.ToArray(), 0, (int)model_Effect_Dataset_MS.Length);

                model_Effect_Dataset_MS_offset += model_Effect_Dataset_MS.Length;
            }

            model_Effect_Hashes_MS.Write(model_Effect_Dataset_MSes.ToArray(), 0, (int)model_Effect_Dataset_MSes.Length);

            model_Effect_MS.Write(model_Effect_Hashes_MS.ToArray(), 0, (int)model_Effect_Hashes_MS.Length);

            return model_Effect_MS;
        }

        public void deserialize_Model_Effects_Data()
        {
            Model_Effects model_Effects = parse_Model_Effects_Data(Properties.Settings.Default.inputModelEffectsBinaryPath);
            string JSON = JsonConvert.SerializeObject(model_Effects, Formatting.Indented);
            StreamWriter oSW = File.CreateText(Properties.Settings.Default.outputModelEffectsJSONPath + @"\Model_Effects.json");
            oSW.Write(JSON);
            oSW.Close();
        }

        public Model_Effects parse_Model_Effects_Data(string path)
        {
            FileStream fs = File.OpenRead(path);
            Model_Effects model_Effects = new Model_Effects();

            model_Effects.version = 1;
            model_Effects.ID_Hash = readUIntBigEndian(fs);
            model_Effects.unit_ID = readUIntBigEndian(fs);

            fs.Seek(0x8, SeekOrigin.Current);

            long model_Hash_Info_Address = fs.Position;

            uint model_Count = readUIntBigEndian(fs);

            for (int i = 0; i < model_Count; i++)
            {
                uint model_Hash = readUIntBigEndian(fs);
                uint model_Hash_Offset = readUIntBigEndian(fs);

                long returnModelCount = fs.Position;

                fs.Seek(model_Hash_Info_Address + model_Hash_Offset, SeekOrigin.Begin);

                long bone_Dataset_Info_Address = fs.Position;

                uint bone_Dataset_Count = readUIntBigEndian(fs);

                List<Model_Bone_Effect_Dataset> model_Bone_Effect_Datasets = new List<Model_Bone_Effect_Dataset>(); 

                for (int j = 0; j < bone_Dataset_Count; j++)
                {
                    Model_Bone_Effect_Dataset model_Bone_Effect_Dataset = new Model_Bone_Effect_Dataset();
                    model_Bone_Effect_Dataset.unk_bone_dataset_enum = readUIntBigEndian(fs);

                    uint bone_Dataset_Offset = readUIntBigEndian(fs);

                    long returnAddressDataset = fs.Position;

                    fs.Seek(bone_Dataset_Info_Address + bone_Dataset_Offset, SeekOrigin.Begin);

                    long model_Bone_Effects_Address = fs.Position;

                    uint model_Bone_Effects_Count = readUIntBigEndian(fs);

                    uint previous_Offset = 0; // For checking purposes

                    for(int k = 0; k < model_Bone_Effects_Count; k++)
                    {
                        Model_Bone_Effects_Data model_Bone_Effects_Data = new Model_Bone_Effects_Data();
                        model_Bone_Effects_Data.bone_index = readUIntBigEndian(fs);

                        uint model_Bone_Effects_Offset = readUIntBigEndian(fs);

                        long returnAddress = fs.Position;

                        // Check if all datasets are 0x50 in size
                        if (previous_Offset != 0 && model_Bone_Effects_Offset - previous_Offset != 0x50)
                            throw new Exception();

                        fs.Seek(model_Bone_Effects_Address + model_Bone_Effects_Offset, SeekOrigin.Begin);

                        // Version 1
                        model_Bone_Effects_Data.ALEO_Hash_0x0 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x4 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.ALEO_Hash_0x8 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0xc = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x10 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x14 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x18 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x1c = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x20 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x24 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x28 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x2c = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x30 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x34 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x38 = readFloat(fs, true);
                        model_Bone_Effects_Data.unk_0x3c = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x40 = readFloat(fs, true);
                        model_Bone_Effects_Data.unk_0x44 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x48 = readUIntBigEndian(fs);
                        model_Bone_Effects_Data.unk_0x4c = readUIntBigEndian(fs);

                        fs.Seek(returnAddress, SeekOrigin.Begin);

                        model_Bone_Effect_Dataset.dataset.Add(model_Bone_Effects_Data);
                        previous_Offset = model_Bone_Effects_Offset;
                    }

                    fs.Seek(returnAddressDataset, SeekOrigin.Begin);

                    model_Bone_Effect_Datasets.Add(model_Bone_Effect_Dataset);
                }

                fs.Seek(returnModelCount, SeekOrigin.Begin);
                model_Effects.model_hashes_and_data[model_Hash] = model_Bone_Effect_Datasets;
            }

            return model_Effects;
        }
    }
}
