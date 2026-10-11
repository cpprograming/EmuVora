$ErrorActionPreference='Stop'
$taskTestRoot=Join-Path $env:RUNNER_TEMP ('EmuVora-install-'+[Guid]::NewGuid().ToString('N'))
$taskInstall=Join-Path $taskTestRoot 'Selected drive with spaces'
$taskData=Join-Path $taskTestRoot 'Separate library'
$taskScript=Join-Path $PSScriptRoot '../publish/Install-EmuVora.ps1'
& $taskScript -DestinationPath $taskInstall -DataPath $taskData -SkipShortcuts
$taskExe=Join-Path $taskInstall 'App/EmuVora.App.exe'
if(!(Test-Path -LiteralPath $taskExe)){throw 'The selected program directory was ignored.'}
$taskLocator=Get-Content -Raw -LiteralPath (Join-Path $taskInstall 'App/emuvora-location.json') | ConvertFrom-Json
if($taskLocator.DataDirectory -ne $taskData){throw 'The separate data directory was ignored.'}
'retain saves' | Set-Content -LiteralPath (Join-Path $taskData 'save-test.txt')
& $taskScript -DestinationPath $taskInstall -DataPath $taskData -SkipShortcuts
if((Get-Content -LiteralPath (Join-Path $taskData 'save-test.txt')) -ne 'retain saves'){throw 'Update changed user data.'}
if(@(Get-ChildItem -LiteralPath $taskInstall -Filter 'App-backup-*').Count -ne 1){throw 'The old program directory was not retained.'}
$taskRejected=$false
try{& $taskScript -DestinationPath $taskInstall -DataPath (Join-Path $taskInstall 'App/unsafe') -SkipShortcuts}catch{$taskRejected=$true}
if(!$taskRejected){throw 'Program/data overlap was accepted.'}
Write-Host 'Selectable installation, separate data, retained saves, backup and overlap validation passed.'
