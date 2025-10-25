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

namespace DesktopHidden.Views
{
    public sealed partial class SubZoneView : UserControl
    {
        public SubZoneView()
        {
            this.InitializeComponent();
        }

        public SubZoneModel SubZoneModel => DataContext as SubZoneModel;

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

        private void DragResizeGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (SubZoneModel != null && SubZoneModel.IsLocked) return; // 锁定状态下不允许拖拽和调整大小
        }

        private void DragResizeGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (SubZoneModel != null && SubZoneModel.IsLocked) return; // 锁定状态下不允许拖拽和调整大小

            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                Windows.Foundation.Point mousePosition = e.GetCurrentPoint(DragResizeGrid).Position;
                // ResizeDirection direction = GetResizeDirection(mousePosition);
                // The logic for changing cursor should be in SubZoneWindow
            }
        }

        private void DragResizeGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // PointerReleased 事件主要用于结束拖拽和调整大小状态，
            // 由于我们使用系统级方法，这里不需要额外的逻辑来停止操作，
            // 只需要确保鼠标光标恢复正常即可。
            // CustomInputCursor.SetCursor(InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 1)));
        }
    }

    // 辅助类用于向上遍历Visual Tree查找特定类型的父级
    /*
    public static class VisualTreeExtensions
    {
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }
    }
    */
}
