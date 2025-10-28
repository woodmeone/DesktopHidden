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
using Windows.Graphics; // 添加此行，用于 RectInt32

namespace DesktopHidden.Views
{
    public sealed partial class SubZoneView : UserControl
    {
        public SubZoneWindow? ParentWindow { get; set; } // 添加 SubZoneWindow 引用，声明为可空

        private bool _isDraggingShortcut = false; // 是否正在拖拽快捷方式
        private Windows.Foundation.Point _dragStartPoint; // 拖拽起始点
        private ShortcutModel? _draggedShortcutModel; // 被拖拽的快捷方式模型
        private StackPanel? _originalPanel; // 被拖拽快捷方式的原始 StackPanel
        private UIElement? _draggedElement; // 拖拽时显示的元素 (例如，一个“鬼影”图标)
        private Window? _overlayWindow; // 用于显示拖拽元素的覆盖窗口

        private const double DRAG_THRESHOLD = 5.0; // 拖拽阈值，鼠标移动超过此距离才认为是拖拽

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
                    string originalDroppedPath = item.Path;
                    System.Diagnostics.Debug.WriteLine($"Drop event: originalDroppedPath = {originalDroppedPath}");

                    bool isShortcutFile = System.IO.Path.GetExtension(originalDroppedPath).Equals(".lnk", StringComparison.OrdinalIgnoreCase);
                    System.Diagnostics.Debug.WriteLine($"Drop event: Is '{originalDroppedPath}' a .lnk file? {isShortcutFile}. Extension: '{System.IO.Path.GetExtension(originalDroppedPath)}'");

                    // 尝试隐藏原始桌面项目，并获取其在隐藏存储中的新路径
                    string? hiddenStoragePath = SystemIntegration.Win32WindowUtility.HideDesktopItem(originalDroppedPath);
                    System.Diagnostics.Debug.WriteLine($"Drop event: hiddenStoragePath (after HideDesktopItem) = {hiddenStoragePath}");

                    if (!string.IsNullOrEmpty(hiddenStoragePath))
                    {
                        string targetExecutablePath;
                        string name;
                        BitmapImage icon;

                        if (isShortcutFile)
                        {
                            // 对于快捷方式，Path 应该指向其目标程序
                            // 注意：这里的 targetExecutablePath 仍然是解析原始桌面快捷方式得到的，而不是隐藏存储中的快捷方式的目标。
                            targetExecutablePath = SystemIntegration.ShortcutParser.GetTargetOfShortcut(originalDroppedPath);
                            name = System.IO.Path.GetFileNameWithoutExtension(targetExecutablePath); // 获取目标程序文件名作为名称
                            icon = await GetAppIcon(targetExecutablePath); // 获取目标程序的图标
                            System.Diagnostics.Debug.WriteLine($"Drop event: Parsed targetExecutablePath for shortcut = {targetExecutablePath}");
                        }
                        else
                        {
                            // 对于非快捷方式文件，Path 应该指向它在隐藏存储中的实际位置
                            targetExecutablePath = hiddenStoragePath; // 修正点：Path 指向隐藏存储中的文件
                            name = System.IO.Path.GetFileNameWithoutExtension(originalDroppedPath); // 获取原始文件名作为名称
                            icon = await GetAppIcon(hiddenStoragePath); // 获取移动后文件的图标
                            System.Diagnostics.Debug.WriteLine($"Drop event: Non-shortcut file. targetExecutablePath = {targetExecutablePath}");
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Drop event: name (for ShortcutModel) = {name}");

                        var shortcut = new ShortcutModel
                        {
                            Path = targetExecutablePath,        // 双击时实际打开的路径
                            Name = name,
                            Icon = icon,
                            OriginalPath = originalDroppedPath, // 原始桌面项目的路径
                            HiddenStoragePath = hiddenStoragePath // 原始桌面项目被移动后的隐藏路径
                        };
                        System.Diagnostics.Debug.WriteLine($"Drop event: ShortcutModel created. Path: {shortcut.Path}, Name: {shortcut.Name}, OriginalPath: {shortcut.OriginalPath}, HiddenStoragePath: {shortcut.HiddenStoragePath}");

                        // 添加快捷方式到 SubZoneModel
                        SubZoneModel?.Shortcuts.Add(shortcut);
                        System.Diagnostics.Debug.WriteLine($"Added shortcut to SubZone: {shortcut.Name} ({shortcut.Path}). SubZone.Shortcuts count: {SubZoneModel?.Shortcuts.Count}");

                        // 添加到全局映射列表
                        App.HiddenShortcutMappings.Add(new Tuple<string, string>(shortcut.OriginalPath, shortcut.HiddenStoragePath));
                        System.Diagnostics.Debug.WriteLine($"Desktop item hidden: {shortcut.OriginalPath} moved to {shortcut.HiddenStoragePath}. Global hidden list count: {App.HiddenShortcutMappings.Count}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to hide desktop item: {originalDroppedPath}");
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
                System.Diagnostics.Debug.WriteLine($"DoubleTapped event: Attempting to open shortcut: {shortcut.Name}, Path: {shortcut.Path}");
                try
                {
                    // 使用 Process.Start 启动程序
                    Process.Start(new ProcessStartInfo(shortcut.Path) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error launching shortcut {shortcut.Name} at {shortcut.Path}: {ex.Message}");
                    // 可以在这里显示一个错误消息给用户
                }
            }
        }

        // 处理快捷方式删除按钮的点击事件
        private void ShortcutDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ShortcutModel shortcut)
            {
                // 确保 SubZoneModel 存在且内容可见
                if (SubZoneModel != null && SubZoneModel.IsContentVisible)
                {
                    // 调用 SubZoneModel 中的移除快捷方式方法
                    SubZoneModel.RemoveShortcut(shortcut);
                    System.Diagnostics.Debug.WriteLine($"Removed shortcut: {shortcut.Name}");
                }
            }
            // 阻止事件冒泡到父级元素，防止触发其他事件（如双击打开程序）
            // 注意: RoutedEventArgs 没有 Handled 属性，通常点击事件本身就被视为已处理。
            // 如果后续出现点击删除按钮导致父级元素的 DoubleTapped 事件触发，需要重新考虑处理方式。
            // e.Handled = true; // 移除此行以解决编译错误
        }

        // 处理快捷方式按下事件
        private void ShortcutItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"ShortcutItem_PointerPressed triggered. SubZoneModel.IsLocked: {SubZoneModel?.IsLocked}");
            if (SubZoneModel != null && SubZoneModel.IsLocked) return; // 锁定状态下不允许拖拽

            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse && e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed)
            {
                _dragStartPoint = e.GetCurrentPoint(sender as UIElement).Position; // 记录起始点
                _isDraggingShortcut = false; // 初始设置为未拖拽状态
                _originalPanel = sender as StackPanel; // 获取原始的 StackPanel
                _draggedShortcutModel = _originalPanel?.DataContext as ShortcutModel; // 获取被拖拽的快捷方式模型
                System.Diagnostics.Debug.WriteLine($"PointerPressed: Drag start point: {_dragStartPoint}, Shortcut: {_draggedShortcutModel?.Name}");

                if (_originalPanel != null)
                {
                    _originalPanel.CapturePointer(e.Pointer); // 捕获指针，确保后续事件由它处理
                    System.Diagnostics.Debug.WriteLine("Pointer captured.");
                }
            }
        }

        // 处理快捷方式移动事件
        private async void ShortcutItem_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (SubZoneModel != null && SubZoneModel.IsLocked) return; // 锁定状态下不允许拖拽

            if (_draggedShortcutModel != null && e.Pointer.PointerDeviceType == PointerDeviceType.Mouse && e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed)
            {
                Windows.Foundation.Point currentPoint = e.GetCurrentPoint(sender as UIElement).Position;
                // 计算拖拽距离
                double distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) + Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));
                System.Diagnostics.Debug.WriteLine($"PointerMoved: Current point: {currentPoint}, Distance: {distance}, IsDragging: {_isDraggingShortcut}");

                if (distance > DRAG_THRESHOLD && !_isDraggingShortcut) // 超过阈值且未开始拖拽，则开始拖拽
                {
                    _isDraggingShortcut = true;
                    System.Diagnostics.Debug.WriteLine($"开始拖拽快捷方式: {_draggedShortcutModel.Name}");

                    // 创建一个视觉副本作为拖拽反馈
                    _draggedElement = CreateDragVisual(_originalPanel, _draggedShortcutModel); // 创建拖拽视觉元素

                    // 创建一个覆盖窗口来显示拖拽元素
                    _overlayWindow = new Window();
                    _overlayWindow.Content = new Grid { Children = { _draggedElement } };
                    _overlayWindow.AppWindow.IsShownInSwitchers = false; // 不在 Alt+Tab 中显示
                    _overlayWindow.AppWindow.SetIcon("Assets/StoreLogo.png"); // 设置一个图标，尽管不会显示

                    // 设置覆盖窗口的透明度和置顶，使其看起来像一个悬浮的拖拽元素
                    IntPtr overlayHwnd = WindowNative.GetWindowHandle(_overlayWindow);
                    Win32WindowUtility.SetWindowTransparent(overlayHwnd); // 设置透明
                    Win32WindowUtility.SetWindowTopmost(overlayHwnd); // 置顶

                    _overlayWindow.Activate();
                    UpdateDragVisualPosition(e.GetCurrentPoint(null).Position); // 初始位置
                    System.Diagnostics.Debug.WriteLine("Overlay window activated and initial position set.");
                }

                if (_isDraggingShortcut && _draggedElement != null && _overlayWindow != null)
                {
                    // 更新拖拽元素的位置
                    UpdateDragVisualPosition(e.GetCurrentPoint(null).Position); // 获取屏幕坐标
                    System.Diagnostics.Debug.WriteLine($"Updating drag visual position to: {e.GetCurrentPoint(null).Position}");
                }
            }
        }

        // 处理快捷方式释放事件
        private void ShortcutItem_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"ShortcutItem_PointerReleased triggered. IsDragging: {_isDraggingShortcut}");
            if (SubZoneModel != null && SubZoneModel.IsLocked) return; // 锁定状态下不允许拖拽

            if (_isDraggingShortcut && _draggedShortcutModel != null)
            {
                System.Diagnostics.Debug.WriteLine($"结束拖拽快捷方式: {_draggedShortcutModel.Name}");

                // 获取鼠标释放时的屏幕坐标
                Windows.Foundation.Point releasePosition = e.GetCurrentPoint(null).Position;
                System.Diagnostics.Debug.WriteLine($"Release position (screen coords): {releasePosition}");

                // 判断是否拖拽到子区外部（例如桌面）
                // 这里的判断逻辑可以根据实际需求调整，例如：检查释放点是否在所有 SubZoneWindow 之外
                bool droppedOutsideSubZone = true; // 简化判断，假设释放到子区外部即为拖出
                System.Diagnostics.Debug.WriteLine($"Dropped outside subzone (simplified): {droppedOutsideSubZone}");

                if (droppedOutsideSubZone)
                {
                    // 从 SubZoneModel 中移除快捷方式，这将自动调用 ShowFile 恢复原始文件
                    SubZoneModel?.RemoveShortcut(_draggedShortcutModel);
                    System.Diagnostics.Debug.WriteLine($"快捷方式 {_draggedShortcutModel.Name} 已拖出到桌面并恢复显示。");
                }

                // 清理拖拽状态和元素
                _isDraggingShortcut = false;
                _draggedShortcutModel = null;
                _originalPanel?.ReleasePointerCaptures(); // 释放指针捕获
                System.Diagnostics.Debug.WriteLine("Drag state cleaned up. Pointer captures released.");

                if (_overlayWindow != null)
                {
                    _overlayWindow.Close(); // 关闭覆盖窗口
                    _overlayWindow = null;
                    System.Diagnostics.Debug.WriteLine("Overlay window closed.");
                }
                _draggedElement = null;
            }
            else if (_originalPanel != null) // 如果没有开始拖拽，但有捕获，则释放
            {
                _originalPanel.ReleasePointerCaptures();
                System.Diagnostics.Debug.WriteLine("Pointer captures released (no drag started).");
            }
        }

        /// <summary>
        /// 创建一个视觉副本作为拖拽时的反馈。
        /// </summary>
        /// <param name="originalElement">原始的 UI 元素。</param>
        /// <param name="shortcut">对应的快捷方式模型。</param>
        /// <returns>用于拖拽反馈的 UI 元素。</returns>
        private UIElement CreateDragVisual(StackPanel? originalElement, ShortcutModel shortcut)
        {
            // 创建一个半透明的 Grid 作为拖拽时的视觉反馈
            var dragVisual = new Grid
            {
                Width = 60, // 与原始快捷方式大小一致
                Height = 80,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Gray) { Opacity = 0.5 }, // 半透明灰色背景
                CornerRadius = new CornerRadius(5) // 圆角
            };

            // 添加图标和文本
            var image = new Image
            {
                Source = shortcut.Icon,
                Width = 32,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(image, 0);

            var textBlock = new TextBlock
            {
                Text = shortcut.Name,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 10,
                Margin = new Thickness(0, 5, 0, 0),
                MaxLines = 2
            };
            Grid.SetRow(textBlock, 1);

            dragVisual.Children.Add(image);
            dragVisual.Children.Add(textBlock);

            // 设置 Canvas.Left 和 Canvas.Top，以便后续定位
            Canvas.SetLeft(dragVisual, 0);
            Canvas.SetTop(dragVisual, 0);

            return dragVisual;
        }

        /// <summary>
        /// 更新拖拽视觉元素的位置。
        /// </summary>
        /// <param name="screenPosition">鼠标当前的屏幕坐标。</param>
        private void UpdateDragVisualPosition(Windows.Foundation.Point screenPosition)
        {
            if (_overlayWindow != null && _draggedElement != null)
            {
                // 计算覆盖窗口的左上角位置
                // 使拖拽元素的中心与鼠标指针对齐
                int overlayX = (int)(screenPosition.X - _draggedElement.ActualSize.X / 2);
                int overlayY = (int)(screenPosition.Y - _draggedElement.ActualSize.Y / 2);

                // 确保在窗口被激活并渲染后才能获取 ActualSize
                if (_draggedElement.ActualSize.X == 0 || _draggedElement.ActualSize.Y == 0)
                {
                    _overlayWindow.Content.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                    _overlayWindow.Content.Arrange(new Rect(0, 0, 60, 80)); // 假设拖拽元素大小为 60x80
                    overlayX = (int)(screenPosition.X - _draggedElement.ActualSize.X / 2);
                    overlayY = (int)(screenPosition.Y - _draggedElement.ActualSize.Y / 2);
                }

                // 移动覆盖窗口
                _overlayWindow.AppWindow.Move(new PointInt32(overlayX, overlayY));
            }
        }
    }
}
