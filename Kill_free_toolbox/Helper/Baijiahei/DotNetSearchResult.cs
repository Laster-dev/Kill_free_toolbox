using System;
using System.Collections.Generic;

namespace Kill_free_toolbox.Helper.Baijiahei
{
    /// <summary>
    /// .Net白文件搜索结果
    /// </summary>
    public class DotNetSearchResult
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Architecture { get; set; }
        public string SignatureStatus { get; set; }
        public bool IsSignatureValid { get; set; }
        public bool IsSignatureExpired { get; set; }
        public bool IsGuiApplication { get; set; }
        public bool IsConsoleApplication { get; set; }
        public string DotNetVersion { get; set; }
        public long FileSize { get; set; }
        public DateTime? SignatureDate { get; set; }
        public string SignerName { get; set; }
        public List<string> NonSystemDlls { get; set; } = new List<string>();
        public int NonSystemDllCount { get; set; }
        
        /// <summary>
        /// 获取非系统DLL的字符串表示（用于界面显示）
        /// </summary>
        public string NonSystemDllsDisplay => string.Join(", ", NonSystemDlls);
        
        /// <summary>
        /// 获取程序类型显示文本
        /// </summary>
        public string ApplicationTypeDisplay
        {
            get
            {
                if (IsGuiApplication) return "GUI";
                if (IsConsoleApplication) return "CUI";
                return "未知";
            }
        }
        
        /// <summary>
        /// 获取签名状态显示文本
        /// </summary>
        public string SignatureStatusDisplay
        {
            get
            {
                if (IsSignatureValid) return "有效";
                if (IsSignatureExpired) return "过期";
                return "无效";
            }
        }
    }
}
