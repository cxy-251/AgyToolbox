param(
    [string]$Version,
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipPublish,
    [switch]$SkipMsi,
    [switch]$SkipExe,
    [switch]$SkipPortable
)

$ErrorActionPreference = "Stop"

$ProjectDir = $PSScriptRoot
$CsprojPath = Join-Path $ProjectDir "AgyToolbox.csproj"
$DistDir = Join-Path $ProjectDir "dist"
$InstallerDir = Join-Path $ProjectDir "installer"

# 1. Resolve version
if (-not $Version -or $Version -eq "master" -or $Version -eq "main") {
    if (Test-Path $CsprojPath) {
        $xml = [xml](Get-Content $CsprojPath)
        $Version = $xml.Project.PropertyGroup.Version
        if (-not $Version) { $Version = "0.1.0" }
    } else {
        $Version = "0.1.0"
    }
}
$CleanVersion = $Version.TrimStart('v')
Write-Host "=== AgyToolbox Packaging (Version: $CleanVersion, Configuration: $Configuration, Runtime: $Runtime) ===" -ForegroundColor Cyan

# 2. Ensure dist directory exists
if (-not (Test-Path $DistDir)) {
    New-Item -ItemType Directory -Path $DistDir | Out-Null
}

$PublishDir = Join-Path $ProjectDir "bin\$Configuration\net8.0-windows\$Runtime\publish"

# 3. Publish Self-Contained directory
if (-not $SkipPublish) {
    Write-Host "`n[1/4] Running dotnet publish (Self-Contained)..." -ForegroundColor Yellow
    dotnet publish $CsprojPath -c $Configuration -r $Runtime --self-contained true -o $PublishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}

# 4. Build MSI Installer (WiX Toolset)
if (-not $SkipMsi) {
    Write-Host "`n[2/4] Building MSI Package with WiX Toolset..." -ForegroundColor Yellow
    $WixCmd = Get-Command wix -ErrorAction SilentlyContinue
    if (-not $WixCmd) {
        Write-Warning "WiX CLI not found. Run: dotnet tool install --global wix --version 5.0.2"
    } else {
        $MsiOutput = Join-Path $DistDir "AgyToolbox-v$CleanVersion-$Runtime.msi"
        $WxsFile = Join-Path $InstallerDir "Package.wxs"
        $ResolvedPublishDir = (Resolve-Path $PublishDir).Path

        & wix build -arch x64 -d Version="$CleanVersion" -d PublishDir="$ResolvedPublishDir" -o "$MsiOutput" "$WxsFile"
        if ($LASTEXITCODE -eq 0 -and (Test-Path $MsiOutput)) {
            Write-Host "MSI built successfully: $MsiOutput" -ForegroundColor Green
        } else {
            Write-Warning "MSI build encountered an issue."
        }
    }
}

# 5. Build Setup.exe Wizard (Inno Setup)
if (-not $SkipExe) {
    Write-Host "`n[3/4] Building EXE Setup Wizard with Inno Setup..." -ForegroundColor Yellow
    $IsccCandidates = @(
        (Get-Command iscc -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
    ) | Where-Object { $_ -and (Test-Path $_) }

    if ($IsccCandidates.Count -eq 0) {
        Write-Warning "Inno Setup (ISCC.exe) not found on this machine. To build Setup.exe locally: choco install innosetup or winget install JRSoftware.InnoSetup"
    } else {
        $IsccPath = $IsccCandidates[0]
        $IssFile = Join-Path $InstallerDir "AgyToolbox.iss"
        $ResolvedPublishDir = (Resolve-Path $PublishDir).Path
        $ResolvedDistDir = (Resolve-Path $DistDir).Path

        & "$IsccPath" "/DMyAppVersion=$CleanVersion" "/DMySourceDir=$ResolvedPublishDir" "/DMyOutputDir=$ResolvedDistDir" "$IssFile"
        $ExeOutput = Join-Path $DistDir "AgyToolbox-Setup-v$CleanVersion-$Runtime.exe"
        if (Test-Path $ExeOutput) {
            Write-Host "EXE Setup built successfully: $ExeOutput" -ForegroundColor Green
        }
    }
}

# 6. Build Portable Single-File EXE
if (-not $SkipPortable) {
    Write-Host "`n[4/4] Building Portable Single-File EXE..." -ForegroundColor Yellow
    $PortableDir = Join-Path $ProjectDir "bin\$Configuration\net8.0-windows\$Runtime\portable"
    dotnet publish $CsprojPath -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $PortableDir
    if ($LASTEXITCODE -eq 0) {
        $SourceExe = Join-Path $PortableDir "AgyToolbox.exe"
        $TargetExe = Join-Path $DistDir "AgyToolbox-Portable-v$CleanVersion-$Runtime.exe"
        if (Test-Path $SourceExe) {
            Copy-Item -Path $SourceExe -Destination $TargetExe -Force
            Write-Host "Portable EXE created successfully: $TargetExe" -ForegroundColor Green
        }
    }
}

# 7. Summary
Write-Host "`n=== Packaging Complete. Dist Files: ===" -ForegroundColor Cyan
Get-ChildItem -Path $DistDir | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
