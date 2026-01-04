using Kill_free_toolbox.Views.Pages;
using Kill_free_toolbox.Views.Windows;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Kill_free_toolbox
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑（已优化：避免启动卡顿）
    /// </summary>
    public partial class MainWindow : Window
    {
        // 按需创建页面，避免在窗口加载时立即构造复杂页面
        private Page_String_Obf page_String_Obf;
        private Page_Bin2head page_Bin2head;
        private Page_Ps1_OBF page_Ps1_OBF;
        private Page_PE2SC page_PE2SC;
        private Page_Loader_Build page_Loader_Build;
        private Page_Defender_Check page_Defender_Check;
        private Page_Baijiahei Page_Baijiahei;
        private Page_Update Page_Update;
        private Page_RubbishCode page_RubbishCode;
        private Page_Network page_Network;

        public MainWindow()
        {
            InitializeComponent();
            // 事件绑定：注意这里不创建页面实例，直到真正需要
            btnStringObf.Checked += (s, args) => NavigateToPage(() =>
            {
                if (page_String_Obf == null)
                {
                    page_String_Obf = new Page_String_Obf();
                }
                return page_String_Obf;
            });

            btnBin2head.Checked += (s, args) => NavigateToPage(() =>
            {
                if (page_Bin2head == null)
                {
                    page_Bin2head = new Page_Bin2head();
                }
                return page_Bin2head;
            });
            btnPowershellObf.Checked += (s, args) => NavigateToPage(() =>
            {
                if (page_Ps1_OBF == null)
                {
                    page_Ps1_OBF = new Page_Ps1_OBF();
                }
                return page_Ps1_OBF;
            });
            btnPE2shellcode.Checked += (s, args) => NavigateToPage(() =>
            {
                if (page_PE2SC == null)
                {
                    page_PE2SC = new Page_PE2SC();
                }
                return page_PE2SC;
            });
            btnloader.Checked += (s, args) => NavigateToPage(() =>
            {
                if (page_Loader_Build == null)
                {
                    page_Loader_Build = new Page_Loader_Build();
                }
                return page_Loader_Build;
            });

            btnbaijiahei.Checked += (s, args) => NavigateToPage(() =>
            {
                if (Page_Baijiahei == null)
                {
                    Page_Baijiahei = new Page_Baijiahei();
                }
                return Page_Baijiahei;
            });
            btndefendercheck.Checked += (s, args) => NavigateToPage(() =>
            {
                if (page_Defender_Check == null)
                {
                    page_Defender_Check = new Page_Defender_Check();
                }
                return page_Defender_Check;
            });
            btnUpdate.Checked += (s, args) => NavigateToPage(() =>
            {
                if (Page_Update == null)
                {
                    Page_Update = new Page_Update();
                }
                return Page_Update;
            });
            btnRubbish.Checked += (s, args) => NavigateToPage(() =>
            {
                if (page_RubbishCode == null)
                {
                    page_RubbishCode = new Page_RubbishCode();
                }
                return page_RubbishCode;
            });
            btnNetwork.Checked += (s, args) => NavigateToPage(() =>
            {
                if (page_Network == null)
                {
                    page_Network = new Page_Network();
                }
                return page_Network;
            });

            // 首次导航到默认页面（按需创建）
            NavigateToPage(() =>
            {
                if (page_String_Obf == null)
                {
                    page_String_Obf = new Page_String_Obf();
                }
                return page_String_Obf;
            });
        }


        #region 窗体控制与初始化


        /// <summary>
        /// 导航到指定页面（按需创建页面），清空并重建框架以避免导航条
        /// 使用 Func<Page> pageFactory 保证页面构造是按需且在 UI 线程进行
        /// </summary>
        private void NavigateToPage(Func<Page> pageFactory)
        {
            if (pageFactory == null) return;

            // 在 UI 线程执行（通常已在 UI 线程）
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => NavigateToPage(pageFactory));
                return;
            }

            var page = pageFactory();
            if (page == null) return;

            try
            {
                // 清空当前内容区域
                MainFram.Children.Clear();

                // 创建新的框架并禁用导航界面
                var frame = new Frame
                {
                    NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden
                };

                // 添加框架到内容区域
                MainFram.Children.Add(frame);

                // 设置页面本身禁用焦点虚线（避免遍历子树）
                page.FocusVisualStyle = null;

                // 导航到指定页面
                frame.Navigate(page);

                Debug.WriteLine("导航到页面: " + page.GetType().FullName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NavigateToPage 出错: " + ex);
            }
        }

        #endregion

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

        #region 窗口生命周期（保证动画与位置）

        // 窗口创建时
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            try
            {
                this.Opacity = 1;

                // 居中显示（基于工作区域）
                this.Left = (SystemParameters.WorkArea.Width - this.Width) / 2;
                this.Top = (SystemParameters.WorkArea.Height - this.Height) / 2;

                // 播放启动动画（如果存在）
                var storyboard = this.Resources["RestartStoryboard"] as Storyboard;
                storyboard?.Begin();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("OnSourceInitialized 出错: " + ex);
            }
        }

        // 窗口状态改变时（例如最大化/恢复）触发动画
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            try
            {
                var storyboard = this.Resources["RestartStoryboard"] as Storyboard;
                storyboard?.Begin();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("OnStateChanged 出错: " + ex);
            }
        }

        #endregion

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Window_About  about = new Window_About();
            about.Show();
        }
    }
}