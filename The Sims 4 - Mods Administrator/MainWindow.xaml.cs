using Microsoft.Win32;
using SharpCompress.Archives;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace The_Sims_4___Mods_Administrator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ScanButton.IsEnabled = false;
            ImportModsButton.IsEnabled = false;
        }

        //Declare strings...
        string SourcePath;
        string ModsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            + @"\Electronic Arts\The Sims 4\Mods\";

        // Declare counters...
        int FileCounterMax = 0;
        int FileCountCurrent = 0;
        int ArchiveFileCounter = 0;
        int ArchiveFileCounterMax = 0;
        long ImportModsCounter = 0;
        long ImportModsCounterMax = 0;

        // Create List<FileInfo> for files list...
        List<FileInfo> FilesList = new List<FileInfo>();
        List<FileInfo> ArchiveFilesList = new List<FileInfo>();
        List<FileInfo> FailedFilesList = new List<FileInfo>();

        // Declare bool for setting true/false...
        bool ProgressChangedStatus = true;

        /*/
         * ################################################################################################
         * ################################ Buttons functions section #####################################
         * ################################################################################################
         /*/

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Browse for patch to scan...
        private void BrowseSourceButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear listview and empty list...
            LogFileViewer.Items.Clear();
            FilesList.Clear();
            ArchiveFilesList.Clear();
            ProgressChangedStatus = true;

            // Reset counters...
            FileCounterMax = 0;
            FileCountCurrent = 0;
            ArchiveFileCounter = 0;
            ArchiveFileCounterMax = 0;

            // Create directory-browser dialog...
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            // Check browser dialog result..
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // Get source patch and update patch textbox...
                SourcePath = dialog.SelectedPath;
                SourceFolderTextBox.Text = SourcePath;
                ScanButton.IsEnabled = true;
            }
        }

        // ScanButton functions...
        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            // Change button before scan and set status...
            ScanButton.Content = "Scanner...";
            ScanButton.IsEnabled = false;
            UpdateProgressTextLabel("Status: Scan startet...");

            // Create BackgroundWorker for Archive Uncompressing Task...
            BackgroundWorker UncompressArchiveWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = true
            };
            UncompressArchiveWorker.DoWork += UncompressArchive_DoWork;
            UncompressArchiveWorker.ProgressChanged += UncompressArchive_ProgressChanged;
            UncompressArchiveWorker.RunWorkerCompleted += UncompressArchive_RunWorkerCompleted;
            UncompressArchiveWorker.RunWorkerAsync(1000);
        }

        // Mods Import button function...
        private void ImportModsButton_Click(object sender, RoutedEventArgs e)
        {
            // Set button settings before import...
            ImportModsButton.IsEnabled = false;
            ImportModsButton.Content = "Importerer...";

            // Clear listview...
            LogFileViewer.Items.Clear();

            // Reset Counters and failed list...
            ImportModsCounter = 0;
            ImportModsCounterMax = 0;
            FailedFilesList.Clear();

            // Create BackgroundWorker for Archive Uncompressing Task...
            BackgroundWorker ImportModsWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = true
            };
            ImportModsWorker.DoWork += ImportMods_DoWork;
            ImportModsWorker.ProgressChanged += ImportMods_ProgressChanged;
            ImportModsWorker.RunWorkerCompleted += ImportMods_RunWorkerCompleted;
            ImportModsWorker.RunWorkerAsync(1000);
        }

        /*/
         * ################################################################################################
         * ########################### Uncompress Archive Files section ###################################
         * ################################################################################################
         /*/

        private void UncompressArchive_DoWork(object sender, DoWorkEventArgs e)
        {
            // Set the file extensions to search for...
            string[] ArchiveExtensions = { "*.zip", "*.rar", "*.gzip", "*.7z" };

            // Run throgh the extensions and check for archive files...
            try
            {
                foreach (string ext in ArchiveExtensions)
                {
                    FileInfo[] folder = new DirectoryInfo(SourcePath).GetFiles(ext, SearchOption.AllDirectories);
                    
                    foreach (FileInfo file in folder)
                    {
                        if ((file.Attributes & FileAttributes.Directory) != 0) continue;
                        {
                            ArchiveFilesList.Add(file);
                            ArchiveFileCounterMax++;
                        }
                    }
                }

                if (ArchiveFilesList.Count != 0)
                {
                    foreach (var file in ArchiveFilesList)
                    {
                        ArchiveFileCounter++;
                        (sender as BackgroundWorker).ReportProgress(ArchiveFileCounter, file);
                        System.Threading.Thread.Sleep(5);
                    }

                    if (MessageBox.Show("Der blev fundet "
                        + ArchiveFileCounterMax
                        + " filer som ikke er udpakket. Ønsker du at udpakke disse filer nu?",
                        "Udpak filer?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        // Set current file counter to 0...
                        ArchiveFileCounter = 0;
                        ProgressChangedStatus = false;
                        Dispatcher.BeginInvoke((Action)(() => LogFileViewer.Items.Clear()));

                        // Extract the files...
                        foreach (var ArchiveFile in ArchiveFilesList)
                        {
                            // Open selected archive to extract...
                            using (var archive = ArchiveFactory.Open(ArchiveFile.FullName))
                            {
                                // Run throgh archive and extract files...
                                foreach (var entry in archive.Entries)
                                {
                                    if (!entry.IsDirectory)
                                    {
                                        entry.WriteToDirectory(ArchiveFile.DirectoryName + "\\", new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                                    }
                                }
                            }

                            ArchiveFileCounter++;
                            (sender as BackgroundWorker).ReportProgress(ArchiveFileCounter, ArchiveFile);
                            System.Threading.Thread.Sleep(50);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Der skete en fejl!");
            }
        }

        private void UncompressArchive_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Calculate and Send progress to ProgressBar...
            double ProgressValue = (((double)e.ProgressPercentage / ArchiveFileCounterMax) * 100);
            UpdateProgressBar1(ProgressValue);

            // Generate fileinfo from userstate...
            FileInfo ArchiveFileName = e.UserState as FileInfo;
            string[] Output = ArchiveFileName.Name.Split('.').ToArray();

            // Set progress status for file search...
            if (ProgressChangedStatus == true)
            {
                // Send output to fileviewer...
                UpdateTextView(new ProcessOutput() { Filename = Output[0], Type = ArchiveFileName.Extension.Replace(".", "") });

                // Send progress status text...
                UpdateProgressTextLabel("Scanner: " + ArchiveFileName.Name);
            }

            else
            {
                // Send output to fileviewer...
                UpdateTextView(new ProcessOutput() { Filename = Output[0], Type = ArchiveFileName.Extension.Replace(".", ""), Status = "Ok" });

                // Send progress status text...
                UpdateProgressTextLabel("Udpakker: " + ArchiveFileName.Name);
            }
        }

        private void UncompressArchive_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Reset ProgressBar Value...
            UpdateProgressBar1(0);

            // If Archive Files are been decompressed...
            if (ArchiveFilesList.Count != 0)
            {
                // Reset progressbar and update status text.
                UpdateProgressTextLabel("Status: Filerne blev udpakket");

                // Ask to delete the extracted archive files...
                if (MessageBox.Show("Alle arkiv filer blev udpakket. Ønsker du at slette de udpakkede arkiv filer?", "Filerne blev udpakket", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    DeleteFiles();
                }
            }

            else
            {
                UpdateProgressTextLabel("Status: Søger efter Mods og CC´s");
            }
            
            // Clear file listview...
            LogFileViewer.Items.Clear();

            // Create backgroudworker for directory processing...
            BackgroundWorker ProcessDirectoryWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = true
            };
            ProcessDirectoryWorker.DoWork += ProcessDirectory_DoWork;
            ProcessDirectoryWorker.ProgressChanged += ProcessDirectory_ProgressChanged;
            ProcessDirectoryWorker.RunWorkerCompleted += ProcessDirectory_RunWorkerCompleted;
            ProcessDirectoryWorker.RunWorkerAsync(1000);
        }

        private void DeleteFiles()
        {
            try
            {
                foreach (var file in ArchiveFilesList)
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Der skete en fejl under forsøget på at slette filerne. Fejlmeddelelse: " + ex.Message, "Der skete en fejl!");
            }
        }

        /*/
         * ################################################################################################
         * ############################# Directory Processing section #####################################
         * ################################################################################################
         /*/

        private void ProcessDirectory_DoWork(object sender, DoWorkEventArgs e)
        {
            // Reset counters and progress bar...
            FileCountCurrent = 0;
            FileCounterMax = 0;

            // Set the file extensions to search for...
            string[] ModExtensions = { "*.blueprint", "*.bpi", "*.package", "*.sims3pack", "*.trayitem" };

            // Check selected path for mods files and count...
            try
            {
                foreach (string ext in ModExtensions)
                {
                    FileInfo[] folder = new DirectoryInfo(SourcePath).GetFiles(ext, SearchOption.AllDirectories);

                    foreach (FileInfo file in folder)
                    {
                        if ((file.Attributes & FileAttributes.Directory) != 0) continue;
                        {
                            FilesList.Add(file);
                            FileCounterMax++;
                        }
                    }
                }

                // Process the mods file list...
                foreach (var file in FilesList)
                {
                    FileCountCurrent++;
                    (sender as BackgroundWorker).ReportProgress(FileCountCurrent, file);
                    System.Threading.Thread.Sleep(5);
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Der skete en fejl!");
            }
        }

        // Directory processing progress change events...
        private void ProcessDirectory_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Convert UserStatet and calculate progress...
            FileInfo fileName = e.UserState as FileInfo;
            double ProgressValue = (((double)e.ProgressPercentage / FileCounterMax) * 100);

            // Send progress to ProgressBar...
            UpdateProgressBar1(ProgressValue);

            // Update Progress TextLabel...
            UpdateProgressTextLabel("Processing: " + fileName.Name);

            // Bind Process Output to DataGrid...
            ProcessOutput processOutput = new ProcessOutput()
            {
                Filename = fileName.Name.Split('.').First(),
                Type = fileName.Extension.TrimStart('.')
            };

            // Update the fileviewer...
            UpdateTextView(processOutput);
        }

        // Directory processing work completed events....
        private void ProcessDirectory_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ScanButton.Content = "Scan";
            ScanButton.IsEnabled = false;
            ImportModsButton.IsEnabled = true;
            UpdateProgressTextLabel("Status: Klar...");
            UpdateProgressBar1(0);
            SourcePath = null;
            SourceFolderTextBox.Text = "Vælg den mappe du vil importere dine mods fra.";
        }

        /*/
         * ################################################################################################
         * ############################ Import Mods to gamefolder section #################################
         * ################################################################################################
         /*/

        private void ImportMods_DoWork(object sender, DoWorkEventArgs e)
        {
            // Count data amount of files to import...
            foreach (var FileToCount in FilesList)
            {
                ImportModsCounterMax += FileToCount.Length;
            }

            // Remove files to the game folder...
            try
            {
                foreach (var Mod in FilesList)
                {
                    // Check if file already exist on destination...
                    if (File.Exists(ModsFolderPath + Mod.Name))
                    {
                        File.Delete(ModsFolderPath + Mod.Name);
                    }
                    
                    // Move file to destination path...
                    Mod.MoveTo(ModsFolderPath + Mod.Name);

                    // Calculate Progress...
                    ImportModsCounter += Mod.Length;
                    double ProgessValue = (ImportModsCounter / ImportModsCounterMax) * 100;
                    (sender as BackgroundWorker).ReportProgress((int)ProgessValue, Mod.Name);

                    // Check if file not moved and add to failed List...
                    if (!File.Exists(ModsFolderPath + Mod.Name))
                    {
                        FailedFilesList.Add(Mod);
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Der skete en fejl!");
            }
        }

        private void ImportMods_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            UpdateProgressBar1(e.ProgressPercentage);
            UpdateProgressTextLabel("Overfører: " + e.UserState.ToString());
        }

        private void ImportMods_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateProgressTextLabel("Status: Gennemført...");
            UpdateProgressBar1(0);

            if (FailedFilesList.Count > 0)
            {
                foreach (var file in FailedFilesList)
                {
                    UpdateTextView(new ProcessOutput() { Filename = file.Name.Split('.').First(), Type = file.Extension, Status = "Fejlede" });
                }

                ImportModsButton.Content = "Forsøg igen";
                ImportModsButton.IsEnabled = true;
            }

            else
            {
                MessageBox.Show("Alle mods blev importeret med success.", "Færdig");
                ImportModsButton.Content = "Import";
                ImportModsButton.IsEnabled = false;
                FilesList.Clear();
            }
        }

        /*/
         * ################################################################################################
         * ############################ User interface update section #####################################
         * ################################################################################################
         /*/

        // Update log viewer text.
        private void UpdateTextView(ProcessOutput processOutput)
        {
            LogFileViewer.Items.Add(processOutput);
        }

        // Update ProgressBar1...
        private void UpdateProgressBar1(double Value)
        {
            ProgressBar1.Value = Value;
        }

        // Update Progress Text Label...
        private void UpdateProgressTextLabel(string ProgressText)
        {
            ProgressTextBox.Content = ProgressText;
        }
    }
}
