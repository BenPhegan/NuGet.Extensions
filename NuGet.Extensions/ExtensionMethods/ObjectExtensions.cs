using System;
using System.Reflection;

namespace NuGet.Extensions.ExtensionMethods
{
    /// <summary>
    /// Object extensions
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Allows typed retrieveal of private fields.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetPrivateField<T>(this object obj, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = obj.GetType();
            var field = type.GetField(name, flags);
            return (T)field.GetValue(obj);
        }

        /// <summary>
        /// Allows typed retrieval of private properties.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetPrivateProperty<T>(this object obj, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = obj.GetType();
            var field = type.GetProperty(name, flags);
            return (T)field.GetValue(obj, null);
        }
    }
}
