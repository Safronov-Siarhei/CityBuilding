$ErrorActionPreference = 'Continue'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
$project = 'D:/Fork/CityBuilding'

function Invoke-Unity {
    param([string[]]$UnityArgs, [string]$Label)
    Write-Output "=== $Label : $(Get-Date -Format o) ==="
    $p = Start-Process -FilePath $unity -ArgumentList $UnityArgs -PassThru -Wait
    Write-Output "=== $Label done, exit $($p.ExitCode) : $(Get-Date -Format o) ==="
}

# Rebuilds BalanceConfig.asset from the CSVs (and everything else SetupProject generates).
Invoke-Unity -Label 'setup' -UnityArgs @(
    '-batchmode', '-nographics', '-quit', '-projectPath', $project,
    '-executeMethod', 'CityBuilder.EditorTools.SetupProject.Run',
    '-logFile', "$project/setup_run.log")

Invoke-Unity -Label 'editmode' -UnityArgs @(
    '-batchmode', '-nographics', '-projectPath', $project,
    '-runTests', '-testPlatform', 'EditMode',
    '-testResults', "$project/test_results.xml",
    '-logFile', "$project/test_run.log")

Invoke-Unity -Label 'playmode' -UnityArgs @(
    '-batchmode', '-nographics', '-projectPath', $project,
    '-runTests', '-testPlatform', 'PlayMode',
    '-testResults', "$project/test_results_playmode.xml",
    '-logFile', "$project/test_run_playmode.log")

Write-Output 'REBUILD+RETEST FINISHED'
