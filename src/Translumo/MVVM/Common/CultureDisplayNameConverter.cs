using System;
using System.Globalization;
using System.Windows.Data;

namespace Translumo.MVVM.Common
{
    /// <summary>
    /// Converts a CultureInfo to its native language name for display in the
    /// language switcher (e.g. 中文 / 繁體中文 / 日本語 / English / Русский).
    /// </summary>
    [ValueConversion(typeof(CultureInfo), typeof(string))]
    public class CultureDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CultureInfo ci)
            {
                switch (ci.Name)
                {
                    case "en-US": return "English";
                    case "ru-RU": return "Русский";
                    case "zh-CN": return "中文";
                    case "zh-TW": return "繁體中文";
                    case "ja-JP": return "日本語";
                    default: return ci.NativeName;
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
