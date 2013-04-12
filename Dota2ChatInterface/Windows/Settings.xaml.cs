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
using System.Globalization;

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

            // Register event for AutoMessageHeight change.
            AUTO_MESSAGE_HEIGHT.Checked += AUTO_MESSAGE_HEIGHT_Checked;
            AUTO_MESSAGE_HEIGHT.Unchecked += AUTO_MESSAGE_HEIGHT_Checked;

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
            FADE_MESSAGES.IsChecked = LoadedSettings.FadeMessages;
            FADE_WAIT.Text = LoadedSettings.FadeWait.ToString();
            FADE_DURATION.Text = LoadedSettings.FadeDuration.ToString();
            AUTO_MESSAGE_HEIGHT.IsChecked = LoadedSettings.AutoMessageHeight;
            MESSAGE_HEIGHT.Text = LoadedSettings.MessageHeight.ToString();

            if (LoadedSettings.DefaultAdapterMAC.Length > 0)
            {
                USE_DEFAULT_ADAPTER.IsChecked = LoadedSettings.UseDefaultAdapter;
            }
            else
            {
                USE_DEFAULT_ADAPTER.IsChecked = false;
                USE_DEFAULT_ADAPTER.IsEnabled = false;
            }
        }

        // Called when SaveButton is clicked.
        public void SaveButton_Click(object sender, EventArgs args)
        {
            // Read values from the fields.
            Boolean closeWindow = !StoreTemporarily();

            // Save to file.
            SettingsHandler.MergeAndSave(LoadedSettings);
            SettingsHandler.SendToOverlay();

            // Close the window if there was no parsing error.
            if (closeWindow)
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

        // Called when the AutoMessageHeight property is changed.
        private void AUTO_MESSAGE_HEIGHT_Checked(object sender, EventArgs args)
        {
            // Disable the MessageHeight textfield if enabled.
            MESSAGE_HEIGHT.IsEnabled = !AUTO_MESSAGE_HEIGHT.IsChecked.Value;
        }

        // Stores values in the SettingsHandler instance.
        private Boolean StoreTemporarily()
        {
            // Indicates whether or not all fields were successfully parsed, if one field or more was not parsed the window will not close.
            Boolean parsingError = false;

            LoadedSettings.ExeName = EXE_NAME.Text.Trim(); // One does not simply use whitespace right before or after the file extension.
            LoadedSettings.FontName = FONT_NAME.Text.Trim();
            LoadedSettings.TranslateTo = TRANSLATE_TO.Text.Trim();
            LoadedSettings.AutoHide = AUTO_HIDE.IsChecked.Value;
            LoadedSettings.AddOnStartup = ADD_ON_STARTUP.IsChecked.Value;
            LoadedSettings.OutputAll = OUTPUT_ALL.IsChecked.Value;
            LoadedSettings.FadeMessages = FADE_MESSAGES.IsChecked.Value;
            LoadedSettings.AutoMessageHeight = AUTO_MESSAGE_HEIGHT.IsChecked.Value;
            LoadedSettings.UseDefaultAdapter = USE_DEFAULT_ADAPTER.IsChecked.Value;

            try
            {
                LoadedSettings.MessagesShown = Int16.Parse(MESSAGES_SHOWN.Text.Trim());
            }
            catch
            {
                // Number parsing failed.
                parsingError = true;

                // Alert the user.
                MessageBox.Show("The field 'Messages shown' does not contain a valid integer.", "A setting failed to save");
            }

            try
            {
                double fadeWait = Double.Parse(FADE_WAIT.Text.Replace(',', '.').Trim(), CultureInfo.InvariantCulture);
                if (fadeWait < 0)
                {
                    // Don't allow values of zero or below.
                    parsingError = true;

                    // Alert the user.
                    MessageBox.Show("The field 'Fade delay' must be greater or equal to zero.", "A setting failed to save");
                }
                else
                {
                    LoadedSettings.FadeWait = fadeWait;
                }
            }
            catch
            {
                // Number parsing failed.
                parsingError = true;

                // Alert the user.
                MessageBox.Show("The field 'Fade delay' does not contain a valid number.", "A setting failed to save");
            }

            try
            {
                double fadeDuration = Double.Parse(FADE_DURATION.Text.Replace(',', '.').Trim(), CultureInfo.InvariantCulture);
                if (fadeDuration < 0)
                {
                    // Don't allow values of zero or below.
                    parsingError = true;

                    // Alert the user.
                    MessageBox.Show("The field 'Fade duration' must be greater or equal to zero.", "A setting failed to save");
                }
                else
                {
                    LoadedSettings.FadeDuration = fadeDuration;
                }
            }
            catch
            {
                // Number parsing failed.
                parsingError = true;

                // Alert the user.
                MessageBox.Show("The field 'Fade duration' does not contain a valid number.", "A setting failed to save");
            }

            try
            {
                LoadedSettings.MessageHeight = Int16.Parse(MESSAGE_HEIGHT.Text.Trim());
            }
            catch
            {
                // Number parsing failed.
                parsingError = true;

                // Alert the user.
                MessageBox.Show("The field 'Message height' does not contain a valid integer.", "A setting failed to save");
            }

            return parsingError;
        }
    }

    public class SettingsHandler
    {
        // Constants for storing of the settings.
        private const String FileName = "Settings.cfg";

        // Default values.
        public String ExeName = "dota";
        public String FontName = "Segoe UI";
        public String TranslateTo = "en";
        public Int16 MessagesShown = 6;
        public Boolean AutoHide = true;
        public Boolean AddOnStartup = false;
        public Boolean OutputAll = true;
        public Boolean FadeMessages = true;
        public double FadeWait = 20.0;
        public double FadeDuration = 2.5;
        public Boolean AutoMessageHeight = true;
        public Int16 MessageHeight = 12;
        public String DefaultAdapterMAC = "";
        public Boolean UseDefaultAdapter = false;
        
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
            String path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), App.AppDataFolder);
            
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

                // Read values added to the settings after release.
                FadeMessages = reader.ReadBoolean();
                FadeWait = reader.ReadDouble();
                FadeDuration = reader.ReadDouble();
                AutoMessageHeight = reader.ReadBoolean();
                MessageHeight = reader.ReadInt16();
                DefaultAdapterMAC = reader.ReadString();
                UseDefaultAdapter = reader.ReadBoolean();
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
                writer = new BinaryWriter(GetFileStream(FileMode.Create));

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

                // Read values added to the settings after release.
                writer.Write(FadeMessages);
                writer.Write(FadeWait);
                writer.Write(FadeDuration);
                writer.Write(AutoMessageHeight);
                writer.Write(MessageHeight);
                writer.Write(DefaultAdapterMAC);
                writer.Write(UseDefaultAdapter);
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
            this.FadeMessages = settingsHandler.FadeMessages;
            this.FadeWait = settingsHandler.FadeWait;
            this.FadeDuration = settingsHandler.FadeDuration;
            this.AutoMessageHeight = settingsHandler.AutoMessageHeight;
            this.MessageHeight = settingsHandler.MessageHeight;
            this.DefaultAdapterMAC = settingsHandler.DefaultAdapterMAC;
            this.UseDefaultAdapter = settingsHandler.UseDefaultAdapter;
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
            handler.FadeMessages = this.FadeMessages;
            handler.FadeWait = this.FadeWait;
            handler.FadeDuration = this.FadeDuration;
            handler.AutoMessageHeight = this.AutoMessageHeight;
            handler.MessageHeight = this.MessageHeight;
            handler.DefaultAdapterMAC = this.DefaultAdapterMAC;
            handler.UseDefaultAdapter = this.UseDefaultAdapter;

            return handler;
        }

        // Sends the relevant settings to the overlay.
        public void SendChangesToOverlay()
        {
            InjectionHelper.SendSetting("FontName", this.FontName);
            InjectionHelper.SendSetting("MessagesShown", this.MessagesShown);
            InjectionHelper.SendSetting("AutoHide", this.AutoHide);
            InjectionHelper.SendSetting("FadeMessages", this.FadeMessages);
            InjectionHelper.SendSetting("FadeWait", this.FadeWait);
            InjectionHelper.SendSetting("FadeDuration", this.FadeDuration);
            InjectionHelper.SendSetting("AutoMessageHeight", this.AutoMessageHeight);
            InjectionHelper.SendSetting("MessageHeight", this.MessageHeight);
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
