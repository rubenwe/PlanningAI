using System.Collections.Generic;
using PlanningAi.Planning;
using Xunit;

namespace PlanningAI.Tests
{
    public class DomainStateTests
    {
        [Fact]
        public void EmptyStatesShouldBeEqual()
        {
            var state1 = DomainState.Empty;
            var state2 = DomainState.Empty;

            Assert.Equal(state1, state2);
        }

        [Fact]
        public void StatesWithSameValuesShouldBeEqual()
        {
            var state1 = DomainState.Empty.Set("key", 1);
            var state2 = DomainState.Empty.Set("key", 1);

            Assert.Equal(state1, state2);
        }

        [Fact]
        public void StatesWithDifferentValuesShouldNotBeEqual()
        {
            var state1 = DomainState.Empty.Set("key", 1);
            var state2 = DomainState.Empty.Set("key", 2);

            Assert.NotEqual(state1, state2);
        }

        [Fact]
        public void SubStateShouldBeTrueIfStateIsSubset()
        {
            var bigState = DomainState.Empty
                .Set("key1", true)
                .Set("key2", 4)
                .Set("key3", 5f);

            Assert.True(DomainState.Empty.IsSubStateOf(bigState));
            
            var smallState = DomainState.Empty
                .Set("key1", true)
                .Set("key3", 5f);

            Assert.True(smallState.IsSubStateOf(bigState));
        }
        
        [Fact]
        public void SubStateShouldBeFalseIfStateIsSubset()
        {
            var bigState = DomainState.Empty
                .Set("key1", true)
                .Set("key2", 4)
                .Set("key3", 5f);

            Assert.False(bigState.IsSubStateOf(DomainState.Empty));
            
            var smallState = DomainState.Empty
                .Set("key1", false);

            Assert.False(smallState.IsSubStateOf(bigState));
        }

        [Fact]
        public void BoolDistance()
        {
            var state1 = DomainState.Empty
                .Set("key1", true)
                .Set("key2", false)
                .Set("key3", true);

            var state2 = DomainState.Empty
                .Set("key1", true)
                .Set("key2", true)
                .Set("key3", false);

            Assert.Equal(2, state1.DistanceTo(state2));
        }

        [Fact]
        public void IntDistance()
        {
            var goal = DomainState.Empty.Set("Gold", 10);
            var current = DomainState.Empty.Set("Gold", 5);

            Assert.Equal(0.5f, current.DistanceTo(goal), 2);
        }
        
        [Fact]
        public void FloatDistance()
        {
            var goal = DomainState.Empty.Set("Gold", 10f);
            var current = DomainState.Empty.Set("Gold", 5f);

            Assert.Equal(0.5f, current.DistanceTo(goal), 2);
        }
        
        [Fact]
        public void LongDistance()
        {
            var goal = DomainState.Empty.Set("Gold", 10L);
            var current = DomainState.Empty.Set("Gold", 5L);

            Assert.Equal(0.5f, current.DistanceTo(goal), 2);
        }
        
        [Fact]
        public void DoubleDistance()
        {
            var goal = DomainState.Empty.Set("Gold", 10d);
            var current = DomainState.Empty.Set("Gold", 5d);

            Assert.Equal(0.5f, current.DistanceTo(goal), 2);
        }
        
        [Fact]
        public void DecimalDistance()
        {
            var goal = DomainState.Empty.Set("Gold", 10m);
            var current = DomainState.Empty.Set("Gold", 5m);

            Assert.Equal(0.5f, current.DistanceTo(goal), 2);
        }
        
        [Fact]
        public void MoreComplexDistance()
        {
            var goal = DomainState.Empty.Set("Gold", 10);
            var current = DomainState.Empty
                .Set("Gold", 5)
                .Set("Silver", 5)
                .Set("HasCheated", true);

            Assert.Equal(2.5f, goal.DistanceTo(current), 2);
            Assert.Equal(0.5f, goal.DistanceTo(current, true), 2);
        }

        [Fact]
        public void ApplyingDictionaryShouldWork()
        {
            var state = DomainState.Empty.Apply(new Dictionary<string, object>
            {
                ["test123"] = 123,
                ["test321"] = true
            });
            
            Assert.Equal(123, state.GetValueOrDefault("test123", 0));
            Assert.True(state.GetValueOrDefault("test321", false));
        }
    }
}