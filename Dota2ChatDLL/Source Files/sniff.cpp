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
#include <iostream>
#include <stdlib.h>
#include <stdio.h>
#include <string>
#include <sstream>
#include <vector>
#include <pcap.h>
#include <math.h>
#include "sniff.h"
#include <winsock2.h>
#include <IPHlpApi.h>
#pragma comment(lib, "IPHLPAPI.lib")

using namespace std;

// Used for retrieving the MAC and IP address of a network adapter.
struct Addresses
{
	unsigned char* MAC;
	char* IP;
};

// Methods.
void HandleIPPacket(unsigned char *data, int length, int more, Dota_ChatMessage_Callback callback);
void HandleUDPPacket(unsigned char *data, int length, int more, Dota_ChatMessage_Callback callback);
bool ExamineTime();
void ReportTime();
Addresses GetAdapterMAC(pcap_if_t* Device);

// Constants identifying the chat messages.
const wstring ALL_CHAT_IDENTIFIER = L"DOTA_Chat_All";
const wstring TEAM_CHAT_IDENTIFIER = L"DOTA_Chat_Team";

// Stored instance of all queried devices, in order to access the correct device using a number later.
pcap_if_t *Alldevs;

// Used for hiding the overlay when disconnected from a match.
time_t LastPacket = NULL;
bool Hidden = false;

// Returns an Addresses struct containing the MAC and IP address of the specified device.
Addresses GetAdapterMAC(pcap_if_t* Device)
{
	// Instance an empty struct that we return if no match was found (which is highely unlikely).
	Addresses a;
	a.MAC = new unsigned char[6];
	a.IP = new char[16];

	// Make room for up to 48 entries of IP_ADAPTER_INFO.
	IP_ADAPTER_INFO* AdapterInfo = new IP_ADAPTER_INFO [48]; 
	ULONG AIS = sizeof(IP_ADAPTER_INFO) * 48; 

	// Load the entries from the system.
	GetAdaptersInfo(AdapterInfo,&AIS);

	// Loop over the entries.
	for(IP_ADAPTER_INFO* Current = AdapterInfo; Current != NULL; Current = Current->Next)
	{		
		// If the current entry equals
		if(strstr(Device->name,Current->AdapterName)!=0)
		{
			// Insert the values into the struct.
			memcpy((void*)a.MAC,(void*)(Current->Address),6);
			a.IP = Current->IpAddressList.IpAddress.String;

			return a;
		}
    }

	return a;
}

// Returns (a string with) all available network adapters to listen on.
string _GetDeviceList()
{
	// The string to return.
	string out = "";

	// An error buffer which is (currently) not in use.
	char errbuf[PCAP_ERRBUF_SIZE];

	// Retrieve all the network adapters on the system.
	pcap_findalldevs_ex(PCAP_SRC_IF_STRING, NULL, &Alldevs, errbuf);

	int i = 0;
	pcap_if_t *d;

	// Loop over the devices, adding them to the output string.
	for (d = Alldevs; d; d = d->next)
	{
		// Load the MAC and IP address for the adapter.
		Addresses addresses = GetAdapterMAC(d);

		// Print the data for the current adapter to a buffer.
		char buffer[256];
		sprintf_s(buffer, sizeof(buffer), "%02x%02x%02x%02x%02x%02x\n%s\n%s\n",
			addresses.MAC[0],
			addresses.MAC[1],
			addresses.MAC[2],
			addresses.MAC[3],
			addresses.MAC[4],
			addresses.MAC[5],
			addresses.IP,
			d->description
		);

		// Above format:
		//   MAC Address\n
		//   IP Address\n
		//   Adapter description\n
		
		// Append the buffer to the output string.
		out = out + buffer;
	}
	
	return out;
}

// Returns a pointer to the specified device. The zero-based device index is based on the list returned by _GetDeviceList which MUST be called before this method.
pcap_if_t* _GetDevice(int inum)
{
	pcap_if_t *d;
	int i;

	// Loop over the list until we have the correct adapter.
	for (d = Alldevs, i = 0; i < inum; d = d->next, i++);

	return d;
}

// Starts listening for packets on the specified device. The callback parameter is called when a Dota 2 message has been received.
void _StartDevice(pcap_if_t* d, Dota_ChatMessage_Callback callback)
{
	// An error buffer which is (also currently) not in use.
	char errbuf[PCAP_ERRBUF_SIZE];

	// Open the device for listening.
	pcap_t *fp = pcap_open(d->name, 100, PCAP_OPENFLAG_PROMISCUOUS, 20, NULL, errbuf);

	int res;
	struct pcap_pkthdr *header;
	const u_char *pkt_data;

	// Listen for packets on the newely opened device.
	while ((res = pcap_next_ex(fp, &header, &pkt_data)) >= 0)
	{
		// Continue listening if nothing has been received.
		if (res == 0)
		{
			continue;
		}

		// Process the packet received.
		HandleIPPacket((unsigned char*)(pkt_data + 14), (header->len) - 14, 1, callback);
		
		// Check if we should hide the overlay.
		bool hide = ExamineTime();
		if (hide && !Hidden)
		{
			// The overlay is not hidden but should be. Send a hide message.
			Dota_ChatMessage hideMessage;
			hideMessage.Type = -1; // The chat message struct handles these things as well.

			// The program will crash due to an access violation if we don't initiate these fields.
			hideMessage.Sender = L"";
			hideMessage.Message = L"";
			callback(hideMessage);

			// Mark the overlay as hidden.
			Hidden = true;
		}
		else if (!hide && Hidden)
		{
			// Overlay is hidden but shouldn't be. Send a show message.
			Dota_ChatMessage showMessage;
			showMessage.Type = -2; // The chat message struct handles these things as well.

			// The program will crash due to an access violation if we don't initiate these fields.
			showMessage.Sender = L"";
			showMessage.Message = L"";
			callback(showMessage);

			// Mark the overlay as not hidden.
			Hidden = false;
		}
	}

	// Close the opened device.
	pcap_close(fp);
}

// Processes the specified packet.
static void HandleIPPacket(unsigned char *data, int length, int more, Dota_ChatMessage_Callback callback)
{
	// Is this an UDP packet? (17 = UDP).
	if (data[9] == 17)
	{
		HandleUDPPacket(data+(((char)(data[0]<<4))>>2), length-(((char)(data[0]<<4))>>2), more, callback);
	}
}

// Returns true if a specific amount of time has passed since the last packet.
bool ExamineTime()
{
	// Don't hide if no Dota packet has been received.
	if (LastPacket == NULL)
		return false;

	time_t now;
	time(&now);

	double diff = difftime(now, LastPacket);

	// Return true if no packet has been received in the last 2 seconds.
	return diff > 1;
}

// Reports the time of the last packet.
void ReportTime()
{
	time(&LastPacket);
}

// Processes the specified UDP packet.
static void HandleUDPPacket(unsigned char *data, int length, int more, Dota_ChatMessage_Callback callback)
{
	int port_dst = (data[2]*256)+data[3];

	// Dota 2 game traffic is received on port 27005.
	if (port_dst == 27005)
	{
		// Report the time of the received Dota 2 packet. The overlay will be hidden if this method is not called more often than every 2 seconds.
		ReportTime();

		// Strip away the UDP fields.
		unsigned char* data2 = data+8;

		// Convert the packet content to a wstring.
		wstring content = wstring(data2, data2 + (length-8));

		// Search for ALL_CHAT_IDENTIFIER in the packet.
		int identifierLength = ALL_CHAT_IDENTIFIER.size() + 2;
		size_t chat_found = content.find(ALL_CHAT_IDENTIFIER);
		int type = 0;

		// Search for TEAM_CHAT_IDENTIFIER if no ALL_CHAT_IDENTIFIER has been found.
		if (chat_found == string::npos)
		{
			chat_found = content.find(TEAM_CHAT_IDENTIFIER);
			identifierLength = TEAM_CHAT_IDENTIFIER.size() + 2;
			type = 1;
		}

		if (chat_found != string::npos)
		{
			// Parse the data from the packet.
			// Format:
			// "[ALL/TEAM]_CHAT_IDENTIFIER"
			// [ unknown (byte) ]
			// [ sender length (byte) ]
			// [ sender (string) ]
			// [ unknown (byte) ]
			// [ message length (byte) ]
			// [ message (string) ]

			int index_name = chat_found + identifierLength;
			int length_name = data2[index_name - 1];
			int length_message = data2[index_name + length_name + 1];
			int index_message = index_name + length_name + 2;

			wstring name = content.substr(index_name, length_name);
			wstring message = content.substr(index_message, length_message);

			// Put the data in a struct.
			Dota_ChatMessage cm;
			cm.Type = type;
			cm.Sender = name.c_str();
			cm.Message = message.c_str();

			// Send the received message to the program.
			callback(cm);
		}		
	}
}

