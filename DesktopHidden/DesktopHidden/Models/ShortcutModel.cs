using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopHidden.Models
{
    public partial class ShortcutModel : ObservableObject
    {
        private string _path;
        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private BitmapImage _icon;
        public BitmapImage Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        private string? _originalPath; // 添加原始快捷方式路径，用于隐藏和恢复，可以为null
        public string? OriginalPath
        {
            get => _originalPath;
            set => SetProperty(ref _originalPath, value);
        }

        public ShortcutModel()
        {
            _path = string.Empty; // 初始化
            _name = string.Empty; // 初始化
            _icon = new BitmapImage(); // 初始化
            _originalPath = null; // 可以为null
        }
    }
}
