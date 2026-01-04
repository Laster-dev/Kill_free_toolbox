using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Kill_free_toolbox.Helper.Baijiahei
{
    /// <summary>
    /// 代码渲染器
    /// </summary>
    public class CodeRenderer
    {
        private readonly RichTextBox _richTextBox;
        private readonly HashSet<string> _currentDllFunctionNames;

        public CodeRenderer(RichTextBox richTextBox)
        {
            _richTextBox = richTextBox ?? throw new ArgumentNullException(nameof(richTextBox));
            _currentDllFunctionNames = new HashSet<string>(StringComparer.Ordinal);
        }

        /// <summary>
        /// 设置当前DLL的函数名集合
        /// </summary>
        /// <param name="functionNames">函数名集合</param>
        public void SetCurrentDllFunctionNames(IEnumerable<string> functionNames)
        {
            _currentDllFunctionNames.Clear();
            if (functionNames != null)
            {
                foreach (var fn in functionNames)
                {
                    if (!string.IsNullOrWhiteSpace(fn))
                    {
                        _currentDllFunctionNames.Add(fn);
                    }
                }
            }
        }

        /// <summary>
        /// 异步渲染纯文本
        /// </summary>
        /// <param name="text">文本内容</param>
        public async Task RenderPlainTextAsync(string text)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                RenderPlainText(text);
            });
        }

        /// <summary>
        /// 渲染纯文本
        /// </summary>
        /// <param name="text">文本内容</param>
        public void RenderPlainText(string text)
        {
            if (_richTextBox == null) return;

            _richTextBox.Document.Blocks.Clear();
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run(text));
            _richTextBox.Document.Blocks.Add(paragraph);
        }

        /// <summary>
        /// 异步渲染C++代码
        /// </summary>
        /// <param name="code">代码内容</param>
        public async Task RenderCppCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                RenderCppCode(code, cancellationToken);
            });
        }

        /// <summary>
        /// 渲染C++代码（带语法高亮）
        /// </summary>
        /// <param name="code">代码内容</param>
        public void RenderCppCode(string code, CancellationToken cancellationToken = default)
        {
            if (_richTextBox == null) return;

            var keywordSet = GetKeywordSet();
            var typeSet = GetTypeSet();
            var brushes = GetColorBrushes();

            _richTextBox.Document.Blocks.Clear();

            bool inBlockComment = false;
            var lines = code.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            
            foreach (var line in lines)
            {
                if (cancellationToken.IsCancellationRequested) return;
                var paragraph = new Paragraph { Margin = new Thickness(0) };
                RenderLine(line, paragraph, keywordSet, typeSet, brushes, ref inBlockComment);
                _richTextBox.Document.Blocks.Add(paragraph);
            }
        }

        /// <summary>
        /// 异步渲染分析结果
        /// </summary>
        /// <param name="summary">分析摘要</param>
        public async Task RenderAnalysisResultsAsync(AnalysisSummary summary)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                RenderAnalysisResults(summary);
            });
        }

        /// <summary>
        /// 渲染分析结果
        /// </summary>
        /// <param name="summary">分析摘要</param>
        public void RenderAnalysisResults(AnalysisSummary summary)
        {
            if (_richTextBox == null || summary == null) return;

            var brushes = GetColorBrushes();
            _richTextBox.Document.Blocks.Clear();

            // 标题
            var title = new Paragraph { Margin = new Thickness(0, 0, 0, 6) };
            title.Inlines.Add(new Run("=== IAT 分析结果 ===") 
            { 
                Foreground = brushes.HeadingBrush, 
                FontSize = 16, 
                FontWeight = FontWeights.Bold 
            });
            _richTextBox.Document.Blocks.Add(title);

            // 元信息
            var meta1 = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
            meta1.Inlines.Add(new Run($"目标文件: {summary.FileName}") 
            { 
                Foreground = brushes.MetaBrush, 
                FontSize = 12 
            });
            _richTextBox.Document.Blocks.Add(meta1);

            var metaArch = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
            metaArch.Inlines.Add(new Run($"架构: {summary.Architecture ?? "-"}") 
            { 
                Foreground = brushes.MetaBrush, 
                FontSize = 12 
            });
            _richTextBox.Document.Blocks.Add(metaArch);

            string sigState;
            if (summary.SignatureValid == null)
                sigState = "未知";
            else
            {
                bool isValid = summary.SignatureValid.Value;
                bool isExpired = false;
                
                // 检查是否过期
                if (summary.Signer != null)
                {
                    isExpired = summary.Signer.NotAfter < DateTime.Now;
                }
                
                if (isValid)
                    sigState = "有效";
                else if (isExpired)
                    sigState = "有效（过期）";
                else
                    sigState = "无效";
            }
            var metaSig = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
            metaSig.Inlines.Add(new Run($"签名: {sigState}") 
            { 
                Foreground = brushes.MetaBrush, 
                FontSize = 12 
            });
            _richTextBox.Document.Blocks.Add(metaSig);

            if (summary.Signer != null)
            {
                var metaSigner = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
                metaSigner.Inlines.Add(new Run($"签名者: {summary.Signer.Subject}") { Foreground = brushes.BodyBrush, FontSize = 12 });
                _richTextBox.Document.Blocks.Add(metaSigner);

                var metaIssuer = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
                metaIssuer.Inlines.Add(new Run($"颁发者: {summary.Signer.Issuer}") { Foreground = brushes.BodyBrush, FontSize = 12 });
                _richTextBox.Document.Blocks.Add(metaIssuer);

                var metaValid = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
                metaValid.Inlines.Add(new Run($"有效期: {summary.Signer.NotBefore:G} - {summary.Signer.NotAfter:G}") { Foreground = brushes.BodyBrush, FontSize = 12 });
                _richTextBox.Document.Blocks.Add(metaValid);
            }

            var meta2 = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
            meta2.Inlines.Add(new Run($"导入DLL数量: {summary.ImportDllCount}") 
            { 
                Foreground = brushes.MetaBrush, 
                FontSize = 12 
            });
            _richTextBox.Document.Blocks.Add(meta2);

            // DLL列表
            foreach (var kvp in summary.Imports)
            {
                // 使用统一的DLL劫持判断逻辑
                bool isHijackable = WhiteFileSearcher.IsHijackableDll(kvp.Key);
                
                var p = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
                p.Inlines.Add(new Run("[") { Foreground = brushes.BodyBrush });
                p.Inlines.Add(new Run(kvp.Key) 
                { 
                    Foreground = isHijackable ? brushes.DllAvailableBrush : brushes.DllUnavailableBrush, 
                    FontWeight = FontWeights.SemiBold 
                });
                p.Inlines.Add(new Run("] ") { Foreground = brushes.BodyBrush });
                p.Inlines.Add(new Run("- ") { Foreground = brushes.BodyBrush });
                p.Inlines.Add(new Run($"{kvp.Value.Count} 个函数") { Foreground = brushes.BodyBrush });
                _richTextBox.Document.Blocks.Add(p);
            }

            // 提示信息
            var tip = new Paragraph { Margin = new Thickness(0, 6, 0, 2) };
            tip.Inlines.Add(new Run("提示：从上方选择要劫持的DLL，系统将生成对应的C++代码。") 
            { 
                Foreground = brushes.BodyBrush, 
                FontSize = 12 
            });
            _richTextBox.Document.Blocks.Add(tip);

            // 注意事项
            var notesTitle = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
            notesTitle.Inlines.Add(new Run("注意：") 
            { 
                Foreground = brushes.HeadingBrush, 
                FontSize = 13, 
                FontWeight = FontWeights.SemiBold 
            });
            _richTextBox.Document.Blocks.Add(notesTitle);

            AddBullet("绿色为可劫持DLL（非系统DLL）", brushes);
            AddBullet("橙色为不可劫持DLL（系统DLL）", brushes);
        }

        private void AddBullet(string text, ColorBrushes brushes)
        {
            var bp = new Paragraph { Margin = new Thickness(12, 0, 0, 0) };
            bp.Inlines.Add(new Run("• ") { Foreground = brushes.MetaBrush });
            bp.Inlines.Add(new Run(text) { Foreground = brushes.BodyBrush, FontSize = 12 });
            _richTextBox.Document.Blocks.Add(bp);
        }

        private void RenderLine(string line, Paragraph paragraph, HashSet<string> keywordSet, 
            HashSet<string> typeSet, ColorBrushes brushes, ref bool inBlockComment)
        {
            int i = 0;

            // 处理预处理指令
            if (!inBlockComment && line.TrimStart().StartsWith("#"))
            {
                RenderPreprocessorDirective(line, paragraph, brushes);
                return;
            }

            // 逐字符处理
            while (i < line.Length)
            {
                if (inBlockComment)
                {
                    i = RenderBlockComment(line, paragraph, brushes, i, ref inBlockComment);
                    continue;
                }

                // 行注释
                if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/')
                {
                    paragraph.Inlines.Add(new Run(line.Substring(i)) { Foreground = brushes.CommentBrush });
                    break;
                }

                // 块注释开始
                if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '*')
                {
                    i = RenderBlockComment(line, paragraph, brushes, i, ref inBlockComment);
                    continue;
                }

                // 字符串字面量
                if (line[i] == '"')
                {
                    i = RenderStringLiteral(line, paragraph, brushes, i);
                    continue;
                }

                // 字符字面量
                if (line[i] == '\'')
                {
                    i = RenderCharLiteral(line, paragraph, brushes, i);
                    continue;
                }

                // 标识符/数字/其他
                if (char.IsLetter(line[i]) || line[i] == '_')
                {
                    i = RenderIdentifier(line, paragraph, keywordSet, typeSet, brushes, i);
                    continue;
                }

                // 数字
                if (char.IsDigit(line[i]))
                {
                    i = RenderNumber(line, paragraph, brushes, i);
                    continue;
                }

                // 单个字符标点/空格
                paragraph.Inlines.Add(new Run(line[i].ToString()));
                i++;
            }
        }

        private void RenderPreprocessorDirective(string line, Paragraph paragraph, ColorBrushes brushes)
        {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("#include"))
            {
                RenderIncludeDirective(line, paragraph, brushes);
            }
            else if (trimmed.StartsWith("#define"))
            {
                RenderDefineDirective(line, paragraph, brushes);
            }
            else
            {
                paragraph.Inlines.Add(new Run(line) { Foreground = brushes.PreprocessorBrush });
            }
        }

        private void RenderIncludeDirective(string line, Paragraph paragraph, ColorBrushes brushes)
        {
            int hash = line.IndexOf('#');
            if (hash < 0) hash = 0;
            
            int kw = line.IndexOf("include", hash + 1, StringComparison.Ordinal);
            if (kw > 0)
            {
                paragraph.Inlines.Add(new Run(line.Substring(0, hash + 1)) { Foreground = brushes.PreprocessorBrush });
                paragraph.Inlines.Add(new Run("include") { Foreground = brushes.PreprocessorBrush });
                
                int k = kw + "include".Length;
                while (k < line.Length && char.IsWhiteSpace(line[k]))
                {
                    paragraph.Inlines.Add(new Run(line[k].ToString()) { Foreground = brushes.DefaultBrush });
                    k++;
                }
                
                if (k < line.Length && (line[k] == '<' || line[k] == '"'))
                {
                    char startCh = line[k];
                    char endCh = startCh == '<' ? '>' : '"';
                    paragraph.Inlines.Add(new Run(startCh.ToString()) { Foreground = brushes.DefaultBrush });
                    
                    int j2 = k + 1;
                    while (j2 < line.Length && line[j2] != endCh) j2++;
                    
                    if (j2 > k + 1)
                        paragraph.Inlines.Add(new Run(line.Substring(k + 1, j2 - k - 1)) { Foreground = brushes.HeaderBrush });
                    
                    if (j2 < line.Length)
                        paragraph.Inlines.Add(new Run(line[j2].ToString()) { Foreground = brushes.DefaultBrush });
                    
                    if (j2 + 1 < line.Length)
                        paragraph.Inlines.Add(new Run(line.Substring(j2 + 1)) { Foreground = brushes.DefaultBrush });
                }
                else
                {
                    if (k < line.Length)
                        paragraph.Inlines.Add(new Run(line.Substring(k)) { Foreground = brushes.HeaderBrush });
                }
            }
            else
            {
                paragraph.Inlines.Add(new Run(line) { Foreground = brushes.PreprocessorBrush });
            }
        }

        private void RenderDefineDirective(string line, Paragraph paragraph, ColorBrushes brushes)
        {
            int hash = line.IndexOf('#');
            if (hash < 0) hash = 0;
            
            int kw = line.IndexOf("define", hash + 1, StringComparison.Ordinal);
            if (kw > 0)
            {
                paragraph.Inlines.Add(new Run(line.Substring(0, hash + 1)) { Foreground = brushes.PreprocessorBrush });
                paragraph.Inlines.Add(new Run("define") { Foreground = brushes.PreprocessorBrush });
                
                int k = kw + "define".Length;
                while (k < line.Length && char.IsWhiteSpace(line[k]))
                {
                    paragraph.Inlines.Add(new Run(line[k].ToString()) { Foreground = brushes.DefaultBrush });
                    k++;
                }
                
                int j2 = k;
                while (j2 < line.Length && (char.IsLetterOrDigit(line[j2]) || line[j2] == '_')) j2++;
                
                if (j2 > k)
                    paragraph.Inlines.Add(new Run(line.Substring(k, j2 - k)) { Foreground = brushes.MacroBrush, FontWeight = FontWeights.SemiBold });
                
                if (j2 < line.Length)
                    paragraph.Inlines.Add(new Run(line.Substring(j2)) { Foreground = brushes.DefaultBrush });
            }
            else
            {
                paragraph.Inlines.Add(new Run(line) { Foreground = brushes.PreprocessorBrush });
            }
        }

        private int RenderBlockComment(string line, Paragraph paragraph, ColorBrushes brushes, int i, ref bool inBlockComment)
        {
            int end = line.IndexOf("*/", i, StringComparison.Ordinal);
            if (end == -1)
            {
                paragraph.Inlines.Add(new Run(line.Substring(i)) { Foreground = brushes.CommentBrush });
                inBlockComment = true;
                return line.Length;
            }
            else
            {
                paragraph.Inlines.Add(new Run(line.Substring(i, end - i + 2)) { Foreground = brushes.CommentBrush });
                inBlockComment = false;
                return end + 2;
            }
        }

        private int RenderStringLiteral(string line, Paragraph paragraph, ColorBrushes brushes, int i)
        {
            int j = i + 1;
            while (j < line.Length)
            {
                if (line[j] == '\\')
                {
                    j += 2;
                    continue;
                }
                if (line[j] == '"') { j++; break; }
                j++;
            }
            paragraph.Inlines.Add(new Run(line.Substring(i, j - i)) { Foreground = brushes.StringBrush });
            return j;
        }

        private int RenderCharLiteral(string line, Paragraph paragraph, ColorBrushes brushes, int i)
        {
            int j = i + 1;
            while (j < line.Length)
            {
                if (line[j] == '\\')
                {
                    j += 2;
                    continue;
                }
                if (line[j] == '\'') { j++; break; }
                j++;
            }
            paragraph.Inlines.Add(new Run(line.Substring(i, j - i)) { Foreground = brushes.CharBrush });
            return j;
        }

        private int RenderIdentifier(string line, Paragraph paragraph, HashSet<string> keywordSet, 
            HashSet<string> typeSet, ColorBrushes brushes, int i)
        {
            int j = i + 1;
            while (j < line.Length && (char.IsLetterOrDigit(line[j]) || line[j] == '_')) j++;
            
            string token = line.Substring(i, j - i);
            Brush brush = brushes.DefaultBrush;
            
            if (keywordSet.Contains(token))
            {
                brush = brushes.KeywordBrush;
            }
            else if (typeSet.Contains(token))
            {
                brush = brushes.TypeBrush;
            }
            else if (_currentDllFunctionNames.Contains(token))
            {
                brush = brushes.FunctionBrush;
            }
            else
            {
                // 启发式：括号紧随视作函数
                int look = j;
                while (look < line.Length && char.IsWhiteSpace(line[look])) look++;
                if (look < line.Length && line[look] == '(')
                {
                    brush = brushes.FunctionBrush;
                }
            }
            
            paragraph.Inlines.Add(new Run(token) { Foreground = brush });
            return j;
        }

        private int RenderNumber(string line, Paragraph paragraph, ColorBrushes brushes, int i)
        {
            int j = i + 1;
            while (j < line.Length && (char.IsDigit(line[j]) || line[j] == 'x' || line[j] == 'X' || 
                (line[j] >= 'a' && line[j] <= 'f') || (line[j] >= 'A' && line[j] <= 'F'))) j++;
            
            paragraph.Inlines.Add(new Run(line.Substring(i, j - i)) { Foreground = brushes.NumberBrush });
            return j;
        }

        private HashSet<string> GetKeywordSet()
        {
            return new HashSet<string>(new[]
            {
                "alignas","alignof","and","and_eq","asm","auto","bitand","bitor","bool","break","case","catch","char","char16_t","char32_t","class","compl","const","constexpr","const_cast","continue","decltype","default","delete","do","double","dynamic_cast","else","enum","explicit","export","extern","false","float","for","friend","goto","if","inline","int","long","mutable","namespace","new","noexcept","not","not_eq","nullptr","operator","or","or_eq","private","protected","public","register","reinterpret_cast","return","short","signed","sizeof","static","static_assert","static_cast","struct","switch","template","this","thread_local","throw","true","try","typedef","typeid","typename","union","unsigned","using","virtual","void","volatile","wchar_t","while","__declspec","dllexport",
                "WINAPI","APIENTRY","CALLBACK","DWORD","WORD","BYTE","LPVOID","LPSTR","LPCSTR","LPWSTR","LPCWSTR","HMODULE","HANDLE","NULL","BOOL","HWND"
            }, StringComparer.Ordinal);
        }

        private HashSet<string> GetTypeSet()
        {
            return new HashSet<string>(new[]
            {
                "HMODULE","HANDLE","HWND","HINSTANCE","HDC","SIZE_T","LPARAM","WPARAM","LRESULT"
            }, StringComparer.Ordinal);
        }

        private ColorBrushes GetColorBrushes()
        {
            return new ColorBrushes
            {
                KeywordBrush = new SolidColorBrush(Color.FromRgb(86, 156, 214)),      // 关键字: 蓝色
                TypeBrush = new SolidColorBrush(Color.FromRgb(78, 201, 176)),         // 类型: 蓝绿
                StringBrush = new SolidColorBrush(Color.FromRgb(214, 157, 133)),      // 字符串: 橙色
                CharBrush = new SolidColorBrush(Color.FromRgb(214, 157, 133)),        // 字符: 橙色
                CommentBrush = new SolidColorBrush(Color.FromRgb(87, 166, 74)),       // 注释: 绿色
                PreprocessorBrush = new SolidColorBrush(Color.FromRgb(155, 155, 155)),// 预处理: 灰
                HeaderBrush = new SolidColorBrush(Color.FromRgb(156, 220, 254)),      // 头文件: 青色
                MacroBrush = new SolidColorBrush(Color.FromRgb(197, 134, 192)),       // 宏: 紫色
                NumberBrush = new SolidColorBrush(Color.FromRgb(181, 206, 168)),      // 数字: 淡绿
                FunctionBrush = new SolidColorBrush(Color.FromRgb(220, 220, 170)),    // 函数名: 黄色
                DefaultBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),     // 默认: 浅灰
                HeadingBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                MetaBrush = new SolidColorBrush(Color.FromRgb(155, 155, 155)),
                DllAvailableBrush = new SolidColorBrush(Color.FromRgb(87, 166, 74)),   // 绿色 - 可劫持DLL
                DllUnavailableBrush = new SolidColorBrush(Color.FromRgb(214, 157, 133)), // 橙色 - 不可劫持DLL
                BodyBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };
        }
    }

    /// <summary>
    /// 颜色画刷集合
    /// </summary>
    public class ColorBrushes
    {
        public Brush KeywordBrush { get; set; }
        public Brush TypeBrush { get; set; }
        public Brush StringBrush { get; set; }
        public Brush CharBrush { get; set; }
        public Brush CommentBrush { get; set; }
        public Brush PreprocessorBrush { get; set; }
        public Brush HeaderBrush { get; set; }
        public Brush MacroBrush { get; set; }
        public Brush NumberBrush { get; set; }
        public Brush FunctionBrush { get; set; }
        public Brush DefaultBrush { get; set; }
        public Brush HeadingBrush { get; set; }
        public Brush MetaBrush { get; set; }
        public Brush DllUnavailableBrush { get; set; }
        public Brush DllAvailableBrush { get; set; }
        public Brush BodyBrush { get; set; }
    }
}
