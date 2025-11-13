# Check DLL Architecture Script

Write-Host "=== DLL Architecture Checker ===" -ForegroundColor Cyan
Write-Host ""

# Get the directory where this script is located
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Write-Host "Checking DLLs in: $scriptDir" -ForegroundColor Yellow
Write-Host ""

# Function to check DLL architecture
function Check-DllArch {
    param($dllPath)
    
    $dllName = Split-Path -Leaf $dllPath
    
    if (-not (Test-Path $dllPath)) {
        Write-Host "$dllName : NOT FOUND" -ForegroundColor Red
        return
    }
    
    try {
        $bytes = [System.IO.File]::ReadAllBytes($dllPath)
        $peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
        $machine = [BitConverter]::ToUInt16($bytes, $peOffset + 4)
        
        $arch = switch ($machine) {
            0x014c { "x86 (32-bit)" }
            0x8664 { "x64 (64-bit)" }
            default { "Unknown (0x$($machine.ToString('X4')))" }
        }
        
        $color = if ($machine -eq 0x8664) { "Green" } else { "Yellow" }
        Write-Host "$dllName : $arch" -ForegroundColor $color
    }
    catch {
        Write-Host "$dllName : ERROR - $_" -ForegroundColor Red
    }
}

# Check each DLL
Check-DllArch (Join-Path $scriptDir "sgfplib.dll")
Check-DllArch (Join-Path $scriptDir "sgfpamx.dll")
Check-DllArch (Join-Path $scriptDir "sgwsqlib.dll")
Check-DllArch (Join-Path $scriptDir "sgfdusdax64.dll")
Check-DllArch (Join-Path $scriptDir "SecuGen.FDxSDKPro.DotNet.Windows.dll")
Check-DllArch (Join-Path $scriptDir "people-pdf.dll")

Write-Host ""
Write-Host "=== Current Process Info ===" -ForegroundColor Cyan
Write-Host "Your app is running as: $(if ([Environment]::Is64BitProcess) { 'x64 (64-bit)' } else { 'x86 (32-bit)' })" -ForegroundColor $(if ([Environment]::Is64BitProcess) { 'Green' } else { 'Yellow' })
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
