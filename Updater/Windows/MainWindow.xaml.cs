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
using System.Windows.Navigation;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Updater
{
    public partial class MainWindow : Window
    {
        // Delegates used to update the UI from another thread.
        private delegate void UpdateUIDelegate_String(String data);
        private delegate void UpdateUIDelegate_Int(int data);
        private delegate void VoidDelegate();

        public MainWindow()
        {
            InitializeComponent();

            // Register window loaded listener.
            Loaded += MainWindow_Loaded;
        }

        // Called when the window has been loaded.
        private void MainWindow_Loaded(object sender, EventArgs args)
        {
            // Wait for the main application to exit.
            Thread t = new Thread(new ThreadStart(WaitForExit));
            t.IsBackground = true;
            t.Start();
        }

        // Waits for the main application to exit.
        private void WaitForExit()
        {
            // Wait until there's no instance of the program running.
            Process[] running;
            while ((running = Process.GetProcessesByName("Dota2ChatInterface.exe")).Length > 0)
            {
                Thread.Sleep(100);
            }

            // Start updating.
            Update();
        }

        // Updates the program.
        public void Update()
        {
            // Retrieve the install directory.
            String installationDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Check what files needs to be updated.
            String[] needingUpdate = UpdateChecker.CheckForUpdates();

            // Update the status.
            ShowDownloadingUI(needingUpdate.Length);
            UpdateStatus("Downloading files (1 of " + needingUpdate.Length + ")");
            
            // Download all files.
            FileDownloader[] files = FileDownloader.GetFiles(needingUpdate);
            int progress = 0;
            foreach (FileDownloader file in files)
            {
                // Download the file.
                Boolean success = file.Download();
                if (!success)
                    // Indicate that the file failed to download.
                    MessageBox.Show("The file " + file.FileName + " failed to download!\n\nMake sure that you have a valid internet connection and your antivirus is not blocking the file.", "An error occurred");

                progress++;

                // Update the progress.
                UpdateProgress(progress);
                UpdateStatus("Downloading files (" + progress + " of " + needingUpdate.Length + ")");
            }

            // Apply all files.
            UpdateStatus("Applying update");
            foreach (FileDownloader file in files)
            {
                // Copy the temporary file to the installation directory.
                Boolean success = file.Apply(installationDirectory);
                if (!success)
                {
                    // Indicate that there has been an error.
                    MessageBox.Show("Failed to copy " + file.FileName + "!\n\nMake sure you're running as an administrator, close all running instances of Dota 2 and try again.", "An error occurred");
                }

                // Remove the temporary file.
                file.Cleanup();
            }

            // Alert the user that the download finished.
            UpdateStatus("Done!");

            try
            {
                // Create a bat script used to update this executable.
                MakeUpdaterBat();
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processInfo.FileName = "updaterpatcher.bat";

                // Run the bat script.
                Process.Start("updaterpatcher.bat");
            }
            catch
            {
            }

            // Close the window.
            CloseWindow();
        }

        // Changes the progress bar to reflect the download progress.
        public void ShowDownloadingUI(int numFiles)
        {
            // Run on UI thread.
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(Delegate.CreateDelegate(typeof(UpdateUIDelegate_Int), this, typeof(MainWindow).GetMethod("ShowDownloadingUI")), new object[] { numFiles });
                return;
            }

            Progress.IsIndeterminate = false;
            Progress.Maximum = numFiles;
            Progress.Value = 0;
        }

        // Changes the status text.
        public void UpdateStatus(String status)
        {
            // Run on UI thread.
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(Delegate.CreateDelegate(typeof(UpdateUIDelegate_String), this, typeof(MainWindow).GetMethod("UpdateStatus")), new object[] { status });
                return;
            }

            Status.Content = status;
        }

        // Changes the current progress.
        public void UpdateProgress(int progress)
        {
            // Run on UI thread.
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(Delegate.CreateDelegate(typeof(UpdateUIDelegate_Int), this, typeof(MainWindow).GetMethod("UpdateProgress")), new object[] { progress });
                return;
            }

            Progress.Value = progress;
        }

        // Close the window.
        public void CloseWindow()
        {
            // Run on UI thread.
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(Delegate.CreateDelegate(typeof(VoidDelegate), this, typeof(MainWindow).GetMethod("Close")), new object[] { });
                return;
            }

            Close();
        }

        // Creates a bat which updates Updater.exe (this executable).
        private void MakeUpdaterBat()
        {
            // The script to write. Replaces Updater.exe with tmp_Updater.exe.
            String script = "@echo off" + "\n\n"
                           + "if NOT EXIST tmp_Updater.exe goto NOUPDATE" + "\n\n"
                           + ":LOOP" + "\n"
                           + "set count=0" + "\n"
                           + "for /f \"usebackq\" %%A in (`tasklist /fo list /fi \"imagename eq Updater.exe\"`) do set /a count+=1" + "\n"
                           + "if %count% GTR 1 (" + "\n"
                           + "Sleep 1" + "\n"
                           + "goto LOOP" + "\n"
                           + ")" + "\n\n"
                           + "MOVE /Y tmp_Updater.exe Updater.exe" + "\n\n"
                           + ":NOUPDATE" + "\n"
                           + "if EXIST Dota2ChatInterface.exe start Dota2ChatInterface.exe";

            // Write the bat script to a file.
            StreamWriter streamWriter = null;
            try
            {
                streamWriter = new StreamWriter("updaterpatcher.bat");
                streamWriter.Write(script);
            }
            finally
            {
                if (streamWriter != null)
                    streamWriter.Close();
            }
        }
    }
}
