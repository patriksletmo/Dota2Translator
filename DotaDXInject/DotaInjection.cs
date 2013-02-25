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
using System.Threading;
using System.Threading.Tasks;
using EasyHook;
using System.Reflection;

namespace DotaDXInject
{
    public class DotaInjection : EasyHook.IEntryPoint
    {
        // The DirectX9 hook we're using.
        private DXHookD3D9 directXHook = null;

        // The interface for communicating with the program.
        private ChatInterface chatInterface = null;

        public DotaInjection(RemoteHooking.IContext context, String channelName)
        {
            // Connect to the program.
            chatInterface = RemoteHooking.IpcConnectClient<ChatInterface>(channelName);
        }

        // Called by the EasyHook API. This method will keep running untill the connection to the program is stopped.
        public void Run(RemoteHooking.IContext context, String channelName)
        {
            try
            {
                // Hook the DirectX API.
                directXHook = new DXHookD3D9();
                directXHook.Hook();
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                while (true)
                {
                    // Check for new events every 100 millis.
                    Thread.Sleep(100);

                    // Parse all the pending events.
                    object[] pendingEvent;
                    while ((pendingEvent = chatInterface.GetPendingEvent()) != null)
                    {
                        // Handle the event and perform the specified action.
                        int type = (int)pendingEvent[0];
                        switch (type)
                        {
                            // Message
                            case 0:
                                String sender = (String)pendingEvent[1];
                                String message = (String)pendingEvent[2];

                                directXHook.Messages.Add(new DXHookD3D9.Message(sender, message));

                                break;
                            // Hide / Show
                            case 1:
                                Boolean hide = (Boolean)pendingEvent[1];

                                directXHook.Hide = hide;

                                break;
                            // Setting change
                            case 2:
                                String fieldName = (String)pendingEvent[1];

                                FieldInfo info = typeof(DXHookD3D9).GetField(fieldName);
                                info.SetValue(directXHook, pendingEvent[2]);

                                break;
                        }
                    }

                    // Make sure we're still connected.
                    chatInterface.Ping();
                }
            }
            catch
            {
                // Ping() will raise an exception if host is unreachable.
            }
            finally
            {
                try
                {
                    // Un-register DirectX hooks, clean up variables.
                    directXHook.Cleanup();
                }
                catch
                {
                }
            }
        }
    }
}
