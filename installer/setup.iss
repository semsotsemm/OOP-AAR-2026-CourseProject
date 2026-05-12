; ===========================================================================
;  Rewind installer (Inno Setup 6)
;  Build: see installer\build-installer.ps1
; ===========================================================================

#define AppName        "Rewind"
#define AppVersion     "1.0.0"
#define AppPublisher   "Rewind"
#define AppExeName     "RewindLauncher.exe"

; Папки с уже опубликованными артефактами (заполняет build-installer.ps1)
#ifndef AppPublishDir
  #define AppPublishDir     "..\build\app"
#endif
#ifndef LauncherPublishDir
  #define LauncherPublishDir "..\build\launcher"
#endif
#ifndef OutputDir
  #define OutputDir         "..\build\installer"
#endif

[Setup]
AppId={{B8A39A82-9B9E-4B3D-9D5C-REWIND0000001}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=RewindSetup
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
WizardStyle=modern
UninstallDisplayName={#AppName}
DisableDirPage=auto
CloseApplications=yes
SetupIconFile={#LauncherPublishDir}\rewind.ico
UninstallDisplayIcon={app}\{#AppExeName}
; Никаких автоматических ребутов из нашего установщика.
AlwaysRestart=no
RestartIfNeededByRun=no

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Опубликованное приложение Rewind
Source: "{#AppPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Лаунчер (Rewind*Launcher.exe + его файлы) — копируем поверх
Source: "{#LauncherPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; docker-compose.yml для БД
Source: "..\docker-compose.yml"; DestDir: "{app}\db"; Flags: ignoreversion

; Образ postgres (заранее экспортированный через `docker save`) — лаунчер сделает
; `docker load` при первом запуске, если такого образа на машине нет.
Source: "vendor\postgres-17-alpine.tar"; DestDir: "{app}\db"; Flags: ignoreversion

; Bundled Docker Desktop installer.
; ВАЖНО: мы НЕ запускаем его автоматически (silent-установка Docker Desktop
; включает Hyper-V/WSL2 и требует перезагрузки — это уже один раз привело
; к зависанию в Windows Recovery). Кладём рядом с приложением, чтобы пользователь
; мог запустить ВРУЧНУЮ в любое удобное время.
Source: "vendor\DockerDesktopInstaller.exe"; DestDir: "{app}\db"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Запустить {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Останавливаем контейнер БД при удалении (без удаления volume — данные сохраняются)
Filename: "{cmd}"; Parameters: "/C docker compose -p rewind -f ""{app}\db\docker-compose.yml"" down"; Flags: runhidden; RunOnceId: "ComposeDown"

; ===========================================================================
;  Pascal Code: проверка Docker Desktop (БЕЗ автоматической установки!)
; ---------------------------------------------------------------------------
;  Silent-установка Docker Desktop включает Hyper-V/WSL2 + требует ребута и
;  ранее приводила к зависанию в Windows Recovery. Поэтому Docker Desktop
;  installer мы только КЛАДЁМ в {app}\db и показываем пользователю инструкцию,
;  но НЕ запускаем сами. Пользователь сам решит, когда ставить Docker.
; ===========================================================================
[Code]
function IsDockerInstalled(): Boolean;
var
  ProgFiles, ProgFilesX86: String;
begin
  ProgFiles := ExpandConstant('{commonpf64}');
  ProgFilesX86 := ExpandConstant('{commonpf32}');
  Result :=
    FileExists(ProgFiles + '\Docker\Docker\Docker Desktop.exe') or
    FileExists(ProgFilesX86 + '\Docker\Docker\Docker Desktop.exe');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  AppDir: String;
  DummyResult: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    if not IsDockerInstalled() then
    begin
      AppDir := ExpandConstant('{app}');
      MsgBox(
        'Установка Rewind завершена.' + #13#10 + #13#10 +
        'ВНИМАНИЕ: для работы базы данных нужен Docker Desktop, а он на этом ПК не обнаружен.' + #13#10 + #13#10 +
        'Установщик Docker Desktop лежит здесь:' + #13#10 +
        AppDir + '\db\DockerDesktopInstaller.exe' + #13#10 + #13#10 +
        'Запустите его ВРУЧНУЮ, когда сможете спокойно перезагрузить компьютер.' + #13#10 +
        'После установки Docker Desktop и одной перезагрузки запустите Rewind с рабочего стола.',
        mbInformation, MB_OK);
      // Открываем папку с установщиком Docker, чтобы пользователю было удобно его найти.
      // ShellExec на папку — это просто explorer.exe, никаких изменений в системе.
      ShellExec('open', AppDir + '\db', '', '', SW_SHOW, ewNoWait, DummyResult);
    end;
  end;
end;
