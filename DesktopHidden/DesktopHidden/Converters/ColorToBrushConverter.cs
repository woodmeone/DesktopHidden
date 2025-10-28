using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.UI;
using Microsoft.UI; // 添加此行

namespace DesktopHidden.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color); // 将 Color 转换为 SolidColorBrush
            }
            return new SolidColorBrush(Colors.Transparent); // 默认返回透明画刷
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        {
            throw new NotImplementedException(); // 反向转换不需要实现
        }
    }
}
