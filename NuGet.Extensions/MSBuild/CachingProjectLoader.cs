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

        public IVsProject GetProject(Guid projectGuid, string absoluteProjectPath)
        {
            IVsProject projectAdapter;
            if (_projectsByGuid.TryGetValue(projectGuid, out projectAdapter)) return projectAdapter;

            projectAdapter = GetProjectAdapterFromPath(absoluteProjectPath);
            _projectsByGuid.Add(projectGuid, projectAdapter); //TODO This could cause an incorrect mapping, get the guid from the loaded project
            return projectAdapter;
        }

        private IVsProject GetProjectAdapterFromPath(string absoluteProjectPath)
        {
            try
            {
                return GetMsBuildProjectAdapterFromPath(absoluteProjectPath);
            }
            catch (Exception e)
            {
                LogProjectLoadException(e);
                return new NullProjectAdapter(absoluteProjectPath);
            }
        }

        private IVsProject GetMsBuildProjectAdapterFromPath(string absoluteProjectPath)
        {
            var msBuildProject = GetMsBuildProject(absoluteProjectPath, _projectCollection, _globalMsBuildProperties);
            return GetRealProjectAdapter(_projectLoader, msBuildProject, _projectsByGuid);
        }

        private void LogProjectLoadException(Exception e)
        {
            _console.WriteWarning("Problem loading {0}, any future messages about modifications to it are speculative only:");
            _console.WriteWarning("  {0}", e.Message);
        }

        private static IVsProject GetRealProjectAdapter(IProjectLoader projectLoader, Project msBuildProject, IDictionary<Guid, IVsProject> projectsByGuidCache)
        {
            var projectGuid = Guid.Parse(GetProjectGuid(msBuildProject));
            IVsProject projectAdapter;
            return projectsByGuidCache.TryGetValue(projectGuid, out projectAdapter) ? projectAdapter : new ProjectAdapter(msBuildProject, projectLoader);
        }

        private static string GetProjectGuid(Project msBuildProject)
        {
            return msBuildProject.GetPropertyValue("ProjectGuid");
        }

        private static Project GetMsBuildProject(string projectPath, ProjectCollection projectCollection, IDictionary<string, string> globalMsBuildProperties)
        {
            var canonicalProjectPath = Path.GetFullPath(projectPath).ToLowerInvariant();
            var existing = projectCollection.GetLoadedProjects(canonicalProjectPath).SingleOrDefault();
            return existing ?? new Project(canonicalProjectPath, globalMsBuildProperties, null, projectCollection);
        }
    }
}