using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    /// <summary>
    /// A simple container for a possible value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Maybe<T>
    {
        private T _value;
        private bool _hasValue;

        public Maybe(T value)
        {
            _value = value;
            _hasValue = true;
        }

        public T Value { get { if (!_hasValue) throw new NullReferenceException(); return _value; } }
        public bool HasValue { get { return _hasValue; } }

        public static implicit operator Maybe<T>(T value)
        {
            return new Maybe<T>(value);
        }

        public static readonly Maybe<T> Nothing = new Maybe<T>();
    }
}
