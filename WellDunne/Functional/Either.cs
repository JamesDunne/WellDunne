using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    /// <summary>
    /// A mutual exclusion container for two values.
    /// </summary>
    /// <typeparam name="TLeft"></typeparam>
    /// <typeparam name="TRight"></typeparam>
    public struct Either<TLeft, TRight>
    {
        public enum Selected
        {
            Neither,
            Left,
            Right
        }

        private readonly Selected _which;
        public Selected Which { get { return _which; } }

        public bool IsLeft { get { return _which == Selected.Left; } }
        public bool IsRight { get { return _which == Selected.Right; } }

        private readonly TLeft _Left;
        public TLeft Left { get { if (_which == Selected.Right) throw new NullReferenceException(); return _Left; } }
        private readonly TRight _Right;
        public TRight Right { get { if (_which == Selected.Left) throw new NullReferenceException(); return _Right; } }

        public Maybe<TLeft> MaybeLeft { get { if (_which == Selected.Left) return _Left; else return Maybe<TLeft>.Nothing; } }
        public Maybe<TRight> MaybeRight { get { if (_which == Selected.Right) return _Right; else return Maybe<TRight>.Nothing; } }

        public Either(TLeft left)
        {
            _which = Selected.Left;
            _Left = left;
            _Right = default(TRight);
        }

        public Either(TRight right)
        {
            _which = Selected.Right;
            _Left = default(TLeft);
            _Right = right;
        }

        public static implicit operator Either<TLeft, TRight>(TLeft left)
        {
            return new Either<TLeft, TRight>(left);
        }

        public static implicit operator Either<TLeft, TRight>(TRight right)
        {
            return new Either<TLeft, TRight>(right);
        }

        public TResult Collapse<TResult>(Func<TLeft, TResult> collapseIfLeft, Func<TRight, TResult> collapseIfRight)
        {
            if (_which == Selected.Left)
                return collapseIfLeft(_Left);
            else if (_which == Selected.Right)
                return collapseIfRight(_Right);
            else
                throw new InvalidOperationException();
        }

        public void Act(Action<TLeft> actionIfLeft, Action<TRight> actionIfRight)
        {
            if (IsLeft)
                actionIfLeft(_Left);
            else if (IsRight)
                actionIfRight(_Right);
            else
                throw new InvalidOperationException();
        }

        public Either<TNewLeft, TNewRight> CastBoth<TNewLeft, TNewRight>(Func<TLeft, TNewLeft> tryLeft, Func<TRight, TNewRight> tryRight)
        {
            if (IsLeft)
                return new Either<TNewLeft, TNewRight>(tryLeft(_Left));
            else if (IsRight)
                return new Either<TNewLeft, TNewRight>(tryRight(_Right));
            else
                throw new InvalidOperationException();
        }

        public Either<TLeft, TNewRight> CastRight<TNewRight>(Func<TRight, TNewRight> tryRight)
        {
            if (IsLeft)
                return _Left;
            else if (IsRight)
                return new Either<TLeft, TNewRight>(tryRight(_Right));
            else
                throw new InvalidOperationException();
        }

        public Either<TNewLeft, TRight> CastLeft<TNewLeft>(Func<TLeft, TNewLeft> tryLeft)
        {
            if (IsRight)
                return _Right;
            else if (IsLeft)
                return new Either<TNewLeft, TRight>(tryLeft(_Left));
            else
                throw new InvalidOperationException();
        }
    }
}
