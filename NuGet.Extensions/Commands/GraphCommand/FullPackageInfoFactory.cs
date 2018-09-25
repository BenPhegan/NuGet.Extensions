using System.Collections.Generic;
using System.Linq;
using QuickGraph.Algorithms.Search;

namespace NuGet.Extensions.Commands.GraphCommand
{
    public class FullPackageInfoFactory : IPackageInfoFactory
    {
        private readonly List<FullPackageInfo> _packages = new List<FullPackageInfo>();

        public IPackageInfo From(IPackage package)
        {
            FullPackageInfo me = _packages.FirstOrDefault(p => p.Is(package));
            if (me == null)
            {
                me = new FullPackageInfo(package);
                _packages.Add(me);
            }
            else
            {
                me.Update(package);
            }

            return me;
        }

        public IPackageInfo From(PackageDependency dependency)
        {
            FullPackageInfo refs = _packages.FirstOrDefault(p => p.Is(dependency));
            if (refs == null)
            {
                refs = new FullPackageInfo(dependency);
                _packages.Add(refs);
            }
            else
            {
                refs.Add(dependency);
            }

            return refs;
        }
    }
}