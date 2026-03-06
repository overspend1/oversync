$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$apiProcess = Start-Process `
  -FilePath "dotnet" `
  -ArgumentList "run --project .\src\OverSync.Api --urls http://localhost:5000" `
  -WorkingDirectory $PSScriptRoot `
  -PassThru

try {
  Start-Sleep -Seconds 3
  dotnet run --project .\src\OverSync.Windows
}
finally {
  if ($apiProcess -and -not $apiProcess.HasExited) {
    Stop-Process -Id $apiProcess.Id -Force
  }
}
