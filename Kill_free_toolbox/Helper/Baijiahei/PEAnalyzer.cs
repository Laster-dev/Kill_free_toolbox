using Kill_free_toolbox.Helper.PE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Helper.Baijiahei
{
    /// <summary>
    /// PE文件分析器
    /// </summary>
    public class PEAnalyzer
    {
        public static string GetArchitectureString(string filePath)
        {
            try
            {
                var pe = new PEParser(filePath);
                return pe.Is64Bit ? "x64" : "x86";
            }
            catch
            {
                return "未知";
            }
        }

        public static bool? IsAuthenticodeSigned(string filePath)
        {
            try
            {
                // 使用 WinVerifyTrust 检查签名有效性
                return VerifyAuthenticode(filePath);
            }
            catch
            {
                return null;
            }
        }

        private static bool VerifyAuthenticode(string filePath)
        {
            // 使用更宽松的验证策略，避免因网络问题导致的误判
            try
            {
                var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(filePath);
                if (!cert.HasPrivateKey && string.IsNullOrEmpty(cert.Subject))
                {
                    return false;
                }

                // 首先检查证书是否在有效期内
                if (cert.NotAfter < DateTime.Now)
                {
                    return false; // 证书已过期
                }

                using (var chain = new System.Security.Cryptography.X509Certificates.X509Chain())
                {
                    // 使用离线模式，避免网络问题导致的误判
                    chain.ChainPolicy.RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.Offline;
                    chain.ChainPolicy.RevocationFlag = System.Security.Cryptography.X509Certificates.X509RevocationFlag.ExcludeRoot;
                    // 忽略一些常见的验证问题，如根证书不受信任等
                    chain.ChainPolicy.VerificationFlags = System.Security.Cryptography.X509Certificates.X509VerificationFlags.IgnoreNotTimeValid |
                                                         System.Security.Cryptography.X509Certificates.X509VerificationFlags.IgnoreCtlNotTimeValid |
                                                         System.Security.Cryptography.X509Certificates.X509VerificationFlags.IgnoreCertificateAuthorityRevocationUnknown |
                                                         System.Security.Cryptography.X509Certificates.X509VerificationFlags.IgnoreEndRevocationUnknown;
                    chain.ChainPolicy.VerificationTime = DateTime.Now; // 使用本地时间
                    bool isValid = chain.Build(cert);
                    return isValid;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取签名者详细信息（若存在）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>签名者信息，若无签名则返回 null</returns>
        public static SignerDetails GetAuthenticodeSignerDetails(string filePath)
        {
            try
            {
                var cert = System.Security.Cryptography.X509Certificates.X509Certificate.CreateFromSignedFile(filePath);
                if (cert == null) return null;

                var cert2 = new System.Security.Cryptography.X509Certificates.X509Certificate2(cert);
                return new SignerDetails
                {
                    Subject = cert2.Subject,
                    Issuer = cert2.Issuer,
                    NotBefore = cert2.NotBefore,
                    NotAfter = cert2.NotAfter
                };
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 异步分析目标文件的IAT
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>导入表字典</returns>
        public static async Task<Dictionary<string, List<string>>> AnalyzeTargetFileIATAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 创建PE解析器实例
                    var peParser = new PEParser(filePath);
                    
                    // 获取导入表信息
                    return peParser.GetImportTable();
                }
                catch (Exception ex)
                {
                    throw new Exception($"分析PE文件失败：{ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// 检查DLL是否在系统PATH中可用
        /// </summary>
        /// <param name="dllName">DLL名称</param>
        /// <returns>是否可用</returns>
        public static bool IsDllAvailableInPath(string dllName)
        {
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv))
                return false;

            string[] paths = pathEnv.Split(';');
            foreach (var dir in paths)
            {
                if (string.IsNullOrWhiteSpace(dir)) 
                    continue;
                
                try
                {
                    string dllPath = Path.Combine(dir.Trim(), dllName);
                    if (File.Exists(dllPath))
                    {
                        return true;
                    }
                }
                catch 
                {
                    // 忽略路径访问错误
                }
            }
            
            return false;
        }

        /// <summary>
        /// 获取分析结果摘要信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="imports">导入表</param>
        /// <returns>摘要信息</returns>
        public static AnalysisSummary GetAnalysisSummary(string filePath, Dictionary<string, List<string>> imports)
        {
            return new AnalysisSummary
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                ImportDllCount = imports.Count,
                TotalFunctionCount = imports.Values.Sum(functions => functions.Count),
                Imports = imports,
                Architecture = GetArchitectureString(filePath),
                SignatureValid = IsAuthenticodeSigned(filePath),
                Signer = GetAuthenticodeSignerDetails(filePath)
            };
        }
    }

    /// <summary>
    /// 分析结果摘要
    /// </summary>
    public class AnalysisSummary
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int ImportDllCount { get; set; }
        public int TotalFunctionCount { get; set; }
        public Dictionary<string, List<string>> Imports { get; set; }
        public string Architecture { get; set; }
        public bool? SignatureValid { get; set; }
        public SignerDetails Signer { get; set; }
    }

    /// <summary>
    /// 签名者详细信息
    /// </summary>
    public class SignerDetails
    {
        public string Subject { get; set; }
        public string Issuer { get; set; }
        public DateTime NotBefore { get; set; }
        public DateTime NotAfter { get; set; }
    }
}
