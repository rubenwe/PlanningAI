using PlanningAi.Utils;
using Xunit;

namespace PlanningAI.Tests
{
    public class EnumerableExtensionTests
    {
        private (string name, int value)[] _data = new[]
        {
            ("a", 1),
            ("b", 0),
            ("c", 5),
            ("d", 2)
        };
        
        [Fact]
        public void ShouldFindValueByMinimum()
        {
            var min = _data.MinBy(t => t.value);
            Assert.Equal("b", min.name);
        }

        [Fact]
        public void ShouldFindValueByMaximum()
        {
            var max = _data.MaxBy(t => t.value);
            Assert.Equal("c", max.name);
        }
    }
}