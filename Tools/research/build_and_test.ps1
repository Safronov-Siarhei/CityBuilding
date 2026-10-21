$ErrorActionPreference = 'Continue'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
$project = 'D:/Fork/CityBuilding'
New-Item -ItemType Directory -Force "$project/RunLogs" | Out-Null

function Wait-ForCityBuilding {
    # Only OUR project's Unity: the user's Editor is open on D:\Fork\AC and would never exit.
    while (Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" |
           Where-Object { $_.CommandLine -like '*CityBuilding*' }) {
        Start-Sleep -Seconds 10
    }
}

function Invoke-Unity {
    param([string[]]$UnityArgs, [string]$Label)
    Write-Output "=== $Label : $(Get-Date -Format o) ==="
    & $unity @UnityArgs
    Wait-ForCityBuilding
    Write-Output "=== $Label done : $(Get-Date -Format o) ==="
}

Wait-ForCityBuilding

# Twice: the first invocation after a .cs edit compiles the new assembly but runs the old one.
Invoke-Unity -Label 'build 1' -UnityArgs @(
    '-batchmode', '-nographics', '-projectPath', $project,
    '-executeMethod', 'CityBuilder.EditorTools.SetupProject.Run', '-quit',
    '-logFile', "$project/RunLogs/unity_build.log")

Invoke-Unity -Label 'build 2' -UnityArgs @(
    '-batchmode', '-nographics', '-projectPath', $project,
    '-executeMethod', 'CityBuilder.EditorTools.SetupProject.Run', '-quit',
    '-logFile', "$project/RunLogs/unity_build2.log")

Invoke-Unity -Label 'editmode' -UnityArgs @(
    '-batchmode', '-nographics', '-projectPath', $project,
    '-runTests', '-testPlatform', 'EditMode',
    '-testResults', "$project/RunLogs/test_results.xml",
    '-logFile', "$project/RunLogs/test_run.log")

Invoke-Unity -Label 'playmode' -UnityArgs @(
    '-batchmode', '-nographics', '-projectPath', $project,
    '-runTests', '-testPlatform', 'PlayMode',
    '-testResults', "$project/RunLogs/test_results_playmode.xml",
    '-logFile', "$project/RunLogs/test_run_playmode.log")

Write-Output 'ALL RUNS FINISHED'
