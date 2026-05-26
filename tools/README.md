# Unity helper scripts

This folder contains small scripts for opening and checking this Unity project without blocking the terminal.

Run from PowerShell:

```powershell
.\tools\Open-UnityProject.ps1
```

To see Unity processes connected to this project:

```powershell
.\tools\Show-UnityProjectProcesses.ps1
```

`Open-UnityProject.ps1` uses `Start-Process`, so the command finishes immediately after starting Unity. It does not wait until the Unity Editor is closed.
