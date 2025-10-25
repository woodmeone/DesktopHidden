using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;

namespace DesktopHidden.Models
{
    public class SubZoneModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Point Position { get; set; }
        public Size Size { get; set; }
        public Color BackgroundColor { get; set; } = Color.FromArgb(255, 0, 0, 0); // 默认黑色，使用 Argb
        public double Opacity { get; set; } = 0.45; // 默认45%透明度
        public bool IsLocked { get; set; }
        public bool IsContentVisible { get; set; } = true; // 默认内容区域显示
        public List<AppShortcutModel> Shortcuts { get; set; } = new List<AppShortcutModel>();

        public SubZoneModel()
        {
            // 默认构造函数
        }

        public SubZoneModel(Point position, Size size)
        {
            Position = position;
            Size = size;
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
