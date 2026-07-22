using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Translumo.Utils;

namespace Translumo.Controls
{
    /// <summary>
    /// Interaction logic for TrayControl.xaml
    /// </summary>
    public partial class TrayControl : UserControl
    {
        public static readonly DependencyProperty SettingsOpeningCommandProperty = DependencyProperty.Register("SettingsOpeningCommand", typeof(ICommand), typeof(TrayControl));
        public static readonly DependencyProperty ChatOpeningCommandProperty = DependencyProperty.Register("ChatOpeningCommand", typeof(ICommand), typeof(TrayControl));

        public ICommand SettingsOpeningCommand
        {
            get { return (ICommand)GetValue(SettingsOpeningCommandProperty); }
            set { SetValue(SettingsOpeningCommandProperty, value); }
        }

        public ICommand ChatOpeningCommand
        {
            get { return (ICommand)GetValue(ChatOpeningCommandProperty); }
            set { SetValue(ChatOpeningCommandProperty, value); }
        }
        

        public TrayControl()
        {
            InitializeComponent();

            // The TaskbarIcon ContextMenu lives in a detached visual tree, so its
            // DynamicResource bindings do not refresh when the app resource dictionary
            // is replaced on culture change. Force-update the headers explicitly.
            LocalizationManager.CultureChanged += OnCultureChanged;
            UpdateMenuHeaders();
        }

        private void OnCultureChanged()
        {
            UpdateMenuHeaders();
        }

        private void UpdateMenuHeaders()
        {
            MenuItemSettings.Header = LocalizationManager.GetValue("Str.Tray.ShowHideSettings");
            MenuItemChat.Header = LocalizationManager.GetValue("Str.Tray.ShowHideTranslation");
            MenuItemExit.Header = LocalizationManager.GetValue("Str.Tray.Exit");
        }
    }
}
