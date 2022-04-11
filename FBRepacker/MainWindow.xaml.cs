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
using static FBRepacker.Data.MBON_Parse.nus3AudioNameHash;
using FBRepacker.Psarc.V2;
using FBRepacker.Tools;

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
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\temp");

            bool thirdPartyExists = Directory.Exists(Directory.GetCurrentDirectory() + @"\3rd Party");
            bool helpersExists = Directory.Exists(Directory.GetCurrentDirectory() + @"\Helpers");

            if(!thirdPartyExists || !helpersExists)
            {
                // https://stackoverflow.com/questions/7646328/how-to-use-the-7z-sdk-to-compress-and-decompress-a-file
                string roccoPath = Directory.GetCurrentDirectory() + @"\rocco.bin";
                if (File.Exists(roccoPath))
                {
                    using (Process sevenzip = new Process())
                    {
                        sevenzip.StartInfo.FileName = "7za.exe";
                        sevenzip.StartInfo.UseShellExecute = false;
                        sevenzip.StartInfo.RedirectStandardOutput = true;
                        sevenzip.StartInfo.CreateNoWindow = true;
                        sevenzip.StartInfo.Arguments = "-y x rocco.bin";
                        sevenzip.Start();
                        string logOutput = sevenzip.StandardOutput.ReadToEnd();
                        sevenzip.WaitForExit();
                    }
                }
            }

            audioFormatComboBox.ItemsSource = Enum.GetValues(typeof(audioFormatEnum)).Cast<audioFormatEnum>();
            PACInfoAudioFormatComboBox.ItemsSource = Enum.GetValues(typeof(audioFormatEnum)).Cast<audioFormatEnum>();
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

        private void Open_Psarc_TBL_Binary_Click(object sender, RoutedEventArgs e)
        {
            // Open file select dialog
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string psarc_TBL_Binary = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputPsarcTBLBinary);
            openFileDialog.InitialDirectory = psarc_TBL_Binary;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            psarc_TBL_Binary = openFileDialog.FileName;
            Properties.Settings.Default.inputPsarcTBLBinary = File.Exists(psarc_TBL_Binary) ? psarc_TBL_Binary : Properties.Settings.Default.inputPsarcTBLBinary;
            Properties.Settings.Default.Save();
        }

        private void Open_Psarc_TBL_JSON_Click(object sender, RoutedEventArgs e)
        {
            // Open file select dialog
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string psarc_TBL_JSON = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputPsarcJSON);
            openFileDialog.InitialDirectory = psarc_TBL_JSON;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            psarc_TBL_JSON = openFileDialog.FileName;
            Properties.Settings.Default.inputPsarcJSON = File.Exists(psarc_TBL_JSON) ? psarc_TBL_JSON : Properties.Settings.Default.inputPsarcJSON;
            Properties.Settings.Default.Save();
        }

        private void change_Psarc_TBL_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputPsarcTBLFolder = openFolderDialog(Properties.Settings.Default.outputPsarcTBLJson);
            if (outputPsarcTBLFolder != string.Empty)
            {
                Properties.Settings.Default.outputPsarcTBLJson = outputPsarcTBLFolder;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Psarc_TBL_JSON_Link_Psarc_Folder_Path_Click(object sender, RoutedEventArgs e)
        {
            string psarcTBLParseRepackFolder = openFolderDialog(Properties.Settings.Default.psarcTBLParseRepackFolder);
            if (psarcTBLParseRepackFolder != string.Empty)
            {
                Properties.Settings.Default.psarcTBLParseRepackFolder = psarcTBLParseRepackFolder;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Psarc_TBL_Binary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputPsarcTBLFolder = openFolderDialog(Properties.Settings.Default.outputPsarcTBLBinary);
            if (outputPsarcTBLFolder != string.Empty)
            {
                Properties.Settings.Default.outputPsarcTBLBinary = outputPsarcTBLFolder;
                Properties.Settings.Default.Save();
            }
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
        private void OpenB4ACTxtFolder_Click(object sender, RoutedEventArgs e)
        {
            string inputScriptRefactorTxtFolder = openFolderDialog(Properties.Settings.Default.inputScriptRefactorTxtFolder);
            if (inputScriptRefactorTxtFolder != string.Empty)
            {
                Properties.Settings.Default.inputScriptRefactorTxtFolder = inputScriptRefactorTxtFolder;
                Properties.Settings.Default.Save();
            }
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

        private void OpeninputMeleeVarBinaryFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string inputMeleeVarBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputMeleeVarBinaryPath);
            openFileDialog.InitialDirectory = inputMeleeVarBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            inputMeleeVarBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputMeleeVarBinaryPath = File.Exists(inputMeleeVarBinaryFilePath) ? inputMeleeVarBinaryFilePath : Properties.Settings.Default.inputMeleeVarBinaryPath;
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

        private void OpenEFPBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string EFPBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputEFPBinary);
            openFileDialog.InitialDirectory = EFPBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            EFPBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputEFPBinary = File.Exists(EFPBinaryFilePath) ? EFPBinaryFilePath : Properties.Settings.Default.inputEFPBinary;
            Properties.Settings.Default.Save();
        }

        private void OpenUnitDataHashSchemaBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string UnitDataHashSchemaBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputUnitDataHashSchemaBinary);
            openFileDialog.InitialDirectory = UnitDataHashSchemaBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            UnitDataHashSchemaBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputUnitDataHashSchemaBinary = File.Exists(UnitDataHashSchemaBinaryFilePath) ? UnitDataHashSchemaBinaryFilePath : Properties.Settings.Default.inputUnitDataHashSchemaBinary;
            Properties.Settings.Default.Save();
        }

        private void OpenUnitDataBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string UnitDataBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputUnitDataBinary);
            openFileDialog.InitialDirectory = UnitDataBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            UnitDataBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputUnitDataBinary = File.Exists(UnitDataBinaryFilePath) ? UnitDataBinaryFilePath : Properties.Settings.Default.inputUnitDataBinary;
            Properties.Settings.Default.Save();
        }

        private void OpenUnitDataReloadBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string UnitDataBinaryReloadFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputUnitDataReloadBinary);
            openFileDialog.InitialDirectory = UnitDataBinaryReloadFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            UnitDataBinaryReloadFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputUnitDataReloadBinary = File.Exists(UnitDataBinaryReloadFilePath) ? UnitDataBinaryReloadFilePath : Properties.Settings.Default.inputUnitDataReloadBinary;
            Properties.Settings.Default.Save();
        }

        private void OpenUnitDataJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string UnitDataJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputUnitDataJSON);
            openFileDialog.InitialDirectory = UnitDataJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            UnitDataJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputUnitDataJSON = File.Exists(UnitDataJSONFilePath) ? UnitDataJSONFilePath : Properties.Settings.Default.inputUnitDataJSON;
            Properties.Settings.Default.Save();
        }

        private void OpenUnitDataHashSchemaJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string UnitDataHashSchemaJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputUnitDataHashSchemaJSON);
            openFileDialog.InitialDirectory = UnitDataHashSchemaJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            UnitDataHashSchemaJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputUnitDataHashSchemaJSON = File.Exists(UnitDataHashSchemaJSONFilePath) ? UnitDataHashSchemaJSONFilePath : Properties.Settings.Default.inputUnitDataHashSchemaJSON;
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

        private void OpenHitBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string HitBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.HitBinaryFilePath);
            openFileDialog.InitialDirectory = HitBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            HitBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.HitBinaryFilePath = File.Exists(HitBinaryFilePath) ? HitBinaryFilePath : Properties.Settings.Default.HitBinaryFilePath;
            Properties.Settings.Default.Save();
        }

        private void OpenFBUnitInfoListBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string inputFBUnitInfoListBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputFBUnitInfoListBinary);
            openFileDialog.InitialDirectory = inputFBUnitInfoListBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            inputFBUnitInfoListBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputFBUnitInfoListBinary = File.Exists(inputFBUnitInfoListBinaryFilePath) ? inputFBUnitInfoListBinaryFilePath : Properties.Settings.Default.inputFBUnitInfoListBinary;
            Properties.Settings.Default.Save();
        }

        private void OpenNTXBBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string inputNTXBBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputNTXBBinaryPath);
            openFileDialog.InitialDirectory = inputNTXBBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            inputNTXBBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputNTXBBinaryPath = File.Exists(inputNTXBBinaryFilePath) ? inputNTXBBinaryFilePath : Properties.Settings.Default.inputNTXBBinaryPath;
            Properties.Settings.Default.Save();
        }

        private void OpenModelEffectsBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string inputModelEffectsBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputModelEffectsBinaryPath);
            openFileDialog.InitialDirectory = inputModelEffectsBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            inputModelEffectsBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputModelEffectsBinaryPath = File.Exists(inputModelEffectsBinaryFilePath) ? inputModelEffectsBinaryFilePath : Properties.Settings.Default.inputModelEffectsBinaryPath;
            Properties.Settings.Default.Save();
        }

        private void OpenHitboxPropertiesBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string inputHitboxPropertiesBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputHitboxPropertiesBinaryPath);
            openFileDialog.InitialDirectory = inputHitboxPropertiesBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            inputHitboxPropertiesBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputHitboxPropertiesBinaryPath = File.Exists(inputHitboxPropertiesBinaryFilePath) ? inputHitboxPropertiesBinaryFilePath : Properties.Settings.Default.inputHitboxPropertiesBinaryPath;
            Properties.Settings.Default.Save();
        }

        private void OpenFBSeriesInfoListBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string inputFBSeriesInfoListBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputFBSeriesInfoListBinary);
            openFileDialog.InitialDirectory = inputFBSeriesInfoListBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            inputFBSeriesInfoListBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputFBSeriesInfoListBinary = File.Exists(inputFBSeriesInfoListBinaryFilePath) ? inputFBSeriesInfoListBinaryFilePath : Properties.Settings.Default.inputFBSeriesInfoListBinary;
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

        private void change_EFP_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputEFPJSONFolderPath = openFolderDialog(Properties.Settings.Default.outputEFPJSONPath);
            if (outputEFPJSONFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputEFPJSONPath = outputEFPJSONFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Unit_Data_Hash_Schema_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputUnitDataHashSchemaFolder = openFolderDialog(Properties.Settings.Default.outputUnitDataHashSchemaJSONPath);
            if (outputUnitDataHashSchemaFolder != string.Empty)
            {
                Properties.Settings.Default.outputUnitDataHashSchemaJSONPath = outputUnitDataHashSchemaFolder;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Unit_Data_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputUnitDataFolder = openFolderDialog(Properties.Settings.Default.outputUnitDataJSONPath);
            if (outputUnitDataFolder != string.Empty)
            {
                Properties.Settings.Default.outputUnitDataJSONPath = outputUnitDataFolder;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Unit_Data_Binary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputUnitDataBinaryFolder = openFolderDialog(Properties.Settings.Default.outputUnitDataBinaryPath);
            if (outputUnitDataBinaryFolder != string.Empty)
            {
                Properties.Settings.Default.outputUnitDataBinaryPath = outputUnitDataBinaryFolder;
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

        private void change_FB_Unit_Info_List_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputFBUnitInfoListJSONFolderPath = openFolderDialog(Properties.Settings.Default.outputFBUnitInfoListJSONFolder);
            if (outputFBUnitInfoListJSONFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputFBUnitInfoListJSONFolder = outputFBUnitInfoListJSONFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_NTXB_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputNTXBJSONFolderPath = openFolderDialog(Properties.Settings.Default.outputNTXBJSONPath);
            if (outputNTXBJSONFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputNTXBJSONPath = outputNTXBJSONFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_ModelEffects_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputModelEffectsJSONFolderPath = openFolderDialog(Properties.Settings.Default.outputModelEffectsJSONPath);
            if (outputModelEffectsJSONFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputModelEffectsJSONPath = outputModelEffectsJSONFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_HitboxProperties_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputHitboxPropertiesJSONFolderPath = openFolderDialog(Properties.Settings.Default.outputHitboxPropertiesJSONPath);
            if (outputHitboxPropertiesJSONFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputHitboxPropertiesJSONPath = outputHitboxPropertiesJSONFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_FB_Series_Info_List_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputFBSeriesInfoListJSONFolderPath = openFolderDialog(Properties.Settings.Default.outputFBSeriesInfoListJSONFolder);
            if (outputFBSeriesInfoListJSONFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputFBSeriesInfoListJSONFolder = outputFBSeriesInfoListJSONFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Hit_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputHitJSONFolderPath = openFolderDialog(Properties.Settings.Default.outputHitJSONFolderPath);
            if (outputHitJSONFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputHitJSONFolderPath = outputHitJSONFolderPath;
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

        private void OpenEFPJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string EFPJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputEFPJSON);
            openFileDialog.InitialDirectory = EFPJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            EFPJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputEFPJSON = File.Exists(EFPJSONFilePath) ? EFPJSONFilePath : Properties.Settings.Default.inputEFPJSON;
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

        private void OpenFBUnitInfoListJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string FBUnitInfoListJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputFBUnitInfoListJSON);
            openFileDialog.InitialDirectory = FBUnitInfoListJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            FBUnitInfoListJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputFBUnitInfoListJSON = File.Exists(FBUnitInfoListJSONFilePath) ? FBUnitInfoListJSONFilePath : Properties.Settings.Default.inputFBUnitInfoListJSON;
            Properties.Settings.Default.Save();
        }

        private void OpenNTXBJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string NTXBJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputNTXBJSONPath);
            openFileDialog.InitialDirectory = NTXBJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            NTXBJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputNTXBJSONPath = File.Exists(NTXBJSONFilePath) ? NTXBJSONFilePath : Properties.Settings.Default.inputNTXBJSONPath;
            Properties.Settings.Default.Save();
        }

        private void OpenModelEffectsJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string ModelEffectsJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputModelEffectsJSONPath);
            openFileDialog.InitialDirectory = ModelEffectsJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            ModelEffectsJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputModelEffectsJSONPath = File.Exists(ModelEffectsJSONFilePath) ? ModelEffectsJSONFilePath : Properties.Settings.Default.inputModelEffectsJSONPath;
            Properties.Settings.Default.Save();
        }

        private void OpenHitboxPropertiesJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string HitboxPropertiesJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputHitboxPropertiesJSONPath);
            openFileDialog.InitialDirectory = HitboxPropertiesJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            HitboxPropertiesJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputHitboxPropertiesJSONPath = File.Exists(HitboxPropertiesJSONFilePath) ? HitboxPropertiesJSONFilePath : Properties.Settings.Default.inputHitboxPropertiesJSONPath;
            Properties.Settings.Default.Save();
        }

        private void OpenFBSeriesInfoListJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string FBSeriesInfoListJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputFBSeriesInfoListJSON);
            openFileDialog.InitialDirectory = FBSeriesInfoListJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            FBSeriesInfoListJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputFBSeriesInfoListJSON = File.Exists(FBSeriesInfoListJSONFilePath) ? FBSeriesInfoListJSONFilePath : Properties.Settings.Default.inputFBSeriesInfoListJSON;
            Properties.Settings.Default.Save();
        }

        private void OpenMapListBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string MapListBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputMapListBinaryPath);
            openFileDialog.InitialDirectory = MapListBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            MapListBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputMapListBinaryPath = File.Exists(MapListBinaryFilePath) ? MapListBinaryFilePath : Properties.Settings.Default.inputMapListBinaryPath;
            Properties.Settings.Default.Save();
        }

        private void OpenMapListJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string MapListJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputMapListJSONPath);
            openFileDialog.InitialDirectory = MapListJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            MapListJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputMapListJSONPath = File.Exists(MapListJSONFilePath) ? MapListJSONFilePath : Properties.Settings.Default.inputMapListJSONPath;
            Properties.Settings.Default.Save();
        }

        private void OpenHitJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string HitJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.HitJSONFilePath);
            openFileDialog.InitialDirectory = HitJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            HitJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.HitJSONFilePath = File.Exists(HitJSONFilePath) ? HitJSONFilePath : Properties.Settings.Default.HitJSONFilePath;
            Properties.Settings.Default.Save();
        }

        private void OpenLMBFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string LMBFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputLMBFilePath);
            openFileDialog.InitialDirectory = LMBFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            LMBFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputLMBFilePath = File.Exists(LMBFilePath) ? LMBFilePath : Properties.Settings.Default.inputLMBFilePath;
            Properties.Settings.Default.Save();
        }

        private void OpenResizeLMBFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string resizeLMBFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputResizeLMBBinaryPath);
            openFileDialog.InitialDirectory = resizeLMBFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            resizeLMBFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputResizeLMBBinaryPath = File.Exists(resizeLMBFilePath) ? resizeLMBFilePath : Properties.Settings.Default.inputResizeLMBBinaryPath;
            Properties.Settings.Default.Save();
        }

        private void change_ALEO_Input_Folder(object sender, RoutedEventArgs e)
        {
            string ALEOFolderPath = openFolderDialog(Properties.Settings.Default.ALEOFolderPath);
            if (ALEOFolderPath != string.Empty)
            {
                Properties.Settings.Default.ALEOFolderPath = ALEOFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void OpenVoiceLogicBinary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string VoiceLogicBinaryFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputVoiceLogicBinary);
            openFileDialog.InitialDirectory = VoiceLogicBinaryFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            VoiceLogicBinaryFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputVoiceLogicBinary = File.Exists(VoiceLogicBinaryFilePath) ? VoiceLogicBinaryFilePath : Properties.Settings.Default.inputVoiceLogicBinary;
            Properties.Settings.Default.Save();
        }

        private void OpenVoiceLogicJSON_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string VoiceLogicJSONFilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputVoiceLogicJSON);
            openFileDialog.InitialDirectory = VoiceLogicJSONFilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            VoiceLogicJSONFilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputVoiceLogicJSON = File.Exists(VoiceLogicJSONFilePath) ? VoiceLogicJSONFilePath : Properties.Settings.Default.inputVoiceLogicJSON;
            Properties.Settings.Default.Save();
        }

        private void OpenNus3File_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string Nus3FilePath = System.IO.Path.GetDirectoryName(Properties.Settings.Default.inputNus3File);
            openFileDialog.InitialDirectory = Nus3FilePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            Nus3FilePath = openFileDialog.FileName;
            Properties.Settings.Default.inputNus3File = File.Exists(Nus3FilePath) ? Nus3FilePath : Properties.Settings.Default.inputNus3File;
            Properties.Settings.Default.Save();
        }

        private void change_Audio_PAC_Info_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputAudioPACInfoFolderPath = openFolderDialog(Properties.Settings.Default.outputAudioPACInfoFolder);
            if (outputAudioPACInfoFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputAudioPACInfoFolder = outputAudioPACInfoFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_VoiceLogicJSON_Input_Path_Click(object sender, RoutedEventArgs e)
        {
            string inputVoiceLogicJSONFolderPath = openFolderDialog(Properties.Settings.Default.inputAudioPACInfoFolder);
            if (inputVoiceLogicJSONFolderPath != string.Empty)
            {
                Properties.Settings.Default.inputAudioPACInfoFolder = inputVoiceLogicJSONFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_VoiceLogicJSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputVoiceLogicJSONFolderPath = openFolderDialog(Properties.Settings.Default.outputVoiceLogicJSONFolder);
            if (outputVoiceLogicJSONFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputVoiceLogicJSONFolder = outputVoiceLogicJSONFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_VoiceLogicBinary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputVoiceLogicBinaryFolderPath = openFolderDialog(Properties.Settings.Default.outputVoiceLogicBinaryFolder);
            if (outputVoiceLogicBinaryFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputVoiceLogicBinaryFolder = outputVoiceLogicBinaryFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_SoundNameandHashFolder_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputSoundNameandHashFolderOutputPath = openFolderDialog(Properties.Settings.Default.outputNameandHashFolder);
            if (outputSoundNameandHashFolderOutputPath != string.Empty)
            {
                Properties.Settings.Default.outputNameandHashFolder = outputSoundNameandHashFolderOutputPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_ALEO_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputALEOFolderPath = openFolderDialog(Properties.Settings.Default.outputALEOFolderPath);
            if (outputALEOFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputALEOFolderPath = outputALEOFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_LMB_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputLMBFolderPath = openFolderDialog(Properties.Settings.Default.outputLMBFolderPath);
            if (outputLMBFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputLMBFolderPath = outputLMBFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_resize_LMB_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputResizeLMBFolderPath = openFolderDialog(Properties.Settings.Default.outputResizeLMBBinaryPath);
            if (outputResizeLMBFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputResizeLMBBinaryPath = outputResizeLMBFolderPath;
                Properties.Settings.Default.Save();
            }
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

        private void change_EFP_Binary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputEFPBinFolderPath = openFolderDialog(Properties.Settings.Default.outputEFPBinaryPath);
            if (outputEFPBinFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputEFPBinaryPath = outputEFPBinFolderPath;
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

        private void change_FB_Unit_Info_List_Binary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputFBUnitInfoListFolderPath = openFolderDialog(Properties.Settings.Default.outputFBUnitInfoListBinaryFolder);
            if (outputFBUnitInfoListFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputFBUnitInfoListBinaryFolder = outputFBUnitInfoListFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_NTXB_Binary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputNTXBFolderPath = openFolderDialog(Properties.Settings.Default.outputNTXBBinaryPath);
            if (outputNTXBFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputNTXBBinaryPath = outputNTXBFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_ModelEffects_Binary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputModelEffectsFolderPath = openFolderDialog(Properties.Settings.Default.outputModelEffectsBinaryPath);
            if (outputModelEffectsFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputModelEffectsBinaryPath = outputModelEffectsFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_HitboxProperties_Binary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputHitboxPropertiesFolderPath = openFolderDialog(Properties.Settings.Default.outputHitboxPropertiesBinaryPath);
            if (outputHitboxPropertiesFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputHitboxPropertiesBinaryPath = outputHitboxPropertiesFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_FB_Series_Info_List_Binary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputFBSeriesInfoListFolderPath = openFolderDialog(Properties.Settings.Default.outputFBSeriesInfoListBinaryFolder);
            if (outputFBSeriesInfoListFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputFBSeriesInfoListBinaryFolder = outputFBSeriesInfoListFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Map_List_JSON_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputMapListJSONFolderPath = openFolderDialog(Properties.Settings.Default.outputMapListJSONPath);
            if (outputMapListJSONFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputMapListJSONPath = outputMapListJSONFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Map_List_Binary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputMapListBinaryFolderPath = openFolderDialog(Properties.Settings.Default.outputMapListBinaryPath);
            if (outputMapListBinaryFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputMapListBinaryPath = outputMapListBinaryFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void change_Hit_Binary_Output_Path_Click(object sender, RoutedEventArgs e)
        {
            string outputHitBinFolderPath = openFolderDialog(Properties.Settings.Default.outputHitBinFolderPath);
            if (outputHitBinFolderPath != string.Empty)
            {
                Properties.Settings.Default.outputHitBinFolderPath = outputHitBinFolderPath;
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
                new Parse_Unit_Data();
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

        private void EFP_Export_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting EFP JSON Export");
            try
            {
                new ParseEFP().parseEFP();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("EFP JSON Export Complete!");
        }

        private void Unit_Data_Hash_Schema_Export_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Unit Data Hash Schema Export");
            try
            {
                new Parse_Unit_Data().writeDataHashSchema();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Unit Data Hash Schema JSON Export Complete!");
        }

        private void Unit_Data_Export_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Unit Data Export");
            new Parse_Unit_Data().readVariables();
            try
            {
                
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Unit Data JSON Export Complete!");
        }

        private void Unit_Data_Export_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Unit Data Export");
            new Parse_Unit_Data().writeVariables();
            try
            {
                
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Unit Data Binary Export Complete!");
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

        private void EFP_Export_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting EFP Binary Export");
            try
            {
                new ParseEFP().serializeEFP();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("EFP Binary Export Complete!");
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

        private void FB_Unit_Info_List_Export_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Unit Info JSON Export");
            try
            {
                new Parse_Unit_Info_List().readFBUnitInfoList();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Reload Unit Info Export Complete!");
        }

        private void NTXB_Export_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting NTXB JSON Export");
            
            try
            {
                new Parse_NTXB().readNTXB();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("NTXB JSON Export Complete!");
        }

        private void Model_Effects_Export_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Model Effects JSON Export");

            try
            {
                new Parse_Model_Effects().deserialize_Model_Effects_Data();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Model Effects JSON Export Complete!");
        }

        private void Hitbox_Properties_Export_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Hitbox Properties JSON Export");

            new Parse_Hitbox_Properties().deserialize_Hitbox_Properties();

            try
            {
                
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Hitbox Properties JSON Export Complete!");
        }

        private void FB_Series_Info_List_Export_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Series Info JSON Export");
            try
            {
                new Parse_Series_Info().readFBSeriesInfoList();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Reload Series Info Export Complete!");
        }

        private void Hit_Export_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Hit JSON Export");
            try
            {
                new Parse_Hit().parse_Hit();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Hit JSON Export Complete!");
        }

        private void Reload_Export_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Reload Binary Export");
            try
            {
                bool? done = new ReloadList().ShowDialog();
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
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
        }

        private void FB_Unit_Info_List_Export_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting FB Unit Info List Binary Export");
            try
            {
                new Parse_Unit_Info_List().writeFBUnitInfoList();
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("FB Unit Info List Binary Export Complete!");
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
        }

        private void NTXB_Export_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting NTXB Binary Export");
            new Parse_NTXB().writeNTXB();
            try
            {
                
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("NTXB Binary Export Complete!");
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
        }

        private void ModelEffects_Export_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting ModelEffects Binary Export");
            new Parse_Model_Effects().serialize_Model_Effects_Data();
            try
            {

                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("ModelEffects Binary Export Complete!");
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
        }

        private void HitboxProperties_Export_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Hitbox Properties Binary Export");
            new Parse_Hitbox_Properties().serialize_Hitbox_Properties();
            try
            {

                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Hitbox Properties Binary Export Complete!");
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
        }

        private void FB_Unit_Series_List_Export_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting FB Series Info List Binary Export");
            try
            {
                new Parse_Series_Info().writeFBSeriesInfoList();
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("FB Series Info List Binary Export Complete!");
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
        }

        private void Map_List_Export_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Map List JSON Export");
            new Parse_Map_List().deserialize_map_list();
            try
            {
                
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Map List JSON Export Complete!");
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
        }

        private void Map_List_Export_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Map List Binary Export");
            new Parse_Map_List().serialize_map_list();
            try
            {

                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Map List Binary Export Complete!");
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
        }

        private void Hit_Export_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting Hit Binary Export");
            try
            {
                new Parse_Hit().write_Hit_Binary();
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Hit Binary Export Complete!");

            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
        }

        private void ALEO_Convert_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting ALEO conversion");
            new ParseALEO();
            try
            {
                //new ParseALEO();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("ALEO conversion complete");
        }

        private void LMB_Convert_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting LMB conversion");
            new Parse_MBON_LMB();
            try
            {
                
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("LMB conversion complete");
        }

        private void resize_LMB_Convert_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starting LMB resizing");
            new resizeMBONLMB().resizeLMB(Properties.Settings.Default.inputResizeLMBBinaryPath, Properties.Settings.Default.outputResizeLMBBinaryPath + @"\resized.LMB", (float)0.6667);
            try
            {

            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("LMB resizing complete");
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

        private void export_Psarc_TBL_JSON_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Exporting TBL in JSON format");
            try
            {
                new RepackPsarcV2().exportTocJSON();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Export complete!");
        }

        private void export_Psarc_TBL_Binary_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Exporting TBL in JSON format");
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
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Export complete!");
        }

        private void repackPsarc_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Starts Repack Psarc to: " + Properties.Settings.Default.OutputRepackPsarc);
            try
            {
                new RepackPsarcV2().repackPsarc(Properties.Settings.Default.PsarcOutputFileName);
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Psarc Repack complete!");
        }

        private void Modify_Unit_Script_Link(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Linking Script from: " + Properties.Settings.Default.CScriptFilePath + " with BABB from " + Properties.Settings.Default.BABBFilePath);
            try
            {
                new ModifyUnitScript();
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

        private void Generate_Melee_Var_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Generating Melee Var from: " + Properties.Settings.Default.inputMeleeVarBinaryPath);
            try
            {
                new Parse_Melee_Variables();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Melee Var Generate Complete!");
        }

        private void Deserialize_Voice_Logic_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Deserializing Voice Logic");
            try
            {
                new Parse_Voice_Line_Logic().deserializeVoiceLogicBinary();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Voice Logic deserialize operation complete");
        }

        private void Serialize_Voice_Logic_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Serializing Voice Logic");
            try
            {
                new Parse_Voice_Line_Logic().serializeVoiceLogicBinary();
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Voice Logic serialize operation complete");
        }
        private void Export_Nus3_Name_and_Hash_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Exporting Nus3 Name and Hash");
            try
            {
                new nus3AudioNameHash((audioFormatEnum)Properties.Settings.Default.Nus3SoundHashFormat, Properties.Settings.Default.soundHashMainTitle);
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Nus3 Name and Hash export complete");

        }
        private void Export_Audio_PAC_Info_Click(object sender, RoutedEventArgs e)
        {
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Exporting Audio PAC Info");
            try
            {
                new GenerateAudioPACInfo((audioFormatEnum)Properties.Settings.Default.audioPACInfoNus3SoundHashFormat);
                //((audioFormatEnum)Properties.Settings.Default.Nus3SoundHashFormat, Properties.Settings.Default.soundHashMainTitle);
            }
            catch (Exception exp)
            {
                debugMessageBox.AppendText(Environment.NewLine);
                debugMessageBox.AppendText("Error: " + exp + "." + @"\n Please restart the application.");
            }
            debugMessageBox.AppendText(Environment.NewLine);
            debugMessageBox.AppendText("Audio PAC Info export complete");
        }

        private void Debug_Click_2(object sender, RoutedEventArgs e)
        {
            new Tools.recompilescript();
        }

        private void Debug_Click(object sender, RoutedEventArgs e)
        {
            //new GenerateAudioPACInfo();
            //new ParseEFP();
            //new Parse_Unit_Data().combineDataHashSchema();

            new Tools.recompilescript();
            // new Tools.Reverse32ByteEndian();
            //new Tools.ReimportAllMBON();
            //new Tools.MBONExport();
            // new Tools.BlankTemplate();

            //new RepackPsarcV2().exportTocJSON();

            //new Parse_Unit_Files_List();
            //new FBRepacker.Tools.MBONExport();
            //new FBRepacker.Tools.simpleExtract();

            //new nus3AudioNameHash();
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
