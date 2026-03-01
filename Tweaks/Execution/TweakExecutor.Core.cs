using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static partial class TweakExecutor
{

    private static void EnsureLogDir()
    {
        try { Directory.CreateDirectory(BaseDir); } catch { }
    }

    private static void LogRaw(string tag, string message)
    {
        try
        {
            using var stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{tag}] {message}");
        }
        catch
        {
        }
    }

    private static void Log(string message) => LogRaw("TWEAK", message);

    /// <summary>
    /// Public logging method for non-tweak components (App, ProgressPage, etc.).
    /// </summary>
    public static void LogWizard(string message)
    {
        EnsureLogDir();
        LogRaw("WIZARD", message);
    }

    /// <summary>
    /// Writes session header: OS version, build, user, admin status, etc.
    /// Call once at application startup.
    /// </summary>
    public static void LogSessionStart(string osFamily, string themeChoice, bool isForceRun, bool isAdmin)
    {
        EnsureLogDir();
        LogRaw("WIZARD", new string('=', 72));
        LogRaw("WIZARD", "SESSION START");
        LogRaw("WIZARD", $"OS: {Environment.OSVersion.VersionString} (Build {Environment.OSVersion.Version.Build})");
        LogRaw("WIZARD", $"OsFamily: {osFamily} | Theme: {themeChoice} | ForceRun: {isForceRun} | Admin: {isAdmin}");
        LogRaw("WIZARD", $"User: {Environment.UserName} | Machine: {Environment.MachineName}");
        LogRaw("WIZARD", $"CLR: {Environment.Version} | 64-bit OS: {Environment.Is64BitOperatingSystem} | 64-bit process: {Environment.Is64BitProcess}");
        LogRaw("WIZARD", new string('=', 72));
    }


    private static void LogReg(string kind, string path, string name, string value)
    {
        Log($"{kind} {path} {name}={value}");
    }


    private static void LogCommand(string prefix, string text)
    {
        var normalized = text.Replace("\r\n", "\\n").Replace("\n", "\\n");
        Log($"{prefix}: {normalized}");
    }


    private static void SetDword(RegistryHive hive, string subKey, string name, int value)
    {
        var path = $"{hive}\\{subKey}";
        try
        {
            LogReg("REG DWORD", path, name, value.ToString(CultureInfo.InvariantCulture));
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.CreateSubKey(subKey, true);
            key?.SetValue(name, value, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            Log($"REG ERROR {path} {name}: {ex.Message}");
        }
    }


    private static void SetDwordIfKeyExists(RegistryHive hive, RegistryView view, string subKey, string name, int value)
    {
        var path = $"{hive}\\{subKey}";
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(subKey, writable: true);
            if (key == null)
                return;
            LogReg("REG DWORD", path, name, value.ToString(CultureInfo.InvariantCulture));
            key.SetValue(name, value, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            Log($"REG ERROR {path} {name}: {ex.Message}");
        }
    }


    private static void SetString(RegistryHive hive, string subKey, string name, string value)
    {
        var path = $"{hive}\\{subKey}";
        try
        {
            LogReg("REG SZ", path, name, value);
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.CreateSubKey(subKey, true);
            key?.SetValue(name, value, RegistryValueKind.String);
        }
        catch (Exception ex)
        {
            Log($"REG ERROR {path} {name}: {ex.Message}");
        }
    }


    private static void SetBinary(RegistryHive hive, string subKey, string name, byte[] value)
    {
        var path = $"{hive}\\{subKey}";
        try
        {
            Log($"REG BINARY {path} {name}=[{value.Length} bytes]");
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.CreateSubKey(subKey, true);
            key?.SetValue(name, value, RegistryValueKind.Binary);
        }
        catch (Exception ex)
        {
            Log($"REG ERROR {path} {name}: {ex.Message}");
        }
    }


    private static void SetDefaultValue(RegistryHive hive, string subKey, string value)
    {
        var path = $"{hive}\\{subKey}";
        try
        {
            LogReg("REG SZ", path, "(Default)", value);
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.CreateSubKey(subKey, true);
            key?.SetValue(null, value, RegistryValueKind.String);
        }
        catch (Exception ex)
        {
            Log($"REG ERROR {path} (Default): {ex.Message}");
        }
    }


    private static void SetDefaultUserDword(string subKey, string name, int value)
    {
        WithDefaultUserHive(root =>
        {
            LogReg("REG-DEFAULT DWORD", $@"HKU\DefaultUser\{subKey}", name, value.ToString(CultureInfo.InvariantCulture));
            using var key = root.CreateSubKey(subKey, true);
            key?.SetValue(name, value, RegistryValueKind.DWord);
        });
    }


    private static void SetDefaultUserString(string subKey, string name, string value)
    {
        WithDefaultUserHive(root =>
        {
            LogReg("REG-DEFAULT SZ", $@"HKU\DefaultUser\{subKey}", name, value);
            using var key = root.CreateSubKey(subKey, true);
            key?.SetValue(name, value, RegistryValueKind.String);
        });
    }


    private static void WithDefaultUserHive(Action<RegistryKey> action)
    {
        const string mountName = "DefaultUser";
        var ntUser = @"C:\Users\Default\NTUSER.DAT";
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
        var alreadyLoaded = baseKey.OpenSubKey(mountName) != null;
        Log($"DefaultUserHive: alreadyLoaded={alreadyLoaded}");

        if (!alreadyLoaded)
            RunProcess("reg.exe", $"load HKU\\{mountName} \"{ntUser}\"");

        try
        {
            using var key = baseKey.OpenSubKey(mountName, writable: true);
            if (key != null)
                action(key);
            else
                Log("DefaultUserHive: WARN key is null after load");
        }
        catch (Exception ex)
        {
            Log($"DefaultUserHive: ERROR {ex.Message}");
        }
        finally
        {
            if (!alreadyLoaded)
                RunProcess("reg.exe", $"unload HKU\\{mountName}");
        }
    }


    private static int RunProcessWithTimeout(string fileName, string arguments, int timeoutMs, out bool timedOut)
    {
        WaitInternetGateIfNeeded();
        timedOut = false;
        int procId = System.Threading.Interlocked.Increment(ref _procSeq);
        Log($"PROC {procId} START: {fileName} {arguments}");
        var processEncoding = GetPreferredProcessEncoding(fileName);
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = processEncoding,
            StandardErrorEncoding = processEncoding
        };
        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Log($"PROC {procId} OUT: {e.Data}");
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Log($"PROC {procId} ERR: {e.Data}");
        };
        if (!proc.Start())
        {
            Log($"PROC {procId} ERROR: failed to start");
            return -1;
        }
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        if (!proc.WaitForExit(timeoutMs))
        {
            timedOut = true;
            try { proc.Kill(true); } catch { }
            Log($"PROC {procId} TIMEOUT after {timeoutMs}ms");
            return -1;
        }
        proc.WaitForExit();
        Log($"PROC {procId} EXIT: {proc.ExitCode}");
        return proc.ExitCode;
    }


    private static void RunPowerShell(string command)
    {
        WaitInternetGateIfNeeded();
        var wrapped =
            "[Console]::InputEncoding=[System.Text.UTF8Encoding]::new($false); " +
            "[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false); " +
            "$OutputEncoding=[System.Text.UTF8Encoding]::new($false); " +
            "$ProgressPreference='SilentlyContinue'; " + command;
        var bytes = Encoding.Unicode.GetBytes(wrapped);
        var encoded = Convert.ToBase64String(bytes);
        LogCommand("PS", command);
        if (encoded.Length > 7000 && TryCreatePowerShellScriptFile(wrapped, out var scriptPath))
        {
            try
            {
                RunProcess("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"");
            }
            finally
            {
                TryDelete(scriptPath);
            }
            return;
        }

        RunProcess("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}");
    }


    private static void RunPowerShellDetached(string command, string tag)
    {
        WaitInternetGateIfNeeded();
        var wrapped =
            "[Console]::InputEncoding=[System.Text.UTF8Encoding]::new($false); " +
            "[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false); " +
            "$OutputEncoding=[System.Text.UTF8Encoding]::new($false); " +
            "$ProgressPreference='SilentlyContinue'; " + command;
        var bytes = Encoding.Unicode.GetBytes(wrapped);
        var encoded = Convert.ToBase64String(bytes);
        LogCommand($"PS-{tag}", command);
        try
        {
            if (encoded.Length > 7000 && TryCreatePowerShellScriptFile(wrapped, out var scriptPath))
            {
                var filePsi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var fileProc = Process.Start(filePsi);
                Log($"PS-{tag} START: pid={(fileProc?.Id.ToString() ?? "n/a")} (file mode)");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            var proc = Process.Start(psi);
            Log($"PS-{tag} START: pid={(proc?.Id.ToString() ?? "n/a")}");
        }
        catch (Exception ex)
        {
            Log($"PS-{tag} ERROR: {ex.Message}");
        }
    }


    private static void RunProcess(string fileName, string arguments)
    {
        WaitInternetGateIfNeeded();
        int procId = System.Threading.Interlocked.Increment(ref _procSeq);
        Log($"PROC {procId} START: {fileName} {arguments}");
        var processEncoding = GetPreferredProcessEncoding(fileName);
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = processEncoding,
            StandardErrorEncoding = processEncoding
        };
        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Log($"PROC {procId} OUT: {e.Data}");
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Log($"PROC {procId} ERR: {e.Data}");
        };
        if (!proc.Start())
        {
            Log($"PROC {procId} ERROR: failed to start");
            return;
        }
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();
        Log($"PROC {procId} EXIT: {proc.ExitCode}");
    }


    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Log($"Delete failed: {path} ({ex.Message})");
        }
    }

    private static bool TryCreatePowerShellScriptFile(string wrappedScript, out string scriptPath)
    {
        scriptPath = string.Empty;
        try
        {
            var scriptsDir = Path.Combine(BaseDir, "Scripts");
            Directory.CreateDirectory(scriptsDir);
            scriptPath = Path.Combine(scriptsDir, $"ps_{Guid.NewGuid():N}.ps1");
            File.WriteAllText(scriptPath, wrappedScript + Environment.NewLine, new UTF8Encoding(false));
            return true;
        }
        catch (Exception ex)
        {
            Log($"PS script-file fallback error: {ex.Message}");
            scriptPath = string.Empty;
            return false;
        }
    }

    private static void ForceDelete(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;
            var cmd = $"takeown /f \"{path}\" /a & icacls \"{path}\" /grant Administrators:F /c & del /f /q \"{path}\"";
            RunProcess("cmd.exe", "/c " + cmd);
        }
        catch (Exception ex)
        {
            Log($"Force delete failed: {path} ({ex.Message})");
        }
    }


    private static void SetQword(RegistryHive hive, string subKey, string name, long value)
    {
        var path = $"{hive}\\{subKey}";
        try
        {
            LogReg("REG QWORD", path, name, value.ToString(CultureInfo.InvariantCulture));
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.CreateSubKey(subKey, true);
            key?.SetValue(name, value, RegistryValueKind.QWord);
        }
        catch (Exception ex)
        {
            Log($"REG ERROR {path} {name}: {ex.Message}");
        }
    }

    private static void EnsureFileWritable(string path)
    {
        var cmd = $"takeown /f \"{path}\" /a & icacls \"{path}\" /setowner \"*S-1-5-32-544\" /c & icacls \"{path}\" /grant \"*S-1-5-32-544:F\" /c";
        RunProcess("cmd.exe", "/c " + cmd);
    }


    private static bool IsInternetAvailable()
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new System.Threading.CancellationTokenSource(3000);
            client.ConnectAsync("1.1.1.1", 443, cts.Token).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Encoding GetPreferredProcessEncoding(string fileName)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var lower = fileName.ToLowerInvariant();
            if (lower.Contains("powershell.exe") || lower.Contains("pwsh.exe"))
                return new UTF8Encoding(false);
        }

        try
        {
            return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        }
        catch
        {
            return new UTF8Encoding(false);
        }
    }

}
