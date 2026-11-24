param(
    [switch]$Development,
    # a build for players: no cheat window, no PerfProbe (BuildAndroid's -release -> CB_RELEASE)
    [switch]$Release,
    # the Google Play upload (Design/RELEASE_PLAN.md): an .aab with an install-time asset pack, no cheats, signed with
    # the upload key from CB_KEYSTORE / CB_KEYSTORE_PASS / CB_KEY_ALIAS / CB_KEY_PASS, versionCode = git commit count
    [switch]$Store
)

# Builds the Android APK through CityBuilder.EditorTools.BuildAndroid.
#
# Requires Android Build Support to be installed for this exact editor version -- Unity Hub ->
# 6000.5.4f1 -> Add modules -> Android Build Support (with SDK/NDK Tools and OpenJDK). Without it
# the run fails immediately with "Unable to find a build target".
#
# The FIRST IL2CPP build takes 10-20 minutes; later ones are much faster. -Development produces a
# separate APK carrying the profiler, so both can sit on the phone at once.

$ErrorActionPreference = 'Continue'
# the project and the editor found as run.ps1 finds them (the machine moved to G: and D:\unity), and
# only an editor that has Android Build Support
$project = (Resolve-Path "$PSScriptRoot/../..").Path.Replace('\', '/')
$version = ((Get-Content "$project/ProjectSettings/ProjectVersion.txt" | Select-String 'm_EditorVersion:') -split ':\s*')[1].Trim()
$unity = $env:UNITY_EXE
if (-not $unity) {
    $unity = @("C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe", "D:\unity\$version-x86_64\Editor\Unity.exe", "D:\unity\$version\Editor\Unity.exe") |
        Where-Object { Test-Path (Join-Path (Split-Path $_) 'Data\PlaybackEngines\AndroidPlayer') } | Select-Object -First 1
}
if (-not $unity) { throw "Unity $version with Android Build Support not found: Unity Hub -> $version -> Add modules -> Android Build Support (+ OpenJDK, SDK/NDK), or set UNITY_EXE" }
New-Item -ItemType Directory -Force "$project/RunLogs" | Out-Null

# No -quit: BuildAndroid.Run exits the editor itself, with 0 or 1 depending on the build. Passing
# both makes Unity return -1 even on a perfectly good build, which had the wrapper reporting FAILED
# over a finished APK.
$unityArgs = @(
    '-batchmode', '-nographics',
    '-projectPath', $project,
    '-buildTarget', 'Android',
    '-executeMethod', 'CityBuilder.EditorTools.BuildAndroid.Run',
    '-logFile', "$project/RunLogs/android_build.log"
)

if ($Development) { $unityArgs += '-development' }
if ($Release) { $unityArgs += '-release' }
if ($Store) {
    if (-not $env:CB_KEYSTORE -or -not (Test-Path $env:CB_KEYSTORE)) { throw "-Store needs CB_KEYSTORE (the upload .keystore, kept OUTSIDE the repo) and CB_KEYSTORE_PASS / CB_KEY_ALIAS / CB_KEY_PASS" }
    $code = [int](git -C $project rev-list --count HEAD)
    $unityArgs += @('-store', '-versionCode', "$code")
}

Write-Output "=== android build ($(if ($Development) { 'development' } elseif ($Release) { 'release, no cheats' } else { 'release' })) : $(Get-Date -Format o) ==="
# WaitForExit, not -Wait: -Wait waits on Unity's helper processes too, which can outlive it and
# leave this script hanging over a finished build.
$p = Start-Process -FilePath $unity -ArgumentList $unityArgs -PassThru
$p.WaitForExit()
Write-Output "=== done, exit $($p.ExitCode) : $(Get-Date -Format o) ==="

if ($p.ExitCode -ne 0) {
    Write-Output 'FAILED -- last errors from the log:'
    Select-String -Path "$project/RunLogs/android_build.log" -Pattern 'error|Error|BuildFailedException|Unable to find' | Select-Object -Last 20
} else {
    if ($Store) {
        Get-ChildItem "$project/Builds/Android" -Filter *.aab -ErrorAction SilentlyContinue |
            Select-Object Name, @{ Name = 'MB'; Expression = { [math]::Round($_.Length / 1MB, 1) } }, LastWriteTime
        Write-Output "versionCode $code -- upload Builds/Android/CityCraft.aab in Play Console"
        return
    }
    $apkName = if ($Development) { 'CityBuilding-dev.apk' } else { 'CityBuilding.apk' }
    $apkPath = Join-Path "$project/Builds/Android" $apkName

    Get-ChildItem "$project/Builds/Android" -Filter *.apk -ErrorAction SilentlyContinue |
        Select-Object Name, @{ Name = 'MB'; Expression = { [math]::Round($_.Length / 1MB, 1) } }, LastWriteTime

    # Every successful build goes to the phone that is going to test it. Part of the build, not a
    # step anyone has to remember: a build nobody installs teaches nobody anything.
    # The user's test folder holds only the latest build: older APKs there go, other builds stay.
    $deliver = 'D:\Builds CB'
    if ((Test-Path $apkPath) -and (Test-Path $deliver)) {
        Get-ChildItem $deliver -Filter *.apk | Remove-Item -Force
        Copy-Item $apkPath $deliver
        Write-Output "copied to $deliver"
    }
    # no Telegram any more (the user, 2026-11-18: «исключи отправку в телеграмм, она больше не нужна»):
    # the APK goes to D:\Builds CB and onto the phone over adb
}
