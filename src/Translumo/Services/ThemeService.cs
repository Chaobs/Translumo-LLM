#nullable enable

using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Translumo.Utils;

namespace Translumo.Services
{
    public enum ThemeMode
    {
        Light = 0,
        Dark = 1
    }

    /// <summary>
    /// Runtime light/dark theme switcher for the settings UI. Keeps the App.xaml brush objects
    /// intact and mutates their <see cref="SolidColorBrush.Color"/> (WPF Freezable change
    /// notification propagates to every StaticResource reference), and swaps the
    /// MaterialDesignExtensions base theme dictionary. Preference is persisted to
    /// <c>config/theme.json</c> independently of <c>ConfigurationStorage</c>.
    /// </summary>
    public static class ThemeService
    {
        private static readonly string ConfigPath = Path.Combine(AppPaths.GetConfigDirectory(), "theme.json");

        public static ThemeMode Current { get; private set; } = ThemeMode.Light;

        /// <summary>Raised after a theme switch completes.</summary>
        public static event EventHandler<ThemeMode>? ThemeChanged;

        /// <summary>
        /// Brush keys whose Color changes between themes. The brush objects themselves are defined
        /// in App.xaml and never replaced, so every StaticResource reference stays valid and is
        /// notified through the Freezable change mechanism.
        /// </summary>
        private static readonly (string Key, Color Light, Color Dark)[] BrushPalette =
        {
            ("PrimaryHueLightBrush",   Color.FromRgb(0xfb, 0xff, 0xff), Color.FromRgb(0xea, 0xf4, 0xf0)),
            ("PrimaryHueMidBrush",     Color.FromRgb(0xc7, 0xf7, 0xff), Color.FromRgb(0x2f, 0xb9, 0x8a)),
            ("PrimaryHueDarkBrush",    Color.FromRgb(0x95, 0xc4, 0xcc), Color.FromRgb(0x5d, 0xca, 0xa5)),
            ("SecondaryHueLightBrush", Color.FromRgb(0xfb, 0xff, 0xff), Color.FromRgb(0x24, 0x27, 0x2e)),
            ("SecondaryHueMidBrush",   Color.FromRgb(0xc7, 0xff, 0xea), Color.FromRgb(0x3a, 0x3f, 0x48)),
            ("SecondaryHueDarkBrush",  Color.FromRgb(0x95, 0xcc, 0xb8), Color.FromRgb(0x9f, 0xe1, 0xcb)),
            ("HoverBackgroundBrush",   Color.FromRgb(0xeb, 0xf5, 0xf7), Color.FromRgb(0x2e, 0x33, 0x3c)),
            ("ChromeBackgroundBrush",  Color.FromRgb(0x95, 0xc4, 0xcc), Color.FromRgb(0x1b, 0x1d, 0x23)),
            // MaterialDesign built-in text/background brushes. In 4.2.1 PaletteHelper.SetBaseTheme
            // does not reliably flip these, so we set them explicitly. Some are Frozen in the theme
            // dictionary, so ApplyBrushes installs a fresh mutable brush at the app level when needed.
            ("MaterialDesignBody",       Color.FromRgb(0x00, 0x00, 0x00), Color.FromRgb(0xF5, 0xF5, 0xF5)),
            ("MaterialDesignBackground", Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0x2B, 0x2B, 0x2B)),
        };

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    Current = JsonSerializer.Deserialize<ThemeMode>(json);
                }
            }
            catch
            {
                Current = ThemeMode.Light;
            }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Current));
            }
            catch
            {
                // best-effort: theme preference is non-critical
            }
        }

        public static void Apply(ThemeMode mode)
        {
            Current = mode;
            ApplyMaterialDesignTheme(mode);
            ApplyMaterialDesignExtensionsTheme(mode);
            ApplyBrushes(mode);
            ThemeChanged?.Invoke(null, mode);
        }

        public static void Toggle()
        {
            Apply(Current == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light);
            Save();
        }

        private static void ApplyBrushes(ThemeMode mode)
        {
            var resources = Application.Current?.Resources;
            if (resources == null)
            {
                return;
            }

            foreach (var (key, light, dark) in BrushPalette)
            {
                var color = mode == ThemeMode.Dark ? dark : light;
                if (resources[key] is SolidColorBrush brush && !brush.IsFrozen)
                {
                    brush.Color = color;
                }
                else
                {
                    // Frozen or missing (e.g. MaterialDesign built-in brushes): install a fresh
                    // mutable brush at the application level so DynamicResource bindings pick it up.
                    resources[key] = new SolidColorBrush(color);
                }
            }
        }

        /// <summary>
        /// Switch the MaterialDesignThemes base theme so the built-in brushes
        /// (MaterialDesignBody text, MaterialDesignBackground, etc.) follow the mode. The
        /// PrimaryHueMidBrush overrides from App.xaml are re-applied afterwards by ApplyBrushes.
        /// </summary>
        private static void ApplyMaterialDesignTheme(ThemeMode mode)
        {
            try
            {
                var helper = new PaletteHelper();
                var theme = helper.GetTheme();
                theme.SetBaseTheme(mode == ThemeMode.Dark ? Theme.Dark : Theme.Light);
                helper.SetTheme(theme);
            }
            catch
            {
                // best-effort: MaterialDesign theme switch is non-critical
            }
        }

        private static void ApplyMaterialDesignExtensionsTheme(ThemeMode mode)
        {
            var resources = Application.Current?.Resources;
            if (resources == null)
            {
                return;
            }

            var targetName = mode == ThemeMode.Dark ? "MaterialDesignDarkTheme.xaml" : "MaterialDesignLightTheme.xaml";
            foreach (var dict in resources.MergedDictionaries)
            {
                var src = dict.Source?.OriginalString ?? string.Empty;
                if (src.Contains("MaterialDesignLightTheme") || src.Contains("MaterialDesignDarkTheme"))
                {
                    if (!src.EndsWith(targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        dict.Source = new Uri(
                            $"pack://application:,,,/MaterialDesignExtensions;component/Themes/{targetName}",
                            UriKind.Absolute);
                    }
                    break;
                }
            }
        }
    }
}
