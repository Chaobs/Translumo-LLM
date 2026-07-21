using System;
using System.Globalization;
using System.Windows.Data;

namespace Translumo.MVVM.Common
{
    /// <summary>
    /// Converts a CultureInfo to a short abbreviation for the language switcher
    /// (EN / RU / 简中 / 繁中 / 日語) to keep the switcher compact.
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
                    case "en-US": return "EN";
                    case "ru-RU": return "RU";
                    case "zh-CN": return "简中";
                    case "zh-TW": return "繁中";
                    case "ja-JP": return "日語";
                    default: return ci.TwoLetterISOLanguageName.ToUpper(culture);
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
