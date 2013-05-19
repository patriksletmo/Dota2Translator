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

#include "stdafx.h"
#include <string>
#include "sniff.h"
#include <comdef.h>

// Returns (a string with) all available network adapters to listen on.
extern "C" __declspec(dllexport) const BSTR __stdcall GetDeviceList()
{
	// Call the underlaying method and convert to a wstring.
	std::string result = _GetDeviceList();
	std::wstring ws(result.begin(), result.end());

	// Return a BSTR so that the content is not corrupted.
	return SysAllocString(ws.c_str());
}

// Returns a pointer to the specified device. The zero-based device index is based on the list returned by GetDeviceList which MUST be called before this method.
extern "C" __declspec(dllexport) pcap_if_t* __stdcall GetDevice(int num)
{
	return _GetDevice(num);
}

// Starts listening for packets on the specified device. The callback parameter is called when a Dota 2 message has been received.
extern "C" __declspec(dllexport) void __stdcall StartDevice(pcap_if_t* d, Dota_ChatMessage_Callback callback)
{
	_StartDevice(d, callback);
}

extern "C" __declspec(dllexport) void __stdcall SetAutoDetectPort(bool autoDetect)
{
	_SetAutoDetectPort(autoDetect);
}