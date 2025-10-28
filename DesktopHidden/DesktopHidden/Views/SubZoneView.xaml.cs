using DesktopHidden.Models;
using DesktopHidden.SystemIntegration;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.InteropServices; // Explicitly add this
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core; // Add this for CoreCursorType
using WinRT.Interop;
using Microsoft.UI.Xaml.Media; // Add this for VisualTreeHelper
using DesktopHidden.Converters; // Add this using statement
using System.IO; // 添加此行
using System.Diagnostics; // 添加此行，用于 Debug.WriteLine
using Microsoft.UI.Xaml.Media.Imaging; // 添加此行
using Windows.Storage;
using Windows.Storage.FileProperties;
using System.Threading.Tasks; // 添加此行

namespace DesktopHidden.Views
{
    public sealed partial class SubZoneView : UserControl
    {
        public SubZoneWindow? ParentWindow { get; set; } // 添加 SubZoneWindow 引用，声明为可空

        public SubZoneView(SubZoneWindow? parentWindow) // 修改构造函数，声明为可空
        {
            this.InitializeComponent();
            ParentWindow = parentWindow;
        }

        public SubZoneModel? SubZoneModel => DataContext as SubZoneModel; // 声明为可空

        /*
        private const double ResizeBorder = 8; // 边缘宽度
        private const double CornerResizeBorder = 16; // 角落宽度

        private enum ResizeDirection
        {
            None,
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private ResizeDirection GetResizeDirection(Point position)
        {
            bool onLeft = position.X < ResizeBorder;
            bool onRight = position.X > ActualWidth - ResizeBorder;
            bool onTop = position.Y < ResizeBorder;
            bool onBottom = position.Y > ActualHeight - ResizeBorder;

            if (onTop && onLeft) return ResizeDirection.TopLeft;
            if (onTop && onRight) return ResizeDirection.TopRight;
            if (onBottom && onLeft) return ResizeDirection.BottomLeft;
            if (onBottom && onRight) return ResizeDirection.BottomRight;
            if (onLeft) return ResizeDirection.Left;
            if (onRight) return ResizeDirection.Right;
            if (onTop) return ResizeDirection.Top;
            if (onBottom) return ResizeDirection.Bottom;

            return ResizeDirection.None;
        }
        */

        private void NavBar_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (SubZoneModel != null && SubZoneModel.IsLocked) return; // 锁定状态下不允许拖拽
            // 调用 SubZoneWindow 的拖拽方法
            ParentWindow?.StartDragging(); // 使用 ParentWindow 属性
        }

        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            if (SubZoneModel != null)
            {
                SubZoneModel.IsLocked = !SubZoneModel.IsLocked; // 切换锁定状态
                ParentWindow?.SetResizable(!SubZoneModel.IsLocked); // 根据锁定状态设置窗口是否可调整大小
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 调用父窗口的 RequestClose 事件来请求关闭子区窗口。
            ParentWindow?.OnRequestClose();
        }

        private void ToggleContentButton_Click(object sender, RoutedEventArgs e)
        {
            if (SubZoneModel != null)
            {
                SubZoneModel.IsContentVisible = !SubZoneModel.IsContentVisible; // 切换内容区域显示状态
                // ContentArea.Visibility = SubZoneModel.IsContentVisible ? Visibility.Visible : Visibility.Collapsed; // 更新UI，通过GridLength Converter处理
            }
        }

        private void DisguiseButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建并显示抠图界面
            var disguiseWindow = new Window();
            disguiseWindow.Title = "伪装抠图";
            disguiseWindow.Content = new DisguiseEditorView();
            disguiseWindow.Activate();
        }

        // 处理拖动进入事件
        private void ContentArea_DragOver(object sender, DragEventArgs e)
        {
            // 检查拖动的数据是否包含文件
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy; // 允许复制操作
                e.Handled = true; // 标记事件已处理
            }
        }

        // 处理拖放事件
        private async void ContentArea_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    // 获取拖放文件的完整路径
                    string droppedPath = item.Path;
                    string targetPath = droppedPath;

                    // 如果是快捷方式文件 (.lnk)，解析其目标路径
                    if (Path.GetExtension(droppedPath).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                    {
                        targetPath = SystemIntegration.ShortcutParser.GetTargetOfShortcut(droppedPath);
                    }

                    if (!string.IsNullOrEmpty(targetPath))
                    {
                        string name = Path.GetFileNameWithoutExtension(targetPath); // 获取文件名作为名称
                        BitmapImage icon = await GetAppIcon(targetPath); // 获取程序图标

                        var shortcut = new ShortcutModel
                        {
                            Path = targetPath,
                            Name = name,
                            Icon = icon,
                            OriginalPath = Path.GetExtension(droppedPath).Equals(".lnk", StringComparison.OrdinalIgnoreCase) ? droppedPath : null // 记录原始快捷方式路径
                        };
                        // 添加快捷方式到 SubZoneModel
                        SubZoneModel?.Shortcuts.Add(shortcut);
                        Debug.WriteLine($"Added shortcut: {shortcut.Name} ({shortcut.Path})");

                        // 如果是快捷方式，则隐藏原始文件
                        if (!string.IsNullOrEmpty(shortcut.OriginalPath))
                        {
                            SystemIntegration.Win32WindowUtility.HideFile(shortcut.OriginalPath);
                            App.HiddenShortcutOriginalPaths.Add(shortcut.OriginalPath); // 添加到全局列表
                            Debug.WriteLine($"Hidden original shortcut: {shortcut.OriginalPath}");
                        }
                    }
                }
                e.Handled = true; // 标记事件已处理
            }
        }

        /// <summary>
        /// 获取指定路径程序的图标。
        /// </summary>
        /// <param name="filePath">程序文件路径。</param>
        /// <returns>程序的图标。</returns>
        public static async Task<BitmapImage?> GetAppIcon(string filePath) // 声明为可空
        {
            try
            {
                // 使用 StorageFile 获取图标
                StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
                StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 32);

                if (thumbnail != null)
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(thumbnail);
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting icon for {filePath}: {ex.Message}");
            }
            return null; // 返回 null 或默认图标
        }

        // 处理快捷方式图标的双击事件
        private void ShortcutItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ShortcutModel shortcut)
            {
                try
                {
                    // 使用 Process.Start 启动程序
                    Process.Start(new ProcessStartInfo(shortcut.Path) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error launching shortcut {shortcut.Name}: {ex.Message}");
                    // 可以在这里显示一个错误消息给用户
                }
            }
        }
    }
}
