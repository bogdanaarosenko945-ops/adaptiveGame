$ErrorActionPreference = "Stop"

$projectPath = Resolve-Path (Join-Path $PSScriptRoot "..")
$versionFile = Join-Path $projectPath "ProjectSettings\ProjectVersion.txt"

if (-not (Test-Path $versionFile)) {
    throw "ProjectSettings\ProjectVersion.txt was not found. Run this script from inside a Unity project."
}

$versionLine = Get-Content $versionFile | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
$unityVersion = ($versionLine -replace "m_EditorVersion:\s*", "").Trim()

if ([string]::IsNullOrWhiteSpace($unityVersion)) {
    throw "Could not read Unity editor version from ProjectSettings\ProjectVersion.txt."
}

$unityExe = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"

if (-not (Test-Path $unityExe)) {
    throw "Unity editor was not found at: $unityExe"
}

$existingEditor = Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -eq "Unity.exe" -and
        $_.CommandLine -like "*-projectpath*" -and
        $_.CommandLine -like "*$projectPath*"
    } |
    Select-Object -First 1

if ($existingEditor) {
    Write-Host "Unity is already open for this project. PID: $($existingEditor.ProcessId)"
    exit 0
}

Write-Host "Opening Unity $unityVersion for: $projectPath"
Start-Process -FilePath $unityExe -ArgumentList @("-projectPath", "$projectPath")
Write-Host "Unity was started without waiting for the editor to close."
