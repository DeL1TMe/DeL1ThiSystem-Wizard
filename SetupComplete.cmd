@echo off
setlocal EnableExtensions

fltmc >nul 2>&1 || exit /b 0

set "BRAND=DeL1ThiSystem"
set "BASE=%ProgramData%\%BRAND%\Wizard"
set "LOG=%BASE%\SetupComplete_DeL1ThiSystem.log"
set "EXE=%BASE%\DeL1ThiSystem.ConfigurationWizard.exe"
set "WALL_DARK=C:\Wallpapers\dark_background_desktop.png"
set "LOCK_DARK=C:\Wallpapers\dark_background_lockscreen.png"
set "TARGET_NAME=DeL1ThiSystem"

if not exist "%BASE%" mkdir "%BASE%" >nul 2>&1

echo ==========================================================>>"%LOG%"
echo [%date% %time%] SetupComplete started>>"%LOG%"
if exist "%EXE%" (
  echo [%date% %time%] EXE found: "%EXE%">>"%LOG%"
  powershell.exe -NoProfile -Command ^
    "try { (Get-Item -LiteralPath '%EXE%').VersionInfo.FileVersion } catch { '' }" >>"%LOG%" 2>&1
) else (
  echo [%date% %time%] WARN: EXE missing: "%EXE%">>"%LOG%"
)

for /f "usebackq delims=" %%H in (`powershell.exe -NoProfile -Command "try { $env:COMPUTERNAME } catch { '' }"`) do set "CUR_NAME=%%H"
if /i not "%CUR_NAME%"=="%TARGET_NAME%" (
  echo [%date% %time%] Renaming computer from "%CUR_NAME%" to "%TARGET_NAME%">>"%LOG%"
  powershell.exe -NoProfile -Command ^
    "try { Rename-Computer -NewName '%TARGET_NAME%' -Force -ErrorAction Stop } catch { Write-Output $_.Exception.Message; exit 1 }" >>"%LOG%" 2>&1
) else (
  echo [%date% %time%] Computer name already "%TARGET_NAME%">>"%LOG%"
)

if exist "%WALL_DARK%" (
  echo [%date% %time%] Set default wallpaper: "%WALL_DARK%">>"%LOG%"
  reg load "HKU\DefaultUser" "C:\Users\Default\NTUSER.DAT" >nul 2>&1
  reg add "HKU\DefaultUser\Control Panel\Desktop" /v Wallpaper /t REG_SZ /d "%WALL_DARK%" /f >nul 2>&1
  reg add "HKU\DefaultUser\Control Panel\Desktop" /v WallpaperStyle /t REG_SZ /d 10 /f >nul 2>&1
  reg add "HKU\DefaultUser\Control Panel\Desktop" /v TileWallpaper /t REG_SZ /d 0 /f >nul 2>&1
  reg unload "HKU\DefaultUser" >nul 2>&1
) else (
  echo [%date% %time%] WARN: Default wallpaper missing: "%WALL_DARK%">>"%LOG%"
)

if exist "%LOCK_DARK%" (
  echo [%date% %time%] Set lockscreen: "%LOCK_DARK%">>"%LOG%"
  reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\PersonalizationCSP" /v LockScreenImagePath /t REG_SZ /d "%LOCK_DARK%" /f >nul 2>&1
  reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\PersonalizationCSP" /v LockScreenImageStatus /t REG_DWORD /d 1 /f >nul 2>&1
) else (
  echo [%date% %time%] WARN: Lockscreen missing: "%LOCK_DARK%">>"%LOG%"
)

echo [%date% %time%] Set user GeoID (Germany, 94)>>"%LOG%"
reg add "HKCU\Control Panel\International\Geo" /v Nation /t REG_SZ /d 94 /f >nul 2>&1
reg add "HKCU\Control Panel\International\Geo" /v Name /t REG_SZ /d DE /f >nul 2>&1

if exist "%BASE%\WizardTask.xml" (
  schtasks /Query /TN "%BRAND%\Wizard" >nul 2>&1
  if errorlevel 1 (
    echo [%date% %time%] Creating task "%BRAND%\Wizard">>"%LOG%"
    schtasks /Create /F /TN "%BRAND%\Wizard" /XML "%BASE%\WizardTask.xml">>"%LOG%" 2>&1
  ) else (
    echo [%date% %time%] Updating task "%BRAND%\Wizard">>"%LOG%"
    schtasks /Delete /F /TN "%BRAND%\Wizard" >nul 2>&1
    schtasks /Create /F /TN "%BRAND%\Wizard" /XML "%BASE%\WizardTask.xml">>"%LOG%" 2>&1
  )
) else (
  echo [%date% %time%] ERROR: Missing "%BASE%\WizardTask.xml">>"%LOG%"
)

reg delete "HKLM\SOFTWARE\Microsoft\Active Setup\Installed Components\DeL1ThiSystemWizard" /f >nul 2>&1

echo [%date% %time%] SetupComplete finished>>"%LOG%"
exit /b 0
