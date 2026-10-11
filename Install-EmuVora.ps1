param([string]$DestinationPath,[string]$DataPath,[switch]$DesktopShortcut,[switch]$SkipShortcuts)
$ErrorActionPreference='Stop'
function Test-EmuVoraAbsolutePath([string]$Value){return $Value -match '^(?:[A-Za-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+)'}
$taskSource=[IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
if(!(Test-Path -LiteralPath (Join-Path $taskSource 'EmuVora.App.exe'))){throw 'Bitte das vollständige Windows-ZIP entpacken und das Installationsskript daraus starten.'}
if(!$DestinationPath){
    Add-Type -AssemblyName System.Windows.Forms
    $taskPicker=New-Object System.Windows.Forms.FolderBrowserDialog
    $taskPicker.Description='Festplatte und Ordner für EmuVora wählen. Programm und Daten liegen in den Unterordnern App und Data.'
    $taskPicker.ShowNewFolderButton=$true
    $taskPicker.SelectedPath=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'EmuVora'
    try{if($taskPicker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK){return};$DestinationPath=$taskPicker.SelectedPath}finally{$taskPicker.Dispose()}
}
if(!(Test-EmuVoraAbsolutePath $DestinationPath)){throw 'Ein vollständiger Installationspfad ist erforderlich.'}
$taskRoot=[IO.Path]::GetFullPath($DestinationPath).TrimEnd('\')
if($taskRoot -eq [IO.Path]::GetPathRoot($taskRoot).TrimEnd('\')){throw 'Bitte einen Unterordner statt eines gesamten Laufwerks wählen.'}
if($taskRoot -eq $taskSource -or $taskRoot.StartsWith($taskSource+'\',[StringComparison]::OrdinalIgnoreCase) -or $taskSource.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Installations- und entpackter Quellordner dürfen sich nicht überlappen.'}
$taskDestination=Join-Path $taskRoot 'App'
if(!$DataPath){$DataPath=Join-Path $taskRoot 'Data'}
if(!(Test-EmuVoraAbsolutePath $DataPath)){throw 'Ein vollständiger Datenpfad ist erforderlich.'}
$taskData=[IO.Path]::GetFullPath($DataPath).TrimEnd('\')
if($taskData -eq [IO.Path]::GetPathRoot($taskData).TrimEnd('\') -or $taskData -eq $taskDestination -or $taskData.StartsWith($taskDestination+'\',[StringComparison]::OrdinalIgnoreCase) -or $taskDestination.StartsWith($taskData+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Der Datenordner muss außerhalb des Programmordners liegen und darf kein Laufwerksstamm sein.'}
foreach($taskTarget in @($taskRoot,$taskData)){
    for($taskAncestor=$taskTarget;$taskAncestor;$taskAncestor=[IO.Path]::GetDirectoryName($taskAncestor)){
        if((Test-Path -LiteralPath $taskAncestor) -and ((Get-Item -LiteralPath $taskAncestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Installationspfade mit Ordnerverknüpfungen werden nicht verwendet.'}
    }
}
if(Get-Process -Name 'EmuVora.App' -ErrorAction SilentlyContinue){throw 'EmuVora vor der Installation schließen.'}
if((Test-Path -LiteralPath $taskDestination) -and !(Test-Path -LiteralPath (Join-Path $taskDestination 'EmuVora.App.exe'))){throw 'Der App-Unterordner enthält keine EmuVora-Installation. Bitte einen anderen Zielordner wählen.'}
New-Item -ItemType Directory -Force $taskRoot,$taskData | Out-Null
$taskStaging=Join-Path $taskRoot ('App-new-'+[Guid]::NewGuid().ToString('N'))
$taskBackup=$null
New-Item -ItemType Directory -Force $taskStaging | Out-Null
foreach($taskFile in Get-ChildItem -LiteralPath $taskSource -Force){
    if($taskFile.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Das entpackte Programmpaket enthält Verknüpfungen.'}
    Copy-Item -LiteralPath $taskFile.FullName -Destination $taskStaging -Recurse
}
@{DataDirectory=$taskData} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskStaging 'emuvora-location.json') -Encoding UTF8
if(Test-Path -LiteralPath $taskDestination){
    $taskBackup=Join-Path $taskRoot ('App-backup-'+[Guid]::NewGuid().ToString('N'))
    foreach($taskMove in @($taskDestination,$taskBackup)){
        if(!([IO.Path]::GetFullPath($taskMove).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase))){throw 'Ungültiger Sicherungspfad.'}
    }
    Move-Item -LiteralPath $taskDestination -Destination $taskBackup
}
if(!([IO.Path]::GetFullPath($taskStaging).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase))){throw 'Ungültiger Programmpfad.'}
try{Move-Item -LiteralPath $taskStaging -Destination $taskDestination}
catch{if($taskBackup -and (Test-Path -LiteralPath $taskBackup)){Move-Item -LiteralPath $taskBackup -Destination $taskDestination};throw}
if(!$SkipShortcuts){
    $taskShell=New-Object -ComObject WScript.Shell
    $taskLinks=@((Join-Path ([Environment]::GetFolderPath('Programs')) 'EmuVora.lnk'))
    if($DesktopShortcut){$taskLinks+=Join-Path ([Environment]::GetFolderPath('Desktop')) 'EmuVora.lnk'}
    foreach($taskLink in $taskLinks){$taskShortcut=$taskShell.CreateShortcut($taskLink);$taskShortcut.TargetPath=Join-Path $taskDestination 'EmuVora.App.exe';$taskShortcut.WorkingDirectory=$taskDestination;$taskShortcut.Save()}
}
Write-Host "EmuVora installiert: $taskDestination"
Write-Host "Bibliothek / Emulatoren / Cores / Cover / Spielstände: $taskData"
Write-Host 'Bestehende Bibliothek: bisherigen Ordner mit -DataPath angeben. ROMs und bestehende Daten werden nicht verschoben oder gelöscht.'
