using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using NuGet.Common;

namespace NuGet.Extensions.MSBuild
{
    public sealed class CachingProjectLoader : IProjectLoader, IDisposable
    {
        private readonly IConsole _console;
        private readonly ProjectCollection _projectCollection;
        private readonly Dictionary<Guid, IVsProject> _projectsByGuid;
        private readonly IDictionary<string, string> _globalMsBuildProperties;
        private readonly IProjectLoader _projectLoader;

        public CachingProjectLoader(IDictionary<string, string> globalMsBuildProperties, IConsole console)
        {
            _globalMsBuildProperties = globalMsBuildProperties;
            _console = console;
            _projectCollection = new ProjectCollection();
            _projectsByGuid = new Dictionary<Guid, IVsProject>();
            _projectLoader = this;
        }

        public void Dispose()
        {
            _projectCollection.Dispose();
        }

        public IVsProject GetProject(Guid? projectGuid, string absoluteProjectPath)
        {
            IVsProject projectAdapter;
            if (projectGuid.HasValue && _projectsByGuid.TryGetValue(projectGuid.Value, out projectAdapter))
            {
                return projectAdapter;
            }

            projectAdapter = GetVsProjectFromPath(absoluteProjectPath);

            //TODO This could cause an incorrect mapping, get the guid from the loaded project
            if (projectGuid.HasValue) _projectsByGuid.Add(projectGuid.Value, projectAdapter);
            else _console.WriteWarning("Attempting to workaround a project reference without a GUID for {0}", absoluteProjectPath);

            return projectAdapter;
        }

        private IVsProject GetVsProjectFromPath(string absoluteProjectPath)
        {
            try
            {
                return GetMsBuildProjectAdapterFromPath(absoluteProjectPath);
            }
            catch (Exception e)
            {
                LogProjectLoadException(e, absoluteProjectPath);
                return new NullProjectAdapter(absoluteProjectPath);
            }
        }

        private IVsProject GetMsBuildProjectAdapterFromPath(string absoluteProjectPath)
        {
            var msBuildProject = GetMsBuildProject(absoluteProjectPath);
            return GetRealProjectAdapter(msBuildProject);
        }

        private void LogProjectLoadException(Exception e, string absoluteProjectPath)
        {
            _console.WriteWarning("Problem loading {0}, any future messages about modifications to it are speculative only:", absoluteProjectPath);
            _console.WriteWarning("  {0}", e.Message);
        }

        private IVsProject GetRealProjectAdapter(Project msBuildProject)
        {
            var projectGuid = GetProjectGuid(msBuildProject);
            IVsProject projectAdapter;
            return _projectsByGuid.TryGetValue(projectGuid, out projectAdapter) ? projectAdapter : new ProjectAdapter(msBuildProject, _projectLoader);
        }

        private static Guid GetProjectGuid(Project msBuildProject)
        {
            return Guid.Parse(msBuildProject.GetPropertyValue("ProjectGuid"));
        }

        private Project GetMsBuildProject(string projectPath)
        {
            var canonicalProjectPath = GetCanonicalPath(projectPath);
            var existing = _projectCollection.GetLoadedProjects(canonicalProjectPath).SingleOrDefault();
            return existing ?? new Project(canonicalProjectPath, _globalMsBuildProperties, null, _projectCollection);
        }

        private static string GetCanonicalPath(string projectPath)
        {
            return Path.GetFullPath(projectPath).ToLowerInvariant();
        }
    }
}