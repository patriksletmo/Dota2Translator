![Dota 2 Translator](http://i.imgur.com/9yz2hyY.png)

## Introduction

Ever been in a game with a team insisting not to speak English? It might start good, but the lack of basic communication will eventually make it very hard to win later in the game. Dota 2 Translator is a free add-on program that elimates the language barriers in Dota 2 by automatically translating incoming chat messages, displaying them ingame into your native language. This program aims to create a more pleasant experience when not playing with pre-made teams.

The addon works by intercepting network traffic, parsing the data stream for incoming chat messages which are then in turn translated using Google Translate into which ever language you choose. The results can be displayed within the application or integrated into the game client using a DirectX 9 overlay which is automatically scaled to match the current display resolution.

It won't trigger any false VAC reports as it does not hook into the Dota 2 network handler but instead works at a lower level (link-layer) that the application which in turn runs ontop of. This method of data inception, for the purpose of language translation been blessed by the anti cheat/hack team at Valve as being above board. i.e don't worry about it :)


## Screenshot

!["Yes I am Russian"](http://i.imgur.com/5AT6JUV.png?1?7557)


## Installation

Download and install [this package](http://sletmo.com/download "Dota 2 Translator Download"), upon first launch it  update to the latest version. From then on, future launches of the addon will check for updates upon startup, notify you of updates and confirm with you before installing any updates.

> This program has not been "signed" - which means that antivirus programs and windows 8 smart screen may display a confirmation of "Continue execution" upon application startup - please select the "confirm/yes" option on this dialog. If you are concerned about this you are totally free to review the application source code, built it yourself and make changes as the addon is open-source under the very liberal MIT license.

## Building From Source

### Main Application

In order to build Dota 2 you must install Visual Studio 2010 (express version works) and the [SlimDX SDK](http://slimdx.org/download.php "SlimDX SDK").

There is a solution file called Dota2ChatDLL.sln which contains all the projects, open it with
Visual Studio and run through this checklist:

1. Make sure that Dota2ChatInterface is the startup project.
    
    This can be changed by right-clicking the project and selecting "Set as Startup Project".

2. If you downloaded the 2012 version of Visual Studio you have to change the platform toolset of the project Dota2ChatDLL:

    Right-click on each project, navigate to properties and changw "Platform Toolset" to "Visual Studio 2012 (v110). 

    While this enables you to build the project without installing Visual Studio 2010, it will result in end users having to download the Visual C++ 2012 Redistributable instead of the 2010 version.

3. Make sure to build the project for Release and x86. 

    The projects have not been configured for building in any other mode and will most likely not succeed.

4. Build the project!

### Application Installer

1. Download and install [NSIS]( http://nsis.sourceforge.net "NSIS"). 

    This program is used to build the setup file from the setup script included. 

2. Open the script ./Setup/CreateSetup.nsi with NSIS.

     If you opened it by right-clicking the file and choosing compile it will be compiled automatically.

3. Compile the script if this was not done automatically.

4. There will be a setup file created at ./Setup/Dota2Translator_Setup.exe.
