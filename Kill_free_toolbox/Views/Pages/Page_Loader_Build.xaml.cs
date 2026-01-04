using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Microsoft.Win32;
using System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using MessageBox = System.Windows.MessageBox;

namespace Kill_free_toolbox.Views.Pages
{
    /// <summary>
    /// Page_Loader_Build.xaml 的交互逻辑
    /// </summary>
    public partial class Page_Loader_Build : Page, INotifyPropertyChanged
    {
        public Page_Loader_Build()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region 必要参数

        #region Shellcode配置参数
        
        /// <summary>
        /// Shellcode文件路径
        /// 用于指定要加载的Shellcode二进制文件位置
        /// </summary>
        private string _shellcodePath = "shellcode文件...";
        public string ShellcodePath
        {
            get => _shellcodePath;
            set => SetProperty(ref _shellcodePath, value);
        }

        /// <summary>
        /// 是否选择x86架构
        /// true: 生成32位加载器
        /// false: 生成64位加载器
        /// </summary>
        private bool _isArchitectureX86 = true;
        public bool IsArchitectureX86
        {
            get => _isArchitectureX86;
            set => SetProperty(ref _isArchitectureX86, value);
        }

        /// <summary>
        /// 是否选择x64架构
        /// true: 生成64位加载器
        /// false: 生成32位加载器
        /// </summary>
        private bool _isArchitectureX64 = false;
        public bool IsArchitectureX64
        {
            get => _isArchitectureX64;
            set => SetProperty(ref _isArchitectureX64, value);
        }

        /// <summary>
        /// 代码膨胀程度 (0-100%)
        /// 用于增加生成的加载器文件大小，提高免杀效果
        /// 值越大，文件越大，但可能影响加载速度
        /// </summary>
        private double _inflationValue = 0;
        public double InflationValue
        {
            get => _inflationValue;
            set => SetProperty(ref _inflationValue, value);
        }
        #endregion

        #region 混淆选项参数
        
        /// <summary>
        /// 是否抹除IAT (Import Address Table)
        /// true: 清除导入地址表，增加逆向分析难度
        /// false: 保留正常的IAT结构
        /// </summary>
        private bool _eraseIAT = true;
        public bool EraseIAT
        {
            get => _eraseIAT;
            set => SetProperty(ref _eraseIAT, value);
        }

        /// <summary>
        /// 是否将Shellcode嵌入到资源中
        /// true: 将Shellcode作为资源嵌入到PE文件中
        /// false: 将Shellcode直接嵌入到代码段中
        /// </summary>
        private bool _resourceEmbed = false;
        public bool ResourceEmbed
        {
            get => _resourceEmbed;
            set => SetProperty(ref _resourceEmbed, value);
        }

        /// <summary>
        /// 是否添加无效导出函数
        /// true: 在PE文件中添加大量无用的导出函数
        /// false: 不添加额外的导出函数
        /// </summary>
        private bool _addFakeExports = false;
        public bool AddFakeExports
        {
            get => _addFakeExports;
            set => SetProperty(ref _addFakeExports, value);
        }

        /// <summary>
        /// 无效导出函数数量 (0-99999)
        /// 当AddFakeExports为true时生效
        /// 用于控制添加的虚假导出函数数量
        /// </summary>
        private double _fakeExportsCount = 0;
        public double FakeExportsCount
        {
            get => _fakeExportsCount;
            set => SetProperty(ref _fakeExportsCount, value);
        }
        #endregion

        #region 文件信息参数
        
        /// <summary>
        /// 图标文件路径
        /// 用于设置生成的可执行文件的图标
        /// 支持.ico格式文件
        /// </summary>
        private string _iconPath = "请选择图标文件...";
        public string IconPath
        {
            get => _iconPath;
            set => SetProperty(ref _iconPath, value);
        }

        /// <summary>
        /// 产品名称
        /// 显示在文件属性中的产品名称
        /// 用于伪装成合法软件
        /// </summary>
        private string _productName = "免杀工具箱";
        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        /// <summary>
        /// 文件描述
        /// 显示在文件属性中的文件描述信息
        /// 用于伪装成合法软件
        /// </summary>
        private string _fileDescription = "免杀加载器";
        public string FileDescription
        {
            get => _fileDescription;
            set => SetProperty(ref _fileDescription, value);
        }

        /// <summary>
        /// 版本信息
        /// 显示在文件属性中的版本号
        /// 格式: 主版本.次版本.修订版本.构建版本
        /// </summary>
        private string _version = "1.0.0.0";
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        /// <summary>
        /// 版权信息
        /// 显示在文件属性中的版权声明
        /// 用于伪装成合法软件
        /// </summary>
        private string _copyright = "Copyright © 2024 免杀工具箱";
        public string Copyright
        {
            get => _copyright;
            set => SetProperty(ref _copyright, value);
        }
        #endregion

        #region 时间戳和签名参数
        
        /// <summary>
        /// 是否使用当前时间戳
        /// true: 使用当前系统时间作为PE文件时间戳
        /// false: 使用其他时间戳设置
        /// </summary>
        private bool _useCurrentTimestamp = true;
        public bool UseCurrentTimestamp
        {
            get => _useCurrentTimestamp;
            set => SetProperty(ref _useCurrentTimestamp, value);
        }

        /// <summary>
        /// 是否使用无效时间戳
        /// true: 使用无效或随机的时间戳
        /// false: 使用正常的时间戳
        /// </summary>
        private bool _useInvalidTimestamp = false;
        public bool UseInvalidTimestamp
        {
            get => _useInvalidTimestamp;
            set => SetProperty(ref _useInvalidTimestamp, value);
        }

        /// <summary>
        /// 签名文件路径
        /// 用于复制其他已签名文件的数字签名
        /// 提高免杀效果，伪装成已签名的合法软件
        /// </summary>
        private string _signaturePath = "请选择有签名的文件...";
        public string SignaturePath
        {
            get => _signaturePath;
            set => SetProperty(ref _signaturePath, value);
        }
        #endregion

        #region 输出参数
        
        /// <summary>
        /// 输出文件路径
        /// 指定生成的加载器文件的保存位置
        /// 如果为空，将使用默认输出目录
        /// </summary>
        private string _outputPath = "";
        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value);
        }
        #endregion

        #endregion

        #region 属性通知机制
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        #region 文件选择事件

        /// <summary>
        /// 选择Shellcode文件
        /// </summary>
        private void SelectShellcodeButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "选择Shellcode文件",
                Filter = "二进制文件 (*.bin)|*.bin|所有文件 (*.*)|*.*",
                DefaultExt = "bin"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ShellcodePath = openFileDialog.FileName;
            }
        }

        /// <summary>
        /// 选择图标文件
        /// </summary>
        private void SelectIconButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "选择图标文件",
                Filter = "图标文件 (*.ico)|*.ico|所有文件 (*.*)|*.*",
                DefaultExt = "ico"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IconPath = openFileDialog.FileName;
            }
        }

        /// <summary>
        /// 选择签名文件
        /// </summary>
        private void SelectSignatureButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "选择有签名的文件",
                Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
                DefaultExt = "exe"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SignaturePath = openFileDialog.FileName;
            }
        }

        /// <summary>
        /// 选择输出目录
        /// </summary>
        private void SelectOutputDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择输出目录"
            };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputPath = folderDialog.SelectedPath;
            }
        }

        #endregion

        #region 滑块值变化事件

        /// <summary>
        /// 膨胀程度滑块值变化
        /// </summary>
        private void InflationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            InflationValue = e.NewValue;
        }

        /// <summary>
        /// 无效导出滑块值变化
        /// </summary>
        private void FakeExportsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FakeExportsCount = e.NewValue;
        }

        #endregion

        #region 复选框事件

 

        #endregion

        #region 按钮事件

        /// <summary>
        /// 预览配置按钮点击
        /// </summary>
        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("别急老铁，加急开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 生成加载器按钮点击
        /// </summary>
        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. 验证必要参数
            if (string.IsNullOrEmpty(ShellcodePath) || ShellcodePath == "shellcode文件...")
            {
                MessageBox.Show("请选择Shellcode文件", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(ShellcodePath))
            {
                MessageBox.Show("Shellcode文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. 禁用按钮并提示用户，防止重复点击
            var generateButton = sender as System.Windows.Controls.Button;
            if (generateButton != null)
            {
                generateButton.IsEnabled = false;
                generateButton.Content = "正在生成...";
            }

            try
            {
                // 3. 异步执行耗时操作
                if (InflationValue != 0)
                {
                    //MessageBox.Show($"正在进行代码处理，将执行 {(int)InflationValue} 次，请稍候...", "信息", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 使用 await 异步等待，不会卡住UI
                    var deopResult = await Helper.LoaderBuild.LoaderBbuild.deopAsync(ShellcodePath, IsArchitectureX86 ? "32" : "64", (int)InflationValue);

                    // 检查执行结果
                    if (!deopResult.Success)
                    {
                        MessageBox.Show($"代码处理失败！\n\n详细日志:\n{deopResult.Log}", "执行失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        return; // 失败则提前退出
                    }
                }

                // 4. 弹出保存对话框（这部分必须在UI线程）
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "保存生成的加载器",
                    Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
                    DefaultExt = "exe",
                    FileName = "loader.exe"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var outputPath = saveFileDialog.FileName;

                    // 5. 将PE文件创建和写入操作放到后台线程
                    bool success = await Task.Run(() =>
                    {
                        try
                        {
                            uint offset = 0; // offset is always 0 per requirement
                            ulong imageBase = IsArchitectureX64 ? (ulong)0x140000000 : (ulong)0x40000000;
                            var pe = Helper.LoaderBbuild.CreatePE(ShellcodePath, offset, Array.Empty<string>(), IsArchitectureX64, imageBase);

                            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                            {
                                pe.Write(fs);
                            }
                            return true;
                        }
                        catch (Exception ex)
                        {
                            // 在后台线程捕获异常，并通过UI线程显示
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"生成PE文件时发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                            return false;
                        }
                    });

                    if (success)
                    {
                        MessageBox.Show($"加载器已成功生成并保存到:\n{outputPath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                // 捕获所有未预料的异常
                MessageBox.Show($"发生未知错误:\n{ex.Message}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 6. 无论成功或失败，最后都恢复按钮状态
                if (generateButton != null)
                {
                    generateButton.IsEnabled = true;
                    generateButton.Content = "Generate"; // 或者您原来的按钮文本
                }
            }
        }

        #endregion
    }
}
