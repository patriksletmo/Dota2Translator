#include <pcap.h>

// The struct that is sent back to the program. An identical struct is defined there.
struct Dota_ChatMessage
{
	int Type; // 0 = All, 1 = Team
	const wchar_t* Sender; // Chat message sender
	const wchar_t* Message; // Chat message content
};

// Definition for the method that will be called when a message has been received.
typedef void (__stdcall *Dota_ChatMessage_Callback)(Dota_ChatMessage data);

// Definitions for the methods used by Dota2ChatDLL.cpp.
std::string _GetDeviceList();
pcap_if_t* _GetDevice(int inum);
void _StartDevice(pcap_if_t* d, Dota_ChatMessage_Callback callback);