using System;
using System.Windows;
using System.Windows.Controls;
using Kill_free_toolbox.Helper;

namespace Kill_free_toolbox.Views.Pages
{
    /// <summary>
    /// 字符串混淆页面的交互逻辑
    /// </summary>
    public partial class Page_String_Obf : Page
    {
        // 构造函数，初始化页面
        public Page_String_Obf()
        {
            InitializeComponent();
        }

        // ===================== 控件事件 =====================
        // C#字符串输入框内容变化
        private void str_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCsStringOutput();
        }

        // C#字符串结果复制按钮
        private void Button_Copy_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard(csstrout?.Text);
        }

        // C字符串输入框内容变化
        private void cstr_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCStringOutput();
        }

        // C字符串结果复制按钮
        private void cButton_Copy_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard(cstrout?.Text);
        }

        // wchar_t复选框选中
        private void checkbox_is_wchar_t_Checked(object sender, RoutedEventArgs e)
        {
            UpdateCStringOutput();
        }

        // wchar_t复选框取消选中
        private void checkbox_is_wchar_t_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateCStringOutput();
        }

        // 变量编码输入框内容变化
        private void varstr_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateVarStringOutput();
        }

        // 变量编码结果复制按钮
        private void varButton_Copy_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard(varstrout?.Text);
        }

        // Python字符串输入框内容变化
        private void pystr_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePythonStringOutput();
        }

        // Python字符串结果复制按钮
        private void pyButton_Copy_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard(pystrout?.Text);
        }

        // Go字符串输入框内容变化
        private void gostr_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateGoStringOutput();
        }

        // Go字符串结果复制按钮
        private void goButton_Copy_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard(gostrout?.Text);
        }

        // Rust字符串输入框内容变化
        private void ruststr_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRustStringOutput();
        }

        // Rust字符串结果复制按钮
        private void rustButton_Copy_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard(ruststrout?.Text);
        }

        // JavaScript字符串输入框内容变化
        private void jsstr_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateJavaScriptStringOutput();
        }

        // JavaScript字符串结果复制按钮
        private void jsButton_Copy_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard(jsstrout?.Text);
        }

        // ===================== 核心逻辑 =====================
        /// <summary>
        /// 更新C#字符串混淆结果
        /// </summary>
        private void UpdateCsStringOutput()
        {
            var input = csstr?.Text ?? string.Empty;
            if (string.IsNullOrEmpty(input))
            {
                csstrout.Text = string.Empty;
                return;
            }
            try
            {
                csstrout.Text = StringObf.CSStringObf(input);
            }
            catch
            {
                csstrout.Text = string.Empty;
            }
        }

        /// <summary>
        /// 更新C字符串混淆结果
        /// </summary>
        private void UpdateCStringOutput()
        {
            var input = cstr?.Text ?? string.Empty;
            if (string.IsNullOrEmpty(input))
            {
                cstrout.Text = string.Empty;
                return;
            }
            try
            {
                if (checkbox_is_wchar_t?.IsChecked == true)
                    cstrout.Text = StringObf.CStringObfW(input);
                else
                    cstrout.Text = StringObf.CStringObfA(input);
            }
            catch
            {
                cstrout.Text = string.Empty;
            }
        }

        /// <summary>
        /// 更新变量编码结果（每个字符用下划线分隔）
        /// </summary>
        private void UpdateVarStringOutput()
        {
            var input = varstr?.Text ?? string.Empty;
            if (string.IsNullOrEmpty(input))
            {
                varstrout.Text = string.Empty;
                return;
            }
            // 用下划线分隔每个字符
            varstrout.Text = string.Join("_", input.ToCharArray());
        }

        /// <summary>
        /// 更新Python字符串混淆结果
        /// </summary>
        private void UpdatePythonStringOutput()
        {
            var input = pystr?.Text ?? string.Empty;
            if (string.IsNullOrEmpty(input))
            {
                pystrout.Text = string.Empty;
                return;
            }
            try
            {
                pystrout.Text = StringObf.PythonStringObf(input);
            }
            catch
            {
                pystrout.Text = string.Empty;
            }
        }

        /// <summary>
        /// 更新Go字符串混淆结果
        /// </summary>
        private void UpdateGoStringOutput()
        {
            var input = gostr?.Text ?? string.Empty;
            if (string.IsNullOrEmpty(input))
            {
                gostrout.Text = string.Empty;
                return;
            }
            try
            {
                gostrout.Text = StringObf.GolangStringObf(input);
            }
            catch
            {
                gostrout.Text = string.Empty;
            }
        }

        /// <summary>
        /// 更新Rust字符串混淆结果
        /// </summary>
        private void UpdateRustStringOutput()
        {
            var input = ruststr?.Text ?? string.Empty;
            if (string.IsNullOrEmpty(input))
            {
                ruststrout.Text = string.Empty;
                return;
            }
            try
            {
                ruststrout.Text = StringObf.RustStringObf(input);
            }
            catch
            {
                ruststrout.Text = string.Empty;
            }
        }

        /// <summary>
        /// 更新JavaScript字符串混淆结果
        /// </summary>
        private void UpdateJavaScriptStringOutput()
        {
            var input = jsstr?.Text ?? string.Empty;
            if (string.IsNullOrEmpty(input))
            {
                jsstrout.Text = string.Empty;
                return;
            }
            try
            {
                jsstrout.Text = StringObf.JavaScriptStringObf(input);
            }
            catch
            {
                jsstrout.Text = string.Empty;
            }
        }

        // ===================== 辅助方法 =====================
        /// <summary>
        /// 复制文本到剪贴板（空文本不复制）
        /// </summary>
        private void CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ClipboardHelper.SetText(text);
        }
    }
}
