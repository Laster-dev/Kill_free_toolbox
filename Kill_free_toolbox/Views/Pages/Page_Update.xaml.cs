using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Kill_free_toolbox.Helper;

namespace Kill_free_toolbox.Views.Pages
{
    /// <summary>
    /// Page_Update.xaml 的交互逻辑
    /// </summary>
    public partial class Page_Update : Page
    {
        public Page_Update()
        {
            InitializeComponent();
            LoadUpdateContent();
        }

        /// <summary>
        /// 加载更新内容
        /// </summary>
        private void LoadUpdateContent()
        {
            // 这里可以从配置文件、数据库或API加载更新内容
            // 目前使用硬编码的示例内容
        }

        /// <summary>
        /// 检查更新按钮点击事件
        /// </summary>
        private void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 这里可以实现检查更新的逻辑
                MessageBox.Show("当前已是最新版本！", "检查更新", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检查更新时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 复制更新日志按钮点击事件
        /// </summary>
        private void CopyLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取更新内容文本
                string updateContent = GetUpdateContentAsText();
                
                // 复制到剪贴板
                ClipboardHelper.SetText(updateContent);
                
                MessageBox.Show("更新日志已复制到剪贴板！", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制更新日志时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 关闭当前页面或窗口
            if (this.Parent is Window window)
            {
                window.Close();
            }
            else if (this.Parent is Frame frame)
            {
                frame.Navigate(null);
            }
        }

        /// <summary>
        /// 获取更新内容为纯文本格式
        /// </summary>
        /// <returns>更新内容的文本表示</returns>
        private string GetUpdateContentAsText()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("免杀工具箱 - 更新日志");
            sb.AppendLine("========================");
            sb.AppendLine();
            sb.AppendLine("版本 1.0.0 - 2024年1月15日");
            sb.AppendLine("🎉 首次发布版本");
            sb.AppendLine();
            sb.AppendLine("欢迎使用免杀工具箱！这是一个功能强大的工具集，帮助您进行各种安全测试和开发工作。");
            sb.AppendLine();
            sb.AppendLine("✨ 主要功能：");
            sb.AppendLine("• 字符串混淆 - 支持多种编程语言的字符串混淆");
            sb.AppendLine("• 二进制转头文件 - 将二进制文件转换为各种编程语言的头文件");
            sb.AppendLine("• PE文件分析 - 深度分析PE文件结构");
            sb.AppendLine("• 白文件搜索 - 快速搜索系统中的白文件");
            sb.AppendLine("• 反病毒检测 - 检测文件是否被杀毒软件识别");
            sb.AppendLine("• PowerShell混淆 - 混淆PowerShell脚本");
            sb.AppendLine();
            sb.AppendLine("💻 使用示例：");
            sb.AppendLine("// C# 字符串混淆示例");
            sb.AppendLine("string result = StringObf.CSStringObf(\"Hello World\");");
            sb.AppendLine("Console.WriteLine(result);");
            sb.AppendLine();
            sb.AppendLine("🔧 技术细节");
            sb.AppendLine("本工具使用WPF框架开发，支持深色主题，界面美观且易于使用。所有功能都经过精心设计，确保稳定性和性能。");
            sb.AppendLine();
            sb.AppendLine("⚠️ 注意事项");
            sb.AppendLine("• 请确保在使用前备份重要文件");
            sb.AppendLine("• 某些功能可能被杀毒软件误报，请添加信任");
            sb.AppendLine("• 建议在虚拟机环境中进行测试");
            sb.AppendLine();
            sb.AppendLine("🚀 未来计划");
            sb.AppendLine("我们正在开发更多功能，包括：");
            sb.AppendLine("• 更多编程语言支持");
            sb.AppendLine("• 图形化PE分析界面");
            sb.AppendLine("• 自动化测试功能");
            sb.AppendLine("• 插件系统");
            sb.AppendLine();
            sb.AppendLine("📞 联系我们");
            sb.AppendLine("如果您在使用过程中遇到问题或有建议，请通过以下方式联系我们：");
            sb.AppendLine("• GitHub: https://github.com/your-repo");
            sb.AppendLine("• Email: your-email@example.com");
            
            return sb.ToString();
        }

        /// <summary>
        /// 动态添加更新内容项
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="type">内容类型（title, subtitle, text, code, list, image, separator）</param>
        /// <param name="content">内容</param>
        /// <param name="parameters">额外参数</param>
        public void AddUpdateItem(Panel parent, string type, string content, Dictionary<string, object> parameters = null)
        {
            switch (type.ToLower())
            {
                case "title":
                    var title = new TextBlock
                    {
                        Text = content,
                        Style = (Style)FindResource("UpdateTitleStyle")
                    };
                    parent.Children.Add(title);
                    break;

                case "subtitle":
                    var subtitle = new TextBlock
                    {
                        Text = content,
                        Style = (Style)FindResource("UpdateSubTitleStyle")
                    };
                    parent.Children.Add(subtitle);
                    break;

                case "text":
                    var text = new TextBlock
                    {
                        Text = content,
                        Style = (Style)FindResource("UpdateTextStyle")
                    };
                    parent.Children.Add(text);
                    break;

                case "code":
                    var codeBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(1),
                        Margin = new Thickness(0, 5, 0, 5)
                    };
                    var codeText = new TextBlock
                    {
                        Text = content,
                        Style = (Style)FindResource("UpdateCodeStyle")
                    };
                    codeBorder.Child = codeText;
                    parent.Children.Add(codeBorder);
                    break;

                case "list":
                    var listItem = new TextBlock
                    {
                        Text = content,
                        Style = (Style)FindResource("UpdateListStyle")
                    };
                    parent.Children.Add(listItem);
                    break;

                case "image":
                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri(content, UriKind.RelativeOrAbsolute)),
                        Style = (Style)FindResource("UpdateImageStyle")
                    };
                    parent.Children.Add(image);
                    break;

                case "separator":
                    var separator = new Rectangle
                    {
                        Style = (Style)FindResource("UpdateSeparatorStyle")
                    };
                    parent.Children.Add(separator);
                    break;
            }
        }

        /// <summary>
        /// 从Markdown格式文本解析并添加内容
        /// </summary>
        /// <param name="markdownText">Markdown格式的文本</param>
        /// <param name="parent">父容器</param>
        public void ParseMarkdownAndAdd(string markdownText, Panel parent)
        {
            var lines = markdownText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;
                
                if (trimmedLine.StartsWith("# "))
                {
                    // 一级标题
                    AddUpdateItem(parent, "title", trimmedLine.Substring(2));
                }
                else if (trimmedLine.StartsWith("## "))
                {
                    // 二级标题
                    AddUpdateItem(parent, "subtitle", trimmedLine.Substring(3));
                }
                else if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
                {
                    // 列表项
                    AddUpdateItem(parent, "list", trimmedLine.Substring(2));
                }
                else if (trimmedLine.StartsWith("```"))
                {
                    // 代码块（简化处理）
                    continue; // 在实际实现中需要更复杂的解析
                }
                else if (trimmedLine.StartsWith("---"))
                {
                    // 分隔线
                    AddUpdateItem(parent, "separator", "");
                }
                else
                {
                    // 普通文本
                    AddUpdateItem(parent, "text", trimmedLine);
                }
            }
        }
    }
}