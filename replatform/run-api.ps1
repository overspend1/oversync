$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

dotnet run --project .\src\OverSync.Api --urls http://localhost:5000
