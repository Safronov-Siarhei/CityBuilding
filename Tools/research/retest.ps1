$ErrorActionPreference = 'Continue'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
$project = 'D:/Fork/CityBuilding'

function Invoke-Unity {
    param([string[]]$UnityArgs, [string]$Label)
    Write-Output "=== $Label : $(Get-Date -Format o) ==="
    # -Wait on the process itself: polling the process list races with Unity's own startup, and an
    # empty list a second after launch reads as "already finished".
    $p = Start-Process -FilePath $unity -ArgumentList $UnityArgs -PassThru -Wait
    Write-Output "=== $Label done, exit $($p.ExitCode) : $(Get-Date -Format o) ==="
}

# The first launch after a .cs edit compiles the new assemblies but runs the old ones, so EditMode
# goes first purely to get the compile out of the way; its results are read too.
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

Write-Output 'RETEST FINISHED'
