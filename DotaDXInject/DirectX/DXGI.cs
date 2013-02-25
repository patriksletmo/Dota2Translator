using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using SlimDX;

namespace DotaDXInject
{
    internal static class DXGI
    {
        public enum DXGISwapChainVTbl : short
        {
            // IUnknown.
            QueryInterface = 0,
            AddRef = 1,
            Release = 2,

            // IDXGIObject.
            SetPrivateData = 3,
            SetPrivateDataInterface = 4,
            GetPrivateData = 5,
            GetParent = 6,

            // IDXGIDeviceSubObject.
            GetDevice = 7,

            // IDXGISwapChain.
            Present = 8,
            GetBuffer = 9,
            SetFullscreenState = 10,
            GetFullscreenState = 11,
            GetDesc = 12,
            ResizeBuffers = 13,
            ResizeTarget = 14,
            GetContainingOutput = 15,
            GetFrameStatistics = 16,
            GetLastPresentCount = 17,
        }

        public const int DXGI_SWAPCHAIN_METHOD_COUNT = 18;

        public static SlimDX.DXGI.SwapChainDescription CreateSwapChainDescription(IntPtr windowHandle)
        {
            return new SlimDX.DXGI.SwapChainDescription
            {
                BufferCount = 1,
                Flags = SlimDX.DXGI.SwapChainFlags.None,
                IsWindowed = true,
                ModeDescription = new SlimDX.DXGI.ModeDescription(100, 100, new Rational(60, 1), SlimDX.DXGI.Format.R8G8B8A8_UNorm),
                OutputHandle = windowHandle,
                SampleDescription = new SlimDX.DXGI.SampleDescription(1, 0),
                SwapEffect = SlimDX.DXGI.SwapEffect.Discard,
                Usage = SlimDX.DXGI.Usage.RenderTargetOutput
            };
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DXGI_MODE_DESC
        {
            public int Width;
            public int Height;
            public SlimDX.Rational RefreshRate;
            public SlimDX.DXGI.Format Format;
            public SlimDX.DXGI.DisplayModeScanlineOrdering ScanlineOrdering;
            public SlimDX.DXGI.DisplayModeScaling Scaling;
        }
    }
}
