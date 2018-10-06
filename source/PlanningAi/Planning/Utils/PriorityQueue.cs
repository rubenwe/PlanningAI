using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PlanningAi.Planning.Utils
{
    public class PriorityQueue<T> 
    {
        private readonly SortedDictionary<int, Queue<T>> _queues = new SortedDictionary<int, Queue<T>>();
        private readonly int _factor;
        private int _count;

        public bool IsEmpty => _count == 0;

        public PriorityQueue(int precision = 3)
        {
            _factor = (int) Math.Pow(10, precision);
        }

        public void Enqueue(float priority, T item)
        {
            var key = GetKey(priority);
            if (!_queues.TryGetValue(key, out var queue))
            {
                queue = new Queue<T>();
                _queues.Add(key, queue);
            }

            queue.Enqueue(item);

            _count++;
        }

        private int GetKey(float priority)
        {
            return (int) (priority * _factor);
        }

        public T Dequeue()
        {
            if (_count == 0) throw new InvalidOperationException("Queue is empty!");

            var pair = First(_queues);
            var queue = pair.Value;
            var item = queue.Dequeue();

            if (queue.Count == 0)
            {
                _queues.Remove(pair.Key);
            }

            _count--;

            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static KeyValuePair<int, Queue<T>> First(SortedDictionary<int, Queue<T>> queues)
        {
            using (var enumerator = queues.GetEnumerator())
            {
                enumerator.MoveNext();
                return enumerator.Current;
            }
        }
    }
}