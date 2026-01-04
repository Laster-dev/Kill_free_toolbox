using Kill_free_toolbox.Helper.C;
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
    /// Page_PE2SC.xaml 的交互逻辑
    /// </summary>
    public partial class Page_PE2SC : Page
    {
        public Page_PE2SC()
        {
            InitializeComponent();
        }
        private void Page_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (HasFile(e))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
                DropTitle.Text = "释放以放入文件";
                DropHint.Text = "将检测架构并生成对应的bin文件";
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
            DropHint.Text = "支持PE文件 — 自动检测x86/x64架构";
        }

        private async void Page_PreviewDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            // 先恢复默认文本（或根据需要设置）
            DropTitle.Text = "将文件拖放到此处";
            DropHint.Text = "支持PE文件 — 自动检测x86/x64架构";

            if (!HasFile(e)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;

            var path = files[0];
            string buildDir = Path.Combine(".", "build","shellcode");
            string outputFilePath = null;
            try
            {
                DropArea.IsEnabled = false;
                DropTitle.Text = "正在转换文件...";
                DropHint.Text = System.IO.Path.GetFileName(path);

                await Task.Run(() =>
                {
                    byte[] peBytes = File.ReadAllBytes(path);
                    // 判断架构
                    bool is64Bit = IsPE64Bit(peBytes);
                    byte[] binData = is64Bit ? x64_bin.Data : x86_bin.Data;
                    

                    byte[] combinedBytes = new byte[binData.Length + peBytes.Length];
                    Array.Copy(binData, 0, combinedBytes, 0, binData.Length);
                    Array.Copy(peBytes, 0, combinedBytes, binData.Length, peBytes.Length);

                    if (!Directory.Exists(buildDir))
                    {
                        Directory.CreateDirectory(buildDir);
                    }

                    string fileName = Path.GetFileNameWithoutExtension(path);
                    string suffix = is64Bit ? ".x64" : ".x86";

                    outputFilePath = Path.Combine(buildDir, fileName + suffix + ".bin");
                    File.WriteAllBytes(outputFilePath, combinedBytes);
                }).ConfigureAwait(false);
                
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        string fullBuildDir = Path.GetFullPath(buildDir);
                        System.Diagnostics.Process.Start("explorer.exe", fullBuildDir);
                    }
                    catch
                    {
                        // 忽略打开资源管理器失败
                    }
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
                    DropHint.Text = "支持PE文件 — 自动检测x86/x64架构";
                });
            }
        }

        private bool HasFile(DragEventArgs e)
        {
            return e.Data.GetDataPresent(DataFormats.FileDrop);
        }

        /// <summary>
        /// 判断PE文件是否为64位
        /// </summary>
        /// <param name="peBytes">PE文件字节数组</param>
        /// <returns>true表示64位，false表示32位</returns>
        private bool IsPE64Bit(byte[] peBytes)
        {
            try
            {
                // 检查DOS头签名 "MZ"
                if (peBytes.Length < 64 || peBytes[0] != 0x4D || peBytes[1] != 0x5A)
                {
                    throw new InvalidDataException("不是有效的PE文件");
                }

                // 获取PE头偏移量（DOS头+60字节处）
                int peOffset = BitConverter.ToInt32(peBytes, 60);
                
                if (peOffset + 24 >= peBytes.Length)
                {
                    throw new InvalidDataException("PE头偏移量无效");
                }

                // 检查PE签名 "PE\0\0"
                if (peBytes[peOffset] != 0x50 || peBytes[peOffset + 1] != 0x45 || 
                    peBytes[peOffset + 2] != 0x00 || peBytes[peOffset + 3] != 0x00)
                {
                    throw new InvalidDataException("PE签名无效");
                }

                // 获取机器类型（PE头+4字节处）
                ushort machineType = BitConverter.ToUInt16(peBytes, peOffset + 4);
                
                // 判断架构
                // 0x014c = IMAGE_FILE_MACHINE_I386 (x86)
                // 0x8664 = IMAGE_FILE_MACHINE_AMD64 (x64)
                switch (machineType)
                {
                    case 0x014c: // x86
                        return false;
                    case 0x8664: // x64
                        return true;
                    default:
                        throw new NotSupportedException($"不支持的机器类型: 0x{machineType:X4}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"解析PE文件失败: {ex.Message}");
            }
        }
    }
}
