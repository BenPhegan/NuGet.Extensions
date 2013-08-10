using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace NuGet.Extras.ExtensionMethods
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
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = obj.GetType();
            FieldInfo field = type.GetField(name, flags);
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
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = obj.GetType();
            PropertyInfo field = type.GetProperty(name, flags);
            return (T)field.GetValue(obj, null);
        }
    }
}
