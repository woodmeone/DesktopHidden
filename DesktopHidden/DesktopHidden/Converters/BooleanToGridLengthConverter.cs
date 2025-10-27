using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace DesktopHidden.Converters
{
    // BooleanToGridLengthConverter 用于将布尔值转换为 GridLength。
    // 当 IsContentVisible 为 true 时，返回 "*" (占据剩余空间)，否则返回 "0" (不占据空间)。
    public class BooleanToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isContentVisible)
            {
                return isContentVisible ? new GridLength(1, GridUnitType.Star) : new GridLength(0); // 如果可见则占据所有剩余空间，否则为0
            }
            return new GridLength(1, GridUnitType.Star); // 默认可见
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
