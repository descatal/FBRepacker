using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FBRepacker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int fileNumber = 1;
        string fileName = string.Empty, currDirectory = string.Empty, rootDirectory = string.Empty;
        FileStream PAC;

        public MainWindow()
        {
            InitializeComponent();
            init();
            Properties.Settings.Default.Save();
        }

        private void init()
        {
            // Init settings for paths.
            if(Properties.Settings.Default.Path == string.Empty || Properties.Settings.Default.Path == null)
                Properties.Settings.Default.Path = Directory.GetCurrentDirectory();

            if (Properties.Settings.Default.ExtractPath == string.Empty || Properties.Settings.Default.ExtractPath == null)
                Properties.Settings.Default.ExtractPath = Directory.GetCurrentDirectory();

            if (Properties.Settings.Default.RepackPath == string.Empty || Properties.Settings.Default.RepackPath == null)
                Properties.Settings.Default.RepackPath = Directory.GetCurrentDirectory();

            Properties.Settings.Default.Save();

            ExtractPath.Text = Properties.Settings.Default.ExtractPath;
            RepackPath.Text = Properties.Settings.Default.RepackPath;
        }

        private void OpenFileMenu_Click(object sender, RoutedEventArgs e)
        {
            // Close the filestream if another file is opened.
            if (PAC != null)
                PAC.Close();

            // Open file select dialog
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string filePath = Properties.Settings.Default.Path;
            openFileDialog.InitialDirectory = filePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();
            
            fileName = openFileDialog.FileName;
            Properties.Settings.Default.Path = File.Exists(fileName) ? System.IO.Path.GetDirectoryName(fileName) : Properties.Settings.Default.Path;
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void selectExtractPathButton(object sender, RoutedEventArgs e)
        {
            string extractPath = openFolderDialog(Properties.Settings.Default.ExtractPath);
            if(extractPath != string.Empty)
            {
                Properties.Settings.Default.ExtractPath = extractPath;
                Properties.Settings.Default.Save();
                ExtractPath.Text = Properties.Settings.Default.ExtractPath;
            }
        }

        private void selectRepackPathButton(object sender, RoutedEventArgs e)
        {
            string repackPath = openFolderDialog(Properties.Settings.Default.RepackPath);
            if (repackPath != string.Empty)
            {
                Properties.Settings.Default.RepackPath = repackPath;
                Properties.Settings.Default.Save();
                RepackPath.Text = Properties.Settings.Default.RepackPath;
            }
        }

        private string openFolderDialog(string path)
        {
            // Open folder select dialog
            CommonOpenFileDialog folderDialog = new CommonOpenFileDialog();
            folderDialog.InitialDirectory = path;
            folderDialog.IsFolderPicker = true;
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return folderDialog.FileName;
            }
            return string.Empty;
        }

        private void extract_Click(object sender, RoutedEventArgs e)
        {
            if (fileName != string.Empty && fileName != null)
            {
                new extractPAC.ExtractPAC(fileName, PAC).extractPAC();
            }
        }

        private void repack_Click(object sender, RoutedEventArgs e)
        {
            new RepackPsarc();
        }

        /*
        public void extractFHM()
        {
            string path = currDirectory + @"\FHM.info";
            int.TryParse(readInfo(path, "--FHM--", "Number of files"), out int numberofFile);
            for(int i = 0; i < numberofFile; i++)
            {
                int.TryParse(readInfo(path, "--" + i.ToString(), "FHMOffset"), out int Size);
                int.TryParse(readInfo(path, "--" + i.ToString(), "FHMOffset"), out int Offset);
                byte[] buffer = new byte[Size];
                PAC.Seek(Offset, 0);
                PAC.Read(buffer, 0x00, Size);
            }
        }

        public string readInfo(string path, string Tag, string Prop)
        {
            // Read the info file, seek to the file tag, take all the lines between tags.
            var FHMTag = File.ReadLines(path).SkipWhile(line => !line.Contains(Tag)).TakeWhile(line => !line.Contains("//"));
            return FHMTag.FirstOrDefault(prop => prop.Contains(Prop)); // In the extracted file tag, search for which lien has the correct Properties, and get the value after :
            
        }
        */
    }
}
