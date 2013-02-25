/*
Copyright (c) 2013 Patrik Sletmo

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace Dota2ChatInterface
{
    public partial class Settings : Window
    {
        // Local copy of the static SettingsHandler.
        private SettingsHandler LoadedSettings;

        public Settings()
        {
            InitializeComponent();

            // Register window loaded event.
            Loaded += Window_Loaded;
        }

        // Called when the window has been loaded.
        public void Window_Loaded(object sender, EventArgs args)
        {
            // Register button events
            SaveButton.Click += SaveButton_Click;
            CancelButton.Click += CancelButton_Click;
            DefaultFontButton.Click += DefaultFontButton_Click;

            // Register easter egg (spoiler?)
            FONT_NAME.TextChanged += FontName_TextChanged;

            // Load settings.
            LoadedSettings = SettingsHandler.CloneInstance();

            // Fill settings in input fields.
            EXE_NAME.Text = LoadedSettings.ExeName;
            FONT_NAME.Text = LoadedSettings.FontName;
            MESSAGES_SHOWN.Text = LoadedSettings.MessagesShown.ToString();
            TRANSLATE_TO.Text = LoadedSettings.TranslateTo;
            AUTO_HIDE.IsChecked = LoadedSettings.AutoHide;
            ADD_ON_STARTUP.IsChecked = LoadedSettings.AddOnStartup;
            OUTPUT_ALL.IsChecked = LoadedSettings.OutputAll;
        }

        // Called when SaveButton is clicked.
        public void SaveButton_Click(object sender, EventArgs args)
        {
            // Read values from the fields.
            StoreTemporarily();

            // Save to file.
            SettingsHandler.MergeAndSave(LoadedSettings);
            SettingsHandler.SendToOverlay();

            // Close the window.
            Close();
        }

        // Called when CancelButton is clicked.
        public void CancelButton_Click(object sender, EventArgs args)
        {
            // Close the window.
            Close();
        }

        // Called when DefaultFontButton is clicked.
        private void DefaultFontButton_Click(object sender, EventArgs args)
        {
            // Reset font name to default.
            FONT_NAME.Text = "Segoe UI";
        }

        // Called when the user (or the application) changes the font name.
        public void FontName_TextChanged(object sender, EventArgs args)
        {
            // Shows easter egg if Comic Sans MS is selected.
            String font = FONT_NAME.Text.Trim().ToLower();
            Boolean comicSans = font.Equals("comic sans ms");
            IsComicSans.Visibility = comicSans ? Visibility.Visible : Visibility.Collapsed;
            DefaultFontButton.Visibility = comicSans ? Visibility.Hidden : Visibility.Visible;
        }

        // Stores values in the SettingsHandler instance.
        private void StoreTemporarily()
        {
            LoadedSettings.ExeName = EXE_NAME.Text.Trim(); // One does not simply use whitespace right before or after the file extension.
            LoadedSettings.FontName = FONT_NAME.Text.Trim();
            LoadedSettings.TranslateTo = TRANSLATE_TO.Text.Trim();
            LoadedSettings.AutoHide = AUTO_HIDE.IsChecked.Value;
            LoadedSettings.AddOnStartup = ADD_ON_STARTUP.IsChecked.Value;
            LoadedSettings.OutputAll = OUTPUT_ALL.IsChecked.Value;

            try
            {
                LoadedSettings.MessagesShown = Int16.Parse(MESSAGES_SHOWN.Text.Trim());
            }
            catch
            {
                // Number parsing failed. Ignore that for now.
            }
        }
    }

    public class SettingsHandler
    {
        // Constants for storing of the settings.
        private const String AppDataFolder = "Dota 2 Translator";
        private const String FileName = "Settings.cfg";

        // Default values.
        public String ExeName = "dota";
        public String FontName = "Segoe UI";
        public String TranslateTo = "en";
        public Int16 MessagesShown = 6;
        public Boolean AutoHide = true;
        public Boolean AddOnStartup = false;
        public Boolean OutputAll = true;
        
        // Indicates whether the instance can be saved or not. Only the original instance can be saved.
        private Boolean CanSave = true;

        // Static instance to use everywhere
        private static SettingsHandler Instance = null;

        // Hidden constructor.
        private SettingsHandler()
        {
        }

        // Returns a FileStream to the settings file.
        private FileStream GetFileStream(FileMode mode)
        {
            // Construct a path leading to AppData/Roaming/[AppDataFolder].
            String path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolder);
            
            // Make sure it exists.
            Directory.CreateDirectory(path);

            // Append the file name to the path.
            path = System.IO.Path.Combine(path, FileName);

            return new FileStream(path, mode);
        }

        // Attempts to load saved settings from disk. The default values are used if no file is found.
        private void LoadSettings()
        {
            BinaryReader reader = null;
            try
            {
                // Open the settings file.
                reader = new BinaryReader(GetFileStream(FileMode.Open));

                // Read string values.
                ExeName = reader.ReadString();
                FontName = reader.ReadString();
                TranslateTo = reader.ReadString();

                // Read int values.
                MessagesShown = reader.ReadInt16();

                // Read boolean values.
                AutoHide = reader.ReadBoolean();
                AddOnStartup = reader.ReadBoolean();
                OutputAll = reader.ReadBoolean();
            }
            catch (Exception)
            {
                // File probably doesn't exist, use default settings.
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        // Saves the current settings to disk.
        public void SaveSettings()
        {
            // Make sure no copy of the settings is saving data.
            if (!CanSave)
            {
                throw new Exception("This instance of SettingsHandler cannot save to disk.");
            }

            BinaryWriter writer = null;
            try
            {
                // Open the settings file for writing.
                writer = new BinaryWriter(GetFileStream(FileMode.OpenOrCreate));

                // Write string values.
                writer.Write(ExeName);
                writer.Write(FontName);
                writer.Write(TranslateTo);

                // Write int values.
                writer.Write(MessagesShown);

                // Write boolean values.
                writer.Write(AutoHide);
                writer.Write(AddOnStartup);
                writer.Write(OutputAll);
            }
            catch (Exception)
            {
                // Save failed, display an error dialog.
                MessageBox.Show("Make sure that this directory is writable by the current user.", "Save failed!");
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }
        
        // Merges an instance with the static instance.
        public void Merge(SettingsHandler settingsHandler)
        {
            this.ExeName = settingsHandler.ExeName;
            this.FontName = settingsHandler.FontName;
            this.TranslateTo = settingsHandler.TranslateTo;
            this.MessagesShown = settingsHandler.MessagesShown;
            this.AutoHide = settingsHandler.AutoHide;
            this.AddOnStartup = settingsHandler.AddOnStartup;
            this.OutputAll = settingsHandler.OutputAll;
        }

        // Returns a copy of the static instance. This copy can not save itself to disk.
        public SettingsHandler Clone()
        {
            SettingsHandler handler = new SettingsHandler();
            handler.CanSave = false;

            handler.ExeName = this.ExeName;
            handler.FontName = this.FontName;
            handler.TranslateTo = this.TranslateTo;
            handler.MessagesShown = this.MessagesShown;
            handler.AutoHide = this.AutoHide;
            handler.AddOnStartup = this.AddOnStartup;
            handler.OutputAll = this.OutputAll;

            return handler;
        }

        // Sends the relevant settings to the overlay.
        public void SendChangesToOverlay()
        {
            InjectionHelper.SendSetting("FontName", this.FontName);
            InjectionHelper.SendSetting("MessagesShown", this.MessagesShown);
            InjectionHelper.SendSetting("AutoHide", this.AutoHide);
        }

        // Returns the static instance or creates one if none is available.
        public static SettingsHandler GetInstance()
        {
            // No previous instance is available - Create one.
            if (Instance == null)
            {
                Instance = new SettingsHandler();

                // Attempt to load saved settings from a file.
                Instance.LoadSettings();
            }

            return Instance;
        }

        // Static access to SettingsHandler.Clone().
        public static SettingsHandler CloneInstance()
        {
            // Make sure there is an instance available.
            GetInstance();

            return Instance.Clone();
        }

        // Static access combining SettingsHandler.Merge() and SettingsHandler.SaveSettings().
        public static void MergeAndSave(SettingsHandler handler)
        {
            // Make sure there is an instance available.
            GetInstance();

            // Don't merge what does not need to be merged.
            if (handler != Instance)
                Instance.Merge(handler);

            // Save settings to disk.
            Instance.SaveSettings();
        }

        // Static access to SettingsHandler.SendChangesToOverlay().
        public static void SendToOverlay()
        {
            // Make sure there is an instance available.
            GetInstance();

            // Send the changes to the overlay.
            Instance.SendChangesToOverlay();
        }

    }
}
