param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$ProgressPreference='SilentlyContinue'
$taskTarget=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $taskTarget -Force | Out-Null
function Test-ControllerFile([string]$Path,[string]$Sha){
    if(![IO.File]::Exists($Path)){return $false}
    $taskHasher=[Security.Cryptography.SHA256]::Create();$taskStream=[IO.File]::OpenRead($Path)
    try {return [BitConverter]::ToString($taskHasher.ComputeHash($taskStream)).Replace('-','').Equals($Sha,[StringComparison]::OrdinalIgnoreCase)}
    finally {$taskStream.Dispose();$taskHasher.Dispose()}
}
$taskDll=Join-Path $taskTarget 'SDL3.dll'
$taskDllSha='2b4b128d8d510cc2d442693cf848c050d700d43dc667afe9e5fd82b0570d667e'
if(!(Test-ControllerFile $taskDll $taskDllSha)){
    $taskArchive=Join-Path $taskTarget ('SDL3-'+[Guid]::NewGuid().ToString('N')+'.zip')
    try {
        Invoke-WebRequest -UseBasicParsing -Uri 'https://github.com/libsdl-org/SDL/releases/download/release-3.4.18/SDL3-3.4.18-win32-x64.zip' -OutFile $taskArchive -TimeoutSec 90
        if(!(Test-ControllerFile $taskArchive '75c2c0fc74e7d1206aaedc22893a35887332f4f85f5c8b74fddc10d177985f2c')){throw 'SDL archive checksum mismatch.'}
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $taskZip=[IO.Compression.ZipFile]::OpenRead($taskArchive)
        try {
            $taskEntry=$taskZip.GetEntry('SDL3.dll')
            if($null -eq $taskEntry -or $taskEntry.Length -ne 2848256){throw 'Unexpected SDL runtime.'}
            $taskInput=$taskEntry.Open();$taskOutput=[IO.File]::Create($taskDll)
            try {$taskInput.CopyTo($taskOutput)} finally {$taskInput.Dispose();$taskOutput.Dispose()}
        } finally {$taskZip.Dispose()}
        if(!(Test-ControllerFile $taskDll $taskDllSha)){throw 'SDL runtime checksum mismatch.'}
    } finally {if(Test-Path -LiteralPath $taskArchive){Remove-Item -LiteralPath $taskArchive -Force}}
}
$taskDb=Join-Path $taskTarget 'gamecontrollerdb.txt'
$taskDbSha='c6587cc65789539e61ec375aad5bdbc14f974508b57e2ae87730ac75c4445dba'
if(!(Test-ControllerFile $taskDb $taskDbSha)){
    $taskPending=$taskDb+'.pending'
    try {
        Invoke-WebRequest -UseBasicParsing -Uri 'https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/ff333823a85ae896e73f4c53670932cf0f728adb/gamecontrollerdb.txt' -OutFile $taskPending -TimeoutSec 90
        if(!(Test-ControllerFile $taskPending $taskDbSha)){throw 'Controller database checksum mismatch.'}
        Move-Item -LiteralPath $taskPending -Destination $taskDb -Force
    } finally {if(Test-Path -LiteralPath $taskPending){Remove-Item -LiteralPath $taskPending -Force}}
}
