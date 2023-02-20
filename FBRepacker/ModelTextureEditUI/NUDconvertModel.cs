using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

struct Books
{
    public string title;
    public string author;
    public string subject;
    public int book_id;
};

namespace FBRepacker.ModelTextureEditUI
{
    public class NUDconvertModel
    {
        public int id { get; set; }


        public NUDconvertModel()
        {

        }

        public NUDconvertModel(int id)
        {
            this.id = id;
        }

        private List<string> vertexTypeList = new List<string>
        {
            "No Normals",
            "Normals (Float)",
            "Normals, Tan, Bi-Tan (Float)",
            "Normals (Half Float)",
            "Normals, Tan, Bi-Tan (Half Float)",
        };

        public List<string> selectVertexTypeList
        {
            get { return vertexTypeList; }
            set {  }
        }

        public string onSelectdVertexType { get; set; }


        Dictionary<string, string> textureTypeDict = new Dictionary<string, string>();

        private List<string> textureTypeList = new List<string>
        {
            "1110",
            "1111",
            "1120",
            "1141",
            "1142",
            "1144",
            "1148",
            "1150",
            "1154",
            "1214",
        };

        public string onSelectdTextureType { get; set; }

        public List<string> selectTextureTypeList
        {
            get { return textureTypeList; }
            set { }
        }





        public string name { get; set; }

    }
}
