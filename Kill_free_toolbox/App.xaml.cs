using System;
using System.Windows;
using Kill_free_toolbox.Authorization;
using Kill_free_toolbox.Views;

namespace Kill_free_toolbox
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 程序启动时进行微信验证，未通过则提示并退出
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 动态加载深色主题
            LoadDarkTheme();
            
            //微信验证
            //if (!Weixin.Verification())
            //{
            //    //MessageBox.Show("需要登陆微信并入群才能使用！", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    // 展示二维码窗口
            //    var qrWin = new QrCodeWindow();
            //    qrWin.Show();
             
                
            //}
            return;
        }
        
        /// <summary>
        /// 加载深色主题
        /// </summary>
        private void LoadDarkTheme()
        {
            try
            {
                var darkTheme = new ResourceDictionary();
                darkTheme.Source = new Uri("pack://application:,,,/Kill_free_toolbox;component/Themes/DarkTheme.xaml");
                this.Resources.MergedDictionaries.Add(darkTheme);
            }
            catch (Exception ex)
            {
                // 如果主题加载失败，使用默认样式
                System.Diagnostics.Debug.WriteLine($"主题加载失败: {ex.Message}");
            }
        }
    }
}
