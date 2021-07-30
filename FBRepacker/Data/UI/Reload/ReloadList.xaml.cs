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
    /// Interaction logic for ReloadList.xaml
    /// </summary>
    /// 
    public partial class ReloadList : Window
    {
        Reload reload { get; set; }
        List<Reload_FB> reload_FBs { get; set; }
        Parse_Reload parseReload { get; set; }

        public int Projectile_Count { get; set; }
        ParseProjectileProperties parseProjectileProperties { get; set; }
        Projectile_Properties projectile_Properties { get; set; }
        public List<Individual_Projectile_Properties> Individual_Projectile_Properties { get; set; }

        public ReloadList()
        {
            InitializeComponent();

            parseReload = new Parse_Reload();
            reload = parseReload.parseReloadJSON();
            reload_FBs = reload.reload_FB;
            reloadListlv.ItemsSource = reload_FBs;
        }

        private void Edit_File_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = ((sender as Button)?.Tag as ListViewItem)?.DataContext;
            var itemId = (item as Reload_FB)?.hash;
            int index = reload_FBs.FindIndex(s => s.hash == itemId);
            Reload_FB selectedInfo = reload_FBs[index];
            Reload_FB backupInfo = (Reload_FB)selectedInfo.DeepClone();
            ReloadEdit PACFileInfoEdit = new ReloadEdit(selectedInfo, reload_FBs);
            bool? save = PACFileInfoEdit.ShowDialog();

            if (save == false)
            {
                reload_FBs[index] = backupInfo;
            }
            else
            {
                reload_FBs[index] = selectedInfo;
            }

            Projectile_Count = projectile_Properties.individual_Projectile_Properties.Count();

            reloadListlv.Items.Refresh();
        }

        private void Delete_File_Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure?", "Deleing entry", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                var item = ((sender as Button)?.Tag as ListViewItem)?.DataContext;
                var itemId = (item as Individual_Projectile_Properties)?.hash;
                int index = Individual_Projectile_Properties.FindIndex(s => s.hash == itemId);
                Individual_Projectile_Properties.RemoveAt(index);

                reloadListlv.Items.Refresh();
            }
        }

        private void Append_Projectile_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            string ReloadJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.ReloadJSONFilePath);
            openFileDialog.InitialDirectory = ReloadJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            ReloadJSONFilePath = openFileDialog.FileName;

            if (File.Exists(ReloadJSONFilePath))
            {
                StreamReader JSONFS = File.OpenText(ReloadJSONFilePath);
                string JSON = JSONFS.ReadToEnd();

                Reload new_reload = JsonConvert.DeserializeObject<Reload>(JSON);

                if (new_reload.game_Ver != Reload.game_ver.FB)
                    throw new Exception("Game version not FB!");

                List<Reload_FB> new_reload_FBs = new_reload.reload_FB;

                for (int i = 0; i < new_reload_FBs.Count; i++)
                {
                    Reload_FB reload_FB = reload_FBs[i];
                    Reload_FB new_reload_FB = new_reload_FBs[i];

                    int hash_exist = reload_FBs.FindIndex(x => x.hash.Equals(new_reload_FB.hash));

                    if (hash_exist != -1)
                    {
                        reload_FBs[hash_exist] = new_reload_FB;
                    }
                    else
                    {
                        reload_FBs.Add(new_reload_FB);
                    }
                }

                reloadListlv.Items.Refresh();
                JSONFS.Close();
            }
        }

        private void Save_JSON_and_Export_bin_Click(object sender, RoutedEventArgs e)
        {
            parseReload.writeReloadBinary(reload);

            // Save JSON
            string JSON = JsonConvert.SerializeObject(reload, Formatting.Indented);

            // Create a backup copy of old JSON.
            string oriJSONFileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ReloadJSONFilePath);
            string oriJSONFilePath = Path.GetDirectoryName(Properties.Settings.Default.ReloadJSONFilePath);

            StreamReader sr = File.OpenText(Properties.Settings.Default.ReloadJSONFilePath);
            StreamWriter sw = File.CreateText(oriJSONFilePath + @"\" + oriJSONFileName + "_backup.JSON");
            sw.Write(sr.ReadToEnd());
            sr.Close();
            sw.Close();

            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ReloadJSONFilePath);
            string outputPath = Properties.Settings.Default.ReloadJSONFilePath;

            //WaitForFile(outputPath);
            StreamWriter fsJSON = File.CreateText(outputPath);
            fsJSON.Write(JSON);
            fsJSON.Close();

            DialogResult = true;
        }

        public void WaitForFile(string filename)
        {
            //This will lock the execution until the file is ready
            //TODO: Add some logic to make it async and cancelable
            while (!IsFileReady(filename)) { }
        }

        public bool IsFileReady(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
