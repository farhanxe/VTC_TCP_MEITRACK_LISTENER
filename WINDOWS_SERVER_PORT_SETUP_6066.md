# 🔧 Windows Server Port 6066 Setup Guide

This guide covers **all steps** to open and configure port 6066 on your Windows Server for VT200L GPS trackers.

---

## 📋 Table of Contents
1. [Open Windows Firewall](#1-open-windows-firewall)
2. [Verify Port is Listening](#2-verify-port-is-listening)
3. [Configure Router/External Firewall](#3-configure-routerexternal-firewall-if-applicable)
4. [Test Port Accessibility](#4-test-port-accessibility)
5. [Troubleshooting](#5-troubleshooting)

---

## 1. Open Windows Firewall

### Method A: PowerShell (Recommended - Fast & Accurate)

**Step 1:** Open PowerShell as Administrator
- Press `Win + X`
- Click "Windows PowerShell (Admin)" or "Terminal (Admin)"

**Step 2:** Run this command:

```powershell
New-NetFirewallRule -DisplayName "VT200L GPS Tracker Port 6066" `
  -Direction Inbound `
  -LocalPort 6066 `
  -Protocol TCP `
  -Action Allow `
  -Profile Any `
  -Enabled True
```

**Step 3:** Verify the rule was created:

```powershell
Get-NetFirewallRule -DisplayName "VT200L GPS Tracker Port 6066" | Format-List
```

You should see:
```
DisplayName  : VT200L GPS Tracker Port 6066
Direction    : Inbound
Action       : Allow
Enabled      : True
Profile      : Any
LocalPort    : 6066
Protocol     : TCP
```

✅ **Done!** Port 6066 is now open.

---

### Method B: Windows Firewall GUI (If you prefer visual interface)

**Step 1:** Open Windows Firewall
- Press `Win + R`
- Type: `wf.msc`
- Press Enter

**Step 2:** Create Inbound Rule
- Click "Inbound Rules" in left panel
- Click "New Rule..." in right panel

**Step 3:** Rule Type
- Select: **Port**
- Click Next

**Step 4:** Protocol and Ports
- Protocol type: **TCP**
- Specific local ports: **6066**
- Click Next

**Step 5:** Action
- Select: **Allow the connection**
- Click Next

**Step 6:** Profile
- Check ALL three boxes:
  - ☑ Domain
  - ☑ Private
  - ☑ Public
- Click Next

**Step 7:** Name
- Name: **VT200L GPS Tracker Port 6066**
- Description: **Allows VT200L GPS devices to connect on port 6066**
- Click Finish

✅ **Done!** Port 6066 is now open.

---

### Method C: Command Prompt (Alternative)

**Step 1:** Open Command Prompt as Administrator
- Press `Win + X`
- Click "Command Prompt (Admin)"

**Step 2:** Run this command:

```cmd
netsh advfirewall firewall add rule name="VT200L GPS Tracker Port 6066" dir=in action=allow protocol=TCP localport=6066 profile=any
```

**Step 3:** Verify:

```cmd
netsh advfirewall firewall show rule name="VT200L GPS Tracker Port 6066"
```

✅ **Done!** Port 6066 is now open.

---

## 2. Verify Port is Listening

After starting your FX_TCP application with frm6066:

### Check if Application is Listening

**PowerShell:**
```powershell
Get-NetTCPConnection -LocalPort 6066 | Format-Table
```

**Command Prompt:**
```cmd
netstat -an | findstr ":6066"
```

**Expected Output:**
```
TCP    0.0.0.0:6066           0.0.0.0:0              LISTENING
```

✅ This means your application successfully bound to port 6066.

**If you see nothing:**
- Application is not running
- Application failed to start (check frm6066 form)
- Port is already in use by another application

---

## 3. Configure Router/External Firewall (If Applicable)

If your server is behind a router/NAT and devices connect from the internet:

### A. Find Your Server's Local IP

**PowerShell:**
```powershell
Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.InterfaceAlias -notlike "*Loopback*"} | Format-Table IPAddress, InterfaceAlias
```

**Command Prompt:**
```cmd
ipconfig
```

Look for "IPv4 Address" under your main network adapter.
Example: `192.168.1.100`

### B. Configure Port Forwarding on Router

**Login to your router** (usually http://192.168.1.1 or similar)

**Create Port Forwarding Rule:**
- Service Name: `GPS Tracker 6066`
- External Port: `6066`
- Internal Port: `6066`
- Internal IP: `192.168.1.100` (your server's IP)
- Protocol: `TCP`
- Enable: `Yes`

**Save and Reboot Router**

### C. Find Your Public IP

Visit: https://www.whatismyip.com/
Or use PowerShell:
```powershell
(Invoke-WebRequest -Uri "https://api.ipify.org").Content
```

This is the IP address your VT200L devices should connect to.

### D. Configure Devices

Send SMS to VT200L devices:
```
100,<your_public_ip>,6066
```

Example: If your public IP is `203.112.45.67`:
```
100,203.112.45.67,6066
```

---

## 4. Test Port Accessibility

### From Local Network (Same LAN)

**PowerShell:**
```powershell
Test-NetConnection -ComputerName localhost -Port 6066
```

**Expected:**
```
TcpTestSucceeded : True
```

### From Internet (If devices connect externally)

Use an online port checker:
- https://www.yougetsignal.com/tools/open-ports/
- https://canyouseeme.org/

**Enter:**
- IP: Your public IP
- Port: 6066

**Expected:** "Port 6066 is open"

### Using Telnet (Manual Test)

**From another computer:**
```cmd
telnet <server_ip> 6066
```

**Expected:** Connection established (black screen)
**Press Ctrl+C to exit**

---

## 5. Troubleshooting

### ❌ Port Test Fails from Internet

**Possible Causes:**
1. **Router port forwarding not configured**
   - Double-check router settings
   - Try rebooting router
   
2. **ISP blocking the port**
   - Some ISPs block non-standard ports
   - Contact ISP or use different port (e.g., 8066)
   
3. **Server firewall blocking**
   - Verify firewall rule is enabled
   - Try temporarily disabling firewall to test
   
4. **Application not listening**
   - Check if FX_TCP.exe is running
   - Check if frm6066 form is open
   - Check status bar shows "Listening on :6066"

### ❌ Application Won't Start on Port 6066

**Error: "Address already in use" or "Port is busy"**

**Find what's using port 6066:**
```powershell
Get-NetTCPConnection -LocalPort 6066 | Format-Table OwningProcess, State
```

**Get process name:**
```powershell
Get-Process -Id <ProcessID>
```

**Kill the process (if safe):**
```powershell
Stop-Process -Id <ProcessID> -Force
```

### ❌ Firewall Rule Not Working

**Check if Windows Firewall is enabled:**
```powershell
Get-NetFirewallProfile | Format-Table Name, Enabled
```

**Disable and re-enable the rule:**
```powershell
# Disable
Set-NetFirewallRule -DisplayName "VT200L GPS Tracker Port 6066" -Enabled False

# Enable
Set-NetFirewallRule -DisplayName "VT200L GPS Tracker Port 6066" -Enabled True
```

**Delete and recreate the rule:**
```powershell
# Delete
Remove-NetFirewallRule -DisplayName "VT200L GPS Tracker Port 6066"

# Recreate
New-NetFirewallRule -DisplayName "VT200L GPS Tracker Port 6066" -Direction Inbound -LocalPort 6066 -Protocol TCP -Action Allow -Profile Any -Enabled True
```

### ❌ No Packets Received from Devices

**Checklist:**
1. ☑ Firewall rule created and enabled
2. ☑ Application running and form open
3. ☑ Status shows "Listening on :6066"
4. ☑ Green indicator light on
5. ☑ Devices configured with correct IP:6066
6. ☑ Devices have internet connectivity
7. ☑ Devices have SIM cards with data plan
8. ☑ Router port forwarding configured (if behind NAT)

**Check device configuration:**
Send SMS to device:
```
101
```
This should reply with current server settings.

---

## 📝 Complete Setup Checklist

### On Windows Server:

- [ ] **Step 1:** Open port 6066 in Windows Firewall
  ```powershell
  New-NetFirewallRule -DisplayName "VT200L GPS Tracker Port 6066" -Direction Inbound -LocalPort 6066 -Protocol TCP -Action Allow -Profile Any -Enabled True
  ```

- [ ] **Step 2:** Run SQL script `VT200L_StoredProcedures.sql`

- [ ] **Step 3:** Build and deploy FX_TCP application

- [ ] **Step 4:** Launch FX_TCP.exe

- [ ] **Step 5:** Open frm6066 form

- [ ] **Step 6:** Verify status shows "Listening on :6066"

- [ ] **Step 7:** Verify green indicator light

- [ ] **Step 8:** Check log shows "VT200L Server started on port 6066"

### On Router (if applicable):

- [ ] **Step 1:** Find server's local IP (e.g., 192.168.1.100)

- [ ] **Step 2:** Login to router admin panel

- [ ] **Step 3:** Create port forwarding rule:
  - External: 6066 → Internal: 6066
  - IP: 192.168.1.100
  - Protocol: TCP

- [ ] **Step 4:** Save and reboot router

- [ ] **Step 5:** Note your public IP address

### On VT200L Devices:

- [ ] **Step 1:** Send SMS configuration:
  ```
  100,<server_ip>,6066
  ```

- [ ] **Step 2:** Set reporting interval:
  ```
  101,30
  ```
  (Reports every 30 seconds)

- [ ] **Step 3:** Enable GPS:
  ```
  102,1
  ```

- [ ] **Step 4:** Verify device replies "OK"

- [ ] **Step 5:** Wait 1-2 minutes for first packet

- [ ] **Step 6:** Check frm6066 connection list for device IMEI

---

## 🔍 Quick Reference Commands

### View Firewall Rules:
```powershell
Get-NetFirewallRule -DisplayName "*6066*" | Format-List
```

### View Listening Ports:
```powershell
Get-NetTCPConnection -State Listen | Where-Object {$_.LocalPort -eq 6066}
```

### View Active Connections on Port 6066:
```powershell
Get-NetTCPConnection -LocalPort 6066 | Format-Table
```

### Test Port Locally:
```powershell
Test-NetConnection -ComputerName localhost -Port 6066
```

### View Windows Firewall Status:
```powershell
Get-NetFirewallProfile | Format-Table Name, Enabled
```

---

## 📞 Support

If issues persist:

1. **Check application logs:**
   - Live log in frm6066 form
   - AuditLog table in SQL database

2. **Check Windows Event Viewer:**
   - Run: `eventvwr.msc`
   - Windows Logs → Application
   - Look for errors from your application

3. **Verify database connectivity:**
   - Connection string in App.config
   - SQL Server allows remote connections
   - User `sa` has correct password

4. **Contact device vendor:**
   - Verify VT200L firmware version
   - Confirm protocol specification
   - Check if device needs special configuration

---

## ✅ Success Indicators

You'll know it's working when you see:

**In frm6066 Form:**
- ✅ Status: "Listening on :6066"
- ✅ Green indicator light
- ✅ Connections: 1 or more
- ✅ RX (Received): Increasing counter
- ✅ Live log shows: "[RX] IMEI:xxxxx..."
- ✅ Live log shows: "[ACK] $$..."
- ✅ Live log shows: "[DB] VT200L-UpdateInsert-OK"

**In Database:**
```sql
-- Check live data
SELECT TOP 10 * FROM VehicleTracking 
WHERE GpsIMEINumber = '<your_device_imei>'
ORDER BY UpdateTime DESC
```

Should show recent timestamps and GPS coordinates.

---

## 🎉 You're Done!

Once you see packets arriving and data in the database, your port 6066 setup is **complete and working!** 🚀

---

**Document Version:** 1.0  
**Last Updated:** 2026-08-18  
**For:** VT200L GPS Tracker Port 6066
