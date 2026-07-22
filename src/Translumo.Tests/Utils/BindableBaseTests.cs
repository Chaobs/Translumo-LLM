using Translumo.Utils;
using Xunit;

namespace Translumo.Tests.Utils
{
    public class BindableBaseTests
    {
        private class TestModel : BindableBase
        {
            private string _name = string.Empty;
            public string Name
            {
                get => _name;
                set => SetProperty(ref _name, value);
            }
        }

        [Fact]
        public void SetProperty_raises_property_changed()
        {
            var m = new TestModel();
            string? raised = null;
            m.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TestModel.Name)) raised = e.PropertyName;
            };
            m.Name = "abc";
            Assert.Equal("abc", m.Name);
            Assert.Equal(nameof(TestModel.Name), raised);
        }

        [Fact]
        public void SetProperty_does_not_notify_when_unchanged()
        {
            var m = new TestModel { Name = "x" };
            bool raised = false;
            m.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TestModel.Name)) raised = true;
            };
            m.Name = "x";
            Assert.False(raised);
        }
    }
}
