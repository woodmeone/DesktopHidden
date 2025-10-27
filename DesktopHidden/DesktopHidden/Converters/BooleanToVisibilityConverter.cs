using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace DesktopHidden.Converters
{
    // BooleanToVisibilityConverter 用于将布尔值转换为 Visibility 枚举。
    // 当值为 true 时，返回 Visibility.Visible，否则返回 Visibility.Collapsed。
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isVisible)
            {
                return isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible; // 默认可见
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
