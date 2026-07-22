using Translumo.Utils;

namespace Translumo.Services
{
    /// <summary>
    /// Bridges <see cref="ILocalizationProvider"/> to the WPF-hosted
    /// <see cref="LocalizationManager"/> so non-UI assemblies can resolve
    /// localized strings without taking a dependency on the host application.
    /// </summary>
    public class LocalizationProvider : ILocalizationProvider
    {
        public string GetValue(string key)
        {
            return LocalizationManager.GetValue(key);
        }
    }
}
