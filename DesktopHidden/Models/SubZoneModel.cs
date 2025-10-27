using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using System.ComponentModel; // 添加这个命名空间
using System.Runtime.CompilerServices; // 添加这个命名空间

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
        public Size Size { get; set; }
        // 子区背景颜色，默认是45%透明度的黑色。例如：Color.FromArgb(115, 0, 0, 0) 表示透明度为115（约45%），RGB为0,0,0（黑色）。
        public Color BackgroundColor { get; set; } = Color.FromArgb(115, 0, 0, 0);
        // 子区整体不透明度，默认为1.0（完全不透明）。由于背景颜色已包含透明度，这里设置为1.0以避免双重透明。
        public double Opacity { get; set; } = 1.0;

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
            Size = size;
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
