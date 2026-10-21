# Unity batchmode loop for this project: any sequence of steps, each waited on properly.
#   run.ps1 -Steps setup,setup,editmode,playmode [-Filter Map4Tests] [-Graphics]
# 'setup' twice after an Editor-assembly edit: the first pass compiles, then runs the OLD code.
# Waits with WaitForExit() on Unity alone -- Start-Process -Wait waits on helper processes too
# and has wedged for minutes.
#
# Prints a SHORT report, so nobody has to read the logs: per step the exit code, compile errors and
# exceptions (unique, capped), and for a test step the counts plus every failed test with the head
# of its message and its first stack line in project code. A results file older than the step is
# called stale instead of being read. Full logs and results land in RunLogs/ (gitignored).
param(
    [string[]]$Steps = @('setup', 'setup', 'editmode', 'playmode'),
    [string]$Filter = '',
    [switch]$Graphics,
    [int]$MaxFailures = 15
)
$ErrorActionPreference = 'Continue'
$project = (Resolve-Path "$PSScriptRoot/../..").Path.Replace('\', '/')
$logs = "$project/RunLogs"
New-Item -ItemType Directory -Force $logs | Out-Null
$version = ((Get-Content "$project/ProjectSettings/ProjectVersion.txt" | Select-String 'm_EditorVersion:') -split ':\s*')[1].Trim()
$unity = $env:UNITY_EXE
if (-not $unity) {
    $unity = @("C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe", "D:\unity\$version-x86_64\Editor\Unity.exe", "D:\unity\$version\Editor\Unity.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $unity) { throw "Unity $version not found; set UNITY_EXE" }

function Invoke-Unity([string]$Label, [string[]]$UnityArgs) {
    $p = Start-Process -FilePath $unity -ArgumentList $UnityArgs -PassThru
    $p.WaitForExit()
    return $p.ExitCode
}

# Compile errors and exceptions out of a Unity log, each distinct line once.
function Write-LogProblems([string]$Log) {
    if (-not (Test-Path $Log)) { Write-Output "  log missing: $Log"; return }
    $lines = Select-String -Path $Log -Pattern 'error CS\d+|Exception:|Aborting batchmode|Scripts have compiler errors' |
        ForEach-Object { $_.Line.Trim() } | Select-Object -Unique
    $cs = @($lines | Where-Object { $_ -match 'error CS' })
    $other = @($lines | Where-Object { $_ -notmatch 'error CS' })
    if ($cs.Count) {
        Write-Output "  COMPILE ERRORS ($($cs.Count)):"
        $cs | Select-Object -First 10 | ForEach-Object { Write-Output "    $_" }
    }
    if ($other.Count) {
        Write-Output "  exceptions/aborts ($($other.Count) distinct, first 5):"
        $other | Select-Object -First 5 | ForEach-Object { Write-Output "    $($_.Substring(0, [Math]::Min(200, $_.Length)))" }
    }
}

function Write-TestReport([string]$Xml, [datetime]$Started) {
    if (-not (Test-Path $Xml) -or (Get-Item $Xml).LastWriteTime -lt $Started) {
        Write-Output "  NO FRESH RESULTS ($Xml) -- the run did not get to the tests"
        return
    }
    [xml]$doc = [System.IO.File]::ReadAllText($Xml)
    $run = $doc.'test-run'
    Write-Output "  total=$($run.total) passed=$($run.passed) failed=$($run.failed) skipped=$($run.skipped)"
    $failed = @($doc.SelectNodes("//test-case[@result='Failed']"))
    foreach ($case in ($failed | Select-Object -First $MaxFailures)) {
        Write-Output "  FAIL $($case.fullname)"
        $message = $case.SelectSingleNode('failure/message')
        $stack = $case.SelectSingleNode('failure/stack-trace')
        if ($message) {
            $message.InnerText.Trim() -split "`r?`n" | Where-Object { $_.Trim() } | Select-Object -First 4 |
                ForEach-Object { Write-Output "       $($_.Trim())" }
        }
        if ($stack) {
            $at = $stack.InnerText -split "`r?`n" | ForEach-Object { if ($_ -match 'Assets[\\/]_Project[\\/][^\s:]*\.cs:\d+') { $Matches[0] } } | Select-Object -First 1
            if ($at) { Write-Output "       at $at" }
        }
    }
    if ($failed.Count -gt $MaxFailures) { Write-Output "  ... and $($failed.Count - $MaxFailures) more failed" }
}

$i = 0
foreach ($step in $Steps) {
    $i++
    $started = Get-Date
    switch ($step) {
        'setup' {
            $log = "$logs/setup_run.log"
            $code = Invoke-Unity "setup$i" @('-batchmode', '-nographics', '-quit', '-projectPath', $project,
                '-executeMethod', 'CityBuilder.EditorTools.SetupProject.Run', '-logFile', $log)
            Write-Output "=== setup$i exit $code ($([int]((Get-Date) - $started).TotalSeconds) s)"
            Write-LogProblems $log
        }
        { $_ -in 'editmode', 'playmode' } {
            $platform = if ($step -eq 'editmode') { 'EditMode' } else { 'PlayMode' }
            $suffix = if ($step -eq 'editmode') { '' } else { '_playmode' }
            $xml = "$logs/test_results$suffix.xml"
            $log = "$logs/test_run$suffix.log"
            $a = @('-batchmode', '-projectPath', $project, '-runTests', '-testPlatform', $platform,
                '-testResults', $xml, '-logFile', $log)
            if (-not $Graphics) { $a = @('-nographics') + $a }
            if ($Filter) { $a += @('-testFilter', $Filter) }
            $code = Invoke-Unity $step $a
            Write-Output "=== $step exit $code ($([int]((Get-Date) - $started).TotalSeconds) s)"
            Write-TestReport $xml $started
            if (-not (Test-Path $xml) -or (Get-Item $xml).LastWriteTime -lt $started) { Write-LogProblems $log }
        }
    }
}
Write-Output "RUN FINISHED -- full logs in RunLogs/"
