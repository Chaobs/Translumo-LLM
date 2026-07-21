using System;
using System.Globalization;
using System.Windows.Data;
using Translumo.Translation;
using Translumo.Utils;

namespace Translumo.MVVM.Common
{
    /// <summary>
    /// Converts a <see cref="Translators"/> enum value into a localized, user-friendly name.
    /// </summary>
    [ValueConversion(typeof(Translators), typeof(string))]
    public class TranslatorNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Translators translator)
            {
                return LocalizationManager.GetValue($"Str.Translator.{translator}") ?? translator.ToString();
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
