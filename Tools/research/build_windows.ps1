param(
    [switch]$Development,
    # a build for players: no cheat window, no PerfProbe (BuildWindows' -release -> CB_RELEASE)
    [switch]$Release
)

# Builds the Windows 64-bit player through CityBuilder.EditorTools.BuildWindows into the user's
# build folder, zips it (an .exe alone does not run -- it needs CityBuilding_Data next to it) and
# leaves the zip in the build folder (Telegram sending dropped 2026-11-18).
#
# The user's rule (2026-10-13): every build goes to D:\Builds CB, and the previous build there is
# deleted first. Only the previous WINDOWS build goes (2026-11-19): the APK there is the other
# platform's latest build and stays, as build_android.ps1 leaves the Windows build alone.

$ErrorActionPreference = 'Continue'
$version = '6000.5.4f1'
$unity = $env:UNITY_EXE
if (-not $unity -or -not (Test-Path $unity)) {
    $unity = @("D:\unity\$version-x86_64\Editor\Unity.exe", "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $unity) { throw "Unity $version not found: set UNITY_EXE" }
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path.Replace('\', '/')
New-Item -ItemType Directory -Force "$project/RunLogs" | Out-Null
$buildsRoot = 'D:\Builds CB'
$playerFolder = Join-Path $buildsRoot 'CityBuilding-Windows'

New-Item -ItemType Directory -Force $buildsRoot | Out-Null
Get-ChildItem -LiteralPath $buildsRoot -Force | Where-Object { $_.Name -like 'CityBuilding-Windows*' } |
    Remove-Item -Recurse -Force -Confirm:$false
Write-Output "cleared the old Windows build in $buildsRoot"

# Start-Process joins its arguments with spaces and quotes nothing, so the path is quoted by hand.
$unityArgs = @(
    '-batchmode', '-nographics',
    '-projectPath', $project,
    '-buildTarget', 'Win64',
    '-executeMethod', 'CityBuilder.EditorTools.BuildWindows.Run',
    '-buildOutput', "`"$playerFolder`"",
    '-logFile', "$project/RunLogs/windows_build.log"
)

if ($Development) { $unityArgs += '-development' }
if ($Release) { $unityArgs += '-release' }

Write-Output "=== windows build ($(if ($Development) { 'development' } elseif ($Release) { 'release, no cheats' } else { 'release' })) : $(Get-Date -Format o) ==="
$p = Start-Process -FilePath $unity -ArgumentList $unityArgs -PassThru -Wait
Write-Output "=== done, exit $($p.ExitCode) : $(Get-Date -Format o) ==="

if ($p.ExitCode -ne 0) {
    Write-Output 'FAILED -- last errors from the log:'
    Select-String -Path "$project/RunLogs/windows_build.log" -Pattern 'error|Error|BuildFailedException|Unable to find' | Select-Object -Last 20
} else {
    $zipName = if ($Development) { 'CityBuilding-Windows-dev.zip' } else { 'CityBuilding-Windows.zip' }
    $zipPath = Join-Path $buildsRoot $zipName

    # The Burst debug folder is for crash symbolication on this machine, not for players.
    $items = Get-ChildItem -LiteralPath $playerFolder | Where-Object { $_.Name -notlike '*DoNotShip*' }
    Compress-Archive -Path $items.FullName -DestinationPath $zipPath -CompressionLevel Optimal

    $mb = [math]::Round((Get-Item -LiteralPath $zipPath).Length / 1MB, 1)
    Write-Output "player: $playerFolder"
    Write-Output "zip: $zipPath, $mb MB"
    # no Telegram any more (the user, 2026-11-18)
}
