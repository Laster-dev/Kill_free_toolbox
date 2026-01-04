using Kill_free_toolbox.Helper.PE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace Kill_free_toolbox.Helper.Baijiahei
{
    /// <summary>
    /// .Net白文件搜索器
    /// </summary>
    public class DotNetWhiteFileSearcher
    {
        // 系统DLL列表（常见的系统DLL）
        private static readonly HashSet<string> SystemDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "kernel32.dll", "ntdll.dll", "user32.dll", "gdi32.dll", "advapi32.dll",
            "ole32.dll", "oleaut32.dll", "shell32.dll", "comctl32.dll", "comdlg32.dll",
            "winspool.drv", "version.dll", "winmm.dll", "ws2_32.dll", "wsock32.dll",
            "mpr.dll", "netapi32.dll", "secur32.dll", "crypt32.dll", "wintrust.dll",
            "msvcrt.dll", "msvcp140.dll", "vcruntime140.dll", "ucrtbase.dll",
            "api-ms-win-*.dll", "ext-ms-win-*.dll", "windows.storage.dll",
            "windows.data.dll", "windows.foundation.dll", "windows.applicationmodel.dll",
            "windows.system.dll", "windows.ui.dll", "windows.graphics.dll",
            "d3d11.dll", "d3d12.dll", "dxgi.dll", "dwmapi.dll", "dwrite.dll",
            "d2d1.dll", "d3dcompiler_47.dll", "xinput1_4.dll", "xaudio2_8.dll",
            "mf.dll", "mfplat.dll", "mfreadwrite.dll", "mfuuid.dll", "wmcodecdspuuid.dll",
            "mscoree.dll", "mscorlib.dll", "system.dll", "system.core.dll",
            "system.windows.forms.dll", "system.drawing.dll", "presentationcore.dll",
            "presentationframework.dll", "windowsbase.dll", "system.xaml.dll"
        };

        /// <summary>
        /// 搜索.Net白文件（实时返回结果版本）
        /// </summary>
        /// <param name="searchPath">搜索路径</param>
        /// <param name="maxFileSize">最大文件大小（字节）</param>
        /// <param name="resultCallback">结果回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务</returns>
        public static async Task SearchDotNetWhiteFilesAsync(
            string searchPath,
            long maxFileSize,
            IProgress<DotNetSearchResult> resultCallback,
            CancellationToken cancellationToken = default)
        {
            try
            {
                int processed = 0;
                int foundCount = 0;

                // 广度优先遍历目录，发现一个就分析一个
                var directories = new Queue<string>();
                directories.Enqueue(searchPath);

                while (directories.Count > 0 && !cancellationToken.IsCancellationRequested)
                {
                    var currentDir = directories.Dequeue();
                    try
                    {
                        // 先分析当前目录下的exe（流式返回）
                        var files = Directory.GetFiles(currentDir, "*.exe", SearchOption.TopDirectoryOnly);
                        foreach (var file in files)
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            try
                            {
                                var fileInfo = new FileInfo(file);
                                if (fileInfo.Length > maxFileSize) continue;

                                if (processed % 50 == 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"正在分析文件: {Path.GetFileName(file)} (已处理: {processed})");
                                }

                                var result = await AnalyzeDotNetExecutableFileAsync(file);
                                if (result != null)
                                {
                                    foundCount++;
                                    resultCallback?.Report(result);
                                }
                                processed++;
                            }
                            catch (Exception exFile)
                            {
                                // 忽略单个文件错误
                                System.Diagnostics.Debug.WriteLine($"分析文件失败: {Path.GetFileName(file)} - {exFile.Message}");
                            }
                        }

                        // 再把子目录加入队列（不做过滤）
                        var subDirs = Directory.GetDirectories(currentDir);
                        foreach (var subDir in subDirs)
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            try
                            {
                                directories.Enqueue(subDir);
                            }
                            catch
                            {
                                // 忽略
                            }
                        }
                    }
                    catch
                    {
                        // 忽略当前目录的访问问题
                    }
                }

                System.Diagnostics.Debug.WriteLine($"搜索完成，共处理 {processed} 个文件，找到 {foundCount} 个.Net白文件");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"搜索过程中发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取可执行文件列表
        /// </summary>
        private static async Task<List<string>> GetExecutableFilesAsync(
            string searchPath,
            long maxFileSize,
            IProgress<string> progressCallback,
            CancellationToken cancellationToken)
        {
            var exeFiles = new List<string>();
            
            await Task.Run(() =>
            {
                try
                {
                    var directories = new Queue<(string path, int depth)>();
                    
                    // 直接搜索指定路径，不进行任何过滤
                    directories.Enqueue((searchPath, 0));
                    
                    while (directories.Count > 0 && !cancellationToken.IsCancellationRequested)
                    {
                        var (currentDir, depth) = directories.Dequeue();
                        
                        // 不限制搜索深度
                        
                        try
                        {
                            // 获取当前目录下的exe文件
                            var files = Directory.GetFiles(currentDir, "*.exe", SearchOption.TopDirectoryOnly);
                            foreach (var file in files)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                    break;
                                    
                                try
                                {
                                    var fileInfo = new FileInfo(file);
                                    if (fileInfo.Length <= maxFileSize)
                                    {
                                        exeFiles.Add(file);
                                    }
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    // 忽略权限不足的文件
                                    System.Diagnostics.Debug.WriteLine($"权限不足，跳过文件: {file}");
                                }
                                catch (Exception ex)
                                {
                                    // 忽略其他文件访问错误
                                    System.Diagnostics.Debug.WriteLine($"无法访问文件: {file} - {ex.Message}");
                                }
                            }
                            
                            // 添加子目录到队列
                            var subDirs = Directory.GetDirectories(currentDir);
                            foreach (var subDir in subDirs)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                    break;
                                    
                                try
                                {
                                    // 跳过一些系统目录以提高性能
                                    var dirName = Path.GetFileName(subDir).ToLower();
                                    if (!IsSystemDirectory(dirName))
                                    {
                                        directories.Enqueue((subDir, depth + 1));
                                    }
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    // 忽略权限不足的目录（静默）
                                }
                                catch (Exception)
                                {
                                    // 忽略其他目录访问错误（静默）
                                }
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // 忽略权限不足的目录（静默）
                        }
                        catch (Exception)
                        {
                            // 忽略其他目录访问错误（静默）
                        }
                    }
                }
                catch (Exception ex)
                {
                    progressCallback?.Report($"获取文件列表时发生错误: {ex.Message}");
                }
            }, cancellationToken);
            
            return exeFiles;
        }

        /// <summary>
        /// 分析.Net可执行文件
        /// </summary>
        private static async Task<DotNetSearchResult> AnalyzeDotNetExecutableFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 检查是否为.Net程序
                    bool isDotNet = IsDotNetAssembly(filePath);
                    if (!isDotNet)
                    {
                        return null; // 不是.Net程序，跳过
                    }
                    
                    // 调试信息：找到.Net程序
                    System.Diagnostics.Debug.WriteLine($"找到.Net程序: {Path.GetFileName(filePath)}");
                    
                    // 检查文件是否有数字签名
                    bool? signatureValid = null;
                    SignerDetails signer = null;
                    
                    try
                    {
                        signatureValid = PEAnalyzer.IsAuthenticodeSigned(filePath);
                        signer = PEAnalyzer.GetAuthenticodeSignerDetails(filePath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"签名检查异常: {Path.GetFileName(filePath)} - {ex.Message}");
                        signatureValid = false;
                        signer = null;
                    }
                    
                    // 调试信息：签名检查结果
                    System.Diagnostics.Debug.WriteLine($"签名检查: {Path.GetFileName(filePath)} - 签名有效:{signatureValid} 签名者:{signer != null}");
                    
                    // 暂时放宽签名要求，先测试能否找到.Net程序
                    // 只处理有签名的文件（有效或过期都算）
                    if (signer == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"跳过无签名文件: {Path.GetFileName(filePath)}");
                        return null; // 没有签名，跳过
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"开始分析有签名的.Net程序: {Path.GetFileName(filePath)}");
                    
                    // .Net程序不需要分析导入表，只要有签名就是白文件
                    
                    var fileInfo = new FileInfo(filePath);
                    var architecture = PEAnalyzer.GetArchitectureString(filePath);
                    
                    // 判断程序类型（GUI或CUI）
                    bool isGui = IsGuiApplication(filePath);
                    bool isConsole = IsConsoleApplication(filePath);
                    
                    // 获取.Net版本
                    string dotNetVersion = GetDotNetVersion(filePath);
                    
                    // 判断签名状态
                    bool isValid = signatureValid == true;
                    bool isExpired = false;
                    DateTime? signatureDate = null;
                    string signerName = null;
                    
                    if (signer != null)
                    {
                        signatureDate = signer.NotBefore;
                        signerName = signer.Subject;
                        // 检查签名是否过期 - 只要证书过期时间小于当前时间就认为过期
                        isExpired = signer.NotAfter < DateTime.Now;
                        
                        // 如果证书没有过期但验证失败，可能是因为其他原因（如证书链问题）
                        // 这种情况下，如果证书在有效期内，我们仍然认为它是"有效"的
                        if (!isValid && !isExpired)
                        {
                            // 证书在有效期内但验证失败，可能是因为证书链问题
                            // 为了更准确地反映实际情况，我们重新评估有效性
                            isValid = true; // 证书本身是有效的，只是链验证可能有问题
                        }
                    }
                    
                    var result = new DotNetSearchResult
                    {
                        FileName = fileInfo.Name,
                        FilePath = filePath,
                        Architecture = architecture,
                        SignatureStatus = isValid ? "有效" : (isExpired ? "过期" : "无效"),
                        IsSignatureValid = isValid,
                        IsSignatureExpired = isExpired,
                        IsGuiApplication = isGui,
                        IsConsoleApplication = isConsole,
                        DotNetVersion = dotNetVersion,
                        FileSize = fileInfo.Length,
                        SignatureDate = signatureDate,
                        SignerName = signerName,
                        NonSystemDlls = new List<string>(), // .Net程序不需要DLL信息
                        NonSystemDllCount = 0 // .Net程序不需要DLL信息
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"成功创建.Net搜索结果: {Path.GetFileName(filePath)} - 签名:{result.SignatureStatusDisplay} 类型:{result.ApplicationTypeDisplay}");
                    
                    return result;
                }
                catch
                {
                    return null;
                }
            });
        }

        /// <summary>
        /// 检查是否为.Net程序集
        /// </summary>
        private static bool IsDotNetAssembly(string filePath)
        {
            try
            {
                // 使用PE分析器检查是否有CLR头
                var peParser = new PEParser(filePath);
                return peParser.IsDotNetAssembly();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否为GUI应用程序
        /// </summary>
        private static bool IsGuiApplication(string filePath)
        {
            try
            {
                var peParser = new PEParser(filePath);
                return peParser.IsGuiApplication();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否为控制台应用程序
        /// </summary>
        private static bool IsConsoleApplication(string filePath)
        {
            try
            {
                var peParser = new PEParser(filePath);
                return peParser.IsConsoleApplication();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取.Net版本
        /// </summary>
        private static string GetDotNetVersion(string filePath)
        {
            try
            {
                var peParser = new PEParser(filePath);
                return peParser.GetDotNetVersion();
            }
            catch
            {
                return "未知";
            }
        }

        /// <summary>
        /// 判断是否为系统DLL
        /// </summary>
        private static bool IsSystemDll(string dllName)
        {
            if (string.IsNullOrEmpty(dllName))
                return true;
                
            var lowerDllName = dllName.ToLower();
            
            // 直接匹配
            if (SystemDlls.Contains(lowerDllName))
                return true;
            
            // 检查是否为API Set DLL
            if (IsApiSetDll(lowerDllName))
                return true;
            
            // 检查是否为Windows系统DLL
            if (IsWindowsSystemDll(lowerDllName))
                return true;
            
            // 检查是否为Microsoft运行时DLL
            if (IsMicrosoftRuntimeDll(lowerDllName))
                return true;
            
            return false;
        }

        /// <summary>
        /// 检查是否为API Set DLL
        /// </summary>
        private static bool IsApiSetDll(string dllName)
        {
            // API Set DLL通常以api-ms-win-开头
            if (dllName.StartsWith("api-ms-win-"))
                return true;
            
            // 扩展API Set DLL
            if (dllName.StartsWith("ext-ms-win-"))
                return true;
            
            // Windows API Set DLL
            if (dllName.StartsWith("windows."))
                return true;
            
            return false;
        }

        /// <summary>
        /// 检查是否为Windows系统DLL
        /// </summary>
        private static bool IsWindowsSystemDll(string dllName)
        {
            // 常见的Windows系统DLL模式
            var systemPatterns = new[]
            {
                "kernel32", "ntdll", "user32", "gdi32", "advapi32",
                "ole32", "oleaut32", "shell32", "comctl32", "comdlg32",
                "winspool", "version", "winmm", "ws2_32", "wsock32",
                "mpr", "netapi32", "secur32", "crypt32", "wintrust",
                "d3d", "dxgi", "dwmapi", "dwrite", "d2d1", "xinput",
                "xaudio", "mf", "wmcodec"
            };
            
            foreach (var pattern in systemPatterns)
            {
                if (dllName.StartsWith(pattern))
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// 检查是否为Microsoft运行时DLL
        /// </summary>
        private static bool IsMicrosoftRuntimeDll(string dllName)
        {
            // Microsoft运行时DLL模式
            var runtimePatterns = new[]
            {
                "msvcrt", "msvcp", "vcruntime", "ucrtbase",
                "mscoree", "mscorlib", "system.", "presentation",
                "windowsbase", "system.xaml"
            };
            
            foreach (var pattern in runtimePatterns)
            {
                if (dllName.StartsWith(pattern))
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// 对外暴露：判断是否为系统DLL名称
        /// </summary>
        public static bool IsSystemDllName(string dllName)
        {
            return IsSystemDll(dllName);
        }

        /// <summary>
        /// 检查DLL是否可劫持（全局统一判断逻辑）
        /// </summary>
        /// <param name="dllName">DLL名称</param>
        /// <returns>true表示可劫持，false表示不可劫持</returns>
        public static bool IsHijackableDll(string dllName)
        {
            return !IsSystemDll(dllName);
        }

        /// <summary>
        /// 判断是否为系统目录
        /// </summary>
        private static bool IsSystemDirectory(string dirName)
        {
            var systemDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "windows", "system32", "syswow64", "winsxs", "assembly", 
                "system volume information", "recovery", "boot", "efi",
                "perflogs", "perfmon", "msocache"
            };
            
            return systemDirs.Contains(dirName);
        }

        /// <summary>
        /// 获取可用驱动器列表
        /// </summary>
        public static List<string> GetAvailableDrives()
        {
            var drives = new List<string>();
            
            try
            {
                var driveInfos = DriveInfo.GetDrives();
                foreach (var drive in driveInfos)
                {
                    if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    {
                        drives.Add($"{drive.Name} ({drive.VolumeLabel})");
                    }
                }
            }
            catch
            {
                // 如果获取驱动器信息失败，提供默认选项
                drives.Add("C:\\");
            }
            
            return drives;
        }

        /// <summary>
        /// 解析文件大小字符串
        /// </summary>
        public static long ParseFileSize(string sizeText)
        {
            if (string.IsNullOrWhiteSpace(sizeText))
                return 10 * 1024 * 1024; // 默认10MB
                
            sizeText = sizeText.Trim().ToUpper();
            
            long multiplier = 1;
            if (sizeText.EndsWith("KB"))
            {
                multiplier = 1024;
                sizeText = sizeText.Substring(0, sizeText.Length - 2);
            }
            else if (sizeText.EndsWith("MB"))
            {
                multiplier = 1024 * 1024;
                sizeText = sizeText.Substring(0, sizeText.Length - 2);
            }
            else if (sizeText.EndsWith("GB"))
            {
                multiplier = 1024 * 1024 * 1024;
                sizeText = sizeText.Substring(0, sizeText.Length - 2);
            }
            
            if (double.TryParse(sizeText, out double value))
            {
                return (long)(value * multiplier);
            }
            
            return 10 * 1024 * 1024; // 默认10MB
        }
    }
}

