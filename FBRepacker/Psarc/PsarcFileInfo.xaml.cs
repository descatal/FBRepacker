using FBRepacker.Psarc.V2;
using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace FBRepacker.Psarc
{
    /// <summary>
    /// Interaction logic for PsarcFileInfo.xaml
    /// </summary>
    public partial class PsarcFileInfo : Window
    {
        //RepackPsarc repackPsarc = new RepackPsarc(Properties.Settings.Default.PsarcRepackFolder);
        TOCFileInfo tocFileInfo;
        RepackPsarcV2 repackPsarcV2 = new RepackPsarcV2();
        public static uint totalFileCount { get; set; }
        public string outputFileName { get; set; }

        public PsarcFileInfo(string outputFileName)
        {
            InitializeComponent();
            this.DataContext = this;
            this.outputFileName = outputFileName;
            
            tocFileInfo = repackPsarcV2.importTocJSON();

            psarcInfolv.ItemsSource = tocFileInfo.allFiles;
            totalFileCount = tocFileInfo.totalFileEntries;
        }

        private void Add_File_Button_Click(object sender, RoutedEventArgs e)
        {
            PACFileInfoV2 newInfo = new PACFileInfoV2();
            uint lastRelativePathIndex = (uint)tocFileInfo.allFiles.Count(); //tocFileInfo.allFiles.Where(s => s.fileFlags.HasFlag(PACFileInfo.fileFlagsEnum.hasFileName)).Last().relativePathIndex;
            uint totalFileCount = tocFileInfo.totalFileEntries;
            PACFileInfoUI PACFileInfoEdit = new PACFileInfoUI(newInfo, lastRelativePathIndex, totalFileCount, lastRelativePathIndex);
            bool? save = PACFileInfoEdit.ShowDialog();
            if (save == true)
            {
                tocFileInfo.allFiles.Add(newInfo);

                if (newInfo.fileInfoIndex > totalFileCount - 1)
                    tocFileInfo.totalFileEntries++;
            }
            psarcInfolv.Items.Refresh();
        }

        private void Export_Psarc_Button_Click(object sender, RoutedEventArgs e)
        {
            repackPsarcV2.exportToc(tocFileInfo);
            
            // repackPsarc.repackPsarc(outputFileName);
            DialogResult = true;
        }

        private void Edit_File_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = ((sender as Button)?.Tag as ListViewItem)?.DataContext;
            var itemId = (item as PACFileInfoV2)?.nameHash;
            int index = tocFileInfo.allFiles.FindIndex(s => s.nameHash == itemId);
            PACFileInfoV2 selectedInfo = tocFileInfo.allFiles[index];
            PACFileInfoV2 backupInfo = (PACFileInfoV2)selectedInfo.Clone();
            uint lastRelativePathIndex = (uint)tocFileInfo.allFiles.Count();
            uint totalFileCount = tocFileInfo.totalFileEntries;
            PACFileInfoUI PACFileInfoEdit = new PACFileInfoUI(selectedInfo, lastRelativePathIndex, totalFileCount, (uint)index);
            bool? save = PACFileInfoEdit.ShowDialog();

            if (save == false)
            {
                tocFileInfo.allFiles[index] = backupInfo;    
            }
            else
            {
                if (selectedInfo.fileInfoIndex > totalFileCount - 1)
                    tocFileInfo.totalFileEntries++;

                tocFileInfo.allFiles[index] = selectedInfo;
            }

            psarcInfolv.Items.Refresh();
        }

        private void Delete_File_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = ((sender as Button)?.Tag as ListViewItem)?.DataContext;
            var itemId = (item as PACFileInfoV2)?.nameHash;
            int index = tocFileInfo.allFiles.FindIndex(s => s.nameHash == itemId);

            System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Are you sure?", "Delete Entry", System.Windows.Forms.MessageBoxButtons.YesNo);
            if (dialogResult == System.Windows.Forms.DialogResult.Yes)
            {
                tocFileInfo.allFiles.RemoveAt(index);
            }
            else if (dialogResult == System.Windows.Forms.DialogResult.No)
            {
                //do something else
            }

            psarcInfolv.Items.Refresh();
        }

        private void sort_Button_Click(object sender, RoutedEventArgs e)
        {
            tocFileInfo.allFiles = tocFileInfo.allFiles.OrderBy(s => s.relativePatchPath).ToList();
            psarcInfolv.ItemsSource = tocFileInfo.allFiles;
            psarcInfolv.Items.Refresh();
        }
    }

    public class UIntToHexStrConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
            string str = value.ToString().ToLower();
            uint.TryParse(str, out uint res);
            return res.ToString("X8");
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
            string str = value.ToString().ToLower();
            try
            {
                uint res = uint.Parse(str, System.Globalization.NumberStyles.HexNumber);
                return res;
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Not a valid Int32 hexadecimal! Resetting to 0", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return 0;
            }
        }
	}
}
