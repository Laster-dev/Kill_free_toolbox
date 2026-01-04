using Kill_free_toolbox.Helper.C;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kill_free_toolbox.Views.Pages
{
    /// <summary>
    /// Page_Bin2head.xaml 的交互逻辑（.NET Framework 4）
    /// 简洁现代化样式，不包含动画。接受任意文件并弹出文件大小，同时在页面下方显示最近一次信息。
    /// </summary>
    public partial class Page_Bin2head : Page
    {
        public Page_Bin2head()
        {
            InitializeComponent();
            // 添加全选/取消全选的快捷键处理
            this.KeyDown += Page_KeyDown;
            
            // 为所有复选框添加事件处理
            CheckC.Checked += CheckBox_Changed;
            CheckC.Unchecked += CheckBox_Changed;
            CheckCSharp.Checked += CheckBox_Changed;
            CheckCSharp.Unchecked += CheckBox_Changed;
            CheckGo.Checked += CheckBox_Changed;
            CheckGo.Unchecked += CheckBox_Changed;
            CheckJS.Checked += CheckBox_Changed;
            CheckJS.Unchecked += CheckBox_Changed;
            CheckPython.Checked += CheckBox_Changed;
            CheckPython.Unchecked += CheckBox_Changed;
            CheckRust.Checked += CheckBox_Changed;
            CheckRust.Unchecked += CheckBox_Changed;
            
            // 初始化提示文本
            UpdateHintText();
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateHintText();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+A 全选所有语言
            if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SelectAllLanguages(true);
                e.Handled = true;
            }
            // Ctrl+D 取消全选
            else if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SelectAllLanguages(false);
                e.Handled = true;
            }
        }

        private void SelectAllLanguages(bool isChecked)
        {
            CheckC.IsChecked = isChecked;
            CheckCSharp.IsChecked = isChecked;
            CheckGo.IsChecked = isChecked;
            CheckJS.IsChecked = isChecked;
            CheckPython.IsChecked = isChecked;
            CheckRust.IsChecked = isChecked;
        }

        private void Page_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (HasFile(e))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
                DropTitle.Text = "释放以放入文件";
                
                // 显示选中的语言数量
                var selectedCount = GetSelectedLanguages().Count;
                if (selectedCount > 0)
                {
                    DropHint.Text = $"将生成 {selectedCount} 种语言的代码文件";
                }
                else
                {
                    DropHint.Text = "请先选择至少一种语言";
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Page_PreviewDragOver(object sender, DragEventArgs e)
        {
            // 保持复制提示，防止系统每帧发出错误声
            if (HasFile(e))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Page_PreviewDragLeave(object sender, DragEventArgs e)
        {
            // 恢复提示文本
            DropTitle.Text = "将文件拖放到此处";
            UpdateHintText();
        }

        private void UpdateHintText()
        {
            var selectedCount = GetSelectedLanguages().Count;
            if (selectedCount > 0)
            {
                DropHint.Text = $"已选择 {selectedCount} 种语言 — 拖放文件开始转换";
            }
            else
            {
                DropHint.Text = "请先选择至少一种语言";
            }
        }

        private async void Page_PreviewDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            // 先恢复默认文本（或根据需要设置）
            DropTitle.Text = "将文件拖放到此处";
            DropHint.Text = "支持任意文件 — 放开后转换文件";

            if (!HasFile(e)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;

            var path = files[0];
            string buildDir = Path.Combine(".", "build","bin2head");
            
            // 检查文件大小
            var fileInfo = new FileInfo(path);
            if (fileInfo.Length > 50 * 1024 * 1024) // 50MB 限制
            {
                var result = MessageBox.Show(
                    $"文件大小为 {fileInfo.Length / (1024 * 1024):F1} MB，较大的文件可能需要较长时间处理。是否继续？",
                    "文件大小警告",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.No)
                    return;
            }

            // 禁用拖拽区域，防止重复触发
            try
            {
                DropArea.IsEnabled = false;
                DropTitle.Text = "正在转换文件...";
                DropHint.Text = System.IO.Path.GetFileName(path);

                // 获取选中的语言
                var selectedLanguages = GetSelectedLanguages();
                
                if (selectedLanguages.Count == 0)
                {
                    MessageBox.Show("请至少选择一种语言！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }



                var generatedFiles = new List<string>();

                // 在后台线程执行耗时工作，避免阻塞 UI
                await Task.Run(() =>
                {
                    // 读取字节（同步读取在后台线程）
                    byte[] fileBytes = File.ReadAllBytes(path);
                    string ident = Bin2head.NormalizeAsCIdent(Path.GetFileName(path));

                    // 确保输出目录存在
                    if (!Directory.Exists(buildDir))
                    {
                        Directory.CreateDirectory(buildDir);
                    }

                    var utf8 = new UTF8Encoding(false);

                    // 为每种选中的语言生成代码
                    for (int i = 0; i < selectedLanguages.Count; i++)
                    {
                        var lang = selectedLanguages[i];
                        
                        // 更新进度提示
                        Dispatcher.Invoke(() =>
                        {
                            DropTitle.Text = $"正在生成 {lang.name} 代码... ({i + 1}/{selectedLanguages.Count})";
                        });
                        
                        try
                        {
                            string code = lang.generator(ident, fileBytes);
                            string fileName = ident + lang.ext;
                            string filePath = Path.Combine(buildDir, fileName);
                            
                            File.WriteAllText(filePath, code, utf8);
                            generatedFiles.Add(fileName);
                        }
                        catch (Exception ex)
                        {
                            // 记录单个语言生成失败，但继续处理其他语言
                            string errorMsg = $"生成 {lang.name} 代码失败：{ex.Message}";
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(errorMsg, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                            });
                        }
                    }
                }).ConfigureAwait(false);
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // 打开资源管理器定位到 build 文件夹
                        string fullBuildDir = Path.GetFullPath(buildDir);
                        System.Diagnostics.Process.Start("explorer.exe", fullBuildDir);
                    }
                    catch
                    {
                        // 忽略打开资源管理器失败
                    }

                    //// 弹窗提示完成
                    //if (generatedFiles.Count > 0)
                    //{
                    //    var fileInfo = new FileInfo(path);
                    //    string message = $"转换完成！\n\n" +
                    //                   $"源文件：{Path.GetFileName(path)} ({FormatFileSize(fileInfo.Length)})\n" +
                    //                   $"生成文件：{generatedFiles.Count} 个\n\n" +
                    //                   string.Join("\n", generatedFiles.Select(f => "• " + f)) + 
                    //                   $"\n\n输出路径：{Path.GetFullPath(buildDir)}";
                    //    MessageBox.Show(message, "转换完成", MessageBoxButton.OK, MessageBoxImage.Information);
                    //}
                    //else
                    //{
                    //    MessageBox.Show("没有成功生成任何文件。请检查选择的语言和文件权限。", "转换失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    //}
                });
            }
            catch (Exception ex)
            {
                // 如果后台任务抛出异常，会在这里捕获
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("转换失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                // 恢复 UI 状态
                Dispatcher.Invoke(() =>
                {
                    DropArea.IsEnabled = true;
                    DropTitle.Text = "将文件拖放到此处";
                    DropHint.Text = "支持任意文件 — 放开后转换文件";
                });
            }
        }

        private bool HasFile(DragEventArgs e)
        {
            return e.Data.GetDataPresent(DataFormats.FileDrop);
        }

        private List<(string name, string ext, Func<string, byte[], string> generator)> GetSelectedLanguages()
        {
            var selectedLanguages = new List<(string name, string ext, Func<string, byte[], string> generator)>();
            
            if (CheckC.IsChecked == true)
                selectedLanguages.Add(("C", ".h", Bin2head.Build_C));
            if (CheckCSharp.IsChecked == true)
                selectedLanguages.Add(("C#", ".cs", Bin2head.Build_CS));
            if (CheckGo.IsChecked == true)
                selectedLanguages.Add(("Go", ".go", Bin2head.Build_GO));
            if (CheckJS.IsChecked == true)
                selectedLanguages.Add(("JavaScript", ".js", Bin2head.Build_JS));
            if (CheckPython.IsChecked == true)
                selectedLanguages.Add(("Python", ".py", Bin2head.Build_Py));
            if (CheckRust.IsChecked == true)
                selectedLanguages.Add(("Rust", ".rs", Bin2head.Build_Rust));
                
            return selectedLanguages;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            SelectAllLanguages(true);
        }

        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
        {
            SelectAllLanguages(false);
        }

        private void BtnSelectCommon_Click(object sender, RoutedEventArgs e)
        {
            // 选择常用语言：C, C#, Python, JavaScript
            CheckC.IsChecked = true;
            CheckCSharp.IsChecked = true;
            CheckGo.IsChecked = false;
            CheckJS.IsChecked = true;
            CheckPython.IsChecked = true;
            CheckRust.IsChecked = false;
        }
    }
}