using System;
using System.Collections.Generic;
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

namespace FBRepacker.ModelTextureEditUI
{
    /// <summary>
    /// ModelTextureEditUI_Main.xaml 的交互逻辑
    /// </summary>
    public partial class ModelTextureEditUI_Main : Window
    {

        List<User> items = new List<User>();

        public ModelTextureEditUI_Main()
        {
            InitializeComponent();
            items.Add(new User() { Name = "John Doe", Age = 42 });
            items.Add(new User() { Name = "Jane Doe", Age = 39 });
            items.Add(new User() { Name = "Sammy Doe", Age = 13 });
            lvDataBinding.ItemsSource = items;
        }

        public class User
        {
            public string Name { get; set; }

            public int Age { get; set; }

            public override string ToString()
            {
                return this.Name + ", " + this.Age + " years old";
            }
        }

        private bool handle = true;
        private void ComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (handle) Handle();
            handle = true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            handle = !cmb.IsDropDownOpen;
            Handle();
        }

        private void Handle()
        {
            switch (cmbSelect.SelectedItem.ToString().Split(new string[] { ": " }, StringSplitOptions.None).Last())
            {
                case "1":
                    //Handle for the first combobox
                    break;
                case "2":
                    //Handle for the second combobox
                    break;
                case "3":
                    //Handle for the third combobox
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            items.Add(new User() { Name = "Sammy Doe", Age = 13 });
            lvDataBinding.Items.Refresh();
        }
    }
}
