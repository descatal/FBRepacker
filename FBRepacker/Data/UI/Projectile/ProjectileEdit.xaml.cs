using FBRepacker.Data.DataTypes;
using FBRepacker.Data.MBON_Parse;
using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace FBRepacker.Data.UI
{
    /// <summary>
    /// Interaction logic for ProjectileEdit.xaml
    /// </summary>
    public partial class ProjectileEdit : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // https://stackoverflow.com/questions/33774318/wpf-databinding-from-custom-property-not-updating
        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private Individual_Projectile_Properties individual_Projectile_Properties_immutable { get; set; }

        public Individual_Projectile_Properties individual_Projectile_Properties
        { 
            get {
                return individual_Projectile_Properties_immutable;
            } 
            set {
                individual_Projectile_Properties_immutable = value;
                NotifyPropertyChanged(null);
            } 
        }

        public List<Individual_Projectile_Properties> individual_Projectile_Properties_List { get; set; }

        public ProjectileEdit(Individual_Projectile_Properties individual_Projectile_Properties, List<Individual_Projectile_Properties> individual_Projectile_Properties_List)
        {
            InitializeComponent();

            this.DataContext = this;
            this.individual_Projectile_Properties = individual_Projectile_Properties;
            this.individual_Projectile_Properties_List = individual_Projectile_Properties_List;
        }

        private void Save_PAC_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Discard_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Encode_Name_Click(object sender, RoutedEventArgs e)
        {
            string name = nameInput.Text;
            var arrayOfBytes = Encoding.ASCII.GetBytes(name);

            var crc32 = new Crc32();
            string hash = crc32.Get(arrayOfBytes).ToString("X");

            hashInput.Text = hash;
        }

        private void hexInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int hexNumber;
            e.Handled = !int.TryParse(e.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hexNumber);
        }

        private void Copy_Info_Click(object sender, RoutedEventArgs e)
        {
            individual_Projectile_Properties = (Individual_Projectile_Properties)copy_info_combobox.SelectedItem;
        }
    }
}
