# ═══════════════════════════════════════════════════════════════════════════
# PORT 6066 - Automated Setup Script
# Windows Server Firewall Configuration for VT200L GPS Trackers
# ═══════════════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  PORT 6066 - Windows Firewall Setup                          " -ForegroundColor Cyan
Write-Host "  VT200L GPS Tracker Configuration                            " -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "❌ ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please:" -ForegroundColor Yellow
    Write-Host "  1. Right-click on PowerShell" -ForegroundColor Yellow
    Write-Host "  2. Select 'Run as Administrator'" -ForegroundColor Yellow
    Write-Host "  3. Run this script again" -ForegroundColor Yellow
    Write-Host ""
    Pause
    Exit 1
}

Write-Host "✅ Running as Administrator" -ForegroundColor Green
Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════
# STEP 1: Create Firewall Rule
# ═══════════════════════════════════════════════════════════════════════════

Write-Host "STEP 1: Creating Windows Firewall Rule..." -ForegroundColor Yellow
Write-Host ""

try {
    # Check if rule already exists
    $existingRule = Get-NetFirewallRule -DisplayName "VT200L GPS Tracker Port 6066" -ErrorAction SilentlyContinue
    
    if ($existingRule) {
        Write-Host "⚠ Firewall rule already exists. Removing old rule..." -ForegroundColor Yellow
        Remove-NetFirewallRule -DisplayName "VT200L GPS Tracker Port 6066" -ErrorAction SilentlyContinue
    }
    
    # Create new firewall rule
    New-NetFirewallRule `
        -DisplayName "VT200L GPS Tracker Port 6066" `
        -Description "Allows VT200L GPS devices to connect on TCP port 6066" `
        -Direction Inbound `
        -LocalPort 6066 `
        -Protocol TCP `
        -Action Allow `
        -Profile Any `
        -Enabled True | Out-Null
    
    Write-Host "✅ Firewall rule created successfully!" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "❌ ERROR: Failed to create firewall rule" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Pause
    Exit 1
}

# ═══════════════════════════════════════════════════════════════════════════
# STEP 2: Verify Firewall Rule
# ═══════════════════════════════════════════════════════════════════════════

Write-Host "STEP 2: Verifying Firewall Rule..." -ForegroundColor Yellow
Write-Host ""

try {
    $rule = Get-NetFirewallRule -DisplayName "VT200L GPS Tracker Port 6066"
    
    Write-Host "  Rule Name     : $($rule.DisplayName)" -ForegroundColor Cyan
    Write-Host "  Direction     : $($rule.Direction)" -ForegroundColor Cyan
    Write-Host "  Action        : $($rule.Action)" -ForegroundColor Cyan
    Write-Host "  Enabled       : $($rule.Enabled)" -ForegroundColor Cyan
    Write-Host "  Port          : 6066" -ForegroundColor Cyan
    Write-Host "  Protocol      : TCP" -ForegroundColor Cyan
    Write-Host ""
    
    if ($rule.Enabled -eq $true) {
        Write-Host "✅ Firewall rule is active and enabled!" -ForegroundColor Green
    } else {
        Write-Host "⚠ WARNING: Firewall rule exists but is disabled!" -ForegroundColor Yellow
    }
    Write-Host ""
}
catch {
    Write-Host "❌ ERROR: Could not verify firewall rule" -ForegroundColor Red
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════════════════
# STEP 3: Display Network Information
# ═══════════════════════════════════════════════════════════════════════════

Write-Host "STEP 3: Network Information..." -ForegroundColor Yellow
Write-Host ""

try {
    # Get local IP addresses
    $ipAddresses = Get-NetIPAddress -AddressFamily IPv4 | 
                   Where-Object {$_.InterfaceAlias -notlike "*Loopback*"} |
                   Select-Object IPAddress, InterfaceAlias
    
    Write-Host "  Local IP Address(es):" -ForegroundColor Cyan
    foreach ($ip in $ipAddresses) {
        Write-Host "    $($ip.IPAddress)  ($($ip.InterfaceAlias))" -ForegroundColor White
    }
    Write-Host ""
    
    # Try to get public IP
    Write-Host "  Attempting to get Public IP..." -ForegroundColor Cyan
    try {
        $publicIp = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing -TimeoutSec 5).Content
        Write-Host "    Public IP: $publicIp" -ForegroundColor White
        Write-Host ""
    }
    catch {
        Write-Host "    (Could not determine public IP - check manually)" -ForegroundColor Gray
        Write-Host ""
    }
}
catch {
    Write-Host "  (Network information unavailable)" -ForegroundColor Gray
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════════════════
# STEP 4: Check if Port is in Use
# ═══════════════════════════════════════════════════════════════════════════

Write-Host "STEP 4: Checking Port 6066 Status..." -ForegroundColor Yellow
Write-Host ""

try {
    $portInUse = Get-NetTCPConnection -LocalPort 6066 -ErrorAction SilentlyContinue
    
    if ($portInUse) {
        Write-Host "  ✅ Port 6066 is LISTENING (Application is running)" -ForegroundColor Green
        Write-Host ""
        Write-Host "  Connection Details:" -ForegroundColor Cyan
        $portInUse | Format-Table LocalAddress, LocalPort, State, OwningProcess -AutoSize
        
        # Get process name
        $processId = $portInUse[0].OwningProcess
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($process) {
            Write-Host "  Process: $($process.ProcessName) (PID: $processId)" -ForegroundColor Cyan
        }
        Write-Host ""
    } else {
        Write-Host "  ℹ Port 6066 is NOT in use (Application not started yet)" -ForegroundColor Yellow
        Write-Host "  This is normal if you haven't started FX_TCP.exe yet." -ForegroundColor Gray
        Write-Host ""
    }
}
catch {
    Write-Host "  ℹ Port 6066 is available (not in use)" -ForegroundColor Yellow
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════════════════
# Summary
# ═══════════════════════════════════════════════════════════════════════════

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  SETUP COMPLETE!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ Windows Firewall port 6066 is now OPEN" -ForegroundColor Green
Write-Host ""

Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Run SQL Script:" -ForegroundColor White
Write-Host "   Execute: FX_TCP\SQL\VT200L_StoredProcedures.sql" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Start Application:" -ForegroundColor White
Write-Host "   Launch: FX_TCP.exe" -ForegroundColor Gray
Write-Host "   Open: frm6066 form" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Configure Router (if behind NAT):" -ForegroundColor White
Write-Host "   Forward port 6066 to this server's local IP" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Configure VT200L Devices:" -ForegroundColor White
Write-Host "   Send SMS: 100,<server_ip>,6066" -ForegroundColor Gray
Write-Host ""

Write-Host "For detailed instructions, see:" -ForegroundColor Cyan
Write-Host "  - WINDOWS_SERVER_PORT_SETUP_6066.md" -ForegroundColor Gray
Write-Host "  - PORT_6066_QUICK_SETUP.txt" -ForegroundColor Gray
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Pause
