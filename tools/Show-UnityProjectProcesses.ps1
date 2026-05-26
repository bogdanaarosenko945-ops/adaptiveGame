$ErrorActionPreference = "Stop"

$projectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPathForward = $projectPath -replace "\\", "/"

$processes = Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -like "Unity*" -and
        (
            $_.CommandLine -like "*$projectPath*" -or
            $_.CommandLine -like "*$projectPathForward*"
        )
    } |
    Select-Object Name, ProcessId, CommandLine

if (-not $processes) {
    Write-Host "No Unity processes were found for this project."
    exit 0
}

$processes | Format-Table Name, ProcessId, @{Label = "Role"; Expression = {
    if ($_.CommandLine -like "*AssetImportWorker*") { "Asset import worker" }
    elseif ($_.CommandLine -like "*-projectpath*" -or $_.CommandLine -like "*-projectPath*") { "Unity editor" }
    else { "Unity helper" }
}} -AutoSize
