using System.Windows.Input;
using Translumo.HotKeys;
using Xunit;

namespace Translumo.Tests.HotKeys
{
    public class HotKeyInfoTests
    {
        [Fact]
        public void Equals_symmetric_and_gethashcode_consistent()
        {
            var a = new HotKeyInfo(Key.A, KeyModifier.Ctrl);
            var b = new HotKeyInfo(Key.A, KeyModifier.Ctrl);
            var c = new HotKeyInfo(Key.B, KeyModifier.Ctrl);

            Assert.Equal(a, b);
            Assert.NotEqual(a, c);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equals_returns_false_for_null_or_other_type()
        {
            var a = new HotKeyInfo(Key.A, KeyModifier.Ctrl);
            Assert.False(a.Equals(null));
            Assert.False(a.Equals("not a hotkey"));
        }

        [Fact]
        public void Gamepad_gethashcode_consistent_for_equal_objects()
        {
            var a = new GamepadHotKeyInfo(SharpDX.XInput.GamepadKeyCode.A);
            var b = new GamepadHotKeyInfo(SharpDX.XInput.GamepadKeyCode.A);

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Gamepad_equals_false_for_different_keys()
        {
            var a = new GamepadHotKeyInfo(SharpDX.XInput.GamepadKeyCode.A);
            var b = new GamepadHotKeyInfo(SharpDX.XInput.GamepadKeyCode.B);
            Assert.NotEqual(a, b);
        }
    }
}
