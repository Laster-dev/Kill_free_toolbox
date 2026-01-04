using Kill_free_toolbox.Helper.Baijiahei;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kill_free_toolbox.Views.Controls
{
    /// <summary>
    /// WhiteFileSearchResultItem.xaml 的交互逻辑
    /// </summary>
    public partial class WhiteFileSearchResultItem : UserControl
    {
        public static readonly DependencyProperty SearchResultProperty =
            DependencyProperty.Register("SearchResult", typeof(WhiteFileSearchResult), 
                typeof(WhiteFileSearchResultItem), new PropertyMetadata(null, OnSearchResultChanged));

        public WhiteFileSearchResult SearchResult
        {
            get { return (WhiteFileSearchResult)GetValue(SearchResultProperty); }
            set { SetValue(SearchResultProperty, value); }
        }

        public WhiteFileSearchResultItem()
        {
            InitializeComponent();
        }

        private static void OnSearchResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WhiteFileSearchResultItem control && e.NewValue is WhiteFileSearchResult result)
            {
                control.UpdateUI(result);
            }
        }

        private void UpdateUI(WhiteFileSearchResult result)
        {
            try
            {
               
                // 更新文件路径
                FilePathText.Text = result.FilePath;
                
                // 更新架构
                ArchitectureText.Text = result.Architecture ?? "未知";

                // 更新DLL数量
                DllCountText.Text = $"非系统导出DLL数量：{result.NonSystemDllCount} 个";

                // 更新文件大小
                FileSizeText.Text = FormatFileSize(result.FileSize);
                
                // 更新文件图标
                UpdateFileIcon(result.FilePath);
                
                // 更新工具提示
                UpdateToolTip(result);
            }
            catch (Exception ex)
            {
                // 忽略UI更新错误
                System.Diagnostics.Debug.WriteLine($"更新UI时发生错误: {ex.Message}");
            }
        }

      

      

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"文件大小：{len:0.##} {sizes[order]}";
        }

        private void UpdateFileIcon(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // 尝试提取文件图标
                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(filePath);
                    if (icon != null)
                    {
                        using (var bitmap = icon.ToBitmap())
                        {
                            var hBitmap = bitmap.GetHbitmap();
                            try
                            {
                                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    hBitmap,
                                    IntPtr.Zero,
                                    System.Windows.Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                                
                                FileIcon.Source = bitmapSource;
                            }
                            finally
                            {
                                NativeMethods.DeleteObject(hBitmap);
                            }
                        }
                    }
                    else
                    {
                        // 使用默认图标
                        SetDefaultIcon();
                    }
                }
                else
                {
                    SetDefaultIcon();
                }
            }
            catch
            {
                SetDefaultIcon();
            }
        }

        private void SetDefaultIcon()
        {
            // 创建一个简单的默认图标
            var drawingGroup = new DrawingGroup();
            var geometryDrawing = new GeometryDrawing(
                new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                new Pen(new SolidColorBrush(Color.FromRgb(150, 150, 150)), 1),
                Geometry.Parse("M 2,2 L 14,2 L 14,14 L 2,14 Z M 4,4 L 12,4 L 12,12 L 4,12 Z"));
            
            drawingGroup.Children.Add(geometryDrawing);
            
            var drawingImage = new DrawingImage(drawingGroup);
            FileIcon.Source = drawingImage;
        }

        private void UpdateToolTip(WhiteFileSearchResult result)
        {
            try
            {
                ToolTipFileName.Text = result.FileName;
                ToolTipDlls.Text = $"可劫持DLL: {result.NonSystemDllsDisplay}";
            }
            catch
            {
            }
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 查找父级
            var listBox = FindParent<ListBox>(this);
            if (listBox != null && SearchResult != null)
            {
                listBox.SelectedItem = SearchResult;
            }
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            
            if (parentObject == null) return null;
            
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            public static extern bool DeleteObject(IntPtr hObject);
        }
    }
}
