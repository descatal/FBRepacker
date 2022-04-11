using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    internal class Model_List
    {
        public uint version { get; set; }
        public uint ID_Hash { get; set; }
        public uint unit_ID { get; set; }
        public List<Model_Hash_Info> model_Hash_Info_List { get; set; }

        public Model_List()
        {
            model_Hash_Info_List = new List<Model_Hash_Info>();
        }
    }

    public class Model_Hash_Info
    {
        public uint hash { get; set; }
        public uint load_enum { get; set; }

        public Model_Hash_Info()
        {

        }
    }
}
