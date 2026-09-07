$ErrorActionPreference = 'Stop'

$Repo = 'willsantos/Cutube'
$BinaryName = 'cutube.exe'

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Write-WarnMsg {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Get-ArchitectureName {
    $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    switch ($arch) {
        'X64' { return 'amd64' }
        'Arm64' { return 'arm64' }
        default {
            throw "Unsupported architecture: $arch"
        }
    }
}

function Get-LatestVersion {
    $apiUrl = "https://api.github.com/repos/$Repo/releases/latest"
    $response = Invoke-RestMethod -Uri $apiUrl -Method Get
    if (-not $response.tag_name) {
        throw 'Could not detect latest release tag.'
    }
    return [string]$response.tag_name
}

function Get-InstallDir {
    $windowsIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $windowsPrincipal = New-Object Security.Principal.WindowsPrincipal($windowsIdentity)
    $isAdmin = $windowsPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

    if ($isAdmin) {
        return [System.IO.Path]::Combine($env:ProgramFiles, 'Cutube')
    }

    return [System.IO.Path]::Combine($env:LocalAppData, 'Programs', 'Cutube')
}

function Add-ToPath {
    param(
        [string]$Directory,
        [bool]$MachineScope
    )

    $target = if ($MachineScope) { 'Machine' } else { 'User' }
    $pathValue = [Environment]::GetEnvironmentVariable('Path', $target)
    $entries = @()
    if ($pathValue) {
        $entries = $pathValue.Split(';')
    }

    if ($entries -contains $Directory) {
        return
    }

    $newPath = if ([string]::IsNullOrWhiteSpace($pathValue)) {
        $Directory
    }
    else {
        "$pathValue;$Directory"
    }

    [Environment]::SetEnvironmentVariable('Path', $newPath, $target)
    $env:Path = "$env:Path;$Directory"
}

function Check-Dependencies {
    $missing = @()

    if (-not (Get-Command yt-dlp -ErrorAction SilentlyContinue) -and -not (Get-Command youtube-dl -ErrorAction SilentlyContinue)) {
        $missing += 'yt-dlp'
    }

    if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
        $missing += 'ffmpeg'
    }

    if ($missing.Count -eq 0) {
        Write-Info 'Optional dependencies detected (yt-dlp/ffmpeg).'
        return
    }

    Write-WarnMsg "Missing dependencies: $($missing -join ', ')"
    Write-Info 'Install hint: winget install yt-dlp.yt-dlp Gyan.FFmpeg'
}

try {
    Write-Host ''
    Write-Host 'Cutube CLI Installer (Windows)'
    Write-Host ''

    $arch = Get-ArchitectureName
    Write-Info "Detected architecture: $arch"

    if ($arch -ne 'amd64') {
        throw "No Windows release available for $arch at this time."
    }

    $version = Get-LatestVersion
    Write-Info "Latest version: $version"

    $assetName = 'cutube-windows-amd64.exe.zip'
    $downloadUrl = "https://github.com/$Repo/releases/download/$version/$assetName"

    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    $zipPath = Join-Path $tempDir $assetName
    Write-Info "Downloading $assetName"
    Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath

    Expand-Archive -Path $zipPath -DestinationPath $tempDir -Force

    $sourceExe = Join-Path $tempDir $BinaryName
    if (-not (Test-Path $sourceExe)) {
        throw "Executable not found in package: $BinaryName"
    }

    $installDir = Get-InstallDir
    $machineScope = $installDir.StartsWith($env:ProgramFiles)

    if (-not (Test-Path $installDir)) {
        New-Item -ItemType Directory -Path $installDir -Force | Out-Null
    }

    $targetExe = Join-Path $installDir $BinaryName
    Copy-Item -Path $sourceExe -Destination $targetExe -Force
    Add-ToPath -Directory $installDir -MachineScope:$machineScope

    Check-Dependencies

    Write-Info "Installed at $targetExe"
    Write-Host ''
    Write-Host 'Use: cutube --help'

    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
catch {
    Write-ErrorMsg $_.Exception.Message
    exit 1
}
