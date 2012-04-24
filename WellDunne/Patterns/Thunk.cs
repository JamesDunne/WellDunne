using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    /// <summary>
    /// A thunk contains either an immediate value or a function to compute a value and cache it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Thunk<T>
    {
        private T _value;
        private bool _hasValue;
        private readonly Func<T> _getValue;

        /// <summary>
        /// Gets the value and caches it or retrieves the already cached value.
        /// </summary>
        /// <remarks>Not thread safe.</remarks>
        public T Value
        {
            get
            {
                if (!_hasValue)
                {
                    _value = _getValue();
                    _hasValue = true;
                }
                return _value;
            }
        }

        /// <summary>
        /// Evaluates the thunk and executes the given function to return a new value of a different type.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="eval"></param>
        /// <returns></returns>
        public U Evaluate<U>(Func<T, U> eval)
        {
            return eval(Value);
        }

        /// <summary>
        /// Creates a thunk with a function to compute its value.
        /// </summary>
        /// <param name="getValue"></param>
        public Thunk(Func<T> getValue)
        {
            if (getValue == null) throw new ArgumentNullException("getValue");

            _getValue = getValue;
            _value = default(T);
            _hasValue = false;
        }

        /// <summary>
        /// Creates a thunk with an immediate value. Consider using the implicit conversion operator instead.
        /// </summary>
        /// <param name="value"></param>
        public Thunk(T value)
        {
            _value = value;
            _hasValue = true;
            _getValue = null;
        }

        /// <summary>
        /// Convert an immediate value to a thunk representing that immediate value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator Thunk<T>(T value)
        {
            return new Thunk<T>(value);
        }

        /// <summary>
        /// Convert a function to a thunk.
        /// </summary>
        /// <param name="getValue"></param>
        /// <returns></returns>
        public static implicit operator Thunk<T>(Func<T> getValue)
        {
            return new Thunk<T>(getValue);
        }

        /// <summary>
        /// Convert the thunk to an immediate value by evaluating the thunk.
        /// </summary>
        /// <param name="thunk"></param>
        /// <returns></returns>
        public static explicit operator T(Thunk<T> thunk)
        {
            return thunk.Value;
        }
    }

    public static class Thunk
    {
        /// <summary>
        /// Create a thunk with an immediate value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Thunk<T> Create<T>(T value)
        {
            return new Thunk<T>(value);
        }

        /// <summary>
        /// Create a thunk with a function to compute its value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="getValue"></param>
        /// <returns></returns>
        public static Thunk<T> CreateDeferred<T>(Func<T> getValue)
        {
            return new Thunk<T>(getValue);
        }

        /// <summary>
        /// If <paramref name="existing"/> is not null return it, else use the provided <paramref name="getValue"/> function to
        /// create a new thunk and return that.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="existing"></param>
        /// <param name="getValue"></param>
        /// <returns></returns>
        public static Thunk<T> CreateDeferredOrReuse<T>(Thunk<T> existing, Func<T> getValue)
        {
            if (existing == null) return new Thunk<T>(getValue);
            return existing;
        }

        /// <summary>
        /// If <paramref name="existing"/> is not null return it, else use the provided <paramref name="value"/> to
        /// create a new thunk and return that.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="existing"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Thunk<T> CreateOrReuse<T>(Thunk<T> existing, T value)
        {
            if (existing == null) return new Thunk<T>(value);
            return existing;
        }

        public static T EvaluateEither<T>(Thunk<T> existing, Func<T> getValue)
        {
            if (existing == null) return getValue();
            return existing.Value;
        }
    }
}
