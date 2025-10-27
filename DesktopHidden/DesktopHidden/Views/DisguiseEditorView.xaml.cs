using Microsoft.UI.Xaml.Controls;
using DesktopHidden.SystemIntegration; // 添加引用
using Microsoft.UI.Xaml.Media.Imaging; // 添加引用
using System;

namespace DesktopHidden.Views
{
    public sealed partial class DisguiseEditorView : UserControl
    {
        public DisguiseEditorView()
        {
            this.InitializeComponent();
            SetWallpaperAsBackground();
        }

        private void SetWallpaperAsBackground()
        {
            string wallpaperPath = Win32WindowUtility.GetDesktopWallpaperPath();
            if (!string.IsNullOrEmpty(wallpaperPath))
            {
                try
                {
                    // 使用 BitmapImage 加载图片
                    BitmapImage bitmapImage = new BitmapImage(new Uri(wallpaperPath));
                    // 将图片设置为 Grid 的背景
                    (Content as Grid).Background = new Microsoft.UI.Xaml.Media.ImageBrush { ImageSource = bitmapImage };
                }
                catch (Exception ex)
                {
                    // 错误处理，例如记录日志
                    System.Diagnostics.Debug.WriteLine($"无法加载壁纸: {ex.Message}");
                }
            }
        }
    }
}
