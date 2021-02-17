using System;
using System.Collections.Generic;

namespace LogGrokCore.Controls
{
    public class GenericEqualityComparer<T> : EqualityComparer<T> where T : struct, IEquatable<T>
    {
        public override bool Equals(T x, T y)
        {
            return x.Equals(y);
        }

        public override int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }
}
