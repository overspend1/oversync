$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

dotnet run --project .\src\OverSync.Windows
