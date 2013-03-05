
------- DOTA 2 TRANSLATOR -------

- INTRODUCTION -
Dota 2 Translator is a free add-on program used to translate the chat in the game.

The program filters packet data from WinPcap and strips out incoming chat messages. The
messages are translated using Google Translate to the language of choise and then either
displayed in the program itself or in the game using hooked DirectX 9 functions. The
overlay is scaled to match the current display resolution and can be fine-tuned from the
settings panel included within the program.

- BUILDING -
In order to build Dota 2 you must first download Visual Studio 2010 (express version 
will work as well) or later and install the SlimDX SDK (which can be downloaded from 
http://slimdx.org/download.php).

When these dependencies have been installed you can open up the solution. There is a
solution file called Dota2ChatDLL.sln which contains all the projects, open it with
Visual Studio.

There will be some changes that needs to be done before you can build the project.
1. Make sure that Dota2ChatInterface is the startup project, you can edit this by
right-clicking the project and selecting "Set as Startup Project".
2. If you downloaded the 2012 version of Visual Studio you have to change the platform
toolset of the project Dota2ChatDLL. Do this by right-clicking the projects, choosing
properties and changing "Platform Toolset" to "Visual Studio 2012 (v110). While this
enables you to build the project without installing Visual Studio 2010, it will
result in end users having to download the Visual C++ 2012 Redistributable instead of
the 2010 version.
3. Make sure to build the project for Release and x86. The projects have not been
configured for building in any other mode and will most likely not succeed.
4. Build the project!

- CREATING A SETUP -
1. Download an install NSIS from http://nsis.sourceforge.net. This program is used to build
the setup file from the setup script included. 
2. Open the script ./Setup/CreateSetup.nsi with NSIS. If you opened it by right-clicking
the file and choosing compile it will be compiled automatically.
3. Compile the script if this was not done automatically.
4. There will be a setup file created at ./Setup/Dota2Translator_Setup.exe.

If I missed something in the steps for building or creating a setup, please report an issue here on Github.
