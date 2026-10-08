; ==============================================================================
; FastyPDF NSIS Installer Script
; ==============================================================================

!include "MUI2.nsh"
!include "x64.nsh"
!include "FileFunc.nsh"

; ------------------------------------------------------------------------------
; General Definitions
; ------------------------------------------------------------------------------
!define APP_NAME "FastyPDF"
!define APP_VERSION "1.0.0"
!define APP_PUBLISHER "FastyPDF"
!define APP_EXE "FastyPDF.exe"
!define APP_ICON "..\src\FastyPDF\Assets\AppIcon.ico"
!define SOURCE_DIR "publish"

Name "${APP_NAME} ${APP_VERSION}"
OutFile "FastyPDF_Setup_v${APP_VERSION}.exe"
InstallDir "$PROGRAMFILES64\${APP_NAME}"
InstallDirRegKey HKLM "Software\${APP_NAME}" "InstallDir"
RequestExecutionLevel admin
SetCompressor /SOLID lzma

; ------------------------------------------------------------------------------
; Interface Configuration
; ------------------------------------------------------------------------------
!define MUI_ABORTWARNING
!define MUI_ICON "${APP_ICON}"
!define MUI_UNICON "${APP_ICON}"

; Welcome Page
!insertmacro MUI_PAGE_WELCOME

; License Page (Optional / Standard MIT)
!insertmacro MUI_PAGE_LICENSE "..\LICENSE"

; Directory Selection
!insertmacro MUI_PAGE_DIRECTORY

; Installation Progress
!insertmacro MUI_PAGE_INSTFILES

; Finish Page with Run Application option
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "FastyPDF uygulamasını başlat"
!insertmacro MUI_PAGE_FINISH

; Uninstaller Pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; ------------------------------------------------------------------------------
; Languages
; ------------------------------------------------------------------------------
!insertmacro MUI_LANGUAGE "Turkish"
!insertmacro MUI_LANGUAGE "English"

; ------------------------------------------------------------------------------
; Installer Initialization
; ------------------------------------------------------------------------------
Function .onInit
    ${IfNot} ${RunningX64}
        MessageBox MB_ICONSTOP "FastyPDF sadece 64-bit (x64) Windows sistemlerini desteklemektedir."
        Abort
    ${EndIf}
FunctionEnd

; ------------------------------------------------------------------------------
; Installation Section
; ------------------------------------------------------------------------------
Section "FastyPDF Core" SecCore
    SectionIn RO

    SetOutPath "$INSTDIR"

    ; Copy published application files
    File /r "${SOURCE_DIR}\*.*"

    ; Store installation folder in Registry
    WriteRegStr HKLM "Software\${APP_NAME}" "InstallDir" "$INSTDIR"

    ; Create Uninstaller
    WriteUninstaller "$INSTDIR\uninstall.exe"

    ; Add/Remove Programs (Control Panel Registry Entries)
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayName" "${APP_NAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayVersion" "${APP_VERSION}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "Publisher" "${APP_PUBLISHER}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayIcon" "$INSTDIR\${APP_EXE}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "UninstallString" '"$INSTDIR\uninstall.exe"'
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "NoRepair" 1

    ; Estimate installed size
    ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
    IntFmt $0 "0x%08X" $0
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "EstimatedSize" "$0"

    ; Start Menu Shortcuts
    CreateDirectory "$SMPROGRAMS\${APP_NAME}"
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\Kaldır ${APP_NAME}.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0

    ; Desktop Shortcut
    CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
SectionEnd

; ------------------------------------------------------------------------------
; Uninstallation Section
; ------------------------------------------------------------------------------
Section "Uninstall"
    ; Remove shortcuts
    Delete "$DESKTOP\${APP_NAME}.lnk"
    Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
    Delete "$SMPROGRAMS\${APP_NAME}\Kaldır ${APP_NAME}.lnk"
    RMDir "$SMPROGRAMS\${APP_NAME}"

    ; Remove installed files
    RMDir /r "$INSTDIR"

    ; Remove Registry keys
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"
    DeleteRegKey HKLM "Software\${APP_NAME}"
SectionEnd
