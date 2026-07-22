using System;
using System.Globalization;
using System.Windows.Data;
using Translumo.Translation.Llm;
using Translumo.Utils;

namespace Translumo.MVVM.Common
{
    /// <summary>
    /// Converts a <see cref="LlmProvider"/> enum value into a localized, user-friendly name.
    /// Well-known brand providers keep their English names; <c>Custom</c> is localized
    /// (e.g. "自定义模型").
    /// </summary>
    [ValueConversion(typeof(LlmProvider), typeof(string))]
    public class LlmProviderNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LlmProvider provider)
            {
                if (provider == LlmProvider.Custom)
                {
                    return LocalizationManager.GetValue("Str.LlmSettings.Provider_Custom") ?? provider.ToString();
                }

                return provider.ToString();
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
