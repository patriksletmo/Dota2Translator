; Modern UI 2.
!include "MUI2.nsh"

; Used to remove all the files in a directory.
!include "RemoveFilesAndSubDirs.nsh"

; Define name and output file.
Name "Dota 2 Translator"
OutFile "Dota2Translator_Setup.exe"

; Install the program in %PROGRAMFILES%\Dota 2 Translator.
InstallDir "$PROGRAMFILES\Dota 2 Translator"

; Define paths for our program output and licenses.
!define OUT "..\Dota2ChatInterface\bin\Release"
!define LICENSES "..\Licenses"

; Run the setup with administrator privileges.
RequestExecutionLevel admin

; Add additional options.
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\Dota2ChatInterface.exe"

; Add pages.
!insertmacro MUI_PAGE_LICENSE "${LICENSES}\LICENSE.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

; Add uninstallation pages.
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

; Mandatory for MUI2.
!insertmacro MUI_LANGUAGE "English"

Section "Dota 2 Translator"
	SetOutPath "$INSTDIR"
	
	; Copy the program files.
	File ${OUT}\Dota2ChatInterface.exe
	File ${OUT}\Dota2ChatDLL.dll
	File ${OUT}\DotaDXInject.dll
	File ${OUT}\EasyHook.dll
	File ${OUT}\EasyHook32.dll
	File ${OUT}\Newtonsoft.Json.dll
	
	; Add license files.
	createDirectory "$INSTDIR\Licenses"
	SetOutPath "$INSTDIR\Licenses"
	File ${LICENSES}\LICENSE.txt
	createDirectory "$INSTDIR\Licenses\Direct3DHook"
	SetOutPath "$INSTDIR\Licenses\Direct3DHook"
	File ${LICENSES}\Direct3DHook\LICENSE.txt
	createDirectory "$INSTDIR\Licenses\EasyHook"
	SetOutPath "$INSTDIR\Licenses\EasyHook"
	File ${LICENSES}\EasyHook\LICENSE.txt
		createDirectory "$INSTDIR\Licenses\SlimDX"
	SetOutPath "$INSTDIR\Licenses\SlimDX"
	File ${LICENSES}\SlimDX\LICENSE.txt
	createDirectory "$INSTDIR\Licenses\WinPcap"
	SetOutPath "$INSTDIR\Licenses\WinPcap"
	File ${LICENSES}\WinPcap\LICENSE.txt
	
	; Reset out path.
	SetOutPath "$INSTDIR"
	
	; Write an uninstaller.
	writeUninstaller "$INSTDIR\uninstall.exe" 
	
	; Create a start menu entry.
	createDirectory "$SMPROGRAMS\Dota 2 Translator"
	createShortCut "$SMPROGRAMS\Dota 2 Translator\Dota 2 Translator.lnk" "$INSTDIR\Dota2ChatInterface.exe" "" ""
	createShortCut "$SMPROGRAMS\Dota 2 Translator\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" ""
SectionEnd

Section "WinPcap"
	; Install WinPcap.
	File "Dependencies\WinPcap_4_1_2.exe"
	ExecWait "$INSTDIR\WinPcap_4_1_2.exe /S"
SectionEnd

Section "SlimDX"
	; Install SlimDX.
	File "Dependencies\SlimDX Runtime .NET 4.0 x86 (January 2012).msi"
	ExecWait "msiexec /package $\"SlimDX Runtime .NET 4.0 x86 (January 2012).msi$\" /passive"
SectionEnd

Section "C++ Redist 2010"
	; Install Microsoft Visual C++ 2010 Redistributable. 
	File "Dependencies\vcredist_x86.exe"
	ExecWait "vcredist_x86.exe /passive"
SectionEnd

Section "Uninstall"
	; Delete the license files.
	!insertmacro RemoveFilesAndSubDirs "$INSTDIR\Licenses"
	rmDir "$INSTDIR\Licenses"

	; Delete dependency installers.
	Delete "$INSTDIR\WinPcap_4_1_2.exe"
	Delete "$INSTDIR\SlimDX Runtime .NET 4.0 x86 (January 2012).msi"
	Delete "$INSTDIR\vcredist_x86.exe"

	; Delete exe's and dll's.
	Delete "$INSTDIR\Dota2ChatInterface.exe"
	Delete "$INSTDIR\Dota2ChatDLL.dll"
	Delete "$INSTDIR\DotaDXInject.dll"
	Delete "$INSTDIR\EasyHook.dll"
	Delete "$INSTDIR\EasyHook32.dll"
	Delete "$INSTDIR\Newtonsoft.Json.dll"
	
	; Delete the start menu entry.
	Delete "$SMPROGRAMS\Dota 2 Translator\Dota 2 Translator.lnk"
	Delete "$SMPROGRAMS\Dota 2 Translator\Uninstall.lnk"
	rmDir "$SMPROGRAMS\Dota 2 Translator"
	
	; Delete the uninstaller.
	Delete "$INSTDIR\uninstall.exe"
	
	; Delete the install directory.
	rmDir "$INSTDIR"
SectionEnd
	
