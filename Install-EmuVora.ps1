param([switch]$DesktopShortcut)
$ErrorActionPreference='Stop'
$taskSource=[System.IO.Path]::GetFullPath($PSScriptRoot)
$taskPrograms=[System.IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs'))
$taskDestination=Join-Path $taskPrograms 'EmuVora'
if(!(Test-Path -LiteralPath (Join-Path $taskSource 'EmuVora.App.exe'))){throw 'Bitte das vollständige Windows-ZIP entpacken und das Installationsskript daraus starten.'}
if([System.IO.Path]::GetFullPath($taskDestination) -ne (Join-Path $taskPrograms 'EmuVora')){throw 'Ungültiges Installationsziel.'}
if($taskSource -ne $taskDestination){
    if(Get-Process -Name 'EmuVora.App' -ErrorAction SilentlyContinue){throw 'EmuVora vor der Installation schließen.'}
    New-Item -ItemType Directory -Force $taskPrograms | Out-Null
    $taskStaging=Join-Path $taskPrograms ('EmuVora-new-'+[Guid]::NewGuid().ToString('N'))
    if([System.IO.Path]::GetFullPath($taskStaging).StartsWith($taskSource+[System.IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Das Installationsziel darf nicht innerhalb des entpackten Quellordners liegen.'}
    New-Item -ItemType Directory -Force $taskStaging | Out-Null
    foreach($taskFile in Get-ChildItem -LiteralPath $taskSource -Force){Copy-Item -LiteralPath $taskFile.FullName -Destination $taskStaging -Recurse}
    if(Test-Path -LiteralPath $taskDestination){
        $taskBackup=Join-Path $taskPrograms ('EmuVora-backup-'+[DateTime]::Now.ToString('yyyyMMddHHmmss'))
        if(!([System.IO.Path]::GetFullPath($taskBackup).StartsWith($taskPrograms+[System.IO.Path]::DirectorySeparatorChar))){throw 'Ungültiger Sicherungspfad.'}
        Move-Item -LiteralPath $taskDestination -Destination $taskBackup
    }
    try{Move-Item -LiteralPath $taskStaging -Destination $taskDestination}
    catch{if($taskBackup -and (Test-Path -LiteralPath $taskBackup)){Move-Item -LiteralPath $taskBackup -Destination $taskDestination};throw}
}
$taskShell=New-Object -ComObject WScript.Shell
$taskStartMenu=Join-Path ([Environment]::GetFolderPath('Programs')) 'EmuVora.lnk'
$taskLinks=@($taskStartMenu)
if($DesktopShortcut){$taskLinks+=Join-Path ([Environment]::GetFolderPath('Desktop')) 'EmuVora.lnk'}
foreach($taskLink in $taskLinks){$taskShortcut=$taskShell.CreateShortcut($taskLink);$taskShortcut.TargetPath=Join-Path $taskDestination 'EmuVora.App.exe';$taskShortcut.WorkingDirectory=$taskDestination;$taskShortcut.Save()}
Write-Host "EmuVora installiert: $taskDestination"
Write-Host 'Deine Bibliothek bleibt unter %LOCALAPPDATA%\EmuVora. Alte Programmdateien bleiben als Sicherung erhalten.'
