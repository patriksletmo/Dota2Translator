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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.NetworkInformation;
using System.Management;

namespace Dota2ChatInterface
{
    public partial class MainWindow : Window
    {
        #region Chat capture

        // The currently selected device.
        private IntPtr Device;

        // Used for transmitting data back to this program.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct Dota_ChatMessage
        {
            public int Type;
            public string Sender;
            public string Message;
        }

        // Delegate used when data is being sent.
        public delegate void Dota_ChatMessageCallback(Dota_ChatMessage data);

        // Starts the device on the calling thread. (Should not be called from the UI thread)
        [DllImport("Dota2ChatDLL.dll")]
        public static extern void StartDevice(IntPtr device, Dota_ChatMessageCallback callback);

        #endregion

        // Used to call AddChatItem from another thread.
        private delegate void addChatItem_delegate(String scope, String sender, String message);

        // Used to call SetInjectOverlayButtonState from another thread.
        private delegate void SetInjectOverlayButtonState_Delegate(Boolean enabled);

        // Static instance of a UTF8 encoding.            
        public static Encoding Utf8 = Encoding.UTF8;

        // The program settings.
        private SettingsHandler Settings;

        // Packet capture thread. Will be killed when closing the window.
        private Thread PacketCaptureThread;

        // Constructor.
        public MainWindow(IntPtr devicePointer)
        {
            InitializeComponent();

            // Register window loaded listener.
            Loaded += Window_Loaded;

            // Register window closed listener.
            Closed += Window_Closed;

            // Load the program settings.
            Settings = SettingsHandler.GetInstance();

            // Start the packet capture.
            Device = devicePointer;
            PacketCaptureThread = new Thread(new ThreadStart(StartPacketCapture));
            PacketCaptureThread.Start();
        }

        // Called when the window has been loaded.
        private void Window_Loaded(object sender, EventArgs args)
        {
            // Register click listeners.
            SettingsButton.Click += SettingsButton_Click;
            InjectButton.Click += InjectButton_Click;

            // Attempt to inject automatically if wanted.
            if (Settings.AddOnStartup)
            {
                // Prevent the inject button from being clicked while already injecting.
                SetInjectOverlayButtonState(false);

                // Start injecting.
                Thread t = new Thread(new ThreadStart(StartOverlay));
                t.Start();
            }
        }
        
        // Called when the window is being closed.
        private void Window_Closed(object sender, EventArgs args)
        {
            // Kill the packet capture thread so that the program stops completely.
            PacketCaptureThread.Abort();
        }

        // Called when SettingsButton has been clicked.
        private void SettingsButton_Click(object sender, EventArgs args)
        {
            Settings settingsWindow = new Settings();
            settingsWindow.ShowDialog();
        }

        // Called when InjectButton has been clicked.
        private void InjectButton_Click(object sender, EventArgs args)
        {
            // Prevent the button from being clicked multiple times.
            SetInjectOverlayButtonState(false);

            // Start injecting.
            Thread t = new Thread(new ThreadStart(StartOverlay));
            t.Start();
        }

        // Translates the message and (optionally) appends it to the overlay.
        private void TranslateMessageAndAdd(object arg)
        {
            // Read the arguments.
            object[] args = (object[])arg;
            String scope = (String)args[0];
            String sender = (String)args[1];
            String message = (String)args[2];

            // Translate the message.
            String translatedMessage = Translate.TranslateString(message);

            // Don't output the message if we don't want to.
            if (!Settings.OutputAll && translatedMessage.Equals(message))
                return;

            // Add the message to the window.
            AddChatItem_Invoke(scope, sender, translatedMessage);

            // Add the message to the overlay.
            InjectionHelper.SendMessage(sender, translatedMessage);
        }

        // Adds the chat item to the main window from another thread.
        private void AddChatItem_Invoke(String scope, String sender, String message)
        {
            Dispatcher.Invoke(Delegate.CreateDelegate(typeof(addChatItem_delegate), this, typeof(MainWindow).GetMethod("AddChatItem")), new object[] { scope, sender, message });
        }

        // Adds the chat item. Must be called from UI thread.
        public void AddChatItem(String scope, String sender, String message)
        {
            ChatItem item = new ChatItem();
            item.Scope = scope;
            item.Sender = sender;
            item.Message = message;

            // Add the item to the container.
            ChatContainer.Children.Add(item);

            // Scroll to the bottom.
            Scroller.ScrollToBottom();
        }

        // Calls SetÍnjectOverlayButtonState from another thread.
        private void SetInjectOverlayButtonState_Invoke(Boolean enabled)
        {
            Dispatcher.Invoke(Delegate.CreateDelegate(typeof(SetInjectOverlayButtonState_Delegate), this, typeof(MainWindow).GetMethod("SetInjectOverlayButtonState")), new object[] { enabled });
        }

        // Sets the enabled state of InjectButton.
        public void SetInjectOverlayButtonState(Boolean enabled)
        {
            InjectButton.IsEnabled = enabled;
        }

        // Starts capturing packets.
        public void StartPacketCapture()
        {
            StartDevice(Device, OnMessageReceived);
        }

        // Injects the overlay.
        public void StartOverlay()
        {
            // Setup the helper.
            InjectionHelper.Setup();

            // Attach the overlay to the dota process.
            Boolean success = InjectionHelper.AttachToProcess();

            // Set the state of the inject button to disabled if the injection succeeded.
            SetInjectOverlayButtonState_Invoke(!success);
        }

        // Called by the C++ code when a message has been received or a status has changed.
        public void OnMessageReceived(Dota_ChatMessage data)
        {
            switch (data.Type)
            {
                // Hide status has been changed, report to DLL.
                case -2:
                case -1:
                    InjectionHelper.SendHideShowMessage(data.Type == -1);

                    break;

                // A message has been received, display in main window and send to DLL.
                case 0:
                case 1:
                    // Scope is determined by the type value: 0 = All, 1 = Team.
                    String scope = (data.Type == 0) ? "ALL" : "TEAM";

                    // Make sure the data is read as UTF8.
                    String sender = ToUTF(data.Sender);
                    String message = ToUTF(data.Message);

                    // Translate the message and add it.
                    new Thread(new ParameterizedThreadStart(TranslateMessageAndAdd)).Start(new object[] { scope, sender, message });

                    break;

            }
        }

        // Decodes the UTF8 message from a C++ wstring.
        private static String ToUTF(String str)
        {
            // Convert the string to a char array.
            char[] chars = str.ToCharArray();

            // Create a byte array and fill it with the byte values from the char array.
            byte[] bytes = new byte[chars.Length];
            int i = 0;
            foreach (char c in chars)
            {
                bytes[i++] = (byte)c;
            }

            // Decode the byte array using UTF8.
            return Utf8.GetString(bytes);
        }
    }
}
