using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;

namespace DesktopHidden.Models
{
    // 用于序列化的简化版 SubZoneModel
    public class SerializableSubZoneModel
    {
        public Guid Id { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }
        public Size OriginalSize { get; set; }
        public Color BackgroundColor { get; set; }
        public double Opacity { get; set; }
        public bool IsLocked { get; set; }
        public bool IsContentVisible { get; set; }
        public List<SerializableShortcutModel> Shortcuts { get; set; } = new List<SerializableShortcutModel>();
    }

    // 用于序列化的简化版 ShortcutModel
    public class SerializableShortcutModel
    {
        public required string Path { get; set; }
        public required string Name { get; set; }
        public required string? OriginalPath { get; set; } // 可以为null
        public required string? HiddenStoragePath { get; set; } // 添加此行，用于隐藏存储中的路径
    }
}
