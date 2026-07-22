using Translumo.Utils;

namespace Translumo.MVVM.Models
{
    /// <summary>
    /// Wraps an enum value together with a localized display name that can be refreshed
    /// in place when the application culture changes. This mirrors the mechanism used by
    /// <see cref="DisplayLanguage"/> for the source/target language dropdowns and avoids
    /// the converter-caching problem: a ComboBox bound through a value converter does not
    /// re-evaluate the converter when only the resource dictionary changes, so localized
    /// option text (e.g. TTS "None") would stay stale until the view was re-created.
    /// By exposing a plain <see cref="DisplayName"/> property and binding it via
    /// <c>DisplayMemberPath</c>, mutating the name raises <see cref="Utils.BindableBase.PropertyChanged"/>
    /// and the UI refreshes immediately — without ever clearing/replacing the collection,
    /// so TwoWay selection bindings are preserved across language switches.
    /// </summary>
    public class DisplayEnumItem : BindableBase
    {
        public object Value { get; }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        private string _displayName;

        public DisplayEnumItem(object value, string displayName)
        {
            Value = value;
            _displayName = displayName;
        }
    }
}
