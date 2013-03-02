using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace Dota2ChatInterface
{
    public partial class App : Application
    {
        // The AppData folder to save data to.
        public const String AppDataFolder = "Dota 2 Translator";

        // The stream the console will write to.
        private StreamWriter ConsoleOutput;

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

            base.OnStartup(e);
        }

    }
}
