using System.Collections.Generic;

namespace NuGet.Extensions.ReferenceAnalysers
{
    public class PublicKeyTokens
    {
        private const string CompactFramework = "969db8053d3322ac";
        private const string Ecma = "b03f5f7f11d50a3a";
        private const string Framework = "b77a5c561934e089";
        private const string Microsoft = "31bf3856ad364e35";
        private const string Silverlight = "7cec85d7bea7798e";
        private const string SomeSilverlight4 = "ddd0da4d3e678217";
        private const string SomeWindowsPhone = "24eec0d8c86cda1e";
        public static HashSet<string> UsedInNetFramework = new HashSet<string> {CompactFramework, Ecma, Framework, Microsoft, Silverlight, SomeSilverlight4, SomeWindowsPhone};
    }
}