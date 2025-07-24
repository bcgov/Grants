# Setup script for PowerShell Keycloak automation
# Installs required dependencies for automated token retrieval

Write-Host "???  Setting up Keycloak Token Automation Environment" -ForegroundColor Cyan
Write-Host ""

# Check PowerShell version
$psVersion = $PSVersionTable.PSVersion
Write-Host "PowerShell Version: $psVersion" -ForegroundColor White

if ($psVersion.Major -lt 5) {
    Write-Host "? PowerShell 5.0 or higher is required" -ForegroundColor Red
    exit 1
}

# Check execution policy
$executionPolicy = Get-ExecutionPolicy
Write-Host "Execution Policy: $executionPolicy" -ForegroundColor White

if ($executionPolicy -eq "Restricted") {
    Write-Host "??  Execution policy is Restricted. You may need to run:" -ForegroundColor Yellow
    Write-Host "   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser" -ForegroundColor White
}

Write-Host ""
Write-Host "?? Installing required modules..." -ForegroundColor Cyan

# Install Selenium WebDriver module
try {
    Write-Host "Installing Selenium WebDriver module..." -ForegroundColor White
    
    if (Get-Module -ListAvailable -Name Selenium) {
        Write-Host "? Selenium module already installed" -ForegroundColor Green
    } else {
        Install-Module -Name Selenium -Force -Scope CurrentUser
        Write-Host "? Selenium module installed successfully" -ForegroundColor Green
    }
} catch {
    Write-Host "? Failed to install Selenium module: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   You can install manually with: Install-Module -Name Selenium" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "?? Checking browser and driver availability..." -ForegroundColor Cyan

# Check for Chrome browser
$chromeInstalled = $false
$chromePaths = @(
    "${env:ProgramFiles}\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
    "${env:LOCALAPPDATA}\Google\Chrome\Application\chrome.exe"
)

foreach ($path in $chromePaths) {
    if (Test-Path $path) {
        Write-Host "? Chrome browser found at: $path" -ForegroundColor Green
        $chromeInstalled = $true
        break
    }
}

if (-not $chromeInstalled) {
    Write-Host "??  Chrome browser not found. Install from: https://www.google.com/chrome/" -ForegroundColor Yellow
}

# Check for Edge browser (fallback)
$edgeInstalled = $false
$edgePaths = @(
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
    "${env:ProgramFiles}\Microsoft\Edge\Application\msedge.exe"
)

foreach ($path in $edgePaths) {
    if (Test-Path $path) {
        Write-Host "? Edge browser found at: $path" -ForegroundColor Green
        $edgeInstalled = $true
        break
    }
}

# Try to download ChromeDriver automatically
Write-Host ""
Write-Host "?? Setting up ChromeDriver..." -ForegroundColor Cyan

try {
    # Check if ChromeDriver is already available
    $chromeDriverPath = Get-Command chromedriver -ErrorAction SilentlyContinue
    
    if ($chromeDriverPath) {
        Write-Host "? ChromeDriver already available in PATH" -ForegroundColor Green
    } else {
        # Create drivers directory
        $driversDir = "$env:USERPROFILE\selenium-drivers"
        if (-not (Test-Path $driversDir)) {
            New-Item -ItemType Directory -Path $driversDir -Force | Out-Null
        }
        
        # Get Chrome version to download matching ChromeDriver
        if ($chromeInstalled) {
            Write-Host "Detecting Chrome version..." -ForegroundColor White
            
            # Try to get Chrome version
            $chromeVersion = $null
            foreach ($path in $chromePaths) {
                if (Test-Path $path) {
                    $chromeVersion = (Get-ItemProperty $path).VersionInfo.ProductVersion
                    break
                }
            }
            
            if ($chromeVersion) {
                Write-Host "Chrome version: $chromeVersion" -ForegroundColor White
                
                # Download latest stable ChromeDriver
                $chromeDriverUrl = "https://chromedriver.storage.googleapis.com/LATEST_RELEASE"
                try {
                    $latestVersion = Invoke-RestMethod -Uri $chromeDriverUrl
                    $chromeDriverDownloadUrl = "https://chromedriver.storage.googleapis.com/$latestVersion/chromedriver_win32.zip"
                    
                    Write-Host "Downloading ChromeDriver version $latestVersion..." -ForegroundColor White
                    
                    $zipPath = "$driversDir\chromedriver.zip"
                    Invoke-WebRequest -Uri $chromeDriverDownloadUrl -OutFile $zipPath
                    
                    # Extract ChromeDriver
                    Expand-Archive -Path $zipPath -DestinationPath $driversDir -Force
                    Remove-Item $zipPath
                    
                    # Add to PATH for current session
                    $env:PATH += ";$driversDir"
                    
                    Write-Host "? ChromeDriver installed successfully" -ForegroundColor Green
                    Write-Host "   Location: $driversDir\chromedriver.exe" -ForegroundColor White
                    
                } catch {
                    Write-Host "??  Could not download ChromeDriver automatically: $($_.Exception.Message)" -ForegroundColor Yellow
                    Write-Host "   Download manually from: https://chromedriver.chromium.org/" -ForegroundColor White
                }
            }
        }
    }
} catch {
    Write-Host "??  ChromeDriver setup failed: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "?? Testing setup..." -ForegroundColor Cyan

# Test Selenium import
try {
    Import-Module Selenium -ErrorAction Stop
    Write-Host "? Selenium module loads successfully" -ForegroundColor Green
} catch {
    Write-Host "? Selenium module failed to load: $($_.Exception.Message)" -ForegroundColor Red
}

# Test basic WebDriver functionality
try {
    if ($chromeInstalled) {
        Write-Host "Testing Chrome WebDriver..." -ForegroundColor White
        
        $chromeOptions = New-Object OpenQA.Selenium.Chrome.ChromeOptions
        $chromeOptions.AddArgument("--headless")
        $chromeOptions.AddArgument("--disable-gpu")
        $chromeOptions.AddArgument("--no-sandbox")
        
        $driver = New-Object OpenQA.Selenium.Chrome.ChromeDriver($chromeOptions)
        $driver.Navigate().GoToUrl("https://www.google.com")
        $title = $driver.Title
        $driver.Quit()
        
        if ($title -like "*Google*") {
            Write-Host "? WebDriver test successful" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "??  WebDriver test failed: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   This may be due to missing ChromeDriver or browser issues" -ForegroundColor White
}

Write-Host ""
Write-Host "?? Setup Summary:" -ForegroundColor Cyan
Write-Host "=================" -ForegroundColor Cyan

if (Get-Module -ListAvailable -Name Selenium) {
    Write-Host "? Selenium Module: Installed" -ForegroundColor Green
} else {
    Write-Host "? Selenium Module: Not installed" -ForegroundColor Red
}

if ($chromeInstalled) {
    Write-Host "? Chrome Browser: Available" -ForegroundColor Green
} else {
    Write-Host "? Chrome Browser: Not found" -ForegroundColor Red
}

if ($edgeInstalled) {
    Write-Host "? Edge Browser: Available" -ForegroundColor Green
} else {
    Write-Host "??  Edge Browser: Not found" -ForegroundColor Yellow
}

$chromeDriverAvailable = (Get-Command chromedriver -ErrorAction SilentlyContinue) -ne $null
if ($chromeDriverAvailable) {
    Write-Host "? ChromeDriver: Available" -ForegroundColor Green
} else {
    Write-Host "??  ChromeDriver: Not in PATH" -ForegroundColor Yellow
}

Write-Host ""

if ((Get-Module -ListAvailable -Name Selenium) -and ($chromeInstalled -or $edgeInstalled)) {
    Write-Host "?? Setup complete! You can now use the automated token scripts." -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage examples:" -ForegroundColor Cyan
    Write-Host "  .\Get-KeycloakToken.ps1 -Username 'your-username' -Password (Read-Host -AsSecureString)" -ForegroundColor White
    Write-Host "  .\Get-KeycloakTokenSimple.ps1 -Username 'your-username' -Password (Read-Host -AsSecureString) -IdentityProvider 'IDIR'" -ForegroundColor White
} else {
    Write-Host "??  Setup incomplete. Please install missing components:" -ForegroundColor Yellow
    
    if (-not (Get-Module -ListAvailable -Name Selenium)) {
        Write-Host "  - Install Selenium: Install-Module -Name Selenium -Force" -ForegroundColor White
    }
    
    if (-not $chromeInstalled -and -not $edgeInstalled) {
        Write-Host "  - Install Chrome: https://www.google.com/chrome/" -ForegroundColor White
    }
    
    if (-not $chromeDriverAvailable) {
        Write-Host "  - Download ChromeDriver: https://chromedriver.chromium.org/" -ForegroundColor White
    }
}

Write-Host ""