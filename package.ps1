#!/bin/pwsh
# Create zip archive containing objects

Write-Host -ForegroundColor Cyan "Re-creating artifacts directory..."
Remove-Item -Force -Recurse artifacts -ErrorAction SilentlyContinue
New-Item -Force -ItemType Directory artifacts,artifacts/objects | Out-Null

Write-Host -ForegroundColor Cyan "Copying objects..."
Push-Location objects
    Copy-Item -Recurse "rct2","rct2ww","rct2tt" ../artifacts/objects
Pop-Location

Write-Host -ForegroundColor Cyan "Creating parkobj files..."
foreach ($d in Get-ChildItem -Directory -Recurse artifacts/objects)
{
    if (Test-Path (Join-Path $d.FullName object.json))
    {
        $src = Resolve-Path -Relative $d.FullName
        $dst = $src + ".parkobj"
        Write-Host "$src -> $dst"
        # We must use .zip extension for Compress-Archive to work
        Compress-Archive -Force "$src/*" -DestinationPath ($dst + ".zip") -CompressionLevel Optimal
        Move-Item ($dst + ".zip") $dst
        Remove-Item -Force -Recurse $src
    }
}

Write-Host -ForegroundColor Cyan "Creating final archive..."
Compress-Archive -Force "artifacts/objects/*" -DestinationPath "artifacts/objects.zip" -CompressionLevel Optimal
Remove-Item -Force -Recurse artifacts/objects
