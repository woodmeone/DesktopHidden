using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using System.ComponentModel; // 添加这个命名空间
using System.Runtime.CompilerServices; // 添加这个命名空间
using Microsoft.UI; // 将 Microsoft.UI.Xaml.Media 替换为 Microsoft.UI
using Microsoft.UI.Xaml.Media; // 添加这个命名空间

namespace DesktopHidden.Models
{
    public class SubZoneModel : INotifyPropertyChanged // 实现 INotifyPropertyChanged 接口
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // 用于通知属性变更的方法
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public Point Position { get; set; }
        public Size Size { get; set; } // 子区当前显示的尺寸
        public Size OriginalSize { get; set; } // 子区原始的完整尺寸，用于恢复内容区域可见时的尺寸
        private SolidColorBrush _backgroundColor = new SolidColorBrush(Colors.Black); // 子区背景颜色，默认是黑色。
        public SolidColorBrush BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    OnPropertyChanged();
                }
            }
        }
        // 子区整体不透明度，默认为1.0（完全不透明）。由于背景颜色已包含透明度，这里设置为1.0以避免双重透明。
        public double Opacity { get; set; } = 0.45; // 子区整体不透明度，默认为0.45（45%透明）。

        private bool _isLocked;
        // 子区是否被锁定，锁定后不能移动或改变大小。
        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked != value)
                {
                    _isLocked = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isContentVisible;
        // 子区内容是否可见，用于控制内容区域的显示/隐藏。
        public bool IsContentVisible
        {
            get => _isContentVisible;
            set
            {
                if (_isContentVisible != value)
                {
                    _isContentVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<AppShortcutModel> Shortcuts { get; set; } = new List<AppShortcutModel>();

        public SubZoneModel()
        {
            // 默认构造函数
            _isContentVisible = true; // 确保默认值被设置并通过属性通知
        }

        public SubZoneModel(Point position, Size size)
        {
            Position = position;
            Size = size; // 初始时，当前尺寸和原始尺寸相同
            OriginalSize = size;
            _isContentVisible = true; // 确保默认值被设置并通过属性通知
        }
    }

    // AppShortcutModel 会在后续步骤中定义
    public class AppShortcutModel
    {
        public required string Path { get; set; }
        public required string IconPath { get; set; }
        public required string Name { get; set; }
    }
}
