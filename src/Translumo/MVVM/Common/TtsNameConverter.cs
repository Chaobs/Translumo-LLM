using System;
using System.Globalization;
using System.Windows.Data;
using Translumo.TTS;
using Translumo.Utils;

namespace Translumo.MVVM.Common
{
    /// <summary>
    /// Converts a <see cref="TTSEngines"/> enum value into a localized, user-friendly name.
    /// </summary>
    [ValueConversion(typeof(TTSEngines), typeof(string))]
    public class TtsNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TTSEngines engine)
            {
                return LocalizationManager.GetValue($"Str.Tts.{engine}") ?? engine.ToString();
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
