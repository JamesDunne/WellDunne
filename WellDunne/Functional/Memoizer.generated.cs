using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
#if NET35
    public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, TResult>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8);
#endif

    /// <summary>
    /// Memoizes the result of a function per each set of parameter values.
    /// </summary>
    /// <remarks>
    /// Not thread safe. Do not access concurrently from multiple threads!
    /// </remarks>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public sealed class Memoizer<T1, TResult>
    {
        private struct Key : IEqualityComparer<Key>
        {
            public readonly T1 _t1;

            public Key(T1 t1)
            {
                _t1 = t1;
            }

            public bool Equals(Key x, Key y)
            {
                if (!EqualityComparer<T1>.Default.Equals(x._t1, y._t1)) return false;
                return true;
            }

            public int GetHashCode(Key obj)
            {
                unchecked
                {
                    int h = -1291968577;
                    h = (-1521134295 * h) + EqualityComparer<T1>.Default.GetHashCode(this._t1);
                    return h;
                }
            }
        }

        private readonly Func<T1, TResult> _func;
        private Dictionary<Key, TResult> _values;

        /// <summary>
        /// Creates a memoizer to cache function results per distinct parameter collection.
        /// </summary>
        public Memoizer(Func<T1, TResult> func)
        {
            if (func == null) throw new ArgumentNullException("func");

            this._func = func;
            this._values = null;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <returns></returns>
        public TResult Invoke(T1 t1)
        {
            TResult val;

            // Construct a key for look-up:
            Key key = new Key(t1);

            // Try to get a memoized value:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();
            else if (_values.TryGetValue(key, out val))
                return val;

            // Add the value to the look-up:
            val = _func(t1);
            _values.Add(key, val);
            return val;
        }

        /// <summary>
        /// Clears the last computed value for the given parameter values.
        /// </summary>
        public void Clear(T1 t1)
        {
            if (_values == null) return;

            // Construct a key for look-up:
            Key key = new Key(t1);

            _values.Remove(key);
        }

        /// <summary>
        /// Sets a specific value to be returned for the given parameter values.
        /// </summary>
        public TResult Override(TResult newValue, T1 t1)
        {
            // Construct a key for look-up:
            Key key = new Key(t1);

            // Create the dictionary if it does not exist yet:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();

            _values[key] = newValue;

            return newValue;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <returns></returns>
        public TResult this[T1 t1]
        {
            get
            {
                return Invoke(t1);
            }
            set
            {
                Override(value, t1);
            }
        }
    }

    /// <summary>
    /// Memoizes the result of a function per each set of parameter values.
    /// </summary>
    /// <remarks>
    /// Not thread safe. Do not access concurrently from multiple threads!
    /// </remarks>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public sealed class Memoizer<T1, T2, TResult>
    {
        private struct Key : IEqualityComparer<Key>
        {
            public readonly T1 _t1;
            public readonly T2 _t2;

            public Key(T1 t1, T2 t2)
            {
                _t1 = t1;
                _t2 = t2;
            }

            public bool Equals(Key x, Key y)
            {
                if (!EqualityComparer<T1>.Default.Equals(x._t1, y._t1)) return false;
                if (!EqualityComparer<T2>.Default.Equals(x._t2, y._t2)) return false;
                return true;
            }

            public int GetHashCode(Key obj)
            {
                unchecked
                {
                    int h = -1291968577;
                    h = (-1521134295 * h) + EqualityComparer<T1>.Default.GetHashCode(this._t1);
                    h = (-1521134295 * h) + EqualityComparer<T2>.Default.GetHashCode(this._t2);
                    return h;
                }
            }
        }

        private readonly Func<T1, T2, TResult> _func;
        private Dictionary<Key, TResult> _values;

        /// <summary>
        /// Creates a memoizer to cache function results per distinct parameter collection.
        /// </summary>
        public Memoizer(Func<T1, T2, TResult> func)
        {
            if (func == null) throw new ArgumentNullException("func");

            this._func = func;
            this._values = null;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public TResult Invoke(T1 t1, T2 t2)
        {
            TResult val;

            // Construct a key for look-up:
            Key key = new Key(t1, t2);

            // Try to get a memoized value:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();
            else if (_values.TryGetValue(key, out val))
                return val;

            // Add the value to the look-up:
            val = _func(t1, t2);
            _values.Add(key, val);
            return val;
        }

        /// <summary>
        /// Clears the last computed value for the given parameter values.
        /// </summary>
        public void Clear(T1 t1, T2 t2)
        {
            if (_values == null) return;

            // Construct a key for look-up:
            Key key = new Key(t1, t2);

            _values.Remove(key);
        }

        /// <summary>
        /// Sets a specific value to be returned for the given parameter values.
        /// </summary>
        public TResult Override(TResult newValue, T1 t1, T2 t2)
        {
            // Construct a key for look-up:
            Key key = new Key(t1, t2);

            // Create the dictionary if it does not exist yet:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();

            _values[key] = newValue;

            return newValue;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public TResult this[T1 t1, T2 t2]
        {
            get
            {
                return Invoke(t1, t2);
            }
            set
            {
                Override(value, t1, t2);
            }
        }
    }

    /// <summary>
    /// Memoizes the result of a function per each set of parameter values.
    /// </summary>
    /// <remarks>
    /// Not thread safe. Do not access concurrently from multiple threads!
    /// </remarks>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public sealed class Memoizer<T1, T2, T3, TResult>
    {
        private struct Key : IEqualityComparer<Key>
        {
            public readonly T1 _t1;
            public readonly T2 _t2;
            public readonly T3 _t3;

            public Key(T1 t1, T2 t2, T3 t3)
            {
                _t1 = t1;
                _t2 = t2;
                _t3 = t3;
            }

            public bool Equals(Key x, Key y)
            {
                if (!EqualityComparer<T1>.Default.Equals(x._t1, y._t1)) return false;
                if (!EqualityComparer<T2>.Default.Equals(x._t2, y._t2)) return false;
                if (!EqualityComparer<T3>.Default.Equals(x._t3, y._t3)) return false;
                return true;
            }

            public int GetHashCode(Key obj)
            {
                unchecked
                {
                    int h = -1291968577;
                    h = (-1521134295 * h) + EqualityComparer<T1>.Default.GetHashCode(this._t1);
                    h = (-1521134295 * h) + EqualityComparer<T2>.Default.GetHashCode(this._t2);
                    h = (-1521134295 * h) + EqualityComparer<T3>.Default.GetHashCode(this._t3);
                    return h;
                }
            }
        }

        private readonly Func<T1, T2, T3, TResult> _func;
        private Dictionary<Key, TResult> _values;

        /// <summary>
        /// Creates a memoizer to cache function results per distinct parameter collection.
        /// </summary>
        public Memoizer(Func<T1, T2, T3, TResult> func)
        {
            if (func == null) throw new ArgumentNullException("func");

            this._func = func;
            this._values = null;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <returns></returns>
        public TResult Invoke(T1 t1, T2 t2, T3 t3)
        {
            TResult val;

            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3);

            // Try to get a memoized value:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();
            else if (_values.TryGetValue(key, out val))
                return val;

            // Add the value to the look-up:
            val = _func(t1, t2, t3);
            _values.Add(key, val);
            return val;
        }

        /// <summary>
        /// Clears the last computed value for the given parameter values.
        /// </summary>
        public void Clear(T1 t1, T2 t2, T3 t3)
        {
            if (_values == null) return;

            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3);

            _values.Remove(key);
        }

        /// <summary>
        /// Sets a specific value to be returned for the given parameter values.
        /// </summary>
        public TResult Override(TResult newValue, T1 t1, T2 t2, T3 t3)
        {
            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3);

            // Create the dictionary if it does not exist yet:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();

            _values[key] = newValue;

            return newValue;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <returns></returns>
        public TResult this[T1 t1, T2 t2, T3 t3]
        {
            get
            {
                return Invoke(t1, t2, t3);
            }
            set
            {
                Override(value, t1, t2, t3);
            }
        }
    }

    /// <summary>
    /// Memoizes the result of a function per each set of parameter values.
    /// </summary>
    /// <remarks>
    /// Not thread safe. Do not access concurrently from multiple threads!
    /// </remarks>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public sealed class Memoizer<T1, T2, T3, T4, TResult>
    {
        private struct Key : IEqualityComparer<Key>
        {
            public readonly T1 _t1;
            public readonly T2 _t2;
            public readonly T3 _t3;
            public readonly T4 _t4;

            public Key(T1 t1, T2 t2, T3 t3, T4 t4)
            {
                _t1 = t1;
                _t2 = t2;
                _t3 = t3;
                _t4 = t4;
            }

            public bool Equals(Key x, Key y)
            {
                if (!EqualityComparer<T1>.Default.Equals(x._t1, y._t1)) return false;
                if (!EqualityComparer<T2>.Default.Equals(x._t2, y._t2)) return false;
                if (!EqualityComparer<T3>.Default.Equals(x._t3, y._t3)) return false;
                if (!EqualityComparer<T4>.Default.Equals(x._t4, y._t4)) return false;
                return true;
            }

            public int GetHashCode(Key obj)
            {
                unchecked
                {
                    int h = -1291968577;
                    h = (-1521134295 * h) + EqualityComparer<T1>.Default.GetHashCode(this._t1);
                    h = (-1521134295 * h) + EqualityComparer<T2>.Default.GetHashCode(this._t2);
                    h = (-1521134295 * h) + EqualityComparer<T3>.Default.GetHashCode(this._t3);
                    h = (-1521134295 * h) + EqualityComparer<T4>.Default.GetHashCode(this._t4);
                    return h;
                }
            }
        }

        private readonly Func<T1, T2, T3, T4, TResult> _func;
        private Dictionary<Key, TResult> _values;

        /// <summary>
        /// Creates a memoizer to cache function results per distinct parameter collection.
        /// </summary>
        public Memoizer(Func<T1, T2, T3, T4, TResult> func)
        {
            if (func == null) throw new ArgumentNullException("func");

            this._func = func;
            this._values = null;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <param name="t4"></param>
        /// <returns></returns>
        public TResult Invoke(T1 t1, T2 t2, T3 t3, T4 t4)
        {
            TResult val;

            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4);

            // Try to get a memoized value:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();
            else if (_values.TryGetValue(key, out val))
                return val;

            // Add the value to the look-up:
            val = _func(t1, t2, t3, t4);
            _values.Add(key, val);
            return val;
        }

        /// <summary>
        /// Clears the last computed value for the given parameter values.
        /// </summary>
        public void Clear(T1 t1, T2 t2, T3 t3, T4 t4)
        {
            if (_values == null) return;

            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4);

            _values.Remove(key);
        }

        /// <summary>
        /// Sets a specific value to be returned for the given parameter values.
        /// </summary>
        public TResult Override(TResult newValue, T1 t1, T2 t2, T3 t3, T4 t4)
        {
            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4);

            // Create the dictionary if it does not exist yet:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();

            _values[key] = newValue;

            return newValue;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <param name="t4"></param>
        /// <returns></returns>
        public TResult this[T1 t1, T2 t2, T3 t3, T4 t4]
        {
            get
            {
                return Invoke(t1, t2, t3, t4);
            }
            set
            {
                Override(value, t1, t2, t3, t4);
            }
        }
    }

    /// <summary>
    /// Memoizes the result of a function per each set of parameter values.
    /// </summary>
    /// <remarks>
    /// Not thread safe. Do not access concurrently from multiple threads!
    /// </remarks>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public sealed class Memoizer<T1, T2, T3, T4, T5, TResult>
    {
        private struct Key : IEqualityComparer<Key>
        {
            public readonly T1 _t1;
            public readonly T2 _t2;
            public readonly T3 _t3;
            public readonly T4 _t4;
            public readonly T5 _t5;

            public Key(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
            {
                _t1 = t1;
                _t2 = t2;
                _t3 = t3;
                _t4 = t4;
                _t5 = t5;
            }

            public bool Equals(Key x, Key y)
            {
                if (!EqualityComparer<T1>.Default.Equals(x._t1, y._t1)) return false;
                if (!EqualityComparer<T2>.Default.Equals(x._t2, y._t2)) return false;
                if (!EqualityComparer<T3>.Default.Equals(x._t3, y._t3)) return false;
                if (!EqualityComparer<T4>.Default.Equals(x._t4, y._t4)) return false;
                if (!EqualityComparer<T5>.Default.Equals(x._t5, y._t5)) return false;
                return true;
            }

            public int GetHashCode(Key obj)
            {
                unchecked
                {
                    int h = -1291968577;
                    h = (-1521134295 * h) + EqualityComparer<T1>.Default.GetHashCode(this._t1);
                    h = (-1521134295 * h) + EqualityComparer<T2>.Default.GetHashCode(this._t2);
                    h = (-1521134295 * h) + EqualityComparer<T3>.Default.GetHashCode(this._t3);
                    h = (-1521134295 * h) + EqualityComparer<T4>.Default.GetHashCode(this._t4);
                    h = (-1521134295 * h) + EqualityComparer<T5>.Default.GetHashCode(this._t5);
                    return h;
                }
            }
        }

        private readonly Func<T1, T2, T3, T4, T5, TResult> _func;
        private Dictionary<Key, TResult> _values;

        /// <summary>
        /// Creates a memoizer to cache function results per distinct parameter collection.
        /// </summary>
        public Memoizer(Func<T1, T2, T3, T4, T5, TResult> func)
        {
            if (func == null) throw new ArgumentNullException("func");

            this._func = func;
            this._values = null;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <param name="t4"></param>
        /// <param name="t5"></param>
        /// <returns></returns>
        public TResult Invoke(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            TResult val;

            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4, t5);

            // Try to get a memoized value:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();
            else if (_values.TryGetValue(key, out val))
                return val;

            // Add the value to the look-up:
            val = _func(t1, t2, t3, t4, t5);
            _values.Add(key, val);
            return val;
        }

        /// <summary>
        /// Clears the last computed value for the given parameter values.
        /// </summary>
        public void Clear(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            if (_values == null) return;

            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4, t5);

            _values.Remove(key);
        }

        /// <summary>
        /// Sets a specific value to be returned for the given parameter values.
        /// </summary>
        public TResult Override(TResult newValue, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4, t5);

            // Create the dictionary if it does not exist yet:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();

            _values[key] = newValue;

            return newValue;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <param name="t4"></param>
        /// <param name="t5"></param>
        /// <returns></returns>
        public TResult this[T1 t1, T2 t2, T3 t3, T4 t4, T5 t5]
        {
            get
            {
                return Invoke(t1, t2, t3, t4, t5);
            }
            set
            {
                Override(value, t1, t2, t3, t4, t5);
            }
        }
    }

    /// <summary>
    /// Memoizes the result of a function per each set of parameter values.
    /// </summary>
    /// <remarks>
    /// Not thread safe. Do not access concurrently from multiple threads!
    /// </remarks>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public sealed class Memoizer<T1, T2, T3, T4, T5, T6, TResult>
    {
        private struct Key : IEqualityComparer<Key>
        {
            public readonly T1 _t1;
            public readonly T2 _t2;
            public readonly T3 _t3;
            public readonly T4 _t4;
            public readonly T5 _t5;
            public readonly T6 _t6;

            public Key(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
            {
                _t1 = t1;
                _t2 = t2;
                _t3 = t3;
                _t4 = t4;
                _t5 = t5;
                _t6 = t6;
            }

            public bool Equals(Key x, Key y)
            {
                if (!EqualityComparer<T1>.Default.Equals(x._t1, y._t1)) return false;
                if (!EqualityComparer<T2>.Default.Equals(x._t2, y._t2)) return false;
                if (!EqualityComparer<T3>.Default.Equals(x._t3, y._t3)) return false;
                if (!EqualityComparer<T4>.Default.Equals(x._t4, y._t4)) return false;
                if (!EqualityComparer<T5>.Default.Equals(x._t5, y._t5)) return false;
                if (!EqualityComparer<T6>.Default.Equals(x._t6, y._t6)) return false;
                return true;
            }

            public int GetHashCode(Key obj)
            {
                unchecked
                {
                    int h = -1291968577;
                    h = (-1521134295 * h) + EqualityComparer<T1>.Default.GetHashCode(this._t1);
                    h = (-1521134295 * h) + EqualityComparer<T2>.Default.GetHashCode(this._t2);
                    h = (-1521134295 * h) + EqualityComparer<T3>.Default.GetHashCode(this._t3);
                    h = (-1521134295 * h) + EqualityComparer<T4>.Default.GetHashCode(this._t4);
                    h = (-1521134295 * h) + EqualityComparer<T5>.Default.GetHashCode(this._t5);
                    h = (-1521134295 * h) + EqualityComparer<T6>.Default.GetHashCode(this._t6);
                    return h;
                }
            }
        }

        private readonly Func<T1, T2, T3, T4, T5, T6, TResult> _func;
        private Dictionary<Key, TResult> _values;

        /// <summary>
        /// Creates a memoizer to cache function results per distinct parameter collection.
        /// </summary>
        public Memoizer(Func<T1, T2, T3, T4, T5, T6, TResult> func)
        {
            if (func == null) throw new ArgumentNullException("func");

            this._func = func;
            this._values = null;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <param name="t4"></param>
        /// <param name="t5"></param>
        /// <param name="t6"></param>
        /// <returns></returns>
        public TResult Invoke(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        {
            TResult val;

            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4, t5, t6);

            // Try to get a memoized value:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();
            else if (_values.TryGetValue(key, out val))
                return val;

            // Add the value to the look-up:
            val = _func(t1, t2, t3, t4, t5, t6);
            _values.Add(key, val);
            return val;
        }

        /// <summary>
        /// Clears the last computed value for the given parameter values.
        /// </summary>
        public void Clear(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        {
            if (_values == null) return;

            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4, t5, t6);

            _values.Remove(key);
        }

        /// <summary>
        /// Sets a specific value to be returned for the given parameter values.
        /// </summary>
        public TResult Override(TResult newValue, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        {
            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4, t5, t6);

            // Create the dictionary if it does not exist yet:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();

            _values[key] = newValue;

            return newValue;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <param name="t4"></param>
        /// <param name="t5"></param>
        /// <param name="t6"></param>
        /// <returns></returns>
        public TResult this[T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6]
        {
            get
            {
                return Invoke(t1, t2, t3, t4, t5, t6);
            }
            set
            {
                Override(value, t1, t2, t3, t4, t5, t6);
            }
        }
    }

    /// <summary>
    /// Memoizes the result of a function per each set of parameter values.
    /// </summary>
    /// <remarks>
    /// Not thread safe. Do not access concurrently from multiple threads!
    /// </remarks>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public sealed class Memoizer<T1, T2, T3, T4, T5, T6, T7, TResult>
    {
        private struct Key : IEqualityComparer<Key>
        {
            public readonly T1 _t1;
            public readonly T2 _t2;
            public readonly T3 _t3;
            public readonly T4 _t4;
            public readonly T5 _t5;
            public readonly T6 _t6;
            public readonly T7 _t7;

            public Key(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
            {
                _t1 = t1;
                _t2 = t2;
                _t3 = t3;
                _t4 = t4;
                _t5 = t5;
                _t6 = t6;
                _t7 = t7;
            }

            public bool Equals(Key x, Key y)
            {
                if (!EqualityComparer<T1>.Default.Equals(x._t1, y._t1)) return false;
                if (!EqualityComparer<T2>.Default.Equals(x._t2, y._t2)) return false;
                if (!EqualityComparer<T3>.Default.Equals(x._t3, y._t3)) return false;
                if (!EqualityComparer<T4>.Default.Equals(x._t4, y._t4)) return false;
                if (!EqualityComparer<T5>.Default.Equals(x._t5, y._t5)) return false;
                if (!EqualityComparer<T6>.Default.Equals(x._t6, y._t6)) return false;
                if (!EqualityComparer<T7>.Default.Equals(x._t7, y._t7)) return false;
                return true;
            }

            public int GetHashCode(Key obj)
            {
                unchecked
                {
                    int h = -1291968577;
                    h = (-1521134295 * h) + EqualityComparer<T1>.Default.GetHashCode(this._t1);
                    h = (-1521134295 * h) + EqualityComparer<T2>.Default.GetHashCode(this._t2);
                    h = (-1521134295 * h) + EqualityComparer<T3>.Default.GetHashCode(this._t3);
                    h = (-1521134295 * h) + EqualityComparer<T4>.Default.GetHashCode(this._t4);
                    h = (-1521134295 * h) + EqualityComparer<T5>.Default.GetHashCode(this._t5);
                    h = (-1521134295 * h) + EqualityComparer<T6>.Default.GetHashCode(this._t6);
                    h = (-1521134295 * h) + EqualityComparer<T7>.Default.GetHashCode(this._t7);
                    return h;
                }
            }
        }

        private readonly Func<T1, T2, T3, T4, T5, T6, T7, TResult> _func;
        private Dictionary<Key, TResult> _values;

        /// <summary>
        /// Creates a memoizer to cache function results per distinct parameter collection.
        /// </summary>
        public Memoizer(Func<T1, T2, T3, T4, T5, T6, T7, TResult> func)
        {
            if (func == null) throw new ArgumentNullException("func");

            this._func = func;
            this._values = null;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <param name="t4"></param>
        /// <param name="t5"></param>
        /// <param name="t6"></param>
        /// <param name="t7"></param>
        /// <returns></returns>
        public TResult Invoke(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
        {
            TResult val;

            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4, t5, t6, t7);

            // Try to get a memoized value:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();
            else if (_values.TryGetValue(key, out val))
                return val;

            // Add the value to the look-up:
            val = _func(t1, t2, t3, t4, t5, t6, t7);
            _values.Add(key, val);
            return val;
        }

        /// <summary>
        /// Clears the last computed value for the given parameter values.
        /// </summary>
        public void Clear(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
        {
            if (_values == null) return;

            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4, t5, t6, t7);

            _values.Remove(key);
        }

        /// <summary>
        /// Sets a specific value to be returned for the given parameter values.
        /// </summary>
        public TResult Override(TResult newValue, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
        {
            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4, t5, t6, t7);

            // Create the dictionary if it does not exist yet:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();

            _values[key] = newValue;

            return newValue;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <param name="t4"></param>
        /// <param name="t5"></param>
        /// <param name="t6"></param>
        /// <param name="t7"></param>
        /// <returns></returns>
        public TResult this[T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7]
        {
            get
            {
                return Invoke(t1, t2, t3, t4, t5, t6, t7);
            }
            set
            {
                Override(value, t1, t2, t3, t4, t5, t6, t7);
            }
        }
    }

    /// <summary>
    /// Memoizes the result of a function per each set of parameter values.
    /// </summary>
    /// <remarks>
    /// Not thread safe. Do not access concurrently from multiple threads!
    /// </remarks>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="T8"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public sealed class Memoizer<T1, T2, T3, T4, T5, T6, T7, T8, TResult>
    {
        private struct Key : IEqualityComparer<Key>
        {
            public readonly T1 _t1;
            public readonly T2 _t2;
            public readonly T3 _t3;
            public readonly T4 _t4;
            public readonly T5 _t5;
            public readonly T6 _t6;
            public readonly T7 _t7;
            public readonly T8 _t8;

            public Key(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
            {
                _t1 = t1;
                _t2 = t2;
                _t3 = t3;
                _t4 = t4;
                _t5 = t5;
                _t6 = t6;
                _t7 = t7;
                _t8 = t8;
            }

            public bool Equals(Key x, Key y)
            {
                if (!EqualityComparer<T1>.Default.Equals(x._t1, y._t1)) return false;
                if (!EqualityComparer<T2>.Default.Equals(x._t2, y._t2)) return false;
                if (!EqualityComparer<T3>.Default.Equals(x._t3, y._t3)) return false;
                if (!EqualityComparer<T4>.Default.Equals(x._t4, y._t4)) return false;
                if (!EqualityComparer<T5>.Default.Equals(x._t5, y._t5)) return false;
                if (!EqualityComparer<T6>.Default.Equals(x._t6, y._t6)) return false;
                if (!EqualityComparer<T7>.Default.Equals(x._t7, y._t7)) return false;
                if (!EqualityComparer<T8>.Default.Equals(x._t8, y._t8)) return false;
                return true;
            }

            public int GetHashCode(Key obj)
            {
                unchecked
                {
                    int h = -1291968577;
                    h = (-1521134295 * h) + EqualityComparer<T1>.Default.GetHashCode(this._t1);
                    h = (-1521134295 * h) + EqualityComparer<T2>.Default.GetHashCode(this._t2);
                    h = (-1521134295 * h) + EqualityComparer<T3>.Default.GetHashCode(this._t3);
                    h = (-1521134295 * h) + EqualityComparer<T4>.Default.GetHashCode(this._t4);
                    h = (-1521134295 * h) + EqualityComparer<T5>.Default.GetHashCode(this._t5);
                    h = (-1521134295 * h) + EqualityComparer<T6>.Default.GetHashCode(this._t6);
                    h = (-1521134295 * h) + EqualityComparer<T7>.Default.GetHashCode(this._t7);
                    h = (-1521134295 * h) + EqualityComparer<T8>.Default.GetHashCode(this._t8);
                    return h;
                }
            }
        }

        private readonly Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> _func;
        private Dictionary<Key, TResult> _values;

        /// <summary>
        /// Creates a memoizer to cache function results per distinct parameter collection.
        /// </summary>
        public Memoizer(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> func)
        {
            if (func == null) throw new ArgumentNullException("func");

            this._func = func;
            this._values = null;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <param name="t4"></param>
        /// <param name="t5"></param>
        /// <param name="t6"></param>
        /// <param name="t7"></param>
        /// <param name="t8"></param>
        /// <returns></returns>
        public TResult Invoke(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
        {
            TResult val;

            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4, t5, t6, t7, t8);

            // Try to get a memoized value:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();
            else if (_values.TryGetValue(key, out val))
                return val;

            // Add the value to the look-up:
            val = _func(t1, t2, t3, t4, t5, t6, t7, t8);
            _values.Add(key, val);
            return val;
        }

        /// <summary>
        /// Clears the last computed value for the given parameter values.
        /// </summary>
        public void Clear(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
        {
            if (_values == null) return;

            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4, t5, t6, t7, t8);

            _values.Remove(key);
        }

        /// <summary>
        /// Sets a specific value to be returned for the given parameter values.
        /// </summary>
        public TResult Override(TResult newValue, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
        {
            // Construct a key for look-up:
            Key key = new Key(t1, t2, t3, t4, t5, t6, t7, t8);

            // Create the dictionary if it does not exist yet:
            if (_values == null)
                _values = new Dictionary<Key, TResult>();

            _values[key] = newValue;

            return newValue;
        }

        /// <summary>
        /// Invokes the function with the parameter values or reuses the result from a previous invocation with the same parameter values.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t3"></param>
        /// <param name="t4"></param>
        /// <param name="t5"></param>
        /// <param name="t6"></param>
        /// <param name="t7"></param>
        /// <param name="t8"></param>
        /// <returns></returns>
        public TResult this[T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8]
        {
            get
            {
                return Invoke(t1, t2, t3, t4, t5, t6, t7, t8);
            }
            set
            {
                Override(value, t1, t2, t3, t4, t5, t6, t7, t8);
            }
        }
    }


    public static class Function
    {
        public static Memoizer<T1, TResult> Memoize<T1, TResult>(Func<T1, TResult> func)
        {
            return new Memoizer<T1, TResult>(func);
        }

        public static Memoizer<T1, T2, TResult> Memoize<T1, T2, TResult>(Func<T1, T2, TResult> func)
        {
            return new Memoizer<T1, T2, TResult>(func);
        }

        public static Memoizer<T1, T2, T3, TResult> Memoize<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func)
        {
            return new Memoizer<T1, T2, T3, TResult>(func);
        }

        public static Memoizer<T1, T2, T3, T4, TResult> Memoize<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func)
        {
            return new Memoizer<T1, T2, T3, T4, TResult>(func);
        }

        public static Memoizer<T1, T2, T3, T4, T5, TResult> Memoize<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> func)
        {
            return new Memoizer<T1, T2, T3, T4, T5, TResult>(func);
        }

        public static Memoizer<T1, T2, T3, T4, T5, T6, TResult> Memoize<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> func)
        {
            return new Memoizer<T1, T2, T3, T4, T5, T6, TResult>(func);
        }

        public static Memoizer<T1, T2, T3, T4, T5, T6, T7, TResult> Memoize<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> func)
        {
            return new Memoizer<T1, T2, T3, T4, T5, T6, T7, TResult>(func);
        }

        public static Memoizer<T1, T2, T3, T4, T5, T6, T7, T8, TResult> Memoize<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> func)
        {
            return new Memoizer<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(func);
        }

    }
}