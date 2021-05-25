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
        RepackPsarc repackPsarc = new RepackPsarc(Properties.Settings.Default.PsarcRepackFolder);
        public static uint totalFileCount { get; set; }
        public string outputFileName { get; set; }

        public PsarcFileInfo(string outputFileName)
        {
            InitializeComponent();
            this.DataContext = this;
            this.outputFileName = outputFileName;
            psarcInfolv.ItemsSource = repackPsarc.TBL.fileInfos;
            totalFileCount = repackPsarc.TBL.totalFileCount;
        }

        private void Add_File_Button_Click(object sender, RoutedEventArgs e)
        {
            PACFileInfo newInfo = new PACFileInfo();
            uint lastRelativePathIndex = repackPsarc.TBL.fileInfos.Where(s => s.fileFlags.HasFlag(PACFileInfo.fileFlagsEnum.hasFileName)).Last().relativePathIndex;
            uint totalFileCount = repackPsarc.TBL.totalFileCount;
            PACFileInfoUI PACFileInfoEdit = new PACFileInfoUI(newInfo, lastRelativePathIndex, totalFileCount);
            bool? save = PACFileInfoEdit.ShowDialog();
            if (save == true)
            {
                repackPsarc.TBL.fileInfos.Add(newInfo);

                if (newInfo.fileInfoIndex > totalFileCount - 1)
                    repackPsarc.TBL.totalFileCount++;
            }
            psarcInfolv.Items.Refresh();
        }

        private void Export_Psarc_Button_Click(object sender, RoutedEventArgs e)
        {
            repackPsarc.exportToc();
            repackPsarc.repackPsarc(outputFileName);
            DialogResult = true;
        }

        private void Edit_File_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = ((sender as Button)?.Tag as ListViewItem)?.DataContext;
            var itemId = (item as PACFileInfo)?.nameHash;
            int index = repackPsarc.TBL.fileInfos.FindIndex(s => s.nameHash == itemId);
            PACFileInfo selectedInfo = repackPsarc.TBL.fileInfos[index];
            PACFileInfo backupInfo = (PACFileInfo)selectedInfo.Clone();
            uint lastRelativePathIndex = repackPsarc.TBL.fileInfos.Where(s => s.fileFlags.HasFlag(PACFileInfo.fileFlagsEnum.hasFileName)).Last().relativePathIndex;
            uint totalFileCount = repackPsarc.TBL.totalFileCount;
            PACFileInfoUI PACFileInfoEdit = new PACFileInfoUI(selectedInfo, lastRelativePathIndex, totalFileCount);
            bool? save = PACFileInfoEdit.ShowDialog();

            if (save == false)
            {
                repackPsarc.TBL.fileInfos[index] = backupInfo;    
            }
            else
            {
                if (selectedInfo.fileInfoIndex > totalFileCount - 1)
                    repackPsarc.TBL.totalFileCount++;

                repackPsarc.TBL.fileInfos[index] = selectedInfo;
            }

            psarcInfolv.Items.Refresh();
        }

        private void Delete_File_Button_Click(object sender, RoutedEventArgs e)
        {

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
