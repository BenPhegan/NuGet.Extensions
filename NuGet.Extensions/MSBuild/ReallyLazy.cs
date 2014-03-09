using System;

namespace NuGet.Extensions.MSBuild
{
    public class ReallyLazy<T> where T: new()
    {
        private readonly Lazy<T> _lazyValue;
        public ReallyLazy(Func<T> getValue)
        {
            _lazyValue = new Lazy<T>(getValue);
        }

        /// <param name="createIfNotExists">Defaults to false, set to true to request the full calculated value</param>
        /// <returns>new <typeparamref name="T"/>() unless <paramref name="createIfNotExists"/> is true</returns>
        public T GetValue(bool createIfNotExists = false)
        {
            if (createIfNotExists) return _lazyValue.Value;
            return _lazyValue.IsValueCreated ? _lazyValue.Value : new T();
        }
    }
}