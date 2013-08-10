using System.Collections.Generic;

namespace NuGet.Extras
{
    /// <summary>
    /// Provides a blank set of user settings...
    /// </summary>
    public class BlankUserSettings : ISettings
    {
        /// <summary>
        /// Deletes a section...or not
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public bool DeleteSection(string section)
        {
            return true;
        }

        /// <summary>
        /// Delete a value or not.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool DeleteValue(string section, string key)
        {
            return true;
        }

        /// <summary>
        /// Doesnt really get a value.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetValue(string section, string key)
        {
            return null;
        }

        public string GetValue(string section, string key, bool isPath)
        {
            return null;
        }

        /// <summary>
        /// Seriously, more comments than sense.
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public IList<KeyValuePair<string, string>> GetValues(string section)
        {
            return null;
        }

        public IList<KeyValuePair<string, string>> GetValues(string section, bool isPath)
        {
            return null;
        }

        /// <summary>
        /// Doesnt set a value
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetValue(string section, string key, string value)
        {
        }

        /// <summary>
        /// Plurally fails to set values.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="values"></param>
        public void SetValues(string section, IList<KeyValuePair<string, string>> values)
        {
        }

        /// <summary>
        /// Tweet tweet (nested...get it?)
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public IList<KeyValuePair<string, string>> GetNestedValues(string section, string key)
        {
            return new List<KeyValuePair<string, string>>();
        }

        /// <summary>
        /// I got nothin....
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="values"></param>
        public void SetNestedValues(string section, string key, IList<KeyValuePair<string, string>> values)
        {
        }
    }
}
