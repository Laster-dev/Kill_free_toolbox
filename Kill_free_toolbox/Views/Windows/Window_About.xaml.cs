using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Kill_free_toolbox.Views.Windows
{
    /// <summary>
    /// Window_About.xaml 的交互逻辑
    /// </summary>
    public partial class Window_About : Window
    {
        public Window_About()
        {
            InitializeComponent();
        }
        #region 标题栏与窗口按钮

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try
                {
                    this.DragMove();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("DragMove 失败: " + ex);
                }
            }
        }

        // 关闭按钮（使用名为 MinimizeStoryboard 的动画资源作为示例，结束后关闭）
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var storyboard = this.Resources["MinimizeStoryboard"] as Storyboard;
            if (storyboard == null)
            {
                // 资源不存在则直接关闭
                this.Close();
                return;
            }

            EventHandler handler = null;
            handler = (s, a) =>
            {
                storyboard.Completed -= handler;
                try
                {
                    this.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("窗口关闭失败: " + ex);
                }
            };

            storyboard.Completed += handler;
            storyboard.Begin();
        }

        // 最小化按钮（使用同名动画，动画结束后最小化）
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var storyboard = this.Resources["MinimizeStoryboard"] as Storyboard;
            if (storyboard == null)
            {
                this.WindowState = WindowState.Minimized;
                return;
            }

            EventHandler handler = null;
            handler = (s, a) =>
            {
                storyboard.Completed -= handler;
                try
                {
                    this.WindowState = WindowState.Minimized;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("最小化失败: " + ex);
                }
            };

            storyboard.Completed += handler;
            storyboard.Begin();
        }

        #endregion
    }
}
