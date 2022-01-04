using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
//using static FBRepacker.Psarc.PACFileInfo;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Buffers.Binary;
using System.Text.RegularExpressions;
using FBRepacker.Psarc.V2;
using static FBRepacker.Psarc.V2.PACFileInfoV2;

namespace FBRepacker.Psarc
{
    /// <summary>
    /// Interaction logic for PACFileInfoUI.xaml
    /// </summary>
    public partial class PACFileInfoUI : Window
    {
        public PACFileInfoV2 pacFileInfo { get; set; }

        public bool hasPath { get; set; }

        private uint lastRelativePathIndex { get; set; }
        public uint currentRelativePathIndex { get;set; }

        private uint totalFileCount { get; set; }

        public PACFileInfoUI(PACFileInfoV2 pacFileInfo, uint lastRelativePathIndex, uint totalFileCount, uint currentRelativePathIndex)
        {
            InitializeComponent();
            this.pacFileInfo = pacFileInfo;
            this.DataContext = this;
            this.totalFileCount = totalFileCount;
            bool hasPath = false;
            if (pacFileInfo.filePath != null)
                hasPath = true;
            this.hasPath = hasPath;
            this.lastRelativePathIndex = lastRelativePathIndex;
            this.currentRelativePathIndex = currentRelativePathIndex;
            patchNoCB.ItemsSource = Enum.GetValues(typeof(patchNoEnum)).Cast<patchNoEnum>();
            prefixCB.ItemsSource = Enum.GetValues(typeof(prefixEnum)).Cast<prefixEnum>();
        }

        private void NameHashInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int hexNumber;
            e.Handled = !int.TryParse(e.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hexNumber);
        }

        private void Get_File_Size_Click(object sender, RoutedEventArgs e)
        {
            string filePath = filePathUI.Text;
            if (!File.Exists(filePath))
            {
                System.Windows.Forms.MessageBox.Show("No file path selected! / File access denied!", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            else
            {
                FileInfo file = new FileInfo(filePath);
                uint size = (uint)file.Length;
                Size1Input.Text = size.ToString();
                Size2Input.Text = size.ToString();
                Size3Input.Text = size.ToString();
            }
        }

        private void Save_Name_Hash_Click(object sender, RoutedEventArgs e)
        {
            uint res;
            string str = NameHashInput.Text.ToLower();
            try
            {
                res = uint.Parse(str, System.Globalization.NumberStyles.HexNumber);
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Not a valid Int32 hexadecimal! Resetting to 0", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                res = 0;
            }
            
            pacFileInfo.nameHash = res;
            concatName();
        }

        private void Save_File_Index_Click(object sender, RoutedEventArgs e)
        {
            int res;
            string str = IndexInput.Text.ToLower();
            try
            {
                res = int.Parse(str);
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Not a valid Int32! Resetting to 0", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                res = 0;
            }

            pacFileInfo.fileFlags |= fileFlagsEnum.hasFileInfo;
            pacFileInfo.fileInfoIndex = res;
        }

        private void Save_Relative_Path_Index_Click(object sender, RoutedEventArgs e)
        {
            uint res;
            string str = relativePathIndexInput.Text.ToLower();
            try
            {
                res = uint.Parse(str);
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Not a valid Int32! Resetting to 0", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                res = 0;
            }

            pacFileInfo.fileFlags |= fileFlagsEnum.hasFileName;
            //pacFileInfo.relativePathIndex = res;
        }

        private void Save_Sub_Directory_Click(object sender, RoutedEventArgs e)
        {
            if(relativeSubPathInput.Text.Length > 0)
            {
                if(!relativeSubPathInput.Text.Contains(" "))
                {
                    string str = pacFileInfo.relativePatchPath;

                    string fileName = Path.GetFileName(str);

                    Match m = Regex.Match(str, @"^patch_0\d_00");
                    if (!m.Success)
                        throw new Exception("Relative patch path does not start in patch_0x_00 format!");

                    string patchStr = str.Substring(0, 11);

                    string convertedSubPath = relativeSubPathInput.Text;
                    if (convertedSubPath.StartsWith("/") || convertedSubPath.StartsWith(@"\"))
                        convertedSubPath = convertedSubPath.Remove(0, 1);

                    string newRelativePatchPath = Path.Combine(new string[] { patchStr, convertedSubPath, fileName }).Replace(@"\", "/");

                    pacFileInfo.relativePatchPath = newRelativePatchPath;
                    relativePathShow.Text = pacFileInfo.relativePatchPath;
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("File path cannot have spaces!", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Cannot be empty", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void Save_Extension_Click(object sender, RoutedEventArgs e)
        {
            if (extensionInput.Text.Length > 0)
            {
                if (!extensionInput.Text.Contains(" "))
                {
                    string str = pacFileInfo.relativePatchPath;

                    string fileNamewoExt = Path.GetFileNameWithoutExtension(str);
                    string dir = Path.GetDirectoryName(str);

                    Match m = Regex.Match(str, @"^patch_0\d_00");
                    if (!m.Success)
                        throw new Exception("Relative patch path does not start in patch_0x_00 format!");

                    string extension = extensionInput.Text;

                    if (!extension.StartsWith("."))
                        extension = "." + extension;

                    fileNamewoExt += extension;

                    string newRelativePatchPath = Path.Combine(new string[] { dir, fileNamewoExt }).Replace(@"\", "/");

                    pacFileInfo.hasRelativePatchSubPath = true;
                    pacFileInfo.relativePatchPath = newRelativePatchPath;
                    relativePathShow.Text = pacFileInfo.relativePatchPath;
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("File path cannot have spaces!", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Cannot be empty", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void Get_Last_Relative_Path_Index_Click(object sender, RoutedEventArgs e)
        {
            relativePathIndexInput.Text = (lastRelativePathIndex + 1).ToString();
        }

        private void Find_Name_Hash_Index_Click(object sender, RoutedEventArgs e)
        {
            List<byte[]> DATATBLs = new List<byte[]> { 
                Properties.Resources.DATA, 
                Properties.Resources._01_PATCH, 
                Properties.Resources._02_PATCH, 
                Properties.Resources._03_PATCH, 
                Properties.Resources._04_PATCH, 
                Properties.Resources._05_PATCH,
                Properties.Resources._06_PATCH,
            };
            bool found = false;
            foreach (byte[] DATATBL in DATATBLs)
            {
                int index = searchTBLIndex(DATATBL);
                if(index != -1)
                {
                    IndexInput.Text = index.ToString();
                    found = true;
                    break;
                }
            }

            if(!found) // If it reach here it means that no index has been found
                System.Windows.Forms.MessageBox.Show("Cannot find Index for nameHash " + pacFileInfo.nameHash.ToString("X8") + " !", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        }

        private void Save_PAC_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Discard_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Change_File_Click(object sender, RoutedEventArgs e)
        {
            // Open file select dialog
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string initPath = pacFileInfo.fileFlags.HasFlag(fileFlagsEnum.hasFilePath) ? pacFileInfo.filePath : System.IO.Path.GetDirectoryName(Properties.Settings.Default.PsarcRepackFolder);
            string filePath = initPath;
            openFileDialog.InitialDirectory = filePath;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            filePath = openFileDialog.FileName;
            if (File.Exists(filePath))
            {
                pacFileInfo.fileFlags |= fileFlagsEnum.hasFilePath;
                pacFileInfo.filePath = openFileDialog.FileName;
                filePathUI.Text = openFileDialog.FileName;
            }
            
            Properties.Settings.Default.Save();
        }

        private void hasSubDir_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            bool has = pacFileInfo.hasRelativePatchSubPath;
            string str = pacFileInfo.relativePatchPath;
            if (!has)
            {
                Match m = Regex.Match(str, @"^patch_0\d_00");
                if (!m.Success)
                    throw new Exception("Relative patch path does not start in patch_0x_00 format!");

                string patchStr = str.Substring(0, 11);
                string fileName = Path.GetFileName(str);

                string redactedPath = Path.Combine(new string[] { patchStr, fileName }).Replace(@"\", "/");
                pacFileInfo.relativePatchPath = redactedPath;
                relativePathShow.Text = redactedPath;
            }
        }
        private void patchCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string relPath = pacFileInfo.relativePatchPath;

            Match m = Regex.Match(relPath, @"^patch_0\d_00");
            if (!m.Success)
                throw new Exception("Relative patch path does not start in patch_0x_00 format!");

            string patchStr = relPath.Substring(12);
            string finalStr = relPath;

            switch (pacFileInfo.patchNo)
            {
                case patchNoEnum.PATCH_1:
                    finalStr = Path.Combine(new string[] { "patch_01_00", patchStr });
                    break;
                case patchNoEnum.PATCH_2:
                    finalStr = Path.Combine(new string[] { "patch_02_00", patchStr });
                    break;
                case patchNoEnum.PATCH_3:
                    finalStr = Path.Combine(new string[] { "patch_03_00", patchStr });
                    break;
                case patchNoEnum.PATCH_4:
                    finalStr = Path.Combine(new string[] { "patch_04_00", patchStr });
                    break;
                case patchNoEnum.PATCH_5:
                    finalStr = Path.Combine(new string[] { "patch_05_00", patchStr });
                    break;
                case patchNoEnum.PATCH_6:
                    finalStr = Path.Combine(new string[] { "patch_06_00", patchStr });
                    break;
                default:
                    break;
            }
            finalStr = finalStr.Replace(@"\", "/");
            pacFileInfo.relativePatchPath = finalStr;
            relativePathShow.Text = finalStr;
        }

        private void prefixCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            concatName();
        }

        private void concatName()
        {
            string relPath = pacFileInfo.relativePatchPath;
            string directory = Path.GetDirectoryName(relPath);
            string fileExt = Path.GetExtension(relPath);
            string finalStr = relPath;
            switch (pacFileInfo.namePrefix)
            {
                case prefixEnum.NONE:
                    finalStr = Path.Combine(new string[] { directory, pacFileInfo.nameHash.ToString("X8") + fileExt });
                    break;
                case prefixEnum.PATCH:
                    finalStr = Path.Combine(new string[] { directory, "PATCH" + pacFileInfo.nameHash.ToString("X8") + fileExt });
                    break;
                case prefixEnum.STREAM:
                    finalStr = Path.Combine(new string[] { directory, "STREAM" + pacFileInfo.nameHash.ToString("X8") + fileExt });
                    break;
                default:
                    break;
            }
            finalStr = finalStr.Replace(@"\", "/");
            pacFileInfo.relativePatchPath = finalStr;
            relativePathShow.Text = finalStr;
        }

        private int searchTBLIndex(byte[] TBL)
        {
            MemoryStream TBLS = new MemoryStream(TBL);
            byte[] nameHashBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(pacFileInfo.nameHash));
            int nameHashPosition = Search(TBL, nameHashBytes);

            if(nameHashPosition != -1)
            {
                int fileInfoPosition = nameHashPosition - 0x1C;
                byte[] fileInfoPositionBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(fileInfoPosition));
                int indexPosition = Search(TBL, fileInfoPositionBytes);

                if (indexPosition == -1)
                    return -1;

                byte[] temp = new byte[4];
                TBLS.Seek(0x8, SeekOrigin.Begin);
                TBLS.Read(temp, 0, 4);
                int skipRange = BinaryPrimitives.ReadInt32BigEndian(temp) * 4;

                int startingPoint = skipRange + 0x10;

                int index = (indexPosition - startingPoint) / 4;

                return index;
            }
            else
            {
                return -1;
            }
        }

        public int Search(byte[] src, byte[] pattern)
        {
            int c = src.Length - pattern.Length + 1;
            int j;
            for (int i = 0; i < c; i++)
            {
                if (src[i] != pattern[0]) continue;
                for (j = pattern.Length - 1; j >= 1 && src[i + j] == pattern[j]; j--) ;
                if (j == 0) return i;
            }
            return -1;
        }

        private void getLastIndex(object sender, RoutedEventArgs e)
        {
            IndexInput.Text = (totalFileCount).ToString();
        }
    }

    public class getPatchSubDirectoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(value != null)
            {
                string filePath = value.ToString();
                string directory = Path.GetDirectoryName(filePath);
                Match m = Regex.Match(directory, @"^patch_0\d_00");
                if (!m.Success)
                    throw new Exception("Relative patch path does not start in patch_0x_00 format!");
                directory = directory.Remove(0, 11);
                if(directory != "")
                {
                    string subDirectoryPath = directory.Replace(@"\", "/");
                    if (subDirectoryPath.StartsWith("/") || subDirectoryPath.StartsWith(@"\"))
                        subDirectoryPath = subDirectoryPath.Remove(0, 1);
                    return subDirectoryPath;
                }
                else
                {
                    return directory;
                }
            }
            else
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class getPatchExtensionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                string filePath = value.ToString();
                Match m = Regex.Match(filePath, @"^patch_0\d_00");
                if (!m.Success)
                    throw new Exception("Relative patch path does not start in patch_0x_00 format!");

                string extension = Path.GetExtension(filePath);
                if (extension != "")
                {
                    string extensionFinal = extension.Replace(@"\", "/");
                    if (extensionFinal.StartsWith("/") || extensionFinal.StartsWith(@"\"))
                        extensionFinal = extensionFinal.Remove(0, 1);
                    return extensionFinal;
                }
                else
                {
                    return extension;
                }
            }
            else
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
