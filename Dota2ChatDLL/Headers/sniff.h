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
void _SetAutoDetectPort(bool autoDetect);