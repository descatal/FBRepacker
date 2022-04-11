using FBRepacker.Data.DataTypes;
using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.FB_Parse
{
    internal class Parse_Model_List : Internals
    {
        public Parse_Model_List()
        {
            
        }

        public Model_List parse_model_list(string input)
        {
            Model_List model_List = new Model_List();

            FileStream fs = File.OpenRead(input);

            model_List.version = 0;
            model_List.ID_Hash = readUIntBigEndian(fs);
            model_List.unit_ID = readUIntBigEndian(fs);

            uint number_of_model = readUIntBigEndian(fs);

            fs.Seek(0x4, SeekOrigin.Current);

            for(int i = 0; i < number_of_model; i++)
            {
                Model_Hash_Info model_Hash_Info = new Model_Hash_Info();

                model_Hash_Info.hash = readUIntBigEndian(fs);
                model_Hash_Info.load_enum = readUIntBigEndian(fs);

                uint checkifzero = readUIntBigEndian(fs);

                if (checkifzero != 0)
                    throw new Exception();

                uint index = readUIntBigEndian(fs);

                model_List.model_Hash_Info_List.Add(model_Hash_Info);
            }

            fs.Close();

            return model_List;
        }
    }
}
