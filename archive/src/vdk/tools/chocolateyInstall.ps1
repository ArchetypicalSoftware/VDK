$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
# $toolsDir = Get-Location                                              # Uncomment this line for debugging

# Copy Powershell Modules
Write-Host "Installing Powershell Module VDK-Tools"
$modulePath = $env:PSModulePath -split [System.IO.Path]::PathSeparator | Where-Object {$_.StartsWith($env:USERPROFILE)} | Select-Object -First 1
Write-Host $modulePath
$moduleSource = Join-Path $toolsDir VDK-Tools
Copy-Item $moduleSource $modulePath -Recurse -Force

# Copy Kind Version Data
Write-Host "Configuring Kind version data"
$configPath = Join-Path $env:USERPROFILE .vdk\config
$kindDataPath = Join-Path $configPath kind-version-data.json
$kindSourcePath = Join-Path $toolsDir kind-version-data.json
if(!(Test-Path $configPath) -eq $true){
    New-Item -ItemType Directory $configPath | Out-Null
}
Copy-Item $kindSourcePath $kindDataPath -Force

# Import Module
RefreshEnv.cmd
Import-Module VDK-Tools