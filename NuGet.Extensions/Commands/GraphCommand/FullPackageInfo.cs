using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.Extensions.Commands.GraphCommand
{
    public class FullPackageInfo : SimplePackageInfo
    {
        private readonly List<PackageDependency> _dependencyConstraints = new List<PackageDependency>();
        private const string NotInstalledName = "<not installed>";

        private string _fullName;

        public FullPackageInfo(IPackage from) : base(from)
        {
            _dependencyConstraints.Add(new PackageDependency(Id));
            _fullName = from.ToString();
        }

        public void Add(PackageDependency dependency)
        {
            if (!_dependencyConstraints.Any(dep => Comparers.DependencyEquals(dependency, dep)))
            {
                var dummy = _dependencyConstraints.SingleOrDefault(dep => dep.VersionSpec == null);
                if (dummy != null)
                {
                    _dependencyConstraints.Remove(dummy);
                }

                _dependencyConstraints.Add(dependency);
                _dependencyConstraints.Sort(Comparers.DependencyComparer);
            }
        }

        public FullPackageInfo(PackageDependency to) : base(to)
        {
            _dependencyConstraints.Add(to);
            TargetInfo = to.ToString();
            _fullName = NotInstalledName;
        }

        public void Update(IPackage installedPackage)
        {
            _fullName = installedPackage.ToString();
            string dependencies = string.Join("\n", installedPackage.DependencySets.SelectMany(p => p.Dependencies));
            Details = "Dependencies: " + (string.IsNullOrEmpty(dependencies) ? "none" : ("\n" + dependencies));
        }

        private bool IsInstalled()
        {
            return _fullName != NotInstalledName;
        }

        public override string ToString()
        {
            string text = (IsInstalled() ? _fullName : "")
                   +
                   (IsInstalled() && _dependencyConstraints.Count == 1
                        ? ""
                        : ("\n" + string.Join("\n", _dependencyConstraints)));

            return text;
        }
    }
}
