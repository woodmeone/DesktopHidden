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

        private string? _originalPath; // 原始桌面路径
        public string? OriginalPath
        {
            get => _originalPath;
            set => SetProperty(ref _originalPath, value);
        }

        private string? _hiddenStoragePath; // 隐藏存储中的路径
        public string? HiddenStoragePath
        {
            get => _hiddenStoragePath;
            set => SetProperty(ref _hiddenStoragePath, value);
        }

        public ShortcutModel()
        {
            _path = string.Empty; // 初始化
            _name = string.Empty; // 初始化
            _icon = new BitmapImage(); // 初始化
            _originalPath = null; // 可以为null
            _hiddenStoragePath = null; // 初始化
        }
    }
}
