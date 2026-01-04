using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WF = System.Windows.Forms;

namespace Kill_free_toolbox.Helper.Baijiahei
{
    /// <summary>
    /// UI辅助类
    /// </summary>
    public class UIHelper
    {
        /// <summary>
        /// 现代样式的选择文件夹对话框（基于 IFileDialog）
        /// </summary>
        public static async Task<string> ShowSelectFolderDialogModernAsync(string title = "选择输出目录")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IFileDialog dialog = null;
                try
                {
                    dialog = (IFileDialog)new FileOpenDialogRCW();
                    uint options;
                    dialog.GetOptions(out options);
                    // FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST | FOS_CREATEPROMPT(可选)
                    options |= 0x00000020 | 0x00000040 | 0x00000800; // PICKFOLDERS, FORCEFILESYSTEM, PATHMUSTEXIST
                    dialog.SetOptions(options);
                    if (!string.IsNullOrEmpty(title)) dialog.SetTitle(title);

                    int hr = dialog.Show(IntPtr.Zero);
                    // 0 为 S_OK，0x800704C7 为取消
                    if (hr == 0)
                    {
                        dialog.GetResult(out var shellItem);
                        string path;
                        shellItem.GetDisplayName(SIGDN.FILESYSPATH, out path);
                        return path;
                    }
                    return null;
                }
                catch
                {
                    // 回退到旧对话框
                    return null;
                }
                finally
                {
                    if (dialog != null) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dialog);
                }
            });
        }

        /// <summary>
        /// 异步显示文件选择对话框
        /// </summary>
        /// <param name="filter">文件过滤器</param>
        /// <param name="title">对话框标题</param>
        /// <returns>选择的文件路径，如果取消则返回null</returns>
        public static async Task<string> ShowOpenFileDialogAsync(string filter = "可执行文件 (*.exe)|*.exe|动态链接库 (*.dll)|*.dll|所有文件 (*.*)|*.*", 
            string title = "选择文件")
        {
            return await Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Filter = filter,
                        Title = title
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        return openFileDialog.FileName;
                    }
                    return null;
                });
            });
        }

        /// <summary>
        /// 异步显示消息框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <param name="button">按钮类型</param>
        /// <param name="icon">图标类型</param>
        /// <returns>消息框结果</returns>
        public static async Task<MessageBoxResult> ShowMessageBoxAsync(string message, string title = "提示", 
            MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(message, title, button, icon);
            });
        }

        /// <summary>
        /// 异步显示选择文件夹对话框
        /// </summary>
        /// <param name="description">描述</param>
        /// <returns>选择的文件夹路径，取消返回null</returns>
        public static async Task<string> ShowSelectFolderDialogAsync(string description = "选择输出目录")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                using (var dialog = new WF.FolderBrowserDialog())
                {
                    dialog.Description = description;
                    dialog.ShowNewFolderButton = true;
                    var result = dialog.ShowDialog();
                    if (result == WF.DialogResult.OK)
                    {
                        return dialog.SelectedPath;
                    }
                    return null;
                }
            });
        }

        /// <summary>
        /// 异步更新ComboBox项目
        /// </summary>
        /// <param name="comboBox">ComboBox控件</param>
        /// <param name="items">项目列表</param>
        /// <param name="clearFirst">是否先清空</param>
        public static async Task UpdateComboBoxAsync(ComboBox comboBox, IEnumerable<string> items, bool clearFirst = true)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (clearFirst)
                {
                    comboBox.Items.Clear();
                }

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        comboBox.Items.Add(item);
                    }
                }
            });
        }

        /// <summary>
        /// 异步设置TextBox文本
        /// </summary>
        /// <param name="textBox">TextBox控件</param>
        /// <param name="text">文本内容</param>
        public static async Task SetTextBoxTextAsync(TextBlock textBox, string text)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                textBox.Text = text;
            });
        }

        /// <summary>
        /// 异步设置ComboBox选中项
        /// </summary>
        /// <param name="comboBox">ComboBox控件</param>
        /// <param name="selectedItem">选中项</param>
        public static async Task SetComboBoxSelectedItemAsync(ComboBox comboBox, object selectedItem)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                comboBox.SelectedItem = selectedItem;
            });
        }

        /// <summary>
        /// 异步清空ComboBox
        /// </summary>
        /// <param name="comboBox">ComboBox控件</param>
        public static async Task ClearComboBoxAsync(ComboBox comboBox)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                comboBox.Items.Clear();
            });
        }

        /// <summary>
        /// 异步添加ComboBox项目
        /// </summary>
        /// <param name="comboBox">ComboBox控件</param>
        /// <param name="item">项目</param>
        public static async Task AddComboBoxItemAsync(ComboBox comboBox, string item)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                comboBox.Items.Add(item);
            });
        }

        /// <summary>
        /// 检查ComboBox选中项是否为提示项（以括号开头）
        /// </summary>
        /// <param name="comboBox">ComboBox控件</param>
        /// <returns>是否为提示项</returns>
        public static bool IsComboBoxSelectedItemPlaceholder(ComboBox comboBox)
        {
            if (comboBox.SelectedItem == null) return true;
            
            string selectedItem = comboBox.SelectedItem.ToString();
            return selectedItem.StartsWith("(");
        }

        /// <summary>
        /// 获取ComboBox选中项文本
        /// </summary>
        /// <param name="comboBox">ComboBox控件</param>
        /// <returns>选中项文本，如果未选中则返回null</returns>
        public static string GetComboBoxSelectedItemText(ComboBox comboBox)
        {
            return comboBox.SelectedItem?.ToString();
        }

        /// <summary>
        /// 异步设置控件可见性
        /// </summary>
        /// <param name="control">控件</param>
        /// <param name="visibility">可见性</param>
        public static async Task SetControlVisibilityAsync(Control control, Visibility visibility)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                control.Visibility = visibility;
            });
        }

        /// <summary>
        /// 异步设置控件启用状态
        /// </summary>
        /// <param name="control">控件</param>
        /// <param name="isEnabled">是否启用</param>
        public static async Task SetControlEnabledAsync(Control control, bool isEnabled)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                control.IsEnabled = isEnabled;
            });
        }

        /// <summary>
        /// 异步显示错误消息
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="title">标题</param>
        public static async Task ShowErrorAsync(string message, string title = "错误")
        {
            await ShowMessageBoxAsync(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 异步显示警告消息
        /// </summary>
        /// <param name="message">警告消息</param>
        /// <param name="title">标题</param>
        public static async Task ShowWarningAsync(string message, string title = "警告")
        {
            await ShowMessageBoxAsync(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// 异步显示信息消息
        /// </summary>
        /// <param name="message">信息消息</param>
        /// <param name="title">标题</param>
        public static async Task ShowInfoAsync(string message, string title = "信息")
        {
            await ShowMessageBoxAsync(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 异步显示确认对话框
        /// </summary>
        /// <param name="message">确认消息</param>
        /// <param name="title">标题</param>
        /// <returns>是否确认</returns>
        public static async Task<bool> ShowConfirmAsync(string message, string title = "确认")
        {
            var result = await ShowMessageBoxAsync(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }

    #region IFileDialog COM interop
    [System.Runtime.InteropServices.ComImport]
    [System.Runtime.InteropServices.Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
    internal class FileOpenDialogRCW { }

    [System.Runtime.InteropServices.ComImport]
    [System.Runtime.InteropServices.Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
    [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileDialog
    {
        // IModalWindow
        [System.Runtime.InteropServices.PreserveSig]
        int Show(IntPtr parent);

        // IFileDialog specific (only what we need)
        void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(uint fos);
        void GetOptions(out uint pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszName);
        void GetFileName([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
        void AddPlace(IShellItem psi, uint fdap);
        void SetDefaultExtension([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close(int hr);
        void SetClientGuid(ref Guid guid);
        void ClearClientData();
        void SetFilter(IntPtr pFilter);
    }

    internal enum SIGDN : uint
    {
        FILESYSPATH = 0x80058000,
    }

    [System.Runtime.InteropServices.ComImport]
    [System.Runtime.InteropServices.Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(SIGDN sigdnName, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }
    #endregion
}
