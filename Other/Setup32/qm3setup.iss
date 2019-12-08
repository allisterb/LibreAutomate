; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "QM3"
#define MyAppVersion "0"
#define MyAppPublisher "Gintaras Did�galvis"
#define MyAppURL "https://www.quickmacros.com/au/help/"
#define MyAppExeName "Au.Editor.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{091E96D4-5062-4119-9F27-76D4BD0AAF79}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=..\..\_
OutputBaseFilename=qm3setup
Compression=lzma/normal
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
;ArchitecturesAllowed=x64 x86
MinVersion=0,6.1
DisableProgramGroupPage=yes
AppMutex=Au.Mutex.1

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
;Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "Q:\app\Au\_\Au.Editor.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Default\*"; DestDir: "{app}\Default"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Q:\app\Au\_\Au.CL.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Au.Controls.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Au.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Au.Editor.dll"; DestDir: "{app}"; Flags: ignoreversion
;Source: "Q:\app\Au\_\Au.Editor32.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Au.Net45.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Au.Task.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Au.Task.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Au.Task32.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\doc.db"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\HtmlRenderer.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Microsoft.CodeAnalysis.CSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Microsoft.CodeAnalysis.CSharp.Features.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Microsoft.CodeAnalysis.CSharp.Workspaces.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Microsoft.CodeAnalysis.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Microsoft.CodeAnalysis.Features.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Microsoft.CodeAnalysis.FlowAnalysis.Utilities.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Microsoft.CodeAnalysis.Workspaces.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Microsoft.DiaSymReader.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\ref.db"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Setup32.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\System.Composition.AttributedModel.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\System.Composition.Hosting.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\System.Composition.Runtime.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\System.Composition.TypedParts.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\SourceGrid.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\TreeList.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Au.Controls.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Au.Net45.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\Au.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\HtmlRenderer.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\SourceGrid.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\TreeList.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "Q:\app\Au\_\64\Au.AppHost.exe"; DestDir: "{app}\64"; Flags: ignoreversion
Source: "Q:\app\Au\_\64\AuCpp.dll"; DestDir: "{app}\64"; Flags: ignoreversion
Source: "Q:\app\Au\_\64\SciLexer.dll"; DestDir: "{app}\64"; Flags: ignoreversion
Source: "Q:\app\Au\_\64\sqlite3.dll"; DestDir: "{app}\64"; Flags: ignoreversion
Source: "Q:\app\Au\_\32\Au.AppHost.exe"; DestDir: "{app}\32"; Flags: ignoreversion
Source: "Q:\app\Au\_\32\AuCpp.dll"; DestDir: "{app}\32"; Flags: ignoreversion
;Source: "Q:\app\Au\_\32\SciLexer.dll"; DestDir: "{app}\32"; Flags: ignoreversion
Source: "Q:\app\Au\_\32\sqlite3.dll"; DestDir: "{app}\32"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
;Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
;register app path
Root: HKLM; Subkey: Software\Microsoft\Windows\CurrentVersion\App Paths\Au.CL.exe; ValueType: string; ValueData: {app}\Au.CL.exe; Flags: uninsdeletevalue uninsdeletekeyifempty

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure Cpp_Install(step: Integer; dir: String);
external 'Cpp_Install@files:Setup32.dll cdecl setuponly delayload';

procedure Cpp_Uninstall(step: Integer);
external 'Cpp_Uninstall@{app}\Setup32.dll cdecl uninstallonly delayload';

function InitializeSetup(): Boolean;
begin
  Cpp_Install(0, '');
  Result:=true;
end;

function InitializeUninstall(): Boolean;
begin
  Cpp_Uninstall(0);
  Result:=true;
end;

procedure CurStepChanged(CurStep: TSetupStep);
//var
//  s1: String;
begin
//  s1:=Format('%d', [CurStep]);
//  MsgBox(s1, mbInformation, MB_OK);
  
  case CurStep of
    ssInstall:
    begin
      //Cpp_Install(1, ExpandConstant('{app}\'));
    end;
    ssPostInstall:
    begin
      Cpp_Install(2, ExpandConstant('{app}\'));
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  s1: String;
begin
//  s1:=Format('%d', [CurUninstallStep]);
//  MsgBox(s1, mbInformation, MB_OK);
  
  case CurUninstallStep of
    usUninstall:
    begin
      Cpp_Uninstall(1);
      UnloadDLL(ExpandConstant('{app}\Setup32.dll'));
    end;
    usPostUninstall:
    begin
      s1:=ExpandConstant('{app}');
      if DirExists(s1) and not RemoveDir(s1) then begin RestartReplace(s1, ''); end;
    end;
  end;
end;
