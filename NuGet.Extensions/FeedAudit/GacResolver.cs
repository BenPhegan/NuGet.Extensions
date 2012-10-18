using System;
using System.Runtime.InteropServices;

namespace NuGet.Extensions.FeedAudit
{
    /// <summary>
    /// http://trikks.wordpress.com/2011/07/13/programmatically-check-if-an-assembly-is-loaded-in-gac-with-c/
    /// </summary>
    public class GacResolver
    {
        public static bool AssemblyExist(string assemblyname, out string response)
        {
            try
            {
                response = QueryAssemblyInfo(assemblyname);
                return true;
            }
            catch (System.IO.FileNotFoundException e)
            {
                response = e.Message;
                return false;
            }
        }

        // If assemblyName is not fully qualified, a random matching may be 
        public static String QueryAssemblyInfo(string assemblyName)
        {
            var assembyInfo = new AssemblyInfo {cchBuf = 512};
            assembyInfo.currentAssemblyPath = new String('\0',assembyInfo.cchBuf);

            IAssemblyCache assemblyCache;

            // Get IAssemblyCache pointer
            var hr = GacApi.CreateAssemblyCache(out assemblyCache, 0);
            if (hr == IntPtr.Zero)
            {
                hr = assemblyCache.QueryAssemblyInfo(0, assemblyName, ref assembyInfo);
                if (hr != IntPtr.Zero)
                {
                    Marshal.ThrowExceptionForHR(hr.ToInt32());
                }
            }
            else
            {
                Marshal.ThrowExceptionForHR(hr.ToInt32());
            }
            return assembyInfo.currentAssemblyPath;
        }
    }

    internal class GacApi
    {
        [DllImport("fusion.dll")]
        internal static extern IntPtr CreateAssemblyCache(
            out IAssemblyCache ppAsmCache, int reserved);
    }

    // GAC Interfaces - IAssemblyCache. As a sample, non used vtable entries 
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
     Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
    internal interface IAssemblyCache
    {
        int Dummy1();
        [PreserveSig]
        IntPtr QueryAssemblyInfo(
            int flags,
            [MarshalAs(UnmanagedType.LPWStr)]
                String assemblyName,
            ref AssemblyInfo assemblyInfo);

        int Dummy2();
        int Dummy3();
        int Dummy4();
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AssemblyInfo
    {
        public int cbAssemblyInfo;
        public int assemblyFlags;
        public long assemblySizeInKB;

        [MarshalAs(UnmanagedType.LPWStr)]
        public String currentAssemblyPath;

        public int cchBuf;
    }
}