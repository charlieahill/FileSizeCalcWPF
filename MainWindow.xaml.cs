using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileSizeCalcWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DefaultPath = @"C:\";
            CleverStartButton.Content = $"Analyse {DefaultPath}";
        }

        string DefaultPath;
        CAHDirectory baseDirectory;

        private void CleverStartButton_Click(object sender, RoutedEventArgs e)
        {
            //Compile the directory data
            baseDirectory = new CAHDirectory(DefaultPath, 0, DefaultPath);
            BackgroundGetDirData(baseDirectory);
        }

        private void BackgroundGetDirData(CAHDirectory baseDirectory)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.RunWorkerAsync(baseDirectory);         
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            GetDirData(baseDirectory);
        }
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AddNewDirectoryToButtons(baseDirectory, true, true);
            //Output the directory data to the screen
            OutputDirData(baseDirectory, FilesListView);
        }

        public void GetDirData(CAHDirectory parentDirectory)
        {
            //Add all files that are within that directory
            DirectoryInfo d = new DirectoryInfo(parentDirectory.DirectoryFull);

            try
            {
                DirectoryInfo[] directoryInfos = d.GetDirectories();
                foreach (DirectoryInfo directory in directoryInfos)
                    parentDirectory.directories.Add(new CAHDirectory(directory.Name, 0, directory.FullName));

                //Add all directories that are within that directory
                foreach (FileInfo file in d.GetFiles())
                {
                    try
                    {
                        parentDirectory.files.Add(new CAHFile(file.Name, file.Length, file.FullName));
                    }
                    catch (Exception ex)
                    {
                        parentDirectory.files.Add(new CAHFile(file.Name, 0, file.FullName));
                        Console.WriteLine("Error: " + ex.ToString());
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }

            //Iterate through each of those directories
            foreach (CAHDirectory directory in parentDirectory.directories)
                GetDirData(directory);

            //Sum the individual filesizes and copy to the parentdirectory size
            parentDirectory.DirectorySize = parentDirectory.files.Select(x => x.FileSize).Sum();

            //Add in the sum of all subdirectories
            parentDirectory.DirectorySize += parentDirectory.directories.Select(x => x.DirectorySize).Sum();
        }

        public void OutputDirData(CAHDirectory directory, ListView outputListView)
        {
            //Clear any existing entries
            outputListView.Items.Clear();

            //Sorting by size
            List<CAHDirectory> outputDirectories = directory.directories.OrderByDescending(x => x.DirectorySize).ToList();

            //Add each directory to the listview
            foreach (CAHDirectory subDirectory in outputDirectories)
            {
                subDirectory.Percentage = Math.Round(100.0 * subDirectory.DirectorySize / directory.DirectorySize, 2);
                Console.WriteLine($"{subDirectory} {subDirectory.DirectorySize}/{directory.DirectorySize}");
                FilesListView.Items.Add(subDirectory);
            }

            //Sorting by size
            List<CAHFile> outputFiles = directory.files.OrderByDescending(x => x.FileSize).ToList();

            //Add each file to the listview
            foreach (CAHFile subFile in outputFiles)
            {
                Console.WriteLine(subFile);
                FilesListView.Items.Add(subFile);
            }
        }

        private void FilesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //try and cast selected item to CAH directory
            CAHDirectory directory;

            try
            {
                directory = (CAHDirectory)((ListView)sender).SelectedItem;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }

            Console.WriteLine($"Selected CAHDirectory double click: {directory}");
            AddNewDirectoryToButtons(directory);
            OutputDirData(directory, FilesListView);
        }

        private void AddNewDirectoryToButtons(CAHDirectory directory, bool ClearButtons = false, bool ShowFullPath = false)
        {
            if (ClearButtons)
                DirectoryLinkStackPanel.Children.Clear();

            Button newDirectoryButton = new Button();

            if (ShowFullPath)
                newDirectoryButton.Content = directory.DirectoryFull;
            else
                newDirectoryButton.Content = directory.DirectoryName;

            newDirectoryButton.Click += NewButton_Click;

            DirectoryLinkStackPanel.Children.Add(newDirectoryButton);
        }

        /// <summary>
        /// If user clicks one of the directory buttons, go back to showing that directory
        /// </summary>
        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            //Output the name of the button clicked
            string ClickedButtonText = ((Button)sender).Content.ToString();
            Console.WriteLine(ClickedButtonText);

            //Check if the route directory was clicked (different rendering!)
            if(ClickedButtonText == baseDirectory.DirectoryFull)
            {
                //display the base directory folder
                OutputDirData(baseDirectory, FilesListView);
                AddNewDirectoryToButtons(baseDirectory, true, true);
                return;
            }

            //Find the directory that relates to the button that was clicked
            int iterationLevel = 1;
            CAHDirectory directory = baseDirectory;
            bool reachedEnd = false;

            do
            {
                foreach (CAHDirectory directoryIteration in directory.directories)
                {
                    Button TestButton = DirectoryLinkStackPanel.Children[iterationLevel] as Button;
                    if(TestButton.Content.ToString() == directoryIteration.DirectoryName)
                    {
                        Console.WriteLine("MATCH!");
                        directory = directoryIteration;
                        iterationLevel += 1;
                        Console.WriteLine($"ITERATION LEVEL: {iterationLevel}");
                        reachedEnd = true;
                        continue;
                    }

                    if (reachedEnd)
                        continue;
                }
            } while (directory.DirectoryName != ClickedButtonText);

            OutputDirData(directory, FilesListView);
            DirectoryLinkStackPanel.Children.RemoveRange(iterationLevel, DirectoryLinkStackPanel.Children.Count - iterationLevel);
        }

        private void RootPathSelector_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                DefaultPath = dialog.SelectedPath;

            CleverStartButton.Content = $"Analyse {DefaultPath}";
        }
    }

    public class CAHDirectory
    {
        public string DirectoryName { get; set; }
        public string DirectoryFull { get; set; }
        public long DirectorySize { get; set; }
        public double Percentage { get; set; } = 0;
        public List<CAHDirectory> directories { get; set; } = new List<CAHDirectory>();
        public List<CAHFile> files { get; set; } = new List<CAHFile>();
        public CAHDirectory(string name, long size, string full)
        {
            DirectoryName = name;
            DirectorySize = size;
            DirectoryFull = full;
        }
        public override string ToString()
        {
            return $"{FormatFileSize(DirectorySize)} | {DirectoryName} ({Percentage}%)";
        }

        private static string FormatFileSize(long v)
        {
            if (v < 1024)
                return v + "B";
            v /= 1024;

            if (v < 1024)
                return v + "KB";
            v /= 1024;

            if (v < 1024)
                return v + "MB";
            v /= 1024;

            return v + "GB";
        }
    }

    public class CAHFile
    {
        public string FileName { get; set; }
        public string FileFull { get; set; }
        public long FileSize { get; set; }
        public CAHFile(string name, long size, string full)
        {
            FileName = name;
            FileSize = size;
            FileFull = full;
        }
        public override string ToString()
        {
            return $"¬{FileName} | {FormatFileSize(FileSize)}";
        }

        private static string FormatFileSize(long v)
        {
            if (v < 1024)
                return v + "B";
            v /= 1024;

            if (v < 1024)
                return v + "KB";
            v /= 1024;

            if (v < 1024)
                return v + "MB";
            v /= 1024;

            return v + "GB";
        }
    }
}
