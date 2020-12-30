using FBRepacker.PAC.Repack.customFileInfo;
using FBRepacker.PAC.Repack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using FBRepacker.PAC;

namespace FBRepacker.PACInfoUI
{
    /// <summary>
    /// Interaction logic for PACInfoUI.xaml
    /// </summary>
    public partial class PACInfoWindow : Window
    {
        RepackPAC repackInstance;
        Dictionary<int, GeneralFileInfo> parsedFileInfo = new Dictionary<int, GeneralFileInfo>();

        public PACInfoWindow(RepackPAC repackInstance)
        {
            InitializeComponent();

            this.repackInstance = repackInstance;

            // TODO: remove this and use ViewModel
            repackInstance.initializePACInfoFileRepack();
            repackInstance.parseInfo();

            parsedFileInfo = repackInstance.parsedFileInfo;

            refreshTree();
        }

        private void refreshTree()
        {
            var PAC = new TreeViewItem();
            PAC.Header = "PAC";
            PAC.IsExpanded = true;

            TreeViewItem treeViewItems = addHierarchy(PAC, parsedFileInfo.First().Value);
            TreeView.Items.Add(treeViewItems);
        }

        private TreeViewItem addHierarchy(TreeViewItem treeViewItem, GeneralFileInfo fileInfo)
        {
            // Convert fileInfo into TreeViewItem
            TreeViewItem newItem = getTreeView(fileInfo);

            if (fileInfo.header == "fhm")
            {
                List<GeneralFileInfo> allChildFileInfos = parsedFileInfo.Values.Where(s => s.FHMFileNumber == fileInfo.fileNo).ToList();

                if (fileInfo.numberofFiles != allChildFileInfos.Count)
                    throw new Exception("number of child files in FHM dosen't match with the total FHM file number!");

                foreach (var childFileInfos in allChildFileInfos)
                {
                    TreeViewItem treeView = addHierarchy(newItem, childFileInfos);
                    //treeView.Header = childFileInfos.fileName;
                }

                treeViewItem.Items.Add(newItem);
            }
            else
            {
                treeViewItem.Items.Add(newItem);
            }

            return treeViewItem;
        }

        private TreeViewItem getTreeView(GeneralFileInfo fileInfo)
        {
            TreeViewItem item = new TreeViewItem();
            item.IsExpanded = true;

            // create stack panel
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            // Label
            Label lbl = new Label();
            lbl.Content = fileInfo.fileNo.ToString("000") + " - " + fileInfo.fileName;
            //stack.Children.Add(border);
            

            if (fileInfo.header == "NTP3" && !fileInfo.isLinked)
            {
                if (!repackInstance.repackNTP3.NTP3FileInfoDic.ContainsKey(fileInfo.fileNo))
                    throw new Exception("Cannot find fileNumber: " + fileInfo.fileNo + " in NTP3FileInfoDic!");

                lbl.Content = fileInfo.fileNo.ToString("000") + "-NTP3";

                var NTP3FileInfo = repackInstance.repackNTP3.NTP3FileInfoDic[fileInfo.fileNo];

                foreach(var NTP3File in NTP3FileInfo)
                {
                    TreeViewItem itemDDS = new TreeViewItem();
                    itemDDS.IsExpanded = true;

                    // create stack panel
                    StackPanel stackDDS = new StackPanel();
                    stackDDS.Orientation = Orientation.Horizontal;

                    // Label
                    Label lblDDS = new Label();
                    lblDDS.Content = NTP3File.fileName;

                    stackDDS.Children.Add(lblDDS);
                    itemDDS.Header = stackDDS;
                    itemDDS.Tag = NTP3File;

                    item.Items.Add(itemDDS);
                }
            }

            /*// create Image
            Border border = new Border();
            border.Width = 8;
            border.Height = 12;
            */

            stack.Children.Add(lbl);

            //item.HeaderTemplate.ad  
            item.Header = stack;
            item.Tag = fileInfo;
            return item;
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            TreeView.Items.Clear();
            refreshTree();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            newFileWindow newFileWindow = new newFileWindow();
            var completed = newFileWindow.ShowDialog();
            
            if(completed == true)
            {

            }
        }

        private void DeleteLast_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RebuildInfoButton_Click(object sender, RoutedEventArgs e)
        {
            repackInstance.initializePACInfoFileExtract();
            repackInstance.rebuildPACInfo(parsedFileInfo);
            repackInstance.writePACInfo();
        }

        private void RepackButton_Click(object sender, RoutedEventArgs e)
        {
            repackInstance.repackPAC();
            DialogResult = true;
        }

        private void isLinkedFile_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void addEIDXButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void addNTP3Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void addFHMButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem selectedItem = (TreeViewItem)TreeView.SelectedItem;

            try
            {
                GeneralFileInfo tag = (GeneralFileInfo)selectedItem.Tag;

                if (tag != null)
                {
                    fileNumberInput.Text = tag.fileNo.ToString();
                    fileNameInput.Text = tag.fileName;
                    headerInput.Text = tag.header;

                    FHMEnumSelect.SelectedIndex = tag.FHMAssetLoadEnum;

                    if (tag.isLinked)
                    {
                        isLinkedFile.IsChecked = true;
                        linkedtoFile.IsEnabled = true;
                        //linkedtoFile.Text = tag.linkedFileName;
                    }
                    else
                    {
                        isLinkedFile.IsChecked = false;
                        linkedtoFile.IsEnabled = false;
                    }
                }
            }
            catch
            {
                NTP3FileInfo tag = (NTP3FileInfo)selectedItem.Tag;

                GeneralFileInfo parentTag = (GeneralFileInfo)(GetSelectedTreeViewItemParent(selectedItem) as TreeViewItem).Tag;

                if (parentTag != null)
                {
                    fileNumberInput.Text = parentTag.fileNo.ToString();
                    fileNameInput.Text = parentTag.fileName;
                    headerInput.Text = parentTag.header;

                    FHMEnumSelect.SelectedIndex = parentTag.FHMAssetLoadEnum;

                    if (parentTag.isLinked)
                    {
                        isLinkedFile.IsChecked = true;
                        linkedtoFile.IsEnabled = true;
                        //linkedtoFile.Text = tag.linkedFileName;
                    }
                    else
                    {
                        isLinkedFile.IsChecked = false;
                        linkedtoFile.IsEnabled = false;
                    }
                }
            }

            

            

        }

        public ItemsControl GetSelectedTreeViewItemParent(TreeViewItem item)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            while (!(parent is TreeViewItem || parent is TreeView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as ItemsControl;
        }
    }
}
