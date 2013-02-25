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