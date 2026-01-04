using Kill_free_toolbox.Helper.Baijiahei;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kill_free_toolbox.Views.Pages
{
    /// <summary>
    /// Page_Baijiahei.xaml 的交互逻辑
    /// </summary>
    public partial class Page_Baijiahei : Page
    {
        private Dictionary<string, List<string>> targetFileImports = new Dictionary<string, List<string>>();
        private string currentSelectedFile = string.Empty;
        private CodeRenderer codeRenderer;
        private System.Threading.CancellationTokenSource currentGenerationCts;
        
        // 白文件搜索相关
        private ObservableCollection<WhiteFileSearchResult> searchResults = new ObservableCollection<WhiteFileSearchResult>();
        private CancellationTokenSource searchCancellationTokenSource;
        
        // .Net白文件搜索相关
        private ObservableCollection<DotNetSearchResult> dotNetSignedGuiResults = new ObservableCollection<DotNetSearchResult>();
        private ObservableCollection<DotNetSearchResult> dotNetSignedCuiResults = new ObservableCollection<DotNetSearchResult>();
        private ObservableCollection<DotNetSearchResult> dotNetExpiredGuiResults = new ObservableCollection<DotNetSearchResult>();
        private ObservableCollection<DotNetSearchResult> dotNetExpiredCuiResults = new ObservableCollection<DotNetSearchResult>();
        private CancellationTokenSource dotNetSearchCancellationTokenSource;

        public Page_Baijiahei()
        {
            InitializeComponent();
            
            // 初始化代码渲染器
            codeRenderer = new CodeRenderer(CppCodeTextBox);
            
            // 初始化界面
            InitializeUI();
        }

        private async void InitializeUI()
        {
            await UIHelper.AddComboBoxItemAsync(DllComboBox, "(请先选择PE文件)");
            await codeRenderer.RenderPlainTextAsync("请先选择PE文件进行IAT分析");
            
            // 初始化白文件搜索界面
            await InitializeWhiteFileSearchUI();
            
            // 初始化.Net白文件搜索界面
            await InitializeDotNetWhiteFileSearchUI();
        }

        /// <summary>
        /// 初始化白文件搜索界面
        /// </summary>
        private async Task InitializeWhiteFileSearchUI()
        {
            // 初始化驱动器列表
            var drives = WhiteFileSearcher.GetAvailableDrives();
            await Dispatcher.InvokeAsync(() =>
            {
                DriveComboBox.Items.Clear();
                foreach (var drive in drives)
                {
                    DriveComboBox.Items.Add(drive);
                }
                if (DriveComboBox.Items.Count > 0)
                {
                    DriveComboBox.SelectedIndex = 0;
                }
            });
            
            // 绑定搜索结果列表
            await Dispatcher.InvokeAsync(() =>
            {
                SearchResultsListBox.ItemsSource = searchResults;
            });
        }

        /// <summary>
        /// 初始化.Net白文件搜索界面
        /// </summary>
        private async Task InitializeDotNetWhiteFileSearchUI()
        {
            // 初始化驱动器列表
            var drives = DotNetWhiteFileSearcher.GetAvailableDrives();
            await Dispatcher.InvokeAsync(() =>
            {
                DotNetDriveComboBox.Items.Clear();
                foreach (var drive in drives)
                {
                    DotNetDriveComboBox.Items.Add(drive);
                }
                if (DotNetDriveComboBox.Items.Count > 0)
                {
                    DotNetDriveComboBox.SelectedIndex = 0;
                }
            });
            
            // 绑定搜索结果列表
            await Dispatcher.InvokeAsync(() =>
            {
                DotNetSignedGuiListBox.ItemsSource = dotNetSignedGuiResults;
                DotNetSignedCuiListBox.ItemsSource = dotNetSignedCuiResults;
                DotNetExpiredGuiListBox.ItemsSource = dotNetExpiredGuiResults;
                DotNetExpiredCuiListBox.ItemsSource = dotNetExpiredCuiResults;
            });
        }



        private async void DllComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DllComboBox.SelectedItem != null)
            {
                string selectedDll = UIHelper.GetComboBoxSelectedItemText(DllComboBox);

                // 跳过分隔符和无效项
                if (UIHelper.IsComboBoxSelectedItemPlaceholder(DllComboBox))
                {
                    return;
                }

                // 只使用目标文件的实际导入函数
                if (targetFileImports.ContainsKey(selectedDll))
                {
                    var functionsToShow = targetFileImports[selectedDll];
                    
                    // 更新代码渲染器的函数名集合
                    codeRenderer.SetCurrentDllFunctionNames(functionsToShow);

                    // 在主页面展示统计与详细信息
                    await RenderFunctionsSummaryAsync(selectedDll, functionsToShow);
                }
            }
        }
        private Task RenderFunctionsSummaryAsync(string dllName, List<string> functions)
        {
            // 构造包含架构、签名有效性/签名方、函数统计和名称列表的文本
            var fullSummary = PEAnalyzer.GetAnalysisSummary(currentSelectedFile, targetFileImports);

            string sigText;
            if (fullSummary.SignatureValid == null)
                sigText = "未知";
            else
            {
                bool isValid = fullSummary.SignatureValid.Value;
                bool isExpired = false;
                
                // 检查是否过期
                if (fullSummary.Signer != null)
                {
                    isExpired = fullSummary.Signer.NotAfter < DateTime.Now;
                }
                
                if (isValid)
                    sigText = "有效";
                else if (isExpired)
                    sigText = "有效（过期）";
                else
                    sigText = "无效";
            }

            string signerInfo = string.Empty;
            if (fullSummary.Signer != null)
            {
                signerInfo = $"\n签名者：{fullSummary.Signer.Subject}\n颁发者：{fullSummary.Signer.Issuer}\n有效期：{fullSummary.Signer.NotBefore:G} - {fullSummary.Signer.NotAfter:G}";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== PE 文件信息 ===");
            sb.AppendLine($"文件：{fullSummary.FileName}");
            sb.AppendLine($"架构：{fullSummary.Architecture ?? "-"}");
            sb.AppendLine($"签名：{sigText}");
            if (!string.IsNullOrEmpty(signerInfo)) sb.AppendLine(signerInfo);
            sb.AppendLine();

            sb.AppendLine($"=== 导入统计（选中 {dllName}） ===");
            sb.AppendLine($"导入DLL总数：{fullSummary.ImportDllCount}");
            sb.AppendLine($"所有导入函数总数：{fullSummary.TotalFunctionCount}");
            sb.AppendLine($"该DLL函数数：{(functions?.Count ?? 0)}");
            sb.AppendLine();

            sb.AppendLine($"=== {dllName} 的函数名称 ===");
            if (functions != null && functions.Count > 0)
            {
                foreach (var fn in functions)
                {
                    sb.AppendLine(fn);
                }
            }
            else
            {
                sb.AppendLine("(无)");
            }

            // 使用代码渲染器进行着色渲染（函数名等语义高亮）
            codeRenderer.RenderPlainText(sb.ToString());
            return Task.CompletedTask;
        }
        private async Task GenerateIATHijackingCodeAsync(string dll, List<string> functions)
        {
            try
            {
                // 取消上一次任务
                currentGenerationCts?.Cancel();
                currentGenerationCts = new System.Threading.CancellationTokenSource();
                var token = currentGenerationCts.Token;

                // 使用Helper类生成代码
                var peParser = new Kill_free_toolbox.Helper.PE.PEParser(currentSelectedFile);
                bool is64Bit = peParser.Is64Bit;
                string code = await IATCodeGenerator.GenerateIATHijackingCodeAsync(dll, functions, currentSelectedFile, is64Bit, token);
                
                // 异步渲染代码
                await codeRenderer.RenderCppCodeAsync(code, token);
            }
            catch (Exception ex)
            {
                await UIHelper.ShowErrorAsync($"生成IAT劫持代码失败：{ex.Message}");
            }
        }



        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 异步显示文件选择对话框
                string selectedFile = await UIHelper.ShowOpenFileDialogAsync(
                    "可执行文件 (*.exe)|*.exe|动态链接库 (*.dll)|*.dll|所有文件 (*.*)|*.*",
                    "选择要劫持的白文件");

                if (!string.IsNullOrEmpty(selectedFile))
                {
                    currentSelectedFile = selectedFile;
                    await UIHelper.SetTextBoxTextAsync(FilePathTextBox, currentSelectedFile);
                    
                    // 异步分析选定文件的IAT
                    await AnalyzeTargetFileIATAsync(currentSelectedFile);
                }
            }
            catch (Exception ex)
            {
                await UIHelper.ShowErrorAsync($"选择文件失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 异步分析目标文件的IAT
        /// </summary>
        private async Task AnalyzeTargetFileIATAsync(string filePath)
        {
            try
            {
                // 使用Helper类异步分析PE文件
                targetFileImports = await PEAnalyzer.AnalyzeTargetFileIATAsync(filePath);
                
                // 更新ComboBox，只显示目标文件实际导入的DLL
                await UpdateDllComboBoxWithTargetImportsAsync();
                
                await codeRenderer.RenderPlainTextAsync("请选择要劫持的DLL，点击上方按钮查看源码或生成项目。");
                
                // 显示分析结果信息
                var summary = PEAnalyzer.GetAnalysisSummary(filePath, targetFileImports);
                await codeRenderer.RenderAnalysisResultsAsync(summary);

                // 更新左侧文件信息（图标、架构、签名）
                await UpdateFileInfoUIAsync(filePath, summary);
            }
            catch (Exception ex)
            {
                await UIHelper.ShowErrorAsync($"分析PE文件失败：{ex.Message}");
                
                // 分析失败时恢复默认ComboBox内容
                await RestoreDefaultDllComboBoxAsync();
            }
        }

        private async Task UpdateFileInfoUIAsync(string filePath, Helper.Baijiahei.AnalysisSummary summary)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // 图标
                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(filePath);
                    if (icon != null)
                    {
                        using (var bmp = icon.ToBitmap())
                        {
                            var hBitmap = bmp.GetHbitmap();
                            try
                            {
                                var wpfBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    hBitmap,
                                    IntPtr.Zero,
                                    Int32Rect.Empty,
                                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                                FileIconImage.Source = wpfBitmap;
                            }
                            finally
                            {
                                NativeMethods.DeleteObject(hBitmap);
                            }
                        }
                    }

                    // 架构
                    ArchTextBlock.Text = $"架构：{summary.Architecture ?? "-"}";

                    // 签名
                    string sig;
                    if (summary.SignatureValid == null)
                        sig = "未知";
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
                            sig = "有效";
                        else if (isExpired)
                            sig = "有效（过期）";
                        else
                            sig = "无效";
                    }
                    
                    if (sig == "有效")
                    {
                        SignatureTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                    }
                    else if (sig == "有效（过期）")
                    {
                        SignatureTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                    }
                    else if (sig == "无效")
                    {
                        SignatureTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    }
                    else
                    {
                        SignatureTextBlock.Foreground = System.Windows.Media.Brushes.Yellow;
                    }
                    SignatureTextBlock.Text = $"签名：{sig}";
                }
                catch
                {
                    // 忽略UI更新错误
                }
            });
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            public static extern bool DeleteObject(IntPtr hObject);
        }

        private async void ViewSourceButton_Click(object sender, RoutedEventArgs e)
        {
            if (DllComboBox.SelectedItem == null || UIHelper.IsComboBoxSelectedItemPlaceholder(DllComboBox)) return;
            string selectedDll = UIHelper.GetComboBoxSelectedItemText(DllComboBox);
            if (!targetFileImports.ContainsKey(selectedDll)) return;

            var functions = targetFileImports[selectedDll];

            try
            {
                // 生成代码并在新窗口中展示
                var tokenSource = new System.Threading.CancellationTokenSource();
                var peParser = new Kill_free_toolbox.Helper.PE.PEParser(currentSelectedFile);
                bool is64Bit = peParser.Is64Bit;
                string code = await IATCodeGenerator.GenerateIATHijackingCodeAsync(selectedDll, functions, currentSelectedFile, is64Bit, tokenSource.Token);
                
                // 获取架构信息用于窗口标题
                string arch = is64Bit ? "x64" : "x86";
                
                var win = new Views.Windows.CodePreviewWindow($"{selectedDll} ({arch})", code);
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex)
            {
                await UIHelper.ShowErrorAsync($"生成源码失败：{ex.Message}");
            }
        }

        private async void GenerateProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (DllComboBox.SelectedItem == null || UIHelper.IsComboBoxSelectedItemPlaceholder(DllComboBox)) return;
            string selectedDll = UIHelper.GetComboBoxSelectedItemText(DllComboBox);
            if (!targetFileImports.ContainsKey(selectedDll)) return;

            var functions = targetFileImports[selectedDll];
            // 输出目录
            string folder = await UIHelper.ShowSelectFolderDialogModernAsync("选择生成项目的输出目录");
           
            if (string.IsNullOrEmpty(folder)) return;

            try
            {
                // 自动检测目标文件的架构
                var peParser = new Kill_free_toolbox.Helper.PE.PEParser(currentSelectedFile);
                string platform = peParser.Is64Bit ? "x64" : "Win32";
                
                // 生成源码、.def文件与工程文件
                bool includeUnlook = AutoGenerateCheckBox.IsChecked == true;
                bool forwardToOriginal = ForwardToOriginalCheckBox.IsChecked == true;
                string cppContent = IATCodeGenerator.GenerateCppFileContent(selectedDll, functions, currentSelectedFile, peParser.Is64Bit, includeUnlook, forwardToOriginal);
                string defContent = IATCodeGenerator.GenerateDefFileContent(selectedDll, functions, peParser.Is64Bit, forwardToOriginal);
                string toolset = await AskToolsetAsync();
                string guid = "{" + System.Guid.NewGuid().ToString().ToUpper() + "}";
                string projContent = IATCodeGenerator.GenerateProjectFileContent(selectedDll, toolset, platform, guid, includeUnlook, forwardToOriginal);

                // 写入文件
                string baseName = System.IO.Path.GetFileNameWithoutExtension(selectedDll);
                string cppPath = System.IO.Path.Combine(folder, baseName + ".cpp");
                string projPath = System.IO.Path.Combine(folder, baseName + ".vcxproj");
                System.IO.File.WriteAllText(cppPath, cppContent, System.Text.Encoding.UTF8);
                System.IO.File.WriteAllText(projPath, projContent, System.Text.Encoding.UTF8);
                
                string defPath = null;
                // 仅在非转发模式下生成DEF文件
                if (!forwardToOriginal)
                {
                    defPath = System.IO.Path.Combine(folder, baseName + ".def");
                    System.IO.File.WriteAllText(defPath, defContent, System.Text.Encoding.UTF8);
                }

                if (includeUnlook)
                {
                    try
                    {
                        var unlook = new Unlook();
                        string unlookPath = System.IO.Path.Combine(folder, "UNLOOK.h");
                        System.IO.File.WriteAllText(unlookPath, unlook.Unlookcode, System.Text.Encoding.UTF8);
                    }
                    catch (Exception exu)
                    {
                        await UIHelper.ShowErrorAsync($"写入 UNLOOK.h 失败：{exu.Message}");
                    }
                }

                // 生成STUB.h文件（两种模式都需要）
                try
                {
                    string stubContent = IATCodeGenerator.GenerateStubHeaderContent(functions, forwardToOriginal, selectedDll);
                    string stubPath = System.IO.Path.Combine(folder, "STUB.h");
                    System.IO.File.WriteAllText(stubPath, stubContent, System.Text.Encoding.UTF8);
                }
                catch (Exception exs)
                {
                    await UIHelper.ShowErrorAsync($"写入 STUB.h 失败：{exs.Message}");
                }

                string infoMessage = $"已生成 {platform} 架构的劫持项目：\n{cppPath}\n{projPath}";
                if (defPath != null)
                {
                    infoMessage += $"\n{defPath}";
                }
                infoMessage += "\n可在VS中打开编译。";
                await UIHelper.ShowInfoAsync(infoMessage);
            }
            catch (Exception ex)
            {
                await UIHelper.ShowErrorAsync($"生成项目失败：{ex.Message}");
            }
        }

        // 拖拽支持：仅允许 .exe/.dll 文件
        private void IATHijackGrid_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var ext = System.IO.Path.GetExtension(files[0]).ToLower();
                    if (ext == ".exe" || ext == ".dll")
                    {
                        e.Effects = System.Windows.DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }
            e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private async void IATHijackGrid_Drop(object sender, System.Windows.DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files == null || files.Length == 0) return;

                var file = files[0];
                var ext = System.IO.Path.GetExtension(file).ToLower();
                if (ext != ".exe" && ext != ".dll") return;

                currentSelectedFile = file;
                await UIHelper.SetTextBoxTextAsync(FilePathTextBox, currentSelectedFile);
                await AnalyzeTargetFileIATAsync(currentSelectedFile);
            }
            catch (Exception ex)
            {
                await UIHelper.ShowErrorAsync($"处理拖拽文件失败：{ex.Message}");
            }
        }

        private Task<string> AskToolsetAsync()
        {
            // 简化：暂以提示框输入，后续可换成设置窗口
            // 若需要无交互默认值，则返回 v143
            return Task.FromResult("v143");
        }


        /// <summary>
        /// 异步更新ComboBox，显示目标文件实际导入的DLL
        /// </summary>
        private async Task UpdateDllComboBoxWithTargetImportsAsync()
        {
            await UIHelper.ClearComboBoxAsync(DllComboBox);
            
            if (targetFileImports.Count == 0)
            {
                await UIHelper.AddComboBoxItemAsync(DllComboBox, "(未找到导入表)");
                return;
            }

            // 只添加目标文件实际导入的非系统DLL（与白文件搜索逻辑保持一致）
            foreach (var dllName in targetFileImports.Keys)
            {
                // 过滤掉系统DLL，只显示可劫持的DLL（使用统一判断逻辑）
                if (WhiteFileSearcher.IsHijackableDll(dllName))
                {
                    await UIHelper.AddComboBoxItemAsync(DllComboBox, dllName);
                }
            }
            
            // 如果没有找到可劫持的DLL，显示提示信息
            if (DllComboBox.Items.Count == 0)
            {
                await UIHelper.AddComboBoxItemAsync(DllComboBox, "(未找到可劫持的DLL)");
            }
        }

        /// <summary>
        /// 异步恢复默认的DLL ComboBox内容
        /// </summary>
        private async Task RestoreDefaultDllComboBoxAsync()
        {
            await UIHelper.ClearComboBoxAsync(DllComboBox);
            await UIHelper.AddComboBoxItemAsync(DllComboBox, "(请先选择PE文件)");
        }

        /// <summary>
        /// 开始搜索按钮点击事件
        /// </summary>
        private async void StartSearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 取消之前的搜索
                searchCancellationTokenSource?.Cancel();
                searchCancellationTokenSource = new CancellationTokenSource();
                
                // 获取搜索参数
                string searchPath = GetSelectedSearchPath();
                bool useDllLimit = SingleDllCheckBox.IsChecked == true;
                int dllCount = int.MaxValue; // 默认不限制
                
                // 如果勾选了DLL限制，使用文本框中的数值
                if (useDllLimit)
                {
                    if (int.TryParse(DllCountTextBox.Text, out int parsedCount) && parsedCount >= 1)
                    {
                        dllCount = parsedCount;
                    }
                    else
                    {
                        dllCount = 1; // 解析失败时默认为1
                    }
                }
                long maxFileSize = WhiteFileSearcher.ParseFileSize(SizeLimitTextBox.Text);
                
                // 调试输出
                System.Diagnostics.Debug.WriteLine($"搜索参数: useDllLimit={useDllLimit}, DllCountTextBox.Text='{DllCountTextBox.Text}', dllCount={dllCount}");
                
                // 清空之前的搜索结果并更新UI状态
                await Dispatcher.InvokeAsync(() =>
                {
                    searchResults.Clear();
                    StartSearchButton.Visibility = Visibility.Collapsed;
                    StopSearchButton.Visibility = Visibility.Visible;
                    StopSearchButton.IsEnabled = true;
                    SearchStatusBar.Visibility = Visibility.Visible;
                    SearchStatusText.Text = "正在搜索...";
                    SearchCountText.Text = "已找到: 0 个";
                    
                    // 禁用DLL相关控件
                    SingleDllCheckBox.IsEnabled = false;
                    DllCountTextBox.IsEnabled = false;
                });
                
                // 创建进度回调，实时添加搜索结果
                var progress = new Progress<WhiteFileSearchResult>(result =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (result != null)
                        {
                            searchResults.Add(result);
                            SearchCountText.Text = $"已找到: {searchResults.Count} 个";

                            // 若勾选了"迁移目录"，则把扫描到的文件迁移到 ./build/白文件/{exe名称}/exe
                            if (MoveToDirectoryCheckBox.IsChecked == true)
                            {
                                // 异步执行迁移，避免阻塞UI
                                _ = Task.Run(() =>
                                {
                                    try
                                    {
                                        AutoMigrateScannedResult(result);
                                    }
                                    catch (Exception mex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"自动迁移失败: {mex.Message}");
                                    }
                                });
                            }
                        }
                    });
                });
                
                // 开始搜索
                await WhiteFileSearcher.SearchWhiteFilesAsync(
                    searchPath,
                    dllCount,
                    maxFileSize,
                    progress,
                    searchCancellationTokenSource.Token);
                
                // 搜索完成，恢复UI状态
                await Dispatcher.InvokeAsync(() =>
                {
                    StartSearchButton.Visibility = Visibility.Visible;
                    StopSearchButton.Visibility = Visibility.Collapsed;
                    StopSearchButton.IsEnabled = false;
                    SearchStatusBar.Visibility = Visibility.Collapsed;
                    
                    // 重新启用DLL相关控件
                    SingleDllCheckBox.IsEnabled = true;
                    DllCountTextBox.IsEnabled = true;
                });
                
                await UIHelper.ShowInfoAsync($"搜索完成，找到 {searchResults.Count} 个白文件");
            }
            catch (OperationCanceledException)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    StartSearchButton.Visibility = Visibility.Visible;
                    StopSearchButton.Visibility = Visibility.Collapsed;
                    StopSearchButton.IsEnabled = false;
                    SearchStatusBar.Visibility = Visibility.Collapsed;
                    
                    // 重新启用DLL相关控件
                    SingleDllCheckBox.IsEnabled = true;
                    DllCountTextBox.IsEnabled = true;
                });
                await UIHelper.ShowInfoAsync("搜索已停止");
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    StartSearchButton.Visibility = Visibility.Visible;
                    StopSearchButton.Visibility = Visibility.Collapsed;
                    StopSearchButton.IsEnabled = false;
                    SearchStatusBar.Visibility = Visibility.Collapsed;
                    
                    // 重新启用DLL相关控件
                    SingleDllCheckBox.IsEnabled = true;
                    DllCountTextBox.IsEnabled = true;
                });
                await UIHelper.ShowErrorAsync($"搜索过程中发生错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 停止搜索按钮点击事件
        /// </summary>
        private void StopSearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 取消搜索
                searchCancellationTokenSource?.Cancel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"停止搜索时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取选择的搜索路径
        /// </summary>
        private string GetSelectedSearchPath()
        {
            if (DriveComboBox.SelectedItem != null)
            {
                string selectedDrive = DriveComboBox.SelectedItem.ToString();
                // 提取驱动器路径（去掉卷标）
                if (selectedDrive.Contains(" ("))
                {
                    return selectedDrive.Substring(0, selectedDrive.IndexOf(" ("));
                }
                return selectedDrive;
            }
            return "C:\\";
        }

        /// <summary>
        /// 迁移选中的文件
        /// </summary>
        private async Task MoveSelectedFilesAsync()
        {
            if (MoveToDirectoryCheckBox.IsChecked != true)
                return;
                
            var selectedItems = SearchResultsListBox.SelectedItems.Cast<WhiteFileSearchResult>().ToList();
            if (selectedItems.Count == 0)
            {
                await UIHelper.ShowInfoAsync("请先选择要迁移的文件");
                return;
            }
            
            string targetDirectory = await UIHelper.ShowSelectFolderDialogAsync("选择迁移目标目录");
            if (string.IsNullOrEmpty(targetDirectory))
                return;
                
            int successCount = 0;
            foreach (var item in selectedItems)
            {
                try
                {
                    bool success = await WhiteFileSearcher.MoveFileToDirectoryAsync(item.FilePath, targetDirectory);
                    if (success)
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    await UIHelper.ShowErrorAsync($"迁移文件 {item.FileName} 失败：{ex.Message}");
                }
            }
            
            await UIHelper.ShowInfoAsync($"成功迁移 {successCount}/{selectedItems.Count} 个文件");
        }

        private void AutoMigrateScannedResult(WhiteFileSearchResult item)
        {
            // 普通白文件迁移到 ./build/白文件/native白/签名状态/架构/程序类型/ 目录
            string exeNameNoExt = System.IO.Path.GetFileNameWithoutExtension(item.FileName);
            string hash8 = ComputeFileHash8(item.FilePath);
            
            // 确定签名状态目录
            string signatureStatus = item.SignatureStatus == "有效" ? "签名有效" : "签名过期";
            
            // 确定架构目录
            string arch = GetNormalizedArch(item);
            
            // 确定程序类型目录
            string appType = item.IsGuiApplication ? "GUI" : "CUI";
            
            // 构建目标目录路径
            string destDir = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "build", 
                "白文件", 
                "native白", 
                signatureStatus, 
                arch, 
                appType, 
                $"{exeNameNoExt}_{hash8}"
            );
            
            System.IO.Directory.CreateDirectory(destDir);

            string destExePath = System.IO.Path.Combine(destDir, item.FileName);
            System.IO.File.Copy(item.FilePath, destExePath, true);

            string readmePath = System.IO.Path.Combine(destDir, "readme.txt");
            string readme = GenerateReadmeContent(item);
            System.IO.File.WriteAllText(readmePath, readme, System.Text.Encoding.UTF8);

            // 携带非系统DLL到目标目录
            CopyNonSystemDlls(item, destDir);
        }

        private static string ComputeFileHash8(string filePath)
        {
            try
            {
                using (var sha = SHA256.Create())
                using (var stream = System.IO.File.OpenRead(filePath))
                {
                    var hash = sha.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", string.Empty).Substring(0, 8).ToLower();
                }
            }
            catch
            {
                // 失败时使用时间戳兜底，避免冲突
                return DateTime.UtcNow.Ticks.ToString("x").Substring(0, 8);
            }
        }

        private static string GetNormalizedArch(WhiteFileSearchResult item)
        {
            var arch = (item.Architecture ?? string.Empty).Trim().ToLower();
            if (arch.Contains("64")) return "x64";
            if (arch.Contains("86")) return "x86";
            return string.IsNullOrEmpty(arch) ? "unknown" : arch;
        }

        /// <summary>
        /// 迁移选中文件菜单项点击事件
        /// </summary>
        private async void MoveSelectedFilesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 直接迁移到 .\\build\\白文件\\{exe名称} 下，并生成readme
            try
            {
                var items = SearchResultsListBox.SelectedItems.Cast<WhiteFileSearchResult>().ToList();
                if (items.Count == 0)
                {
                    await UIHelper.ShowInfoAsync("请先选择要迁移的文件");
                    return;
                }

                int success = 0;
                foreach (var item in items)
                {
                    try
                    {
                        string exeNameNoExt = System.IO.Path.GetFileNameWithoutExtension(item.FileName);
                        string hash8 = ComputeFileHash8(item.FilePath);
                        string arch = GetNormalizedArch(item);
                        string destDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "build", "白文件", arch, $"{exeNameNoExt}_{hash8}");
                        System.IO.Directory.CreateDirectory(destDir);

                        // 复制exe
                        string destExePath = System.IO.Path.Combine(destDir, item.FileName);
                        System.IO.File.Copy(item.FilePath, destExePath, true);

                        // 生成readme
                        string readmePath = System.IO.Path.Combine(destDir, "readme.txt");
                        string readme = GenerateReadmeContent(item);
                        System.IO.File.WriteAllText(readmePath, readme, System.Text.Encoding.UTF8);

                        // 携带非系统DLL到目标目录
                        CopyNonSystemDlls(item, destDir);

                        success++;
                    }
                    catch (Exception exx)
                    {
                        await UIHelper.ShowErrorAsync($"迁移 {item.FileName} 失败：{exx.Message}");
                    }
                }

                await UIHelper.ShowInfoAsync($"已迁移 {success}/{items.Count} 个文件到 ./build/白文件/ 目录");
            }
            catch (Exception ex)
            {
                await UIHelper.ShowErrorAsync($"迁移失败：{ex.Message}");
            }
        }

        private string GenerateReadmeContent(WhiteFileSearchResult item)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"文件名: {item.FileName}");
            sb.AppendLine($"原始路径: {item.FilePath}");
            sb.AppendLine($"架构: {item.Architecture}");
            sb.AppendLine($"签名: {item.SignatureStatus}");
            sb.AppendLine($"程序类型: {item.ApplicationTypeDisplay}");
            sb.AppendLine($"可劫持DLL数量: {item.NonSystemDllCount}");
            sb.AppendLine();
            sb.AppendLine($"迁移路径: Build/白文件/native白/{(item.SignatureStatus.Contains("有效") ? "签名有效" : "签名过期")}/{GetNormalizedArch(item)}/{(item.IsGuiApplication ? "GUI" : "CUI")}");
            sb.AppendLine();
            sb.AppendLine("可劫持DLL列表及其导入函数:");

            try
            {
                // 尝试重新解析该EXE的导入表，列出每个非系统DLL的函数名
                var pe = new Kill_free_toolbox.Helper.PE.PEParser(item.FilePath);
                var imports = pe.GetImportTable();
                foreach (var kv in imports)
                {
                    string dll = kv.Key;
                    if (!WhiteFileSearcher.IsHijackableDll(dll)) continue;
                    sb.AppendLine($"- {dll}");
                    foreach (var fn in kv.Value)
                    {
                        sb.AppendLine($"    - {fn}");
                    }
                }
            }
            catch
            {
                // 若解析失败，退化为仅输出已保存的NonSystemDlls
                if (item.NonSystemDlls != null && item.NonSystemDlls.Count > 0)
                {
                    foreach (var dll in item.NonSystemDlls)
                    {
                        sb.AppendLine($"- {dll}");
                    }
                }
            }

            return sb.ToString();
        }

        private void CopyNonSystemDlls(WhiteFileSearchResult item, string destDir)
        {
            try
            {
                var dllNames = item.NonSystemDlls;
                if (dllNames == null || dllNames.Count == 0)
                {
                    // 若模型里无列表，尝试重新解析
                    try
                    {
                        var pe = new Kill_free_toolbox.Helper.PE.PEParser(item.FilePath);
                        var imports = pe.GetImportTable();
                        dllNames = imports.Keys.Where(n => WhiteFileSearcher.IsHijackableDll(n)).ToList();
                    }
                    catch { dllNames = new List<string>(); }
                }

                string exeDir = System.IO.Path.GetDirectoryName(item.FilePath);
                foreach (var dll in dllNames)
                {
                    try
                    {
                        var dllPath = TryResolveDllPath(exeDir, dll);
                        if (!string.IsNullOrEmpty(dllPath) && System.IO.File.Exists(dllPath))
                        {
                            var destPath = System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(dllPath));
                            System.IO.File.Copy(dllPath, destPath, true);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private string TryResolveDllPath(string baseDir, string dllName)
        {
            try
            {
                // 1) EXE 同目录
                string p1 = System.IO.Path.Combine(baseDir, dllName);
                if (System.IO.File.Exists(p1)) return p1;

                // 2) PATH 目录
                var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                foreach (var dir in pathEnv.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        string candidate = System.IO.Path.Combine(dir.Trim(), dllName);
                        if (System.IO.File.Exists(candidate))
                        {
                            // 避免携带系统DLL
                            if (WhiteFileSearcher.IsHijackableDll(System.IO.Path.GetFileName(dllName)))
                                return candidate;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 打开文件位置菜单项点击事件
        /// </summary>
        private async void OpenFileLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = SearchResultsListBox.SelectedItem as WhiteFileSearchResult;
            if (selectedItem != null)
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{selectedItem.FilePath}\"");
                }
                catch (Exception ex)
                {
                    await UIHelper.ShowErrorAsync($"打开文件位置失败：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 复制文件路径菜单项点击事件
        /// </summary>
        private async void CopyFilePathMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = SearchResultsListBox.SelectedItems.Cast<WhiteFileSearchResult>().ToList();
            if (selectedItems.Count == 0)
            {
                await UIHelper.ShowInfoAsync("请先选择要复制的文件");
                return;
            }

            try
            {
                var paths = selectedItems.Select(item => item.FilePath).ToArray();
                System.Windows.Clipboard.SetText(string.Join(Environment.NewLine, paths));
                await UIHelper.ShowInfoAsync($"已复制 {paths.Length} 个文件路径到剪贴板");
            }
            catch (Exception ex)
            {
                await UIHelper.ShowErrorAsync($"复制文件路径失败：{ex.Message}");
            }
        }

        #region .Net白文件搜索相关方法

        /// <summary>
        /// .Net开始搜索按钮点击事件
        /// </summary>
        private async void DotNetStartSearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 取消之前的搜索
                dotNetSearchCancellationTokenSource?.Cancel();
                dotNetSearchCancellationTokenSource = new CancellationTokenSource();
                
                // 获取搜索参数
                string searchPath = GetDotNetSelectedSearchPath();
                long maxFileSize = DotNetWhiteFileSearcher.ParseFileSize(DotNetSizeLimitTextBox.Text);
                
                // 清空之前的搜索结果并更新UI状态
                await Dispatcher.InvokeAsync(() =>
                {
                    dotNetSignedGuiResults.Clear();
                    dotNetSignedCuiResults.Clear();
                    dotNetExpiredGuiResults.Clear();
                    dotNetExpiredCuiResults.Clear();
                    DotNetStartSearchButton.Visibility = Visibility.Collapsed;
                    DotNetStopSearchButton.Visibility = Visibility.Visible;
                    DotNetStopSearchButton.IsEnabled = true;
                    DotNetSearchStatusBar.Visibility = Visibility.Visible;
                    DotNetSearchStatusText.Text = "正在搜索...";
                    DotNetSearchCountText.Text = "已找到: 0 个";
                    DotNetSignedGuiCountText.Text = "0 个";
                    DotNetSignedCuiCountText.Text = "0 个";
                    DotNetExpiredGuiCountText.Text = "0 个";
                    DotNetExpiredCuiCountText.Text = "0 个";
                    
                    // 禁用DLL相关控件
                    SingleDllCheckBox.IsEnabled = false;
                    DllCountTextBox.IsEnabled = false;
                });
                
                // 创建进度回调，实时添加搜索结果
                var progress = new Progress<DotNetSearchResult>(result =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (result != null)
                        {
                            // 根据签名状态和程序类型分类
                            if (result.IsSignatureValid)
                            {
                                if (result.IsGuiApplication)
                                {
                                    dotNetSignedGuiResults.Add(result);
                                    DotNetSignedGuiCountText.Text = $"{dotNetSignedGuiResults.Count} 个";
                                }
                                else if (result.IsConsoleApplication)
                                {
                                    dotNetSignedCuiResults.Add(result);
                                    DotNetSignedCuiCountText.Text = $"{dotNetSignedCuiResults.Count} 个";
                                }
                            }
                            else if (result.IsSignatureExpired)
                            {
                                if (result.IsGuiApplication)
                                {
                                    dotNetExpiredGuiResults.Add(result);
                                    DotNetExpiredGuiCountText.Text = $"{dotNetExpiredGuiResults.Count} 个";
                                }
                                else if (result.IsConsoleApplication)
                                {
                                    dotNetExpiredCuiResults.Add(result);
                                    DotNetExpiredCuiCountText.Text = $"{dotNetExpiredCuiResults.Count} 个";
                                }
                            }
                            
                            int totalCount = dotNetSignedGuiResults.Count + dotNetSignedCuiResults.Count + 
                                           dotNetExpiredGuiResults.Count + dotNetExpiredCuiResults.Count;
                            DotNetSearchCountText.Text = $"已找到: {totalCount} 个";

                            if (DotNetMoveToDirectoryCheckBox.IsChecked == true)
                            {
                                // 异步执行迁移，避免阻塞UI
                                _ = Task.Run(() =>
                                {
                                    try
                                    {
                                        AutoMigrateDotNetScannedResult(result);
                                    }
                                    catch (Exception mex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"自动迁移失败: {mex.Message}");
                                    }
                                });
                            }
                        }
                    });
                });
                
                // 开始搜索
                await DotNetWhiteFileSearcher.SearchDotNetWhiteFilesAsync(
                    searchPath,
                    maxFileSize,
                    progress,
                    dotNetSearchCancellationTokenSource.Token);
                
                // 搜索完成，恢复UI状态
                await Dispatcher.InvokeAsync(() =>
                {
                    DotNetStartSearchButton.Visibility = Visibility.Visible;
                    DotNetStopSearchButton.Visibility = Visibility.Collapsed;
                    DotNetStopSearchButton.IsEnabled = false;
                    DotNetSearchStatusBar.Visibility = Visibility.Collapsed;
                    
                    // 重新启用DLL相关控件
                    SingleDllCheckBox.IsEnabled = true;
                    DllCountTextBox.IsEnabled = true;
                });
                
                int totalFound = dotNetSignedGuiResults.Count + dotNetSignedCuiResults.Count + 
                                dotNetExpiredGuiResults.Count + dotNetExpiredCuiResults.Count;
                await UIHelper.ShowInfoAsync($"搜索完成，找到 {totalFound} 个.Net白文件");
            }
            catch (OperationCanceledException)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    DotNetStartSearchButton.Visibility = Visibility.Visible;
                    DotNetStopSearchButton.Visibility = Visibility.Collapsed;
                    DotNetStopSearchButton.IsEnabled = false;
                    DotNetSearchStatusBar.Visibility = Visibility.Collapsed;
                    
                    // 重新启用DLL相关控件
                    SingleDllCheckBox.IsEnabled = true;
                    DllCountTextBox.IsEnabled = true;
                });
                await UIHelper.ShowInfoAsync("搜索已停止");
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    DotNetStartSearchButton.Visibility = Visibility.Visible;
                    DotNetStopSearchButton.Visibility = Visibility.Collapsed;
                    DotNetStopSearchButton.IsEnabled = false;
                    DotNetSearchStatusBar.Visibility = Visibility.Collapsed;
                    
                    // 重新启用DLL相关控件
                    SingleDllCheckBox.IsEnabled = true;
                    DllCountTextBox.IsEnabled = true;
                });
                await UIHelper.ShowErrorAsync($"搜索过程中发生错误：{ex.Message}");
            }
        }

        /// <summary>
        /// .Net停止搜索按钮点击事件
        /// </summary>
        private void DotNetStopSearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 取消搜索
                dotNetSearchCancellationTokenSource?.Cancel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"停止搜索时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取.Net搜索选择的搜索路径
        /// </summary>
        private string GetDotNetSelectedSearchPath()
        {
            if (DotNetDriveComboBox.SelectedItem != null)
            {
                string selectedDrive = DotNetDriveComboBox.SelectedItem.ToString();
                // 提取驱动器路径（去掉卷标）
                if (selectedDrive.Contains(" ("))
                {
                    return selectedDrive.Substring(0, selectedDrive.IndexOf(" ("));
                }
                return selectedDrive;
            }
            return "C:\\";
        }

        /// <summary>
        /// 自动迁移.Net扫描结果
        /// </summary>
        private void AutoMigrateDotNetScannedResult(DotNetSearchResult item)
        {
            // .Net文件迁移到 ./build/白文件/.Net/签名状态/架构/程序类型/ 目录
            string exeNameNoExt = System.IO.Path.GetFileNameWithoutExtension(item.FileName);
            string hash8 = ComputeFileHash8(item.FilePath);
            
            // 确定签名状态目录
            string signatureStatus = item.IsSignatureValid ? "签名有效" : "签名过期";
            
            // 确定架构目录
            string arch = GetNormalizedArch(item);
            
            // 确定程序类型目录
            string appType = item.IsGuiApplication ? "GUI" : "CUI";
            
            // 构建目标目录路径
            string destDir = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "build", 
                "白文件", 
                ".Net", 
                signatureStatus, 
                arch, 
                appType, 
                $"{exeNameNoExt}_{hash8}"
            );
            
            System.IO.Directory.CreateDirectory(destDir);

            string destExePath = System.IO.Path.Combine(destDir, item.FileName);
            System.IO.File.Copy(item.FilePath, destExePath, true);

            string readmePath = System.IO.Path.Combine(destDir, "readme.txt");
            string readme = GenerateDotNetReadmeContent(item);
            System.IO.File.WriteAllText(readmePath, readme, System.Text.Encoding.UTF8);

            // .Net程序不需要携带DLL，因为不需要分析导入表
        }

        private string GenerateDotNetReadmeContent(DotNetSearchResult item)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"文件名: {item.FileName}");
            sb.AppendLine($"原始路径: {item.FilePath}");
            sb.AppendLine($"架构: {item.Architecture}");
            sb.AppendLine($"签名: {item.SignatureStatusDisplay}");
            sb.AppendLine($"程序类型: {item.ApplicationTypeDisplay}");
            sb.AppendLine($".Net版本: {item.DotNetVersion}");
            sb.AppendLine();
            sb.AppendLine("注意：.Net程序不需要分析导入表，只要有签名就是白文件");
            sb.AppendLine($"迁移路径: Build/白文件/.Net/{(item.IsSignatureValid ? "签名有效" : "签名过期")}/{GetNormalizedArch(item)}/{(item.IsGuiApplication ? "GUI" : "CUI")}");

            return sb.ToString();
        }

        private void CopyNonSystemDlls(DotNetSearchResult item, string destDir)
        {
            try
            {
                var dllNames = item.NonSystemDlls;
                if (dllNames == null || dllNames.Count == 0)
                {
                    // 若模型里无列表，尝试重新解析
                    try
                    {
                        var pe = new Kill_free_toolbox.Helper.PE.PEParser(item.FilePath);
                        var imports = pe.GetImportTable();
                        dllNames = imports.Keys.Where(n => DotNetWhiteFileSearcher.IsHijackableDll(n)).ToList();
                    }
                    catch { dllNames = new List<string>(); }
                }

                string exeDir = System.IO.Path.GetDirectoryName(item.FilePath);
                foreach (var dll in dllNames)
                {
                    try
                    {
                        var dllPath = TryResolveDllPath(exeDir, dll);
                        if (!string.IsNullOrEmpty(dllPath) && System.IO.File.Exists(dllPath))
                        {
                            var destPath = System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(dllPath));
                            System.IO.File.Copy(dllPath, destPath, true);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private string GetNormalizedArch(DotNetSearchResult item)
        {
            var arch = (item.Architecture ?? string.Empty).Trim().ToLower();
            if (arch.Contains("64")) return "x64";
            if (arch.Contains("86")) return "x86";
            return string.IsNullOrEmpty(arch) ? "unknown" : arch;
        }

        /// <summary>
        /// .Net迁移选中文件菜单项点击事件
        /// </summary>
        private async void DotNetMoveSelectedFilesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 直接迁移到 .\\build\\白文件\\{exe名称} 下，并生成readme
            try
            {
                var items = new List<DotNetSearchResult>();
                
                // 获取当前选中的ListBox
                var listBox = GetListBoxFromMenuItem(sender);
                if (listBox != null)
                {
                    items.AddRange(listBox.SelectedItems.Cast<DotNetSearchResult>());
                }
                
                if (items.Count == 0)
                {
                    await UIHelper.ShowInfoAsync("请先选择要迁移的文件");
                    return;
                }

                int success = 0;
                foreach (var item in items)
                {
                    try
                    {
                        string exeNameNoExt = System.IO.Path.GetFileNameWithoutExtension(item.FileName);
                        string hash8 = ComputeFileHash8(item.FilePath);
                        string arch = GetNormalizedArch(item);
                        string destDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "build", "白文件", arch, $"{exeNameNoExt}_{hash8}");
                        System.IO.Directory.CreateDirectory(destDir);

                        // 复制exe
                        string destExePath = System.IO.Path.Combine(destDir, item.FileName);
                        System.IO.File.Copy(item.FilePath, destExePath, true);

                        // 生成readme
                        string readmePath = System.IO.Path.Combine(destDir, "readme.txt");
                        string readme = GenerateDotNetReadmeContent(item);
                        System.IO.File.WriteAllText(readmePath, readme, System.Text.Encoding.UTF8);

                        // 携带非系统DLL到目标目录
                        CopyNonSystemDlls(item, destDir);

                        success++;
                    }
                    catch (Exception exx)
                    {
                        await UIHelper.ShowErrorAsync($"迁移 {item.FileName} 失败：{exx.Message}");
                    }
                }

                await UIHelper.ShowInfoAsync($"已迁移 {success}/{items.Count} 个文件到 ./build/白文件/ 目录");
            }
            catch (Exception ex)
            {
                await UIHelper.ShowErrorAsync($"迁移失败：{ex.Message}");
            }
        }

        /// <summary>
        /// .Net打开文件位置菜单项点击事件
        /// </summary>
        private async void DotNetOpenFileLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var listBox = GetListBoxFromMenuItem(sender);
            var selectedItem = listBox?.SelectedItem as DotNetSearchResult;
            if (selectedItem != null)
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{selectedItem.FilePath}\"");
                }
                catch (Exception ex)
                {
                    await UIHelper.ShowErrorAsync($"打开文件位置失败：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// .Net复制文件路径菜单项点击事件
        /// </summary>
        private async void DotNetCopyFilePathMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var listBox = GetListBoxFromMenuItem(sender);
            var selectedItems = listBox?.SelectedItems.Cast<DotNetSearchResult>().ToList();
            if (selectedItems == null || selectedItems.Count == 0)
            {
                await UIHelper.ShowInfoAsync("请先选择要复制的文件");
                return;
            }

            try
            {
                var paths = selectedItems.Select(item => item.FilePath).ToArray();
                System.Windows.Clipboard.SetText(string.Join(Environment.NewLine, paths));
                await UIHelper.ShowInfoAsync($"已复制 {paths.Length} 个文件路径到剪贴板");
            }
            catch (Exception ex)
            {
                await UIHelper.ShowErrorAsync($"复制文件路径失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 从MenuItem获取对应的ListBox
        /// </summary>
        private ListBox GetListBoxFromMenuItem(object sender)
        {
            if (sender is MenuItem menuItem)
            {
                // 获取ContextMenu
                var contextMenu = menuItem.Parent as ContextMenu;
                if (contextMenu != null)
                {
                    // 通过ContextMenu的PlacementTarget获取ListBox
                    var listBox = contextMenu.PlacementTarget as ListBox;
                    if (listBox != null)
                    {
                        return listBox;
                    }
                }
            }
            return null;
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
            
            if (parentObject == null) return null;
            
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }

        #endregion

        private void DllCountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit); // 非数字输入拦截
        }

    }
}
