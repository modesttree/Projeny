
;--------------------------------
;Defines

    !define APP_NAME "Projeny"
    !define INSTALLER_FILE_NAME "ProjenyInstaller"
    !define BIN_FOLDER_PATH "Build"

;--------------------------------
;Includes

    !include "MUI2.nsh"

;--------------------------------
;General

  ;Name and file
  Name "${APP_NAME}"
  OutFile "Dist/${INSTALLER_FILE_NAME}.exe"

  ;Default installation folder
  InstallDir "$PROGRAMFILES32\${APP_NAME}"

  ;Get installation folder from registry if available
  InstallDirRegKey HKCU "Software\${APP_NAME}" ""

  ;It seems we need admin to install to Program Files for Windows 8
  RequestExecutionLevel admin

  BrandingText "Projeny"

;--------------------------------
;Interface Settings

  !define MUI_ABORTWARNING
  !define MUI_ICON "Images\Logo-Icon.ico"
  !define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\classic-uninstall.ico"
  !define MUI_WELCOMEFINISHPAGE_BITMAP "Images\Sidebar.bmp"
  !define MUI_UNWELCOMEFINISHPAGE_BITMAP "Images\Sidebar.bmp"
  ;!define MUI_BGCOLOR FF0000

;--------------------------------
;Pages

  !insertmacro MUI_PAGE_WELCOME
  !insertmacro MUI_PAGE_DIRECTORY

  ;Start Menu Folder Page Configuration

  !insertmacro MUI_PAGE_INSTFILES

  !define MUI_FINISHPAGE_NOREBOOTSUPPORT

  !insertmacro MUI_PAGE_FINISH

  !insertmacro MUI_UNPAGE_WELCOME
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES
  !insertmacro MUI_UNPAGE_FINISH

;--------------------------------
;Languages

  !insertmacro MUI_LANGUAGE "English"

;--------------------------------
;Installer Sections

Section "Main Section" SecMain

  SetOutPath "$INSTDIR"

  File /r "${BIN_FOLDER_PATH}\*.*"

  ;Store installation folder
  WriteRegStr HKCU "Software\${APP_NAME}" "" $INSTDIR

  ;Create uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"

SectionEnd

;--------------------------------
;Descriptions

  ;Language strings
  LangString MUI_TEXT_WELCOME_INFO_TEXT ${LANG_ENGLISH} "This will install ${APP_NAME} on your computer.  Projeny is a project and package manager for Unity3D.  $\r$\n$\r$\nClick Next to continue."
  LangString MUI_TEXT_FINISH_INFO_TEXT ${LANG_ENGLISH} "Projeny has been installed on your computer.  $\r$\n$\r$\nNote that you may want to add the following directory to your windows PATH (otherwise you will have to include this full path when running Projeny from the command line):  $\r$\n$\r$\n$INSTDIR\Bin $\r$\n$\r$\nClick Finish to close Setup."


;--------------------------------
;Uninstaller Section

Section "Uninstall"

  RMDir /r "$INSTDIR"

SectionEnd
