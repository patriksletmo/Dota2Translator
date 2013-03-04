using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Security.Principal;
using System.Security.AccessControl;

namespace Dota2ChatInterface
{
    public partial class App : Application
    {
        // The AppData folder to save data to.
        public const String AppDataFolder = "Dota 2 Translator";

        // The stream the console will write to.
        private StreamWriter ConsoleOutput;

        // Delegate used to shutdown the application.
        private delegate void ShutdownDelegate();

        // Called when the application is starting.
        protected override void OnStartup(StartupEventArgs e)
        {
            // Construct a path leading to AppData/Roaming/[AppDataFolder].
            String path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolder);

            // Make sure it exists.
            Directory.CreateDirectory(path);

            // Append the file name to the path.
            path = System.IO.Path.Combine(path, "log.txt");

            // Redirect the console output.
            ConsoleOutput = new StreamWriter(new FileStream(path, FileMode.Create));
            ConsoleOutput.AutoFlush = true;
            Console.SetOut(ConsoleOutput);

            // Write down the start time of the application.
            Console.WriteLine("[Program started at {0}]", DateTime.Now.ToString("g"));

            // Check if an update is available.
            Thread updateThread = new Thread(new ThreadStart(CheckForUpdate));
            updateThread.IsBackground = true;
            updateThread.Start();

            base.OnStartup(e);
        }

        // Checks for updates and prompts the user to update if one is available.
        private void CheckForUpdate()
        {
            // Check for available updates on the server.
            Boolean hasUpdate = Updater.UpdateChecker.HasUpdate();
            if (hasUpdate)
            {
                // An update is available, inform the user.
                MessageBoxResult result = MessageBox.Show("There is an update available, do you want to update now?", "An update is available", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    // Start the updater.
                    ProcessStartInfo processInfo = new ProcessStartInfo();
                    processInfo.FileName = "Updater.exe";

                    // Request administrator privileges if the current installation cannot be written by the current user.
                    if (!HasWriteAccess("Dota2ChatInterface.exe"))
                    {
                        processInfo.Verb = "runas";
                    }
                    
                    // Start the process.
                    Process.Start(processInfo);
                    
                    // Quit the program.
                    Dispatcher.Invoke(Delegate.CreateDelegate(typeof(ShutdownDelegate), this, typeof(App).GetMethod("Shutdown", new Type[0])), new object[] { });
                }
            }
        }

        // Checks if the current user has write access to the file.
        private Boolean HasWriteAccess(String fileName)
        {
            // No user has access to edit read-only file.
            if ((File.GetAttributes(fileName) & FileAttributes.ReadOnly) != 0)
                return false;

            // Retrieve the access rules of the specified file.
            AuthorizationRuleCollection rules = File.GetAccessControl(fileName).GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));

            // Retrieve the groups the user is member in.
            IdentityReferenceCollection groups = WindowsIdentity.GetCurrent().Groups;

            // Retrieve the SecuityIdentifier of the current user.
            String sidCurrentUser = WindowsIdentity.GetCurrent().User.Value;

            // Check if writing is denied for the current user or group.
            if (rules.OfType<FileSystemAccessRule>().Any(r => (groups.Contains(r.IdentityReference) || r.IdentityReference.Value == sidCurrentUser) && r.AccessControlType == AccessControlType.Deny && (r.FileSystemRights & FileSystemRights.WriteData) == FileSystemRights.WriteData))
                return false;

            // Check if writing is allowed for the current user or group.
            return rules.OfType<FileSystemAccessRule>().Any(r => (groups.Contains(r.IdentityReference) || r.IdentityReference.Value == sidCurrentUser) && r.AccessControlType == AccessControlType.Allow && (r.FileSystemRights & FileSystemRights.WriteData) == FileSystemRights.WriteData);
        }

    }
}
