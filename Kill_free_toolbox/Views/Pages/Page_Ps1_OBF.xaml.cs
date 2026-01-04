using Kill_free_toolbox.Helper.C;
using Kill_free_toolbox.Helper.PowershellObf;
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
    /// Page_Ps1_OBF.xaml 的交互逻辑
    /// </summary>
    public partial class Page_Ps1_OBF : Page
    {
        public Page_Ps1_OBF()
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
                DropHint.Text = "我们将读取并转换该文件";
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
            DropHint.Text = "放开后混淆文件";
        }

        private async void Page_PreviewDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            // 先恢复默认文本（或根据需要设置）
            DropTitle.Text = "将文件拖放到此处";
            DropHint.Text = "放开后混淆文件";

            if (!HasFile(e)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;

            var path = files[0];
            //判断文件是不是ps1结尾
            if (path == null) return;
            if (!path.ToLower().EndsWith(".ps1"))
            {
                MessageBox.Show("仅支持 .ps1 文件！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string buildDir = Path.Combine(".", "build","PS1Obf");

            // 禁用拖拽区域，防止重复触发
            try
            {
                DropArea.IsEnabled = false;
                DropTitle.Text = "正在混淆文件...";
                DropHint.Text = System.IO.Path.GetFileName(path);

                // 在后台线程执行耗时工作，避免阻塞 UI
                await Task.Run(() =>
                {
                    PowershellObf.Obfuscate(path);
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
                    DropHint.Text = "放开后混淆文件";
                });
            }
        }

        private bool HasFile(DragEventArgs e)
        {
            return e.Data.GetDataPresent(DataFormats.FileDrop);
        }
    }
}
