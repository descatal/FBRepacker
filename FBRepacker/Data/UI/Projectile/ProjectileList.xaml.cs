using FBRepacker.Data.DataTypes;
using FBRepacker.Data.MBON_Parse;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace FBRepacker.Data.UI
{
    /// <summary>
    /// Interaction logic for ProjectileList.xaml
    /// </summary>
    public partial class ProjectileList : Window
    {
        public int Projectile_Count { get; set; }
        ParseProjectileProperties parseProjectileProperties { get; set; }
        Projectile_Properties projectile_Properties { get; set; }
        public List<Individual_Projectile_Properties> Individual_Projectile_Properties { get; set; }

        public ProjectileList()
        {
            InitializeComponent();

            parseProjectileProperties = new ParseProjectileProperties();
            projectile_Properties = parseProjectileProperties.parseProjectileJSON();
            Projectile_Count = projectile_Properties.individual_Projectile_Properties.Count();
            Individual_Projectile_Properties = projectile_Properties.individual_Projectile_Properties;
            projectileListlv.ItemsSource = projectile_Properties.individual_Projectile_Properties;
        }

        private void Edit_File_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = ((sender as Button)?.Tag as ListViewItem)?.DataContext;
            var itemId = (item as Individual_Projectile_Properties)?.hash;
            int index = Individual_Projectile_Properties.FindIndex(s => s.hash == itemId);
            Individual_Projectile_Properties selectedInfo = Individual_Projectile_Properties[index];
            Individual_Projectile_Properties backupInfo = (Individual_Projectile_Properties)selectedInfo.Clone();
            ProjectileEdit PACFileInfoEdit = new ProjectileEdit(selectedInfo, Individual_Projectile_Properties);
            bool? save = PACFileInfoEdit.ShowDialog();

            if (save == false)
            {
                Individual_Projectile_Properties[index] = backupInfo;
            }
            else
            {
                Individual_Projectile_Properties[index] = selectedInfo;
            }

            Projectile_Count = projectile_Properties.individual_Projectile_Properties.Count();

            projectileListlv.Items.Refresh();
        }

        private void Delete_File_Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure?", "Deleing entry", MessageBoxButton.YesNo);
            if(messageBoxResult == MessageBoxResult.Yes)
            {
                var item = ((sender as Button)?.Tag as ListViewItem)?.DataContext;
                var itemId = (item as Individual_Projectile_Properties)?.hash;
                int index = Individual_Projectile_Properties.FindIndex(s => s.hash == itemId);
                Individual_Projectile_Properties.RemoveAt(index);

                projectileListlv.Items.Refresh();
            }
        }

        private void Add_Projectile_Button_Click(object sender, RoutedEventArgs e)
        {
            Individual_Projectile_Properties newInfo = new Individual_Projectile_Properties();
            ProjectileEdit projectileEdit = new ProjectileEdit(newInfo, Individual_Projectile_Properties);
            bool? save = projectileEdit.ShowDialog();
            if (save == true)
            {
                Individual_Projectile_Properties.Add(newInfo);

            }
            projectileListlv.Items.Refresh();
        }

        private void Save_JSON_and_Export_bin_Click(object sender, RoutedEventArgs e)
        {
            // write Binary
            parseProjectileProperties.writeProjectileBinary(projectile_Properties);

            // Save JSON
            string JSON = JsonConvert.SerializeObject(projectile_Properties, Formatting.Indented);

            // Create a backup copy of old JSON.
            string oriJSONFileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ProjecitleJSONFilePath);
            string oriJSONFilePath = Path.GetDirectoryName(Properties.Settings.Default.ProjecitleJSONFilePath);
            File.Copy(Properties.Settings.Default.ProjecitleJSONFilePath, oriJSONFilePath + @"\" + oriJSONFileName + "_backup.JSON", true);

            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ProjecitleBinaryFilePath);
            string outputPath = Properties.Settings.Default.outputProjectileJSONFolderPath + @"\" + fileName + @"_Projectile.JSON";

            StreamWriter fsJSON = File.CreateText(outputPath);
            fsJSON.Write(JSON);
            fsJSON.Close();

            DialogResult = true;
        }
    }
}
