param([Parameter(Mandatory=$true)][string]$SetupExe)
$ErrorActionPreference='Stop'
$taskExe=[IO.Path]::GetFullPath($SetupExe)
$taskRoot=Join-Path $env:RUNNER_TEMP ('EmuVora-exe-test-'+[Guid]::NewGuid().ToString('N'))
$taskInstall=Join-Path $taskRoot 'Chosen drive & spaces'
function Invoke-TestSetup{
    $taskProcess=Start-Process -FilePath $taskExe -ArgumentList @('--install-dir',('"'+$taskInstall+'"'),'--no-shortcuts','--no-launch') -WindowStyle Hidden -Wait -PassThru
    if($taskProcess.ExitCode -ne 0){throw 'Setup EXE failed.'}
}
Invoke-TestSetup
if(!(Test-Path -LiteralPath (Join-Path $taskInstall 'App/EmuVora.App.exe'))){throw 'Application missing.'}
if(!(Test-Path -LiteralPath (Join-Path $taskInstall 'App/EmuVora.ico'))){throw 'Own shortcut icon missing.'}
$taskLocator=Get-Content -Raw -LiteralPath (Join-Path $taskInstall 'App/emuvora-location.json') | ConvertFrom-Json
if($taskLocator.DataDirectory -ne (Join-Path $taskInstall 'Data')){throw 'Incorrect data path.'}
'retained game save' | Set-Content -LiteralPath (Join-Path $taskLocator.DataDirectory 'save-test.txt')
Invoke-TestSetup
if((Get-Content -Raw -LiteralPath (Join-Path $taskLocator.DataDirectory 'save-test.txt')).Trim() -ne 'retained game save'){throw 'Save was overwritten.'}
'Setup EXE installation, update, own icon and retained saves verified.'
