using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


enum vertexType
{
    NoNormals = 0,
    NormalsFloat = 1,
    NormalsTanBiTanFloat = 2,
    NormalsHalfFloat = 3,
    NormalsTanBiTanHalfFloat = 4
}


namespace FBRepacker.ModelTextureEditUI
{

    public partial class ModelTextureEditUI_Main : Window
    {

        private ObservableCollection<NUDconvertModel> ItemsSource = new ObservableCollection<NUDconvertModel>();
        public bool isCancel = false;

        public ModelTextureEditUI_Main(int shapeCount)
        {
            InitializeComponent();
            for (int i = 0; i < shapeCount; i++)
            {
                ItemsSource.Add(new NUDconvertModel(id: i, "No Normals", "1144", "", "", "", true, false, false));
            }
            lvDataBinding.ItemsSource = ItemsSource;
        }

        public ObservableCollection<NUDconvertModel> getItemsSource()
        {
            return ItemsSource;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            isCancel = true;
            this.Close();
        }

        private void DoneBtn_Click(object sender, RoutedEventArgs e)
        {
            bool canClose = false;
            for(int i = 0; i < ItemsSource.Count; i++)
            {
                if (ItemsSource[i].onSelectdVertexType.Length < 1)
                {
                    canClose = false;
                    break;
                }
                if (ItemsSource[i].onSelectdTextureType.Length < 1)
                {
                    canClose = false;
                    break;
                }
                canClose = true;
            }
            if (canClose)
            {
                this.Close();
            }
            else
            {
                MessageBox.Show("You not select the Vertex Type or Texture Type");
            }
        }

        private void NUDconvertVertexComboBoxOnChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private List<bool> setThreeTextureNameEnable(int type)
        {
            List<bool> list = new List<bool>();
            switch (type)
            {
                case 0:
                    list.Add(false);
                    list.Add(false);
                    list.Add(false);
                    return list;
                case 1:
                    list.Add(true);
                    list.Add(false);
                    list.Add(false);
                    return list;
                case 2:
                    list.Add(true);
                    list.Add(true);
                    list.Add(false);
                    return list;
                case 3:
                    list.Add(true);
                    list.Add(true);
                    list.Add(true);
                    return list;
                default:
                    list.Add(false);
                    list.Add(false);
                    list.Add(false);
                    return list;
            }

        }

        private void NUDconvertTextureComboBoxOnChanged(object sender, SelectionChangedEventArgs e)
        {
            for(int i = 0; i < ItemsSource.Count; i++)
            {
                switch (ItemsSource[i].onSelectdTextureType)
                {
                    case "1110":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id, 
                            ItemsSource[i].onSelectdVertexType, 
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(1)[0],
                            setThreeTextureNameEnable(1)[1],
                            setThreeTextureNameEnable(1)[2]);
                        break;
                    case "1111":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id,
                            ItemsSource[i].onSelectdVertexType,
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(1)[0],
                            setThreeTextureNameEnable(1)[1],
                            setThreeTextureNameEnable(1)[2]);
                        break;
                    case "1120":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id,
                            ItemsSource[i].onSelectdVertexType,
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(1)[0],
                            setThreeTextureNameEnable(1)[1],
                            setThreeTextureNameEnable(1)[2]);
                        break;
                    case "113001":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id,
                            ItemsSource[i].onSelectdVertexType,
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(1)[0],
                            setThreeTextureNameEnable(1)[1],
                            setThreeTextureNameEnable(1)[2]);
                        break;
                    case "1141":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id,
                            ItemsSource[i].onSelectdVertexType,
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(1)[0],
                            setThreeTextureNameEnable(1)[1],
                            setThreeTextureNameEnable(1)[2]);
                        break;
                    case "1142":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id,
                            ItemsSource[i].onSelectdVertexType,
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(1)[0],
                            setThreeTextureNameEnable(1)[1],
                            setThreeTextureNameEnable(1)[2]);
                        break;
                    case "1144":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id,
                            ItemsSource[i].onSelectdVertexType,
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(1)[0],
                            setThreeTextureNameEnable(1)[1],
                            setThreeTextureNameEnable(1)[2]);
                        break;
                    case "1148":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id,
                            ItemsSource[i].onSelectdVertexType,
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(1)[0],
                            setThreeTextureNameEnable(1)[1],
                            setThreeTextureNameEnable(1)[2]);
                        break;
                    case "1150":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id,
                            ItemsSource[i].onSelectdVertexType,
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(2)[0],
                            setThreeTextureNameEnable(2)[1],
                            setThreeTextureNameEnable(2)[2]);
                        break;
                    case "1154":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id,
                            ItemsSource[i].onSelectdVertexType,
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(2)[0],
                            setThreeTextureNameEnable(2)[1],
                            setThreeTextureNameEnable(2)[2]);
                        break;
                    case "1160":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id,
                            ItemsSource[i].onSelectdVertexType,
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(2)[0],
                            setThreeTextureNameEnable(2)[1],
                            setThreeTextureNameEnable(2)[2]);
                        break;
                    case "1214":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id,
                            ItemsSource[i].onSelectdVertexType,
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(3)[0],
                            setThreeTextureNameEnable(3)[1],
                            setThreeTextureNameEnable(3)[2]);
                        break;
                    case "2210":
                        ItemsSource[i] = new NUDconvertModel(id: ItemsSource[i].id,
                            ItemsSource[i].onSelectdVertexType,
                            ItemsSource[i].onSelectdTextureType,
                            ItemsSource[i].TextureName_One,
                            ItemsSource[i].TextureName_Two,
                            ItemsSource[i].TextureName_Three,
                            setThreeTextureNameEnable(3)[0],
                            setThreeTextureNameEnable(3)[1],
                            setThreeTextureNameEnable(3)[2]);
                        break;
                    default:
                        break;
                }
            }
        }


    }
}

