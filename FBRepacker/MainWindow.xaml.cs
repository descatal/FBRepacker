using FBRepacker.PAC.Repack;
using FBRepacker.NUD;
using FBRepacker.PACInfoUI;
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
using System.Windows.Forms;

namespace FBRepacker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int fileNumber = 1;
        string filePath = string.Empty, currDirectory = string.Empty, rootDirectory = string.Empty;
        FileStream PAC;

        public MainWindow()
        {
            InitializeComponent();
            init();
            Properties.Settings.Default.Save();
        }

        private void init()
        {
            // Init settings for paths. (TODO cleanup)
            if(Properties.Settings.Default.OpenExtractPath == string.Empty || Properties.Settings.Default.OpenExtractPath == null)
                Properties.Settings.Default.OpenExtractPath = Directory.GetCurrentDirectory();

            if (Properties.Settings.Default.OpenRepackPath == string.Empty || Properties.Settings.Default.OpenRepackPath == null)
                Properties.Settings.Default.OpenRepackPath = Directory.GetCurrentDirectory();

            if (Properties.Settings.Default.ExtractPath == string.Empty || Properties.Settings.Default.ExtractPath == null)
                Properties.Settings.Default.ExtractPath = Directory.GetCurrentDirectory();

            if (Properties.Settings.Default.RepackPath == string.Empty || Properties.Settings.Default.RepackPath == null)
                Properties.Settings.Default.RepackPath = Directory.GetCurrentDirectory();

            if (Properties.Settings.Default.NUDPathNUDtoDAE == string.Empty || Properties.Settings.Default.NUDPathNUDtoDAE == null)
                Properties.Settings.Default.NUDPathNUDtoDAE = Directory.GetCurrentDirectory();

            if (Properties.Settings.Default.VBNPathNUDtoDAE == string.Empty || Properties.Settings.Default.VBNPathNUDtoDAE == null)
                Properties.Settings.Default.VBNPathNUDtoDAE = Directory.GetCurrentDirectory();

            if (Properties.Settings.Default.DAEPathDAEtoNUD == string.Empty || Properties.Settings.Default.DAEPathDAEtoNUD == null)
                Properties.Settings.Default.DAEPathDAEtoNUD = Directory.GetCurrentDirectory();

            if (Properties.Settings.Default.VBNPathDAEtoNUD == string.Empty || Properties.Settings.Default.VBNPathDAEtoNUD == null)
                Properties.Settings.Default.VBNPathDAEtoNUD = Directory.GetCurrentDirectory();

            if (Properties.Settings.Default.OutputPathNUDtoDAE == string.Empty || Properties.Settings.Default.OutputPathNUDtoDAE == null)
                Properties.Settings.Default.OutputPathNUDtoDAE = Directory.GetCurrentDirectory();

            if (Properties.Settings.Default.OutputPathDAEtoNUD == string.Empty || Properties.Settings.Default.OutputPathDAEtoNUD == null)
                Properties.Settings.Default.OutputPathDAEtoNUD = Directory.GetCurrentDirectory();

            Properties.Settings.Default.Save();

            ExtractPath.Text = Properties.Settings.Default.ExtractPath;
            RepackPath.Text = Properties.Settings.Default.RepackPath;
            NUDtoDAEOutputPath.Text = Properties.Settings.Default.OutputPathNUDtoDAE;
            DAEtoNUDOutputPath.Text = Properties.Settings.Default.OutputPathDAEtoNUD;
        }

        private void OpenExtractFileMenu_Click(object sender, RoutedEventArgs e)
        {
            // Close the filestream if another file is opened. This should not be here (TODO)
            if (PAC != null)
                PAC.Close();

            // Open file select dialog
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string filePath = Properties.Settings.Default.OpenExtractPath;
            openFileDialog.InitialDirectory = filePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();
            
            this.filePath = openFileDialog.FileName;
            Properties.Settings.Default.OpenExtractPath = File.Exists(this.filePath) ? System.IO.Path.GetDirectoryName(this.filePath) : Properties.Settings.Default.OpenExtractPath;
        }

        private void OpenRepackFileMenu_Click(object sender, RoutedEventArgs e)
        {
            // Close the filestream if another file is opened.
            if (PAC != null)
                PAC.Close();

            // Open file select dialog
            string openRepackPath = openFolderDialog(Properties.Settings.Default.OpenRepackPath);

            string filePath = openRepackPath;
            if (Directory.Exists(filePath))
                Properties.Settings.Default.OpenRepackPath = filePath;

            Properties.Settings.Default.Save();
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

        private void selectNUDtoDAEOutputPathButton(object sender, RoutedEventArgs e)
        {
            string NUDtoDAEPath = openFolderDialog(Properties.Settings.Default.OutputPathNUDtoDAE);
            if (NUDtoDAEPath != string.Empty)
            {
                Properties.Settings.Default.OutputPathNUDtoDAE = NUDtoDAEPath;
                Properties.Settings.Default.Save();
                NUDtoDAEOutputPath.Text = Properties.Settings.Default.OutputPathNUDtoDAE;
            }
        }

        private void selectDAEtoNUDOutputPathButton(object sender, RoutedEventArgs e)
        {
            string DAEtoNUDPath = openFolderDialog(Properties.Settings.Default.OutputPathDAEtoNUD);
            if (DAEtoNUDPath != string.Empty)
            {
                Properties.Settings.Default.OutputPathDAEtoNUD = DAEtoNUDPath;
                Properties.Settings.Default.Save();
                DAEtoNUDOutputPath.Text = Properties.Settings.Default.OutputPathDAEtoNUD;
            }
        }

        private string openFolderDialog(string initialPath)
        {
            // Open folder select dialog
            CommonOpenFileDialog folderDialog = new CommonOpenFileDialog();
            folderDialog.InitialDirectory = initialPath;
            folderDialog.IsFolderPicker = true;
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return folderDialog.FileName;
            }
            return string.Empty;
        }

        private void OpenNUDandVBNFile_Click(object sender, RoutedEventArgs e)
        {
            // Open file select dialog
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string NUDfileDirectoryPath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.NUDPathNUDtoDAE);
            openFileDialog.InitialDirectory = NUDfileDirectoryPath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            NUDfileDirectoryPath = openFileDialog.FileName;
            Properties.Settings.Default.NUDPathNUDtoDAE = File.Exists(NUDfileDirectoryPath) ? NUDfileDirectoryPath : Properties.Settings.Default.NUDPathNUDtoDAE;
            Properties.Settings.Default.Save();

            string VBNfileDirectoryPath = Properties.Settings.Default.VBNPathNUDtoDAE;
            openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(Properties.Settings.Default.VBNPathNUDtoDAE);
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            VBNfileDirectoryPath = openFileDialog.FileName;
            Properties.Settings.Default.VBNPathNUDtoDAE = File.Exists(VBNfileDirectoryPath) ? VBNfileDirectoryPath : Properties.Settings.Default.VBNPathNUDtoDAE;
            Properties.Settings.Default.Save();
        }

        private void OpenDAEandVBNFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string DAEfilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.DAEPathDAEtoNUD);
            openFileDialog.InitialDirectory = DAEfilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            DAEfilePath = openFileDialog.FileName;
            Properties.Settings.Default.DAEPathDAEtoNUD = File.Exists(DAEfilePath) ? DAEfilePath : Properties.Settings.Default.DAEPathDAEtoNUD;
            Properties.Settings.Default.Save();

            if (!Properties.Settings.Default.exportVBN)
            {
                string VBNfileDirectoryPath = Properties.Settings.Default.VBNPathDAEtoNUD;
                openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(Properties.Settings.Default.VBNPathDAEtoNUD);
                openFileDialog.RestoreDirectory = true;
                openFileDialog.ShowDialog();

                VBNfileDirectoryPath = openFileDialog.FileName;
                Properties.Settings.Default.VBNPathDAEtoNUD = File.Exists(VBNfileDirectoryPath) ? VBNfileDirectoryPath : Properties.Settings.Default.VBNPathDAEtoNUD;
                Properties.Settings.Default.Save();
            }
        }

        private void NUDtoDAE_Click(object sender, RoutedEventArgs e)
        {
            new ModelConverter().fromNUDtoDAE();
        }

        private void DAEtoNUD_Click(object sender, RoutedEventArgs e)
        {
            new ModelConverter().fromDAEtoNUD();
        }

        private void extractPAC_Click(object sender, RoutedEventArgs e)
        {

            if (filePath != string.Empty && filePath != null)
            {
                DialogResult askMultiplePAC = System.Windows.Forms.MessageBox.Show("Extract Multiple FHM?", "Extract Multiple FHM?", MessageBoxButtons.YesNo);

                Stream stream = File.Open(filePath, FileMode.Open);
                long streamSize = stream.Length;
                stream.Close();

                string baseExtractPath = Properties.Settings.Default.ExtractPath + @"\" + Path.GetFileNameWithoutExtension(filePath);

                if (askMultiplePAC == System.Windows.Forms.DialogResult.Yes)
                {
                    long PACEndPosition = 0;
                    int i = 0;
                    do
                    {
                        string extractPath = baseExtractPath + @"\" + i.ToString();
                        new PAC.Extract.ExtractPAC(filePath, PAC).extractPAC(PACEndPosition, out PACEndPosition, extractPath);
                        i++;
                    } while (PACEndPosition < streamSize);
                }
                else
                {
                    new PAC.Extract.ExtractPAC(filePath, PAC).extractPAC(0, out long unused, baseExtractPath);
                }
            }
        }

        private void repackPAC_Click(object sender, RoutedEventArgs e)
        {
            RepackPAC repackInstance = new RepackPAC(Properties.Settings.Default.RepackPath);
            PACInfoWindow pacInfoUI = new PACInfoWindow(repackInstance);
            pacInfoUI.ShowDialog();
        }

        private void repackPsarc_Click(object sender, RoutedEventArgs e)
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
