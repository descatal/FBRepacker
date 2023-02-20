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



        public ModelTextureEditUI_Main()
        {
            InitializeComponent();
            for(int i = 0; i < 1; i++)
            {
                ItemsSource.Add(new NUDconvertModel(id: i));
            }
            lvDataBinding.ItemsSource = ItemsSource;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            lvDataBinding.Items.Refresh();

        }

        private void NUDconvertVertexComboBoxOnChanged(object sender, SelectionChangedEventArgs e)
        {

            //for (int i = 0; i < ItemsSource.Count; i++)
            //{
            //    Console.WriteLine(ItemsSource[i].id);
            //    Console.WriteLine(ItemsSource[i].selectdVertexType);
            //    Console.WriteLine("//");
            //}



            //List<string> myList = new List<string>() { "鼠", "牛", "虎", "兔", "龍", "蛇", "馬", "羊", "猴", "雞", "狗", "豬" };
            //List<NUDTextureConvertModel> NTCMList = new List<NUDTextureConvertModel>();
            //NTCMList.Add(new NUDTextureConvertModel(1, myList));

            Console.WriteLine(ItemsSource[0].onSelectdVertexType);
        }

        private void NUDconvertTextureComboBoxOnChanged(object sender, SelectionChangedEventArgs e)
        {
            Console.WriteLine(ItemsSource[0].onSelectdTextureType);

        }
    }





    public class NUDTextureConvertModel
    {
        private int vertexType;
        private int weightType;
        private List<string> textureList;

        public NUDTextureConvertModel(int vertexType, List<string> textureList)
        {
            this.vertexType = vertexType;
            this.textureList = textureList;
        }
    }
}

