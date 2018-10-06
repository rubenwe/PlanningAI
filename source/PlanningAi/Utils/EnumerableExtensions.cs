using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
// ReSharper disable InvertIf

namespace PlanningAi.Utils
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Utility Class")]
    public static class EnumerableExtensions
    {
        public static T MinBy<T>(this IEnumerable<T> source, Func<T, float> selector)
        {
            T minItem = default;
            var minValue = float.MaxValue;
            foreach (var item in source)
            {
                var value = selector(item);
                if (value.CompareTo(minValue) < 0)
                {
                    minItem = item;
                    minValue = value;
                }
            }

            return minItem;
        }
        
        public static T MaxBy<T>(this IEnumerable<T> source, Func<T, float> selector)
        {
            T minItem = default;
            var minValue = float.MinValue;
            foreach (var item in source)
            {
                var value = selector(item);
                if (value.CompareTo(minValue) > 0)
                {
                    minItem = item;
                    minValue = value;
                }
            }

            return minItem;
        }
    }
}