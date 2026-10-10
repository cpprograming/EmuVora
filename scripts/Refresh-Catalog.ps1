param([string]$OutputPath=(Join-Path $PSScriptRoot '../catalog/packages.json'))
$ErrorActionPreference='Stop'
$taskHeaders=@{'User-Agent'='EmuVora-catalog';'Accept'='application/vnd.github+json'}
if($env:GITHUB_TOKEN){$taskHeaders.Authorization='Bearer '+$env:GITHUB_TOKEN}
$taskPackages=[System.Collections.Generic.List[object]]::new()
function Add-Package([string]$Id,[string]$Version,[string]$Url,[string]$Executable,[string]$Digest=''){
    if($Url -notmatch '^https://(github.com/(stenzek/duckstation|PCSX2/pcsx2|hrydgard/ppsspp)/releases/download/|dl.dolphin-emu.org/releases/|buildbot.libretro.com/)'){throw "Unexpected package source: $Url"}
    $taskSha=$Digest -replace '^sha256:',''
    if($taskSha -notmatch '^[0-9a-fA-F]{64}$'){
        $taskTemporary=Join-Path ([System.IO.Path]::GetTempPath()) ('emuvora-'+[Guid]::NewGuid().ToString('N'))
        try{Invoke-WebRequest $Url -OutFile $taskTemporary; $taskSha=(Get-FileHash $taskTemporary -Algorithm SHA256).Hash.ToLowerInvariant()}
        finally{if(Test-Path -LiteralPath $taskTemporary){Remove-Item -LiteralPath $taskTemporary}}
    }
    if($Id.StartsWith('core-')){$Version=$taskSha.Substring(0,16)}
    $taskPackages.Add(@{Id=$Id;Version=$Version;Url=$Url;Sha256=$taskSha;Executable=$Executable})
    Write-Host "$Id $Version verified"
}
foreach($taskSpec in @(
    @{Id='duckstation';Repo='stenzek/duckstation';Pattern='^duckstation-windows-x64-release.zip$';Exe='duckstation-qt-x64-ReleaseLTCG.exe'},
    @{Id='pcsx2';Repo='PCSX2/pcsx2';Pattern='windows-x64-Qt\.7z$';Exe='pcsx2-qt.exe'},
    @{Id='ppsspp';Repo='hrydgard/ppsspp';Pattern='Windows-x64\.zip$';Exe='PPSSPPWindows64.exe'}
)){
    $taskRelease=Invoke-RestMethod "https://api.github.com/repos/$($taskSpec.Repo)/releases/latest" -Headers $taskHeaders
    $taskAsset=$taskRelease.assets | Where-Object name -Match $taskSpec.Pattern | Select-Object -First 1
    if(!$taskAsset){throw "Official asset missing: $($taskSpec.Id)"}
    Add-Package $taskSpec.Id ($taskRelease.tag_name+'-'+$taskAsset.id) $taskAsset.browser_download_url $taskSpec.Exe $taskAsset.digest
}
$taskDolphin=Invoke-RestMethod 'https://dolphin-emu.org/update/latest/beta/'
$taskArtifact=$taskDolphin.artifacts | Where-Object system -EQ 'Windows x64' | Select-Object -First 1
Add-Package 'dolphin' $taskDolphin.shortrev $taskArtifact.url 'Dolphin.exe'
$taskRetro=Invoke-RestMethod 'https://api.github.com/repos/libretro/RetroArch/releases/latest' -Headers $taskHeaders
$taskRetroVersion=$taskRetro.tag_name -replace '^v',''
Add-Package 'retroarch' $taskRetroVersion "https://buildbot.libretro.com/stable/$taskRetroVersion/windows/x86_64/RetroArch.7z" 'retroarch.exe'
foreach($taskCore in @('fceumm','snes9x','gambatte','mgba','mupen64plus_next','genesis_plus_gx')){
    Add-Package "core-$taskCore" ([DateTime]::UtcNow.ToString('yyyyMMdd')) "https://buildbot.libretro.com/nightly/windows/x86_64/latest/$($taskCore)_libretro.dll.zip" "$($taskCore)_libretro.dll"
}
$taskParent=Split-Path -Parent ([System.IO.Path]::GetFullPath($OutputPath))
New-Item -ItemType Directory -Force $taskParent | Out-Null
@{SchemaVersion=1;GeneratedAt=[DateTime]::UtcNow.ToString('O');Packages=@($taskPackages.ToArray())} | ConvertTo-Json -Depth 5 | Set-Content $OutputPath -Encoding utf8
