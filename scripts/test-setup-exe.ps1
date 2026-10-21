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
if(!(Test-Path -LiteralPath (Join-Path $taskInstall 'App/ConsoleLogos.LICENSE.txt'))){throw 'Console logo attribution and license missing.'}
foreach($taskControllerFile in @('SDL3.dll','gamecontrollerdb.txt','SDL3.LICENSE.txt','GameControllerDB.LICENSE.txt')){
    if(!(Test-Path -LiteralPath (Join-Path $taskInstall ('App/'+$taskControllerFile)))){throw ('Controller component missing: '+$taskControllerFile)}
}
if((Get-FileHash -LiteralPath (Join-Path $taskInstall 'App/SDL3.dll') -Algorithm SHA256).Hash -ne '2B4B128D8D510CC2D442693CF848C050D700D43DC667AFE9E5FD82B0570D667E'){throw 'Installed SDL runtime checksum mismatch.'}
if((Get-FileHash -LiteralPath (Join-Path $taskInstall 'App/gamecontrollerdb.txt') -Algorithm SHA256).Hash -ne 'C6587CC65789539E61EC375AAD5BDBC14F974508B57E2AE87730AC75C4445DBA'){throw 'Installed controller database checksum mismatch.'}
'SDL runtime, database and license files verified.'
$taskLocator=Get-Content -Raw -LiteralPath (Join-Path $taskInstall 'App/emuvora-location.json') | ConvertFrom-Json
if($taskLocator.DataDirectory -ne (Join-Path $taskInstall 'Data')){throw 'Incorrect data path.'}
'retained game save' | Set-Content -LiteralPath (Join-Path $taskLocator.DataDirectory 'save-test.txt')
Invoke-TestSetup
if((Get-Content -Raw -LiteralPath (Join-Path $taskLocator.DataDirectory 'save-test.txt')).Trim() -ne 'retained game save'){throw 'Save was overwritten.'}
'Setup EXE installation, update, own icon and retained saves verified.'
$taskSeparateData=Join-Path $taskRoot 'Separate data & saves'
New-Item -ItemType Directory -Path $taskSeparateData | Out-Null
'retained separate save' | Set-Content -LiteralPath (Join-Path $taskSeparateData 'save-test.txt')
$taskPrevious=Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile','-Command','Start-Sleep -Seconds 8') -WindowStyle Hidden -PassThru
$taskUpdate=Start-Process -FilePath $taskExe -ArgumentList @('--install-dir',('"'+$taskInstall+'"'),'--data-dir',('"'+$taskSeparateData+'"'),'--wait-for-pid',$taskPrevious.Id,'--no-shortcuts','--no-launch') -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 1
if($taskUpdate.HasExited){throw 'Updater did not wait for the old process.'}
$taskPrevious.WaitForExit()
if(!$taskUpdate.WaitForExit(60000) -or $taskUpdate.ExitCode -ne 0){throw 'Update handoff failed.'}
$taskLocator=Get-Content -Raw -LiteralPath (Join-Path $taskInstall 'App/emuvora-location.json') | ConvertFrom-Json
if($taskLocator.DataDirectory -ne $taskSeparateData){throw 'Updater lost the explicit data path.'}
if((Get-Content -Raw -LiteralPath (Join-Path $taskSeparateData 'save-test.txt')).Trim() -ne 'retained separate save'){throw 'Updater overwrote separate saves.'}
'Self-update process handoff and explicit data path verified.'
