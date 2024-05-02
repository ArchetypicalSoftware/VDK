Push-Location
try {
    Set-Location .\src\vdk
    if(!(Test-Path ..\..\.pack)){
        New-Item -ItemType Directory ..\..\.pack | Out-Null
    }
    choco pack -out ..\..\.pack
}
catch {
    Write-Host "Build Failed"
}
finally {
    Pop-Location
}
Get-ChildItem .\.pack

