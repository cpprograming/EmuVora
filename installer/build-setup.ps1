param([Parameter(Mandatory=$true)][string]$PackageZip,[Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop'
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(!(Test-Path -LiteralPath $taskCompiler)){throw 'Der Windows-.NET-Framework-Compiler fehlt.'}
$taskBuild=Join-Path ([IO.Path]::GetTempPath()) ('EmuVora-setup-build-'+[Guid]::NewGuid().ToString('N'))
try{
    New-Item -ItemType Directory -Path $taskBuild | Out-Null
    $taskPackage=Join-Path $taskBuild 'package'
    Expand-Archive -LiteralPath $PackageZip -DestinationPath $taskPackage
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../Install-EmuVora.ps1') -Destination $taskPackage
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'EmuVora.ico') -Destination $taskPackage
    $taskArchive=Join-Path $taskBuild 'package.zip'
    Compress-Archive -Path (Join-Path $taskPackage '*') -DestinationPath $taskArchive
    & $taskCompiler /nologo /target:winexe ('/out:'+[IO.Path]::GetFullPath($OutputPath)) ('/win32icon:'+(Join-Path $PSScriptRoot 'EmuVora.ico')) ('/resource:'+$taskArchive+',EmuVora.package.zip') ('/resource:'+(Join-Path $PSScriptRoot 'EmuVora.ico')+',EmuVora.ico') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $PSScriptRoot 'Setup.cs')
    if($LASTEXITCODE -ne 0){throw 'Setup-EXE konnte nicht erstellt werden.'}
}finally{
    if([IO.Path]::GetFullPath($taskBuild).StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()),[StringComparison]::OrdinalIgnoreCase) -and (Split-Path $taskBuild -Leaf) -match '^EmuVora-setup-build-[a-f0-9]{32}$'){
        Remove-Item -LiteralPath $taskBuild -Recurse -Force -ErrorAction SilentlyContinue
    }
}
