$ErrorActionPreference = 'Continue'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
$project = 'D:/Fork/CityBuilding'
New-Item -ItemType Directory -Force "$project/RunLogs" | Out-Null

function Invoke-Unity {
    param([string[]]$UnityArgs, [string]$Label)
    Write-Output "=== $Label : $(Get-Date -Format o) ==="
    # -Wait on the process itself: polling the process list races with Unity's own startup, and an
    # empty list a second after launch reads as "already finished".
    $p = Start-Process -FilePath $unity -ArgumentList $UnityArgs -PassThru -Wait
    Write-Output "=== $Label done, exit $($p.ExitCode) : $(Get-Date -Format o) ==="
}

# Three launches, and the first one's results are deliberately thrown away. A batchmode run that
# follows a .cs edit compiles the new assembly and then executes the OLD one, so the first EditMode
# pass reports on code that no longer exists. retest.ps1 reads that pass -- fine when only PlayMode
# changed, wrong whenever an EditMode test was edited too, which is what this script is for.
Invoke-Unity -Label 'compile' -UnityArgs @(
    '-batchmode', '-nographics', '-projectPath', $project,
    '-runTests', '-testPlatform', 'EditMode',
    '-testResults', "$project/RunLogs/test_results_compile.xml",
    '-logFile', "$project/RunLogs/test_run_compile.log")

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

Write-Output 'RETEST FRESH FINISHED'
