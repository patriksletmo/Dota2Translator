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
using SlimDX.Direct3D9;
using EasyHook;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Collections;

namespace DotaDXInject
{
    internal class DXHookD3D9
    {
        // Reference to the ChatInterface instance used.
        private ChatInterface Interface;

        // Whether or not to hide the overlay because auto hide triggered.
        public Boolean Hide = false;

        // The messages to display.
        public List<Message> Messages = new List<Message>();

        // The font used to draw the overlay.
        private SlimDX.Direct3D9.Font OverlayFont = null;

        // The last font name, used to refresh the font.
        private String LastFontName = null;

        // The last amount of messages shown, used to refresh the font.
        private int LastMessagesShown = 0;

        // The last font height, used to refresh the font.
        private int FontHeight = 0;

        // The last width, used to refresh the font.
        private int PreviousWidth = 0;

        // The last height, used to refresh the font.
        private int PreviousHeight = 0;

        // Available width for the text, used by wrapping.
        private int AvailableTextWidth = 0;

        // X position for the overlay.
        private int TextStartWidth = 0;

        // Y position for the overlay.
        private int TextStartHeight = 0;

        // The color used to draw the text.
        private Color TextColor = Color.FromArgb(250, 234, 201);

        // The color used to draw the text shadow.
        private Color TextShadowColor = Color.Black;

        // The font name to use.
        public String FontName = "Segoe UI";

        // The amount of messages to show.
        public Int16 MessagesShown = 6;

        // Whether or not to actually auto hide.
        public Boolean AutoHide = true;

        // Whether or not to fade messages.
        public Boolean FadeMessages = true;

        // How long the messages will stay visible before fading.
        public double FadeWait = 20.0;

        // How long the messages will fade.
        public double FadeDuration = 2.5;

        // The time the overlay was added.
        private DateTime TimeAdded;

        // The amount of milliseconds the success message is shown.
        private double SuccessMessageTime = 3000;

        // Debugging
        private Boolean EndSceneCalled = false;

        public DXHookD3D9(ChatInterface chatInterface)
        {
            this.Interface = chatInterface;
        }

        #region Hooking

        private LocalHook Direct3DDevice_EndSceneHook = null;
        private LocalHook Direct3DDevice_ResetHook = null;
        private object _lockRenderTarget = new object();
        private Surface _renderTarget;
        private const int D3D9_DEVICE_METHOD_COUNT = 119;

        // Returns the virtual table addresses of the specified pointer.
        protected IntPtr[] GetVTblAddresses(IntPtr pointer, int numberOfMethods)
        {
            List<IntPtr> vtblAddresses = new List<IntPtr>();

            IntPtr vTable = Marshal.ReadIntPtr(pointer);
            for (int i = 0; i < numberOfMethods; i++)
                vtblAddresses.Add(Marshal.ReadIntPtr(vTable, i * IntPtr.Size));

            return vtblAddresses.ToArray();
        }

        // Hooks the DirectX functions we are interested in.
        public void Hook()
        {
            try
            {
                Interface.Debug("Hooking DirectX functions...");

                // Register the time.
                TimeAdded = DateTime.Now;

                // Retrieve the function addresses.
                Device device;
                List<IntPtr> id3dDeviceFunctionAddresses = new List<IntPtr>();
                using (Direct3D d3d = new Direct3D())
                {
                    using (device = new Device(d3d, 0, DeviceType.NullReference, IntPtr.Zero, CreateFlags.HardwareVertexProcessing, new PresentParameters() { BackBufferWidth = 1, BackBufferHeight = 1 }))
                    {
                        id3dDeviceFunctionAddresses.AddRange(GetVTblAddresses(device.ComPointer, D3D9_DEVICE_METHOD_COUNT));
                    }
                }

                Interface.Debug("Hooking EndScene at " + id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.EndScene]);

                // Hook EndScene.
                Direct3DDevice_EndSceneHook = LocalHook.Create(
                    id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.EndScene],
                    new Direct3D9Device_EndSceneDelegate(EndSceneHook),
                    this
                    );

                Interface.Debug("Hooking Reset at " + id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Reset]);

                // Hook Reset.
                Direct3DDevice_ResetHook = LocalHook.Create(
                    id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Reset],
                    new Direct3D9Device_ResetDelegate(ResetHook),
                    this);

                // Activate the hooks.
                Direct3DDevice_EndSceneHook.ThreadACL.SetExclusiveACL(new Int32[1]);
                Direct3DDevice_ResetHook.ThreadACL.SetExclusiveACL(new Int32[1]);

                Interface.Debug("All DirectX functions hooked!\n");
            }
            catch (Exception e)
            {
                Interface.Debug("Hooking failed!\nAdditional info: " + e.ToString());
                throw e;
            }
        }

        // Cleans up after us.
        public void Cleanup()
        {
            try
            {
                lock (_lockRenderTarget)
                {
                    if (_renderTarget != null)
                    {
                        // Dispose the render target.
                        _renderTarget.Dispose();
                        _renderTarget = null;
                    }

                    // Dispose the hooks.
                    Direct3DDevice_EndSceneHook.Dispose();
                    Direct3DDevice_EndSceneHook = null;
                    Direct3DDevice_ResetHook.Dispose();
                    Direct3DDevice_ResetHook = null;

                    // Clear the messages.
                    Messages.Clear();
                    Messages = null;
                }
            }
            catch
            {
            }
        }

        // Delegate for EndScene.
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate int Direct3D9Device_EndSceneDelegate(IntPtr device);

        // Delegate for Reset.
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate int Direct3D9Device_ResetDelegate(IntPtr device, ref D3DPRESENT_PARAMETERS presentParameters);

        #endregion

        #region Reset Hook

        int ResetHook(IntPtr devicePtr, ref D3DPRESENT_PARAMETERS presentParameters)
        {
            using (Device device = Device.FromPointer(devicePtr))
            {
                PresentParameters pp = new PresentParameters()
                {
                    AutoDepthStencilFormat = (Format)presentParameters.AutoDepthStencilFormat,
                    BackBufferCount = presentParameters.BackBufferCount,
                    BackBufferFormat = (Format)presentParameters.BackBufferFormat,
                    BackBufferHeight = presentParameters.BackBufferHeight,
                    BackBufferWidth = presentParameters.BackBufferWidth,
                    DeviceWindowHandle = presentParameters.DeviceWindowHandle,
                    EnableAutoDepthStencil = presentParameters.EnableAutoDepthStencil,
                    FullScreenRefreshRateInHertz = presentParameters.FullScreen_RefreshRateInHz,
                    Multisample = (MultisampleType)presentParameters.MultiSampleType,
                    MultisampleQuality = presentParameters.MultiSampleQuality,
                    PresentationInterval = (PresentInterval)presentParameters.PresentationInterval,
                    PresentFlags = (PresentFlags)presentParameters.Flags,
                    SwapEffect = (SwapEffect)presentParameters.SwapEffect,
                    Windowed = presentParameters.Windowed
                };
                lock (_lockRenderTarget)
                {
                    if (_renderTarget != null)
                    {
                        _renderTarget.Dispose();
                        _renderTarget = null;
                    }
                }
                // EasyHook has already repatched the original Reset so calling it here will not cause an endless recursion to this function
                return device.Reset(pp).Code;
            }
        }

        #endregion

        // Called every frame from the DirectX API. We draw our overlay here (on top of everything).
        int EndSceneHook(IntPtr devicePtr)
        {
            // Used to debug whether or not the EndScene is called.
            if (!EndSceneCalled)
            {
                EndSceneCalled = true;
                Interface.Debug("EndScene called successfully!");
            }

            using (Device device = Device.FromPointer(devicePtr))
            {
                // Retrieve the current time.
                DateTime now = DateTime.Now;

                // Retrieve the width and height of the surface.
                int width, height = 0;
                using (Surface renderTargetTemp = device.GetRenderTarget(0))
                {
                    width = renderTargetTemp.Description.Width;
                    height = renderTargetTemp.Description.Height;

                    _renderTarget = renderTargetTemp;
                }

                // Re-calculate overlay position and dimension if the width or height changes.
                if (PreviousWidth != width || PreviousHeight != height)
                {
                    // Refresh position and dimension.
                    CalculatePositions(width, height);
                    PreviousWidth = width;
                    PreviousHeight = height;

                    // Reset font.
                    OverlayFont = null;
                }

                try
                {
                    // Render a success message to indicate that the overlay was added successfully.
                    long millisSinceAdd = (now - TimeAdded).Ticks / TimeSpan.TicksPerMillisecond;
                    if (millisSinceAdd < SuccessMessageTime)
                    {
                        // Scale the font to the current resolution.
                        using (SlimDX.Direct3D9.Font successFont = new SlimDX.Direct3D9.Font(device, new System.Drawing.Font("Times New Roman", 80.0f / 1920f * width)))
                        {
                            // Draw the font with an alpha value related to the time passed since the overlay was added. The font will be completely invisible when the time has passed.
                            successFont.DrawString(null, "Overlay added!", 50, 50, Color.FromArgb((int)((SuccessMessageTime - millisSinceAdd) / SuccessMessageTime * 255.0), TextColor));
                        }
                    }

                    // Don't render the overlay if it's in auto hide mode and hidden.
                    if (!Hide || !AutoHide)
                    {
                        // Refresh the font if the settings has been changed or create a font if null.
                        if (OverlayFont == null || !FontName.Equals(LastFontName) || LastMessagesShown != MessagesShown)
                        {
                            LastFontName = FontName;
                            LastMessagesShown = MessagesShown;
                            LoadFont(device);
                        }

                        // Draw the last messages received. Amount defined by MessagesShown.
                        int y = MessagesShown - 1;
                        int i = Messages.Count - 1;
                        while (y >= 0)
                        {
                            // Abort the loop if there are no more messages available.
                            if (!(i >= Math.Max(0, Messages.Count - MessagesShown)))
                                break;

                            Message message = Messages[i];

                            // Wrap the message if it has not been done yet or the font/dimensions has been changed.
                            if (!OverlayFont.Equals(message.FontUsed))
                            {
                                message.WrapMessage(OverlayFont, AvailableTextWidth);
                            }

                            // Store the original text color.
                            Color drawColor = TextColor;

                            // Keep track of whether to draw the shadow or not (The shadow will not be drawn when fading).
                            Boolean drawShadow = true;

                            // Fade the color if in the fading phase.
                            if (FadeMessages && message.Time.AddSeconds(FadeWait) <= now)
                            {
                                if (message.Time.AddSeconds(FadeWait + FadeDuration) <= now)
                                {
                                    // The message has already been faded, don't display it.
                                    i--;
                                    continue;
                                }

                                // Fade the color according to the duration and delay of the fade.
                                float fadeDuration = (message.Time.AddSeconds(FadeWait + FadeDuration) - message.Time.AddSeconds(FadeWait)).Ticks / TimeSpan.TicksPerMillisecond;
                                float fadeProgress = (now - message.Time.AddSeconds(FadeWait)).Ticks / TimeSpan.TicksPerMillisecond;
                                float fadePercentage = fadeProgress / fadeDuration;
                                drawColor = Color.FromArgb((int) ((1f - fadePercentage)*255), drawColor);
                                drawShadow = false;
                            }
                            
                            // Draw the wrapped message from the bottom.
                            for (int j = message.WrappedMessage.Length - 1; j >= 0 && y >= 0; j--)
                            {
                                if (drawShadow)
                                    OverlayFont.DrawString(null, message.WrappedMessage[j], TextStartWidth + FontHeight / 2 + message.SenderWidth, TextStartHeight + 1 + FontHeight * y, TextShadowColor);

                                OverlayFont.DrawString(null, message.WrappedMessage[j], TextStartWidth - 1 + FontHeight / 2 + message.SenderWidth, TextStartHeight + FontHeight * y, drawColor);

                                // Only draw the sender on the upmost line.
                                if (j == 0)
                                {
                                    if (drawShadow)
                                        OverlayFont.DrawString(null, message.Sender + ": ", TextStartWidth, TextStartHeight + 1 + FontHeight * y, TextShadowColor);

                                    OverlayFont.DrawString(null, message.Sender + ": ", TextStartWidth - 1, TextStartHeight + FontHeight * y, drawColor);
                                }
                                y--;
                            }

                            i--;
                        }
                    }
                   
                }
                catch
                {
                    // Don't crash the hooked application if an exception is thrown.
                }

                // EasyHook has already repatched the original EndScene so we will not loop our method forever.
                return device.EndScene().Code;
            }
        }

        // Refreshes the font with the specified name and size.
        private void LoadFont(Device device)
        {
            // Calculate the height of the font.
            float fontHeight = 12.0f * 6 / MessagesShown / 1080f * PreviousHeight;

            // Used to space the lines.
            FontHeight = (int) (fontHeight * 1.6);

            // Only used to get an appropirate height, could probably be replaced somehow.
            System.Drawing.Font f = new System.Drawing.Font(FontName, fontHeight);

            // Create the font.
            OverlayFont = new SlimDX.Direct3D9.Font(
                device,
                f.Height,
                0,
                FontWeight.DemiBold,
                0,
                false,
                CharacterSet.Default,
                Precision.Default,
                FontQuality.ClearType,
                PitchAndFamily.Default,
                FontName
                );
        }

        // Calculates dimension and position for the overlay using the specified width and height.
        private void CalculatePositions(int width, int height)
        {
            // *Everything* in the Dota 2 interface is sized by percentage, woho!
            AvailableTextWidth = (int)(515f / 1920f * width);
            TextStartHeight = (int)(640f / 1080f * height);
            TextStartWidth = (int)(29f / 1920f * width);
        }

        // Class mainely used to calculate message wrapping.
        public class Message
        {
            // The message sender.
            public String Sender;

            // The message content.
            public String Content;
            
            // The final wrapped message.
            public String[] WrappedMessage;

            // Used to refresh the wrapping.
            public SlimDX.Direct3D9.Font FontUsed;

            // The width of the message sender string.
            public int SenderWidth;

            // The time the message was added.
            public DateTime Time;
            
            public Message(String sender, String content)
            {
                this.Sender = sender;
                this.Content = content;
                this.Time = DateTime.Now;
            }

            // Calculates wrapping and places in WrappedMessage.
            public void WrapMessage(SlimDX.Direct3D9.Font overlayFont, int width)
            {
                try
                {
                    // Rectangle to store the dimensions in.
                    Rectangle rect = new Rectangle();

                    // Measure the sender string.
                    overlayFont.MeasureString(null, Sender, DrawTextFormat.SingleLine, ref rect);
                    SenderWidth = rect.Width;
                    width -= SenderWidth;

                    // List containing the split lines.
                    List<String> lines = new List<String>();

                    // The start index for the measured substring.
                    int start = 0;

                    // The length of the measured substring.
                    int length = 1;

                    // The last location of a space char.
                    int lastSpace = -1;

                    // The current substring.
                    String text;

                    // Split the message into lines.
                    while (start + length - 1 < Content.Length)
                    {
                        // Retrieve a substring of the message.
                        text = Content.Substring(start, length);

                        // Check if the last char is whitespace.
                        char c = Content[start + length - 1];
                        if (c == ' ')
                            lastSpace = length;

                        // Measure the substring.
                        overlayFont.MeasureString(null, text, DrawTextFormat.SingleLine, ref rect);

                        if (rect.Width > width)
                        {
                            // Split the message.

                            // Attempt to avoid splitting words in the middle.
                            if (length > 1)
                            {
                                if (lastSpace != -1)
                                    length = lastSpace;
                                else
                                    length--;
                            }

                            lines.Add(Content.Substring(start, length));
                            start += length;
                            length = 1;
                            lastSpace = -1;

                            continue;
                        }
                        else if (start + length >= Content.Length)
                        {
                            // Add the last line.

                            lines.Add(text);
                            break;
                        }

                        length++;
                    }

                    // Store the lines.
                    WrappedMessage = lines.ToArray();

                    // Don't re-attempt the wrapping.
                    FontUsed = overlayFont;
                }
                // Don't interrupt the drawing of the other messages.
                catch
                {
                    // Do not re-attempt.
                    WrappedMessage = new String[0];
                    FontUsed = overlayFont;
                }
            }
        }

    }
}
