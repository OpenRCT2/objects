#!/bin/pwsh
$ErrorActionPreference = "Stop"

# Create zip archive containing objects
$useZip = $false
if (Get-Command "zip" -ErrorAction SilentlyContinue)
{
    # Use zip if possible as it handles permissions better on unix
    $useZip = $true
    Write-Host "Using zip instead of Compress-Archive"
}

Write-Host -ForegroundColor Cyan "Re-creating artifacts directory..."
Remove-Item -Force -Recurse artifacts -ErrorAction SilentlyContinue
New-Item -Force -ItemType Directory artifacts,artifacts/objects | Out-Null

Write-Host -ForegroundColor Cyan "Copying objects..."
Push-Location objects
    Copy-Item -Recurse "official","rct1","rct2","rct2ww","rct2tt" ../artifacts/objects
Pop-Location

Write-Host -ForegroundColor Cyan "Creating parkobj files..."
foreach ($d in Get-ChildItem -Directory -Recurse artifacts/objects)
{
    if (Test-Path (Join-Path $d.FullName object.json))
    {
        $objName = $d.Name
        $src = Resolve-Path -Relative $d.FullName
        $dst = $src + ".parkobj"
        Write-Host "$src -> $dst"
        if ($useZip)
        {
            Push-Location $src
                zip -r9 "../$objName.parkobj" (Get-ChildItem).Name
                if ($LASTEXITCODE -ne 0)
                {
                    throw "zip failed with $LASTEXITCODE"
                }
            Pop-Location
        }
        else
        {
            # We must use .zip extension for Compress-Archive to work
            Compress-Archive -Force "$src/*" -DestinationPath ($dst + ".zip") -CompressionLevel Optimal
            Move-Item ($dst + ".zip") $dst
        }
        Remove-Item -Force -Recurse $d.FullName
    }
}

Write-Host -ForegroundColor Cyan "Creating final archive..."
if ($useZip)
{
    Push-Location "artifacts/objects"
        zip -r9 "../objects.zip" (Get-ChildItem).Name
        if ($LASTEXITCODE -ne 0)
        {
            throw "zip failed with $LASTEXITCODE"
        }
    Pop-Location
}
else
{
    Compress-Archive -Force "artifacts/objects/*" -DestinationPath "artifacts/objects.zip" -CompressionLevel Optimal
}
Remove-Item -Force -Recurse artifacts/objects
