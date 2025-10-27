using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;
using System;

namespace DesktopHidden.Converters
{
    // BooleanToLockIconConverter 用于将布尔值转换为锁定/解锁图标的 Glyph 值。
    // 当 IsLocked 为 true 时，显示锁定图标 (&#xE72E;)，否则显示解锁图标 (&#xE785;)。
    public class BooleanToLockIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isLocked)
            {
                return isLocked ? "\uE72E" : "\uE785"; // &#xE72E; (Locked) : &#xE785; (Unlocked)
            }
            return "\uE72E"; // 默认锁定图标
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
