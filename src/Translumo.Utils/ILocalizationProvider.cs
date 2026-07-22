namespace Translumo.Utils
{
    /// <summary>
    /// Provides access to localized string resources from any assembly
    /// (including those that cannot reference the WPF host application).
    /// </summary>
    public interface ILocalizationProvider
    {
        string GetValue(string key);
    }
}
