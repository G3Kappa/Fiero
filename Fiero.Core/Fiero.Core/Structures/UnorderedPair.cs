﻿namespace Fiero.Core.Structures
{
    public readonly struct UnorderedPair<T>
    {
        public readonly T Left;
        public readonly T Right;

        public bool Contains(T v) => Equals(Left, v) || Equals(Right, v);

        public UnorderedPair(T l, T r)
        {
            Left = l;
            Right = r;
        }

        public override int GetHashCode() => Left.GetHashCode() * Right.GetHashCode();
        public override bool Equals(object obj)
        {
            if (obj is UnorderedPair<T> other)
            {
                return Equals(other.Left, Left) && Equals(other.Right, Right)
                    || Equals(other.Right, Left) && Equals(other.Left, Right);
            }
            return false;
        }

        public void Deconstruct(out T left, out T right)
        {
            left = Left;
            right = Right;
        }

        public static bool operator ==(UnorderedPair<T> left, UnorderedPair<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnorderedPair<T> left, UnorderedPair<T> right)
        {
            return !(left == right);
        }

        public override string ToString() => $"({Left}, {Right})";
    }
}
