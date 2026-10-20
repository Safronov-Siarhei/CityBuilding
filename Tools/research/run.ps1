# Unity batchmode loop for this project: any sequence of steps, each waited on properly.
#   run.ps1 -Steps setup,setup,editmode,playmode [-Filter Map4Tests] [-Graphics]
# 'setup' twice after an Editor-assembly edit: the first pass compiles, then runs the OLD code.
# Waits with WaitForExit() on Unity alone -- Start-Process -Wait waits on helper processes too
# and has wedged for minutes.
param(
    [string[]]$Steps = @('setup', 'setup', 'editmode', 'playmode'),
    [string]$Filter = '',
    [switch]$Graphics
)
$ErrorActionPreference = 'Continue'
$project = (Resolve-Path "$PSScriptRoot/../..").Path.Replace('\', '/')
$version = ((Get-Content "$project/ProjectSettings/ProjectVersion.txt" | Select-String 'm_EditorVersion:') -split ':\s*')[1].Trim()
$unity = $env:UNITY_EXE
if (-not $unity) {
    $unity = @("C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe", "D:\unity\$version-x86_64\Editor\Unity.exe", "D:\unity\$version\Editor\Unity.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $unity) { throw "Unity $version not found; set UNITY_EXE" }

function Invoke-Unity([string]$Label, [string[]]$UnityArgs) {
    Write-Output "=== $Label : $(Get-Date -Format o) ==="
    $p = Start-Process -FilePath $unity -ArgumentList $UnityArgs -PassThru
    $p.WaitForExit()
    Write-Output "=== $Label done, exit $($p.ExitCode) : $(Get-Date -Format o) ==="
}

$i = 0
foreach ($step in $Steps) {
    $i++
    switch ($step) {
        'setup' {
            Invoke-Unity "setup$i" @('-batchmode', '-nographics', '-quit', '-projectPath', $project,
                '-executeMethod', 'CityBuilder.EditorTools.SetupProject.Run', '-logFile', "$project/setup_run.log")
        }
        { $_ -in 'editmode', 'playmode' } {
            $platform = if ($step -eq 'editmode') { 'EditMode' } else { 'PlayMode' }
            $suffix = if ($step -eq 'editmode') { '' } else { '_playmode' }
            $a = @('-batchmode', '-projectPath', $project, '-runTests', '-testPlatform', $platform,
                '-testResults', "$project/test_results$suffix.xml", '-logFile', "$project/test_run$suffix.log")
            if (-not $Graphics) { $a = @('-nographics') + $a }
            if ($Filter) { $a += @('-testFilter', $Filter) }
            Invoke-Unity $step $a
            $xml = [System.IO.File]::ReadAllText("$project/test_results$suffix.xml")
            if ($xml -match '<test-run[^>]*') { Write-Output ($Matches[0] -replace '.*?(total="\d+").*?(passed="\d+").*?(failed="\d+").*?(start-time="[^"]+").*', '$1 $2 $3 $4') }
        }
    }
}
Write-Output 'RUN FINISHED'
