using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace FBRepacker
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            init();
        }

        private void init()
        {
            outputWAVCheck.IsChecked = Properties.Settings.Default.outputWAV;
            exportVBNCheck.IsChecked = Properties.Settings.Default.exportVBN;
        }

        private void exportVBNCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.exportVBN = true;
            Properties.Settings.Default.Save();
        }

        private void exportVBNCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.exportVBN = false;
            Properties.Settings.Default.Save();
        }

        private void outputWAVCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.outputWAV = true;
            Properties.Settings.Default.Save();
        }

        private void outputWAVCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.outputWAV = false;
            Properties.Settings.Default.Save();
        }
    }
}
