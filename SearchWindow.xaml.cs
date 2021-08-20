using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace FileSizeCalcWPF
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        public SearchWindow(List<CAHFile> files)
        {
            InitializeComponent();

            AllFiles = files;
            DisplayedFiles = AllFiles;

            ResetFilesListView();
        }

        public List<CAHFile> AllFiles { get; set; }
        public List<CAHFile> DisplayedFiles { get; set; }

        private void FilesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FilesListView.SelectedItem != null)
                Process.Start(((CAHFile)FilesListView.SelectedItem).FileFull);
        }

        private void SearchValueTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            string searchString = SearchValueTextBox.Text;
            DisplayedFiles = new List<CAHFile>();

            foreach (CAHFile file in AllFiles)
                if (file.FileName.Contains(searchString))
                    DisplayedFiles.Add(file);

            ResetFilesListView();
        }

        private void ResetFilesListView()
        {
            FilesListView.ItemsSource = null;
            FilesListView.ItemsSource = DisplayedFiles;
        }
    }
}
