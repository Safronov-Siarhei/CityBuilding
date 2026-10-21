$ErrorActionPreference = 'Continue'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
$project = 'D:/Fork/CityBuilding'
New-Item -ItemType Directory -Force "$project/RunLogs" | Out-Null
function Invoke-Unity { param([string[]]$UnityArgs, [string]$Label)
    Write-Output "=== $Label : $(Get-Date -Format o) ==="
    $p = Start-Process -FilePath $unity -ArgumentList $UnityArgs -PassThru -Wait
    Write-Output "=== $Label done, exit $($p.ExitCode) : $(Get-Date -Format o) ==="
}
# First launch compiles the edited assembly (and runs the OLD one), second actually exercises it.
Invoke-Unity -Label 'compile' -UnityArgs @('-batchmode','-nographics','-projectPath',$project,'-runTests','-testPlatform','EditMode','-testResults',"$project/RunLogs/test_results.xml",'-logFile',"$project/RunLogs/test_run.log")
Invoke-Unity -Label 'playmode' -UnityArgs @('-batchmode','-nographics','-projectPath',$project,'-runTests','-testPlatform','PlayMode','-testResults',"$project/RunLogs/test_results_playmode.xml",'-logFile',"$project/RunLogs/test_run_playmode.log")
Write-Output 'PLAYMODE ONLY FINISHED'
