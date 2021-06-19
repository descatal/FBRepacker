using FBRepacker.PAC.Repack;
using FBRepacker.NUD;
using FBRepacker.PACInfoUI;
using FBRepacker.Psarc;
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
using FBRepacker.Data;
using FBRepacker.Data.MBON_Parse;
using FBRepacker.Data.FB_Parse;
using System.Globalization;
using FBRepacker.Data.UI;

namespace FBRepacker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string filePath = string.Empty, currDirectory = string.Empty, rootDirectory = string.Empty;
        FileStream PAC;

        public MainWindow()
        {
            InitializeComponent();
            init();
            this.DataContext = this;
            Properties.Settings.Default.Save();
        }

        private void init()
        {
            tabCont.SelectedIndex = Properties.Settings.Default.SelectedTab;
        }

        private void Open_Extract_PAC_File_Click(object sender, RoutedEventArgs e)
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
            Properties.Settings.Default.Save();
        }

        private void Open_Repack_PAC_Folder_Click(object sender, RoutedEventArgs e)
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

        private void Open_Psarc_PAC_File_List_Click(object sender, RoutedEventArgs e)
        {
            // Open file select dialog
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string NUDfileDirectoryPath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.PsarcPACFilePathList);
            openFileDialog.InitialDirectory = NUDfileDirectoryPath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            NUDfileDirectoryPath = openFileDialog.FileName;
            Properties.Settings.Default.PsarcPACFilePathList = File.Exists(NUDfileDirectoryPath) ? NUDfileDirectoryPath : Properties.Settings.Default.PsarcPACFilePathList;
            Properties.Settings.Default.Save();
        }

        private void Open_Psarc_PAC_Repack_Folder_Click(object sender, RoutedEventArgs e)
        {
            // Close the filestream if another file is opened.
            if (PAC != null)
                PAC.Close();

            // Open file select dialog
            string openRepackPath = openFolderDialog(Properties.Settings.Default.PsarcRepackFolder);

            string filePath = openRepackPath;
            if (Directory.Exists(filePath))
                Properties.Settings.Default.PsarcRepackFolder = filePath;

            Properties.Settings.Default.Save();
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void change_Extract_Output_Folder_Button(object sender, RoutedEventArgs e)
        {
            string extractPath = openFolderDialog(Properties.Settings.Default.OutputExtractPAC);
            if(extractPath != string.Empty)
            {
                Properties.Settings.Default.OutputExtractPAC = extractPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Repack_Output_Folder_Button(object sender, RoutedEventArgs e)
        {
            string repackPath = openFolderDialog(Properties.Settings.Default.OutputRepackPAC);
            if (repackPath != string.Empty)
            {
                Properties.Settings.Default.OutputRepackPAC = repackPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Psarc_Repack_Output_Folder_Button(object sender, RoutedEventArgs e)
        {
            string repackPath = openFolderDialog(Properties.Settings.Default.OutputRepackPsarc);
            if (repackPath != string.Empty)
            {
                Properties.Settings.Default.OutputRepackPsarc = repackPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_NUD_to_DAE_Output_Path_Button(object sender, RoutedEventArgs e)
        {
            string NUDtoDAEPath = openFolderDialog(Properties.Settings.Default.OutputPathNUDtoDAE);
            if (NUDtoDAEPath != string.Empty)
            {
                Properties.Settings.Default.OutputPathNUDtoDAE = NUDtoDAEPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_DAE_to_NUD_Output_Path_Button(object sender, RoutedEventArgs e)
        {
            string DAEtoNUDPath = openFolderDialog(Properties.Settings.Default.OutputPathDAEtoNUD);
            if (DAEtoNUDPath != string.Empty)
            {
                Properties.Settings.Default.OutputPathDAEtoNUD = DAEtoNUDPath;
                Properties.Settings.Default.Save();
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

        private void OpenNUDFile_Click(object sender, RoutedEventArgs e)
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
        }

        private void OpenVBNFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string VBNfileDirectoryPath = Properties.Settings.Default.VBNPathNUDtoDAE;
            openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(Properties.Settings.Default.VBNPathNUDtoDAE);
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            VBNfileDirectoryPath = openFileDialog.FileName;
            Properties.Settings.Default.VBNPathNUDtoDAE = File.Exists(VBNfileDirectoryPath) ? VBNfileDirectoryPath : Properties.Settings.Default.VBNPathNUDtoDAE;
            Properties.Settings.Default.Save();
        }

        private void OpenDAEFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string DAEfilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.DAEPathDAEtoNUD);
            openFileDialog.InitialDirectory = DAEfilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            DAEfilePath = openFileDialog.FileName;
            Properties.Settings.Default.DAEPathDAEtoNUD = File.Exists(DAEfilePath) ? DAEfilePath : Properties.Settings.Default.DAEPathDAEtoNUD;
            Properties.Settings.Default.Save();
        }

        private void OpenCScriptFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string ScriptFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.CScriptFilePath);
            openFileDialog.InitialDirectory = ScriptFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            ScriptFilePath = openFileDialog.FileName;
            Properties.Settings.Default.CScriptFilePath = File.Exists(ScriptFilePath) ? ScriptFilePath : Properties.Settings.Default.CScriptFilePath;
            Properties.Settings.Default.Save();
        }

        private void OpenBABBFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string BABBFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.BABBFilePath);
            openFileDialog.InitialDirectory = BABBFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            BABBFilePath = openFileDialog.FileName;
            Properties.Settings.Default.BABBFilePath = File.Exists(BABBFilePath) ? BABBFilePath : Properties.Settings.Default.BABBFilePath;
            Properties.Settings.Default.Save();
        }

        private void OpenB4ACFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string B4ACFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.B4ACFilePath);
            openFileDialog.InitialDirectory = B4ACFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            B4ACFilePath = openFileDialog.FileName;
            Properties.Settings.Default.B4ACFilePath = File.Exists(B4ACFilePath) ? B4ACFilePath : Properties.Settings.Default.B4ACFilePath;
            Properties.Settings.Default.Save();
        }
        
        private void OpenDAEVBNFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
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

        private void OpenMBONDataFile_Click(object sender, RoutedEventArgs e)
        {
            string MBONDataFolderPath = openFolderDialog(Properties.Settings.Default.MBONDataFolderPath);
            if (MBONDataFolderPath != string.Empty)
            {
                Properties.Settings.Default.MBONDataFolderPath = MBONDataFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void OpenFBDataFile_Click(object sender, RoutedEventArgs e)
        {
            string FBDataFolderPath = openFolderDialog(Properties.Settings.Default.FBDataFolderPath);
            if (FBDataFolderPath != string.Empty)
            {
                Properties.Settings.Default.FBDataFolderPath = FBDataFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_MBON_to_FB_Data_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputDataFolderPath = openFolderDialog(Properties.Settings.Default.outputDataFolderPath);
            if (outputDataFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputDataFolderPath = outputDataFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void OpenProjectileBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string ProjecitleBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.ProjecitleBinaryFilePath);
            openFileDialog.InitialDirectory = ProjecitleBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            ProjecitleBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.ProjecitleBinaryFilePath = File.Exists(ProjecitleBinaryFilePath) ? ProjecitleBinaryFilePath : Properties.Settings.Default.ProjecitleBinaryFilePath;
            Properties.Settings.Default.Save();
        }

        private void OpenReloadBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string ReloadBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.ReloadBinaryFilePath);
            openFileDialog.InitialDirectory = ReloadBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            ReloadBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.ReloadBinaryFilePath = File.Exists(ReloadBinaryFilePath) ? ReloadBinaryFilePath : Properties.Settings.Default.ReloadBinaryFilePath;
            Properties.Settings.Default.Save();
        }

        private void change_Projecitle_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputProjectileJSONFolderPath = openFolderDialog(Properties.Settings.Default.outputProjectileJSONFolderPath);
            if (outputProjectileJSONFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputProjectileJSONFolderPath = outputProjectileJSONFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Reload_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputReloadJSONFolderPath = openFolderDialog(Properties.Settings.Default.outputReloadJSONFolderPath);
            if (outputReloadJSONFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputReloadJSONFolderPath = outputReloadJSONFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void OpenProjectileJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string ProjecitleJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.ProjecitleJSONFilePath);
            openFileDialog.InitialDirectory = ProjecitleJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            ProjecitleJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.ProjecitleJSONFilePath = File.Exists(ProjecitleJSONFilePath) ? ProjecitleJSONFilePath : Properties.Settings.Default.ProjecitleJSONFilePath;
            Properties.Settings.Default.Save();
        }

        private void OpenReloadJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string ReloadJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.ReloadJSONFilePath);
            openFileDialog.InitialDirectory = ReloadJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            ReloadJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.ReloadJSONFilePath = File.Exists(ReloadJSONFilePath) ? ReloadJSONFilePath : Properties.Settings.Default.ReloadJSONFilePath;
            Properties.Settings.Default.Save();
        }

        private void change_Projecitle_Binary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputProjectileBinFolderPath = openFolderDialog(Properties.Settings.Default.outputProjectileBinFolderPath);
            if (outputProjectileBinFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputProjectileBinFolderPath = outputProjectileBinFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Reload_Binary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputReloadBinFolderPath = openFolderDialog(Properties.Settings.Default.outputReloadBinFolderPath);
            if (outputReloadBinFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputReloadBinFolderPath = outputReloadBinFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Script_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputScriptFolderPath = openFolderDialog(Properties.Settings.Default.outputScriptFolderPath);
            if (outputScriptFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputScriptFolderPath = outputScriptFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void NUDtoDAE_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting NUD to DAE conversion");
            try
            {
                new ModelConverter().fromNUDtoDAE();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("NUD to DAE conversion complete");
        }

        private void DAEtoNUD_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting DAE to NUD conversion");
            try
            {
                new ModelConverter().fromDAEtoNUD();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("DAE to NUD conversion complete");
        }

        private void MBON_to_FB_Data_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting MBON to FB Data conversion");
            try
            {
                new Unit_Data();
                //new ModelConverter().fromDAEtoNUD();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("MBON to FB Data conversion complete");
        }

        private void Projectile_Export_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Projectile JSON Export");
            try
            {
                new ParseProjectileProperties().convertProjectileBintoJSON();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Projectile JSON Export Complete!");
        }

        private void Projectile_Export_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Projectile Binary Export");
            try
            {
                bool? done = new ProjectileList().ShowDialog();
                if (done == true)
                {
                    debugMessageBox.AppendText(Environment.NewLine);
                    debugMessageBox.AppendText("Projectile Binary Export Complete!");
                }
                else
                {
                    debugMessageBox.AppendText(Environment.NewLine);
                    debugMessageBox.AppendText("Projectile Binary Export Aborted!");
                }
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
        }

        private void Reload_Export_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Reload JSON Export");
            try
            {
                new Parse_Reload().parse_Reload();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Reload JSON Export Complete!");
        }

        private void Reload_Export_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Reload Binary Export");
            try
            {
                new ReloadList().ShowDialog();
                /*
                bool done = true;
                if (done == true)
                {
                    debugMessageBox.AppendText(Environment.NewLine);
                    debugMessageBox.AppendText("Reload Binary Export Complete!");
                }
                else
                {
                    debugMessageBox.AppendText(Environment.NewLine);
                    debugMessageBox.AppendText("Reload Binary Export Aborted!");
                }
                */
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
        }

        private void extractPAC_Click(object sender, RoutedEventArgs e)
        {
            if (filePath != string.Empty && filePath != null)
            {
                DialogResult askMultiplePAC = System.Windows.Forms.MessageBox.Show("Extract Multiple FHM?", "Extract Multiple FHM?", MessageBoxButtons.YesNo);

                Stream stream = File.Open(filePath, FileMode.Open);
                long streamSize = stream.Length;
                stream.Close();

                string baseExtractPath = Properties.Settings.Default.OutputExtractPAC + @"\" + Path.GetFileNameWithoutExtension(filePath);

                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Starts Extracting " + filePath);

                if (askMultiplePAC == System.Windows.Forms.DialogResult.Yes)
                {
                    long PACEndPosition = 0;
                    int i = 0;
                    do
                    {
                        string extractPath = baseExtractPath + @"\" + i.ToString();
                        try
                        {
                            new PAC.Extract.ExtractPAC(filePath, PAC).extractPAC(PACEndPosition, out PACEndPosition, extractPath);
                        }
                        catch(Exception exp)
                        {
                            debugMessageBox.AppendText(Environment.NewLine);
                            debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
                        }
                        i++;
                    } while (PACEndPosition < streamSize);
                }
                else
                {
                    new PAC.Extract.ExtractPAC(filePath, PAC).extractPAC(0, out long unused, baseExtractPath);
                    try
                    {
                        
                    }
                    catch (Exception exp)
                    {
                        debugMessageBox.AppendText(Environment.NewLine);
                        debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
                    }
                }
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Extract complete");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.Text = "Log: ";
        }

        private void repackPAC_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starts Repack PAC to: " + Properties.Settings.Default.OutputRepackPAC);
            try
            {
                RepackPAC repackInstance = new RepackPAC(Properties.Settings.Default.OutputRepackPAC);
                PACInfoWindow pacInfoUI = new PACInfoWindow(repackInstance);
                pacInfoUI.ShowDialog();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Repack complete");
        }

        private void TabControl_Selected(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SelectedTab = tabCont.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        private void Copy_PAC_to_Psarc_Repack_Folder(object sender, RoutedEventArgs e)
        {
            new CopyPACFiles(Properties.Settings.Default.PsarcPACFilePathList, Properties.Settings.Default.PsarcRepackFolder);
        }

        private void repackPsarc_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starts Repack Psarc to: " + Properties.Settings.Default.OutputRepackPsarc);
            try
            {
                Properties.Settings.Default.PsarcOutputFileName = PsarcFileName.Text;
                Properties.Settings.Default.Save();
                bool? done = new PsarcFileInfo(PsarcFileName.Text).ShowDialog();
                if (done == true)
                {
                    debugMessageBox.AppendText(Environment.NewLine);
                    debugMessageBox.AppendText("Psarc Repack Complete!");
                }
                else
                {
                    debugMessageBox.AppendText(Environment.NewLine);
                    debugMessageBox.AppendText("Psarc Repack Aborted!");
                }
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
        }

        private void Link_Script_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Linking Script from: " + Properties.Settings.Default.CScriptFilePath + " with BABB from " + Properties.Settings.Default.BABBFilePath);
            try
            {
                new LinkScriptFunc();
                //new ModelConverter().fromDAEtoNUD();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Linking Script Complete!");
        }

        private void Generate_B4AC_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Generating B4AC from: " + Properties.Settings.Default.B4ACFilePath);
            try
            {
                new Generate_Script_B4AC();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("B4AC Generate Complete!");
        }

        private void Debug_Click(object sender, RoutedEventArgs e)
        {
            new GenerateAudioPACInfo();
            //new ParseALEO();
            //new MBON_Image_List();
            /*
            string[] allfiles = Directory.GetFiles(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Input\MBON\v2\All Local Sound", "*.*", SearchOption.AllDirectories);
            for(int i = 0; i < allfiles.Length; i++)
            {
                filePath = allfiles[i];
                string baseExtractPath = Properties.Settings.Default.OutputExtractPAC + @"\" + Directory.GetParent(filePath).Name + " - " + Path.GetFileNameWithoutExtension(filePath) + @"\" + Path.GetFileNameWithoutExtension(filePath);
                try
                {
                    new PAC.Extract.ExtractPAC(filePath, PAC).extractPAC(0, out long unused, Path.GetDirectoryName(baseExtractPath));
                }
                catch (Exception exp)
                {
                    debugMessageBox.AppendText(Environment.NewLine);
                    debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
                }
            }            
             
             */
            /*
            string[] allfiles = Directory.GetFiles(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Input\MBON\v2\All Boss Unit Image", "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < allfiles.Length; i++)
            {
                filePath = allfiles[i];
                string baseExtractPath = Properties.Settings.Default.OutputExtractPAC + @"\" + Directory.GetParent(filePath).Name + @"\" + Path.GetFileNameWithoutExtension(filePath) + @"\";
                try
                {
                    new PAC.Extract.ExtractPAC(filePath, PAC).extractPAC(0, out long unused, Path.GetDirectoryName(baseExtractPath));
                }
                catch (Exception exp)
                {
                    debugMessageBox.AppendText(Environment.NewLine);
                    debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
                }
            }
             */
            /*
            string[] allfiles = Directory.GetFiles(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Input\MBON\v2\All Playable Unit Image", "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < allfiles.Length; i++)
            {
                filePath = allfiles[i];
                string baseExtractPath = Properties.Settings.Default.OutputExtractPAC + @"\" + Directory.GetParent(filePath).Name + @"\" + Path.GetFileNameWithoutExtension(filePath) + @"\";
                try
                {
                    new PAC.Extract.ExtractPAC(filePath, PAC).extractPAC(0, out long unused, Path.GetDirectoryName(baseExtractPath));
                }
                catch (Exception exp)
                {
                    debugMessageBox.AppendText(Environment.NewLine);
                    debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
                }
            }
             */

            /*
            string[] allfiles = Directory.GetFiles(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Extract\Input\MBON\v2\All Pilot Image", "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < allfiles.Length; i++)
            {
                filePath = allfiles[i];
                string baseExtractPath = Properties.Settings.Default.OutputExtractPAC + @"\" + Directory.GetParent(filePath).Name + @"\" + Path.GetFileNameWithoutExtension(filePath) + @"\";
                try
                {
                    new PAC.Extract.ExtractPAC(filePath, PAC).extractPAC(0, out long unused, Path.GetDirectoryName(baseExtractPath));
                }
                catch (Exception exp)
                {
                    debugMessageBox.AppendText(Environment.NewLine);
                    debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
                }
            }
             */
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

    public class RadioBoolToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int integer = (int)value;
            if (integer == int.Parse(parameter.ToString()))
                return true;
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int.TryParse((string)parameter, out int res);
            Properties.Settings.Default.ProjectileBinaryInputGameVer = res;
            Properties.Settings.Default.Save();
            return parameter;
        }
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }


}
