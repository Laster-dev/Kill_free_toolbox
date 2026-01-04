using Kill_free_toolbox.Helper.DefenderScanner;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using Path = System.IO.Path;

namespace Kill_free_toolbox.Views.Pages
{
    /// <summary>
    /// Page_Defender_Check.xaml 的交互逻辑
    /// </summary>
    public partial class Page_Defender_Check : Page
    {
        private DefenderScanner _scanner;
        private string _selectedFilePath;

        public Page_Defender_Check()
        {
            InitializeComponent();
            InitializeScanner();        }
        private void InitializeScanner()
        {
            _scanner = new DefenderScanner();
            _scanner.OnOutput += OnScannerOutput;
            _scanner.OnScanCompleted += OnScannerCompleted;
        }

        private void OnScannerOutput(string output)
        {
            Dispatcher.Invoke(() =>
            {
                var ts = DateTime.Now.ToString("HH:mm:ss");
                OutputTextBlock.Text += $"[{ts}] {output}";
                OutputScrollViewer.ScrollToEnd();
            });
        }



        private void OnScannerCompleted(bool success)
        {
            Dispatcher.Invoke(() =>
            {
                StartScanButton.IsEnabled = !string.IsNullOrEmpty(_selectedFilePath);
                StopScanButton.IsEnabled = false;
            });
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "选择要检测的文件",
                Filter = "可执行文件 (*.exe)|*.exe|动态链接库 (*.dll)|*.dll|所有文件 (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SetSelectedFile(openFileDialog.FileName);
            }
        }

        private void SetSelectedFile(string filePath)
        {
            _selectedFilePath = filePath;
            FilePathTextBox.Text = _selectedFilePath;
            StartScanButton.IsEnabled = true;

            // 清空之前的输出
            OutputTextBlock.Text = $"已选择文件: {Path.GetFileName(_selectedFilePath)}\n文件路径: {_selectedFilePath}\n文件大小: {new FileInfo(_selectedFilePath).Length} 字节\n\n准备开始检测...\n";
        }

        private async void StartScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                MessageBox.Show("请先选择一个有效的文件！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 检查是否有管理员权限
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show("此程序需要管理员权限才能正常运行。请以管理员身份重新启动程序。",
                    "需要管理员权限", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StartScanButton.IsEnabled = false;
            StopScanButton.IsEnabled = true;

            OutputTextBlock.Text = "";

            await _scanner.StartScanAsync(_selectedFilePath, true);
        }

        private void StopScanButton_Click(object sender, RoutedEventArgs e)
        {
            _scanner.StopScan();
            StopScanButton.IsEnabled = false;
        }

        private bool IsRunningAsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}
