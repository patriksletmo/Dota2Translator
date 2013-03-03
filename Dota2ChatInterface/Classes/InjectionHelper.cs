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
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Ipc;
using DotaDXInject;
using EasyHook;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace Dota2ChatInterface
{
    class InjectionHelper
    {        
        private static String ChannelName = null;
        private static IpcServerChannel InjectionServer;
        private static int processId;
        private static Process process;

        public static void Setup()
        {
            // Reset the channel name.
            ChannelName = null;

            // Start an Ipc server for the DLL to connect to.
            InjectionServer = RemoteHooking.IpcCreateServer<ChatInterface>(ref ChannelName, WellKnownObjectMode.Singleton);
        }

        // Attaches the DLL to the game.
        public static Boolean AttachToProcess(IntPtr hWnd)
        {
            // Load stored settings.
            SettingsHandler settings = SettingsHandler.GetInstance();
            
            // This is the process we will be looking for.
            String exe = settings.ExeName;

            // Get all running processes the user has access to.
            Process[] processes = Process.GetProcessesByName(exe);
            foreach (Process process in processes)
            {
                // We won't be able to use a process with no MainWindowHandle.
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    return false;
                }

                InjectionHelper.processId = process.Id;
                InjectionHelper.process = process;

                // Inject DLL into the game.
                try
                {
                    RemoteHooking.Inject(
                        process.Id,                                     // Process ID
                        InjectionOptions.Default,                       // InjectionOptions
                        typeof(DotaInjection).Assembly.Location,        // 32-bit DLL
                        typeof(DotaInjection).Assembly.Location,        // 64-bit DLL - Will never be used
                        // Optional parameters.
                        ChannelName,                                     // The name of the IPC channel for the injected assembly to connect to
                        hWnd                                             // A reference to the window handler. Used to create a DirectX device.
                    );
                }
                catch (System.IO.FileNotFoundException)
                {
                    // SlimDX is not installed, inform the user.
                    System.Windows.MessageBox.Show("It appears that SlimDX is not installed, please run the SlimDX installer found in the installation directory and then attempt to add the overlay again. You do NOT have to close this program.\n\nIt should be noted that the installation of SlimDX not always works. Make sure that Dota 2 is closed before attempting to install it.", "Couldn't add overlay");

                    return false;
                }

                // The game can end up in a state where no user input is received until a double tab out of/into the game. The window is brought to front in order to avoid this.
                BringProcessWindowToFront(process);
                
                // Make sure that the overlay receives all the settings.
                settings.SendChangesToOverlay();

                return true;
            }

            return false;
        }

        // Appends a message to the in-game overlay.
        public static void SendMessage(String sender, String message)
        {
            MessageManager.AddPendingEvent(
                0,
                new object[] { sender, message }
                );

            //Console.WriteLine(DotaDXInject.EventHandler.PendingEvents.Count);
        }

        // Hides the overlay using the auto-hide feature.
        public static void SendHideShowMessage(Boolean hide)
        {
            MessageManager.AddPendingEvent(
                1,
                new object[] { hide }
                );
        }

        // Pushes a setting to the overlay DLL.
        public static void SendSetting(String name, object value)
        {
            MessageManager.AddPendingEvent(
                2,
                new object[] { name, value }
                );
        }


        #region Window functions

        // Brings the specified process to front.
        private static Boolean BringProcessWindowToFront(Process process)
        {
            if (process == null)
                return false;

            // Retrieve the MainWindowHandle.
            IntPtr handle = process.MainWindowHandle;
            int i = 0;

            // Attempt to bring the window to front.
            while (!NativeMethods.IsWindowInForeground(handle))
            {
                if (i == 0)
                {
                    // Wait before the initial attempt to bring the window to front.
                    Thread.Sleep(250);
                }

                // Checks if the window is minimized.
                if (NativeMethods.IsIconic(handle))
                {
                    // Call the restore method.
                    NativeMethods.ShowWindow(handle, NativeMethods.WindowShowStyle.Restore);
                }
                else
                {
                    // Already visible, just bring the window to front.
                    NativeMethods.SetForegroundWindow(handle);
                }
                Thread.Sleep(250);

                // Check if the window is in front.
                if (NativeMethods.IsWindowInForeground(handle))
                {
                    // Wait (enough time) for the screen to re-draw.
                    Thread.Sleep(1000);
                    return true;
                }

                // Don't loop forever.
                if (i > 40) // Abort if no success has been made the first 10 seconds.
                {
                    return false;
                }
                i++;
            }

            return true;
        }

        #endregion

    }

    #region Window functions helper
    [System.Security.SuppressUnmanagedCodeSecurity()]
    internal sealed class NativeMethods
    {
        private NativeMethods() { }

        internal static bool IsWindowInForeground(IntPtr hWnd)
        {
            return hWnd == GetForegroundWindow();
        }

        #region user32

        #region ShowWindow
        /// <summary>Shows a Window</summary>
        /// <remarks>
        /// <para>To perform certain special effects when showing or hiding a
        /// window, use AnimateWindow.</para>
        ///<para>The first time an application calls ShowWindow, it should use
        ///the WinMain function's nCmdShow parameter as its nCmdShow parameter.
        ///Subsequent calls to ShowWindow must use one of the values in the
        ///given list, instead of the one specified by the WinMain function's
        ///nCmdShow parameter.</para>
        ///<para>As noted in the discussion of the nCmdShow parameter, the
        ///nCmdShow value is ignored in the first call to ShowWindow if the
        ///program that launched the application specifies startup information
        ///in the structure. In this case, ShowWindow uses the information
        ///specified in the STARTUPINFO structure to show the window. On
        ///subsequent calls, the application must call ShowWindow with nCmdShow
        ///set to SW_SHOWDEFAULT to use the startup information provided by the
        ///program that launched the application. This behavior is designed for
        ///the following situations: </para>
        ///<list type="">
        ///    <item>Applications create their main window by calling CreateWindow
        ///    with the WS_VISIBLE flag set. </item>
        ///    <item>Applications create their main window by calling CreateWindow
        ///    with the WS_VISIBLE flag cleared, and later call ShowWindow with the
        ///    SW_SHOW flag set to make it visible.</item>
        ///</list></remarks>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="nCmdShow">Specifies how the window is to be shown.
        /// This parameter is ignored the first time an application calls
        /// ShowWindow, if the program that launched the application provides a
        /// STARTUPINFO structure. Otherwise, the first time ShowWindow is called,
        /// the value should be the value obtained by the WinMain function in its
        /// nCmdShow parameter. In subsequent calls, this parameter can be one of
        /// the WindowShowStyle members.</param>
        /// <returns>
        /// If the window was previously visible, the return value is nonzero.
        /// If the window was previously hidden, the return value is zero.
        /// </returns>
        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

        /// <summary>Enumeration of the different ways of showing a window using
        /// ShowWindow</summary>
        internal enum WindowShowStyle : uint
        {
            /// <summary>Hides the window and activates another window.</summary>
            /// <remarks>See SW_HIDE</remarks>
            Hide = 0,
            /// <summary>Activates and displays a window. If the window is minimized
            /// or maximized, the system restores it to its original size and
            /// position. An application should specify this flag when displaying
            /// the window for the first time.</summary>
            /// <remarks>See SW_SHOWNORMAL</remarks>
            ShowNormal = 1,
            /// <summary>Activates the window and displays it as a minimized window.</summary>
            /// <remarks>See SW_SHOWMINIMIZED</remarks>
            ShowMinimized = 2,
            /// <summary>Activates the window and displays it as a maximized window.</summary>
            /// <remarks>See SW_SHOWMAXIMIZED</remarks>
            ShowMaximized = 3,
            /// <summary>Maximizes the specified window.</summary>
            /// <remarks>See SW_MAXIMIZE</remarks>
            Maximize = 3,
            /// <summary>Displays a window in its most recent size and position.
            /// This value is similar to "ShowNormal", except the window is not
            /// actived.</summary>
            /// <remarks>See SW_SHOWNOACTIVATE</remarks>
            ShowNormalNoActivate = 4,
            /// <summary>Activates the window and displays it in its current size
            /// and position.</summary>
            /// <remarks>See SW_SHOW</remarks>
            Show = 5,
            /// <summary>Minimizes the specified window and activates the next
            /// top-level window in the Z order.</summary>
            /// <remarks>See SW_MINIMIZE</remarks>
            Minimize = 6,
            /// <summary>Displays the window as a minimized window. This value is
            /// similar to "ShowMinimized", except the window is not activated.</summary>
            /// <remarks>See SW_SHOWMINNOACTIVE</remarks>
            ShowMinNoActivate = 7,
            /// <summary>Displays the window in its current size and position. This
            /// value is similar to "Show", except the window is not activated.</summary>
            /// <remarks>See SW_SHOWNA</remarks>
            ShowNoActivate = 8,
            /// <summary>Activates and displays the window. If the window is
            /// minimized or maximized, the system restores it to its original size
            /// and position. An application should specify this flag when restoring
            /// a minimized window.</summary>
            /// <remarks>See SW_RESTORE</remarks>
            Restore = 9,
            /// <summary>Sets the show state based on the SW_ value specified in the
            /// STARTUPINFO structure passed to the CreateProcess function by the
            /// program that started the application.</summary>
            /// <remarks>See SW_SHOWDEFAULT</remarks>
            ShowDefault = 10,
            /// <summary>Windows 2000/XP: Minimizes a window, even if the thread
            /// that owns the window is hung. This flag should only be used when
            /// minimizing windows from a different thread.</summary>
            /// <remarks>See SW_FORCEMINIMIZE</remarks>
            ForceMinimized = 11
        }
        #endregion

        /// <summary>
        /// The GetForegroundWindow function returns a handle to the foreground window.
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsIconic(IntPtr hWnd);

        #endregion
    }

    #endregion
}
