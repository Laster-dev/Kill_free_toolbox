using Kill_free_toolbox.Helper.PE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Helper.Baijiahei
{
    /// <summary>
    /// 白文件搜索结果
    /// </summary>
    public class WhiteFileSearchResult
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Architecture { get; set; }
        public string SignatureStatus { get; set; }
        public int NonSystemDllCount { get; set; }
        public List<string> NonSystemDlls { get; set; } = new List<string>();
        public long FileSize { get; set; }
        public bool IsSingleDllHijackable { get; set; }
        public bool IsGuiApplication { get; set; }
        public bool IsConsoleApplication { get; set; }
        
        /// <summary>
        /// 获取非系统DLL的字符串表示（用于界面显示）
        /// </summary>
        public string NonSystemDllsDisplay => string.Join(", ", NonSystemDlls);
        
        /// <summary>
        /// 获取程序类型显示文本
        /// </summary>
        public string ApplicationTypeDisplay => IsGuiApplication ? "GUI" : (IsConsoleApplication ? "CUI" : "未知");
    }

    /// <summary>
    /// 白文件搜索器
    /// </summary>
    public class WhiteFileSearcher
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
            "presentationframework.dll", "windowsbase.dll", "system.xaml.dll","rpcrt4.dll"
        };

        /// <summary>
        /// 搜索白文件
        /// </summary>
        /// <param name="searchPath">搜索路径</param>
        /// <param name="singleDllOnly">是否只搜索单DLL劫持</param>
        /// <param name="maxFileSize">最大文件大小（字节）</param>
        /// <param name="progressCallback">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>搜索结果</returns>
        public static async Task<List<WhiteFileSearchResult>> SearchWhiteFilesAsync(
            string searchPath,
            bool singleDllOnly,
            long maxFileSize,
            //IProgress<string> progressCallback = null,
            IProgress<string>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<WhiteFileSearchResult>();
            
            try
            {
                progressCallback?.Report("开始搜索带签名的可执行文件...");
                
                // 获取所有exe文件
                var exeFiles = await GetExecutableFilesAsync(searchPath, maxFileSize, progressCallback, cancellationToken);
                
                progressCallback?.Report($"找到 {exeFiles.Count} 个可执行文件，开始分析...");
                
                int processed = 0;
                foreach (var exeFile in exeFiles)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    try
                    {
                        progressCallback?.Report($"分析文件: {Path.GetFileName(exeFile)} ({processed + 1}/{exeFiles.Count})");
                        
                        var result = await AnalyzeExecutableFileAsync(exeFile, singleDllOnly);
                        if (result != null)
                        {
                            results.Add(result);
                        }
                        
                        processed++;
                    }
                    catch (Exception ex)
                    {
                        // 忽略单个文件的分析错误，继续处理其他文件
                        progressCallback?.Report($"分析文件失败: {Path.GetFileName(exeFile)} - {ex.Message}");
                    }
                }
                
                progressCallback?.Report($"搜索完成，找到 {results.Count} 个白文件");
            }
            catch (Exception ex)
            {
                progressCallback?.Report($"搜索过程中发生错误: {ex.Message}");
            }
            
            return results;
        }

        /// <summary>
        /// 搜索白文件（实时返回结果版本）
        /// </summary>
        /// <param name="searchPath">搜索路径</param>
        /// <param name="singleDllOnly">是否只搜索单DLL劫持</param>
        /// <param name="maxFileSize">最大文件大小（字节）</param>
        /// <param name="resultCallback">结果回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务</returns>
        public static async Task SearchWhiteFilesAsync(
            string searchPath,
            bool singleDllOnly,
            long maxFileSize,
            IProgress<WhiteFileSearchResult> resultCallback,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await SearchWhiteFilesRealtimeAsync(searchPath, singleDllOnly, maxFileSize, resultCallback, cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"搜索过程中发生错误: {ex.Message}");
            }
        }

        public static async Task SearchWhiteFilesAsync(
            string searchPath,
            int dllCount,
            long maxFileSize,
            IProgress<WhiteFileSearchResult> resultCallback,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await SearchWhiteFilesRealtimeAsync(searchPath, dllCount, maxFileSize, resultCallback, cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"搜索过程中发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 实时搜索白文件（找到一个验证一个）
        /// </summary>
        private static async Task SearchWhiteFilesRealtimeAsync(
            string searchPath,
            bool singleDllOnly,
            long maxFileSize,
            IProgress<WhiteFileSearchResult> resultCallback,
            CancellationToken cancellationToken)
        {
            await SearchWhiteFilesRealtimeAsync(searchPath, singleDllOnly ? 1 : int.MaxValue, maxFileSize, resultCallback, cancellationToken);
        }

        /// <summary>
        /// 实时搜索白文件（找到一个验证一个）
        /// </summary>
        private static async Task SearchWhiteFilesRealtimeAsync(
            string searchPath,
            int dllCount,
            long maxFileSize,
            IProgress<WhiteFileSearchResult> resultCallback,
            CancellationToken cancellationToken)
        {
            var directories = new Queue<string>();
            directories.Enqueue(searchPath);
            
            int processed = 0;
            int foundCount = 0;
            
            await Task.Run(() =>
            {
                while (directories.Count > 0 && !cancellationToken.IsCancellationRequested)
                {
                    string currentDir = directories.Dequeue();
                    
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
                                    // 立即验证这个文件
                                    var result = AnalyzeExecutableFileAsync(file, dllCount).Result;
                                    if (result != null)
                                    {
                                        foundCount++;
                                        // 实时返回结果
                                        resultCallback?.Report(result);
                                    }
                                    
                                    processed++;
                                    
                                    // 每处理50个文件输出一次进度
                                    if (processed % 50 == 0)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"已处理 {processed} 个文件，找到 {foundCount} 个白文件");
                                    }
                                }
                            }
                            catch
                            {
                                // 忽略无法访问的文件
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
                                directories.Enqueue(subDir);
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
            });
            
            System.Diagnostics.Debug.WriteLine($"搜索完成，共处理 {processed} 个文件，找到 {foundCount} 个白文件");
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
                    var directories = new Queue<string>();
                    directories.Enqueue(searchPath);
                    
                    while (directories.Count > 0 && !cancellationToken.IsCancellationRequested)
                    {
                        string currentDir = directories.Dequeue();
                        
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
                                catch
                                {
                                    // 忽略无法访问的文件
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
                                        directories.Enqueue(subDir);
                                    }
                                }
                                catch
                                {
                                    // 忽略无法访问的目录
                                }
                            }
                        }
                        catch
                        {
                            // 忽略无法访问的目录
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
        /// 分析可执行文件
        /// </summary>
        private static async Task<WhiteFileSearchResult> AnalyzeExecutableFileAsync(string filePath, bool singleDllOnly)
        {
            return await AnalyzeExecutableFileAsync(filePath, singleDllOnly ? 1 : int.MaxValue);
        }

        private static async Task<WhiteFileSearchResult> AnalyzeExecutableFileAsync(string filePath, int dllCount)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 检查文件是否有数字签名（包括过期签名）
                    var signatureValid = PEAnalyzer.IsAuthenticodeSigned(filePath);
                    var signer = PEAnalyzer.GetAuthenticodeSignerDetails(filePath);
                    
                    // 如果没有签名信息，跳过
                    if (signer == null)
                    {
                        return null; // 没有签名，跳过
                    }
                    
                    // 检查签名是否过期
                    bool isExpired = signer.NotAfter < DateTime.Now;
                    
                    // 如果签名既无效又未过期，可能是证书链问题，仍然接受
                    // 如果签名过期，也接受（过期签名仍然算白文件）
                    if (signatureValid != true && !isExpired)
                    {
                        // 证书在有效期内但验证失败，可能是因为证书链问题，仍然接受
                    }
                    
                    // 分析PE文件
                    var peParser = new PEParser(filePath);
                    var imports = peParser.GetImportTable();
                    
                    // 过滤出可劫持DLL（使用统一判断逻辑）
                    var allDlls = imports.Keys.ToList();
                    var hijackableDlls = allDlls.Where(dll => IsHijackableDll(dll)).ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"文件 {filePath}: 总DLL数={allDlls.Count}, 可劫持DLL数={hijackableDlls.Count}, 限制={dllCount}");
                    System.Diagnostics.Debug.WriteLine($"  所有DLL: {string.Join(", ", allDlls)}");
                    System.Diagnostics.Debug.WriteLine($"  可劫持DLL: {string.Join(", ", hijackableDlls)}");
                    
                    // 根据DLL数量要求进行过滤（小于等于指定数量）
                    if (dllCount != int.MaxValue && hijackableDlls.Count > dllCount)
                    {
                        System.Diagnostics.Debug.WriteLine($"文件 {filePath} 被过滤: DLL数量 {hijackableDlls.Count} > 限制 {dllCount}");
                        return null;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"文件 {filePath} 通过DLL数量检查: {hijackableDlls.Count} <= {dllCount}");
                    
                    // 如果没有可劫持DLL，跳过
                    if (hijackableDlls.Count == 0)
                    {
                        return null;
                    }
                    
                    var fileInfo = new FileInfo(filePath);
                    var architecture = PEAnalyzer.GetArchitectureString(filePath);
                    
                    // 使用上面已经定义的签名信息
                    bool isValid = signatureValid == true;
                    
                    string signatureStatus;
                    if (isValid)
                        signatureStatus = "有效";
                    else if (isExpired)
                        signatureStatus = "有效（过期）";
                    else
                        signatureStatus = "无效";
                    
                    // 判断程序类型
                    bool isGui = IsGuiApplication(filePath);
                    bool isConsole = IsConsoleApplication(filePath);
                    
                    return new WhiteFileSearchResult
                    {
                        FileName = fileInfo.Name,
                        FilePath = filePath,
                        Architecture = architecture,
                        SignatureStatus = signatureStatus,
                        NonSystemDllCount = hijackableDlls.Count,
                        NonSystemDlls = hijackableDlls,
                        FileSize = fileInfo.Length,
                        IsSingleDllHijackable = hijackableDlls.Count == 1,
                        IsGuiApplication = isGui,
                        IsConsoleApplication = isConsole
                    };
                }
                catch
                {
                    return null;
                }
            });
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
                "xaudio", "mf", "wmcodec","shlwapi","shcore"
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
                "windowsbase", "system.xaml","api-ms-win","msvcr"
            };
            
            foreach (var pattern in runtimePatterns)
            {
                if (dllName.StartsWith(pattern))//这里使用StartsWith，因为有些DLL名称可能包含运行时DLL名称
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
                "windows", "program files", "program files (x86)", "programdata",
                "system32", "syswow64", "winsxs", "assembly", "microsoft.net",
                "common files", "internet explorer", "windows defender",
                "windows mail", "windows media player", "windows photo viewer",
                "windows sidebar", "windows sidebar gadgets", "x86_microsoft",
                "amd64_microsoft", "wow64_microsoft", "msil_microsoft"
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

        /// <summary>
        /// 迁移文件到指定目录
        /// </summary>
        public static async Task<bool> MoveFileToDirectoryAsync(string sourceFilePath, string targetDirectory)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
                    var targetPath = Path.Combine(targetDirectory, "白文件", fileName, "exe");
                    
                    // 创建目标目录
                    Directory.CreateDirectory(targetPath);
                    
                    // 复制文件
                    var targetFilePath = Path.Combine(targetPath, Path.GetFileName(sourceFilePath));
                    File.Copy(sourceFilePath, targetFilePath, true);
                    
                    return true;
                }
                catch
                {
                    return false;
                }
            });
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
    }
}
