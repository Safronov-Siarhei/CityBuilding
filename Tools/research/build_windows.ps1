param(
    [switch]$Development
)

# Builds the Windows 64-bit player through CityBuilder.EditorTools.BuildWindows into the user's
# build folder, zips it (an .exe alone does not run -- it needs CityBuilding_Data next to it) and
# sends the zip to the same Telegram topic as the APKs. The zip must stay under the Bot API's 50 MB.
#
# The user's rule (2026-10-13): every build goes to D:\Builds CB, and the previous build there is
# deleted first -- the folder only ever holds the latest one.

$ErrorActionPreference = 'Continue'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
$project = 'D:/Fork/CityBuilding'
New-Item -ItemType Directory -Force "$project/RunLogs" | Out-Null
$buildsRoot = 'D:\Builds CB'
$playerFolder = Join-Path $buildsRoot 'CityBuilding-Windows'

# The previous build, whatever it was, goes. Only the folder's contents -- never the folder itself.
New-Item -ItemType Directory -Force $buildsRoot | Out-Null
Get-ChildItem -LiteralPath $buildsRoot -Force | Remove-Item -Recurse -Force -Confirm:$false
Write-Output "cleared $buildsRoot"

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

Write-Output "=== windows build ($(if ($Development) { 'development' } else { 'release' })) : $(Get-Date -Format o) ==="
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

    Write-Output '--- sending to Telegram ---'
    $label = if ($Development) { 'Windows development build (profiler)' } else { 'Windows build' }
    python "$project/Tools/research/send_build_to_telegram.py" $zipPath $label
}
