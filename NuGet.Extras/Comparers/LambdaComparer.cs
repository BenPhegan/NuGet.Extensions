using System;
using System.Collections.Generic;

namespace NuGet.Extras.Comparers
{
    /// <summary>
    /// Allows comparers to be used via Lambdas
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _comparer;
        private readonly Func<T, int> _hashCodeResolver;

        /// <summary>
        /// Creates a LambdaComparer
        /// </summary>
        /// <param name="comparer"></param>
        /// <param name="hashCodeResolver"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public LambdaComparer(Func<T, T, bool> comparer, Func<T, int> hashCodeResolver)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");
            if (hashCodeResolver == null)
                throw new ArgumentNullException("hashCodeResolver");

            _comparer = comparer;
            _hashCodeResolver = hashCodeResolver;
        }

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        /// <param name="x">The first object of type <paramref>
        ///                                            <name>T</name>
        ///                                          </paramref> to compare.</param><param name="y">The second object of type <paramref>
        ///                                                                                                                     <name>T</name>
        ///                                                                                                                   </paramref> to compare.</param>
        public bool Equals(T x, T y)
        {
            return _comparer(x, y);
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <returns>
        /// A hash code for the specified object.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param><exception cref="T:System.ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is null.</exception>
        public int GetHashCode(T obj)
        {
            return _hashCodeResolver(obj);
        }
    }
}
