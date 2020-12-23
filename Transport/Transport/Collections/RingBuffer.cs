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

        public T Peek()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException();
            }
            return _elements[_tail];
        }
        public void Push(T item)
        {
            if (IsFull)
            {
                throw new InvalidOperationException();
            }

            _elements[_head] = item;
            _head         =  (_head + 1) % _elements.Length;
            Count        += 1;
        }

        public void Clear()
        {
            _head = 0;
            _tail = 0;
            Count = 0;

            Array.Clear(_elements, 0, _elements.Length);
        }

        internal T Pop()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException();
            }

            var item = _elements[_tail];

            _elements[_tail] = default;
            _tail = (_tail + 1) % _elements.Length;
            Count -= 1;

            return item;
        }
    }
}