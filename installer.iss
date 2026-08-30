; Modern TRCC - Inno Setup 安装脚本
; 用法: ISCC.exe installer.iss

#define MyAppName "Modern TRCC"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "穗月"
#define MyAppExeName "ModernTRCC.exe"
#define MyAppId "{{8F3C1E2A-5B4D-4C6E-9A7F-2D1E0B3C4A5D}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\ModernTRCC
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=installer
OutputBaseFilename=ModernTRCC-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
SetupIconFile=publish\Resources\TRCC.ico
UninstallDisplayIcon={app}\Resources\TRCC.ico
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} 安装程序
MinVersion=10.0.17763
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "chinesesimp"; MessagesFile: "installer-lang\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\ModernTRCC.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\Resources\*"; DestDir: "{app}\Resources"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "publish\ui\*"; DestDir: "{app}\ui"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "publish\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "publish\ffmpeg\*"; DestDir: "{app}\ffmpeg"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// 检查 .NET 8 Desktop Runtime 是否已安装（x64）
function IsDotNet8DesktopInstalled(): Boolean;
var
  subKey: String;
begin
  Result := False;
  subKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  if RegKeyExists(HKLM, subKey) or RegKeyExists(HKCU, subKey) then
  begin
    if RegValueExists(HKLM, subKey, '8.0.0') or RegValueExists(HKCU, subKey, '8.0.0') then
      Result := True;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNet8DesktopInstalled() then
  begin
    if MsgBox('未检测到 .NET 8 Desktop Runtime。' + #13#10 +
              'Modern TRCC 需要该运行库才能运行。' + #13#10 + #13#10 +
              '是否继续安装？（安装后请手动安装运行库）' + #13#10 +
              '下载地址: https://dotnet.microsoft.com/download/dotnet/8.0',
              mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // 首次安装后创建媒体目录，避免启动时扫描报错
    if not DirExists(ExpandConstant('{userappdata}\ModernTRCC\Media')) then
      CreateDir(ExpandConstant('{userappdata}\ModernTRCC\Media'));
  end;
end;
