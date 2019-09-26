function Remove($path) {
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force
    } else {
        Write-Host "Path don't exist: $path"
    }
}

Remove("obj")
Remove(".vs")
Remove("*.sln")
Remove("*.csproj.user")
Remove("bin\Debug\*.runtimeconfig.dev.json")