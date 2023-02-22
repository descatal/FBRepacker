using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public string TextureName_One { get; set; }
        public string TextureName_Two { get; set; }
        public string TextureName_Three { get; set; }

        public bool TextureName_One_Enable { get; set; }
        public bool TextureName_Two_Enable { get; set; }
        public bool TextureName_Three_Enable { get; set; }


        public NUDconvertModel()
        {

        }


        public NUDconvertModel(int id,
            string VertexType, string TextureType,
            string TextureName_One, string TextureName_Two, string TextureName_Three,
            bool TextureName_One_Enable, bool TextureName_Two_Enable, bool TextureName_Three_Enable)
        {
            this.id = id;

            this.onSelectdVertexType = VertexType;
            this.onSelectdTextureType = TextureType;

            this.TextureName_One = TextureName_One;
            this.TextureName_Two = TextureName_Two;
            this.TextureName_Three = TextureName_Three;

            this.TextureName_One_Enable = TextureName_One_Enable;
            this.TextureName_Two_Enable = TextureName_Two_Enable;
            this.TextureName_Three_Enable = TextureName_Three_Enable;

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

        private List<string> textureTypeList = new List<string>
        {
            "1110",
            "111011",
            "1111",
            "1120",
            "112011",
            "113001",
            "1141",
            "1142",
            "1144",
            "1148",
            "1150",
            "1154",
            "1160",
            "117101",
            "1214",
            "2210",
        };

        public List<string> selectTextureTypeList
        {
            get { return textureTypeList; }
            set { }
        }

        public string onSelectdTextureType { get; set; }






    }
}
