using System;

namespace Transport.Collections
{
    public class RingBuffer<T>
    {
        public int Count { get; private set; }
        public bool IsFull => _head == _elements.Length;

        private int _head;
        private int _tail;
        private readonly T[] _elements;

        public RingBuffer(int capacity)
        {
            _elements = new T[capacity];
        }

        public void Push(T item)
        {
            if (IsFull)
            {
                throw new InvalidOperationException();
            }

            _head = (_head + 1) % _elements.Length;
            _elements[_head] = item;
            if (Count == _elements.Length)
            {
                _tail = (_tail + 1) % _elements.Length;
            }
            else
            {
                Count++;
            }
        }

        public void Clear()
        {
            _head = 0;
            _tail = 0;
            Count = 0;

            Array.Clear(_elements, 0, _elements.Length);
        }
    }
}