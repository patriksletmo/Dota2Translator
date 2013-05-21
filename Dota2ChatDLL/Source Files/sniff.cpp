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
#include <TlHelp32.h>
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
double GetTimeDiff(time_t t, double defaultValue);
void ReportTime(time_t *t);
void RetrievePIDs(int *resultBuffer, int *resultLength, int maxLength);
Addresses GetAdapterMAC(pcap_if_t* Device);

// Constants identifying the chat messages.
const wstring ALL_CHAT_IDENTIFIER = L"DOTA_Chat_All";
const wstring TEAM_CHAT_IDENTIFIER = L"DOTA_Chat_Team";

// Amount of seconds to wait before each automatic port scan.
const int AUTO_DETECT_RATE = 10;

// The port to use if autodetection is off.
const int DEFAULT_PORT = 27005;

// Stored instance of all queried devices, in order to access the correct device using a number later.
pcap_if_t *Alldevs;

// Used for hiding the overlay when disconnected from a match.
time_t LastPacket = NULL;
bool Hidden = false;

// Whether or not to automatically detect the port number(s).
bool AutoDetectPort = true;

// The time of the last automated port scan.
time_t LastScan = NULL;

// An array containing all the detected ports.
int *DetectedPorts;
int DetectedPortsLength = 0;

// The name of the Dota executable.
WCHAR *ExecutableName = L"dota.exe";

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
	pcap_t *fp = pcap_open(d->name, 100, 0, 20, NULL, errbuf);

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

		// Determine ethernet packet type.
		int packet_type = (pkt_data[12]*256) + pkt_data[13];

		if (packet_type == 0x8864)
		{
			// We're receiving a PPPoE packet.

			// Shift the packet data by 8 bytes.
			pkt_data += 8;
			header->len -= 8;
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

		bool updatePorts = AutoDetectPort && GetTimeDiff(LastScan, 10) >= 10;
		if (updatePorts)
		{
			// Retrieve the PIDs representing a Dota 2 executable.
			int dotaPIDs[16];
			int numPIDs;
			RetrievePIDs(dotaPIDs, &numPIDs, 16); // Limit the results to 16 PIDs. It's hard to imagine a possible scenario where one would want to have this many Dota 2 instances running simultaneously.

			MIB_UDPTABLE_OWNER_PID *table;
			DWORD buffSize;

			// Retrieve the structure size.
			GetExtendedUdpTable(NULL, &buffSize, false, AF_INET, UDP_TABLE_OWNER_PID, 0);

			// Alloc the structure memory and retrieve the table.
			table = (MIB_UDPTABLE_OWNER_PID *) malloc(buffSize);
			GetExtendedUdpTable(table, &buffSize, false, AF_INET, UDP_TABLE_OWNER_PID, 0);

			DWORD numEntries = table->dwNumEntries;

			// Reset the port list.
			DetectedPorts = (int *)malloc(numEntries);
			DetectedPortsLength = 0;
			
			for (int i = 0; i < numEntries; i++)
			{
				MIB_UDPROW_OWNER_PID row = table->table[i];
				int pid = row.dwOwningPid;
				int port = ntohs(row.dwLocalPort);

				// Loop over every found PID and check if there's a match.
				bool isDota = false;
				for (int j = 0; j < numPIDs; j++)
				{
					if (dotaPIDs[j] == pid)
					{
						isDota = true;
						break;
					}
				}

				if (isDota)
				{
					// Add the port to the list.
					DetectedPorts[DetectedPortsLength] = port;
					DetectedPortsLength++;
				}
			}

			ReportTime(&LastScan);
		}
	}

	// Close the opened device.
	pcap_close(fp);
}

void _SetAutoDetectPort(bool autoDetect)
{
	AutoDetectPort = autoDetect;

	// Trigger update.
	LastScan = NULL;
}

void _SetAutoDetectProgram(char* _exeName)
{
	// Convert _exeName to WCHAR.
	int length = MultiByteToWideChar(CP_ACP, 0, _exeName, -1, NULL, 0);
	WCHAR *exeName = new WCHAR[length];
	MultiByteToWideChar(CP_ACP, 0, _exeName, -1, (LPWSTR)exeName, length);

	ExecutableName = exeName;

	// Trigger update.
	LastScan = NULL;
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
	double diff = GetTimeDiff(LastPacket, 0);

	// Return true if no packet has been received in the last 2 seconds.
	return diff > 1;
}

double GetTimeDiff(time_t t, double defaultValue)
{
	// Return default value if the parameter is NULL.
	if (t == NULL)
		return defaultValue;

	time_t now;
	time(&now);

	return difftime(now, t);
}

// Reports the time of the last packet.
void ReportTime(time_t *t)
{
	time(t);
}

void RetrievePIDs(int *resultBuffer, int *resultLength, int maxLength)
{
	PROCESSENTRY32 entry;
	entry.dwSize = sizeof(PROCESSENTRY32);

	HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, NULL);

	// Loop over every open program.
	int offset = 0;
	if ((Process32First(snapshot, &entry) == TRUE))
	{
		while (Process32Next(snapshot, &entry) == TRUE)
		{
			// Check if the program is Dota using it's executable name.
			if (_wcsicmp(entry.szExeFile, ExecutableName) == 0)
			{
				// Add the PID to the result buffer.
				resultBuffer[offset] = entry.th32ProcessID;
				offset++;

				// Avoid buffer overflows.
				if (offset >= maxLength)
					break;
			}
		}
	}

	*resultLength = offset;
}

// Processes the specified UDP packet.
static void HandleUDPPacket(unsigned char *data, int length, int more, Dota_ChatMessage_Callback callback)
{
	int port_dst = (data[2]*256)+data[3];

	bool portMatch = false;
	if (AutoDetectPort && DetectedPortsLength > 0) // Fall back to manual mode if no port has been detected.
	{
		// Check if the port is used by any Dota 2 process.
		for (int i = 0; i < DetectedPortsLength; i++)
		{
			if (port_dst == DetectedPorts[i])
			{
				portMatch = true;
				break;
			}
		}
	}
	else
	{
		// Manual mode, use the default port.
		portMatch = port_dst == DEFAULT_PORT;
	}

	if (portMatch)
	{
		// Report the time of the received Dota 2 packet. The overlay will be hidden if this method is not called more often than every 2 seconds.
		ReportTime(&LastPacket);

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
		else
		{
			// Search for DotaTV chat.
			for (int i = 0; i < length - 8 - 6; i++)
			{
				// The structure for DotaTV is a bit more complicated than the normal chat,
				// we have to search for multiple bytes at different locations.
				// 
				// Structure:
				// 01 1a XX 08 01 12 [Sender length, 1 byte] [Sender] XX XX 08 01 12 [Message length, 1 byte] [Message]
				// Numbers in hexadecimal represent constant bytes, text in brackets represents wanted data
				// and XX represents (currently) unknown bytes.

				int b0 = data2[i];
				int b1 = data2[i + 1];
				int b3 = data2[i + 3];
				int b4 = data2[i + 4];
				int b5 = data2[i + 5];

				if (b0 == 0x01 &&
					b1 == 0x1a &&
					b3 == 0x08 &&
					b4 == 0x01 &&
					b5 == 0x12)
				{
					chat_found = i + 7;

					int index_name = chat_found;
					int length_name = data2[index_name - 1];

					// Double check that this indeed is a DotaTV chat.
					int b6 = data2[index_name + length_name + 2];
					int b7 = data2[index_name + length_name + 3];
					int b8 = data2[index_name + length_name + 4];

					if (b6 == 0x08 &&
						b7 == 0x01 &&
						b8 == 0x12)
					{
						int index_message = index_name + length_name + 6;
						int length_message = data2[index_message - 1];

						wstring name = content.substr(index_name, length_name);
						wstring message = content.substr(index_message, length_message);

						// Put the data in a struct.
						Dota_ChatMessage cm;
						cm.Type = 2;
						cm.Sender = name.c_str();
						cm.Message = message.c_str();

						// Send the received message to the program.
						callback(cm);
					}
				}
			}
		}
	}
}

