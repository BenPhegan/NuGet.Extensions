using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using NuGet.Common;

namespace NuGet.Extensions.MSBuild
{
    public class SolutionProjectLoader : IDisposable, IProjectLoader
    {
        private readonly FileInfo _solutionFile;
        private readonly IConsole _console;
        private readonly ProjectCollection _projectCollection;
        private readonly ReallyLazy<Dictionary<Guid, IVsProject>> _projectsByGuid;
        private readonly IDictionary<string, string> _globalMsBuildProperties;

        public SolutionProjectLoader(FileInfo solutionFile, IConsole console, IDictionary<string, string> globalMsBuildProperties = null)
        {
            _globalMsBuildProperties = globalMsBuildProperties ?? new Dictionary<string, string>();
            _globalMsBuildProperties["SolutionDir"] = solutionFile.Directory.FullName;
            _solutionFile = solutionFile;
            _console = console;
            _projectCollection = new ProjectCollection();
            _projectsByGuid = new ReallyLazy<Dictionary<Guid, IVsProject>>(LoadProjectsInSolutionByGuid);
        }

        public List<IVsProject> GetProjects()
        {
            return _projectsByGuid.GetValue(true).Values.ToList();
        }
        
        private Dictionary<Guid, IVsProject> LoadProjectsInSolutionByGuid()
        {
            var solution = new Solution(_solutionFile.FullName);
            return solution.Projects.Where(ProjectExists).ToDictionary(ProjectGuid, CreateProjectAdapter);
        }


        private static Guid ProjectGuid(SolutionProject p)
        {
            return Guid.Parse(p.ProjectGuid);
        }

        private IVsProject CreateProjectAdapter(SolutionProject p)
        {
            return CreateProjectAdapter(GetAbsoluteProjectPath(p.RelativePath));
        }

        private bool ProjectExists(SolutionProject simpleProject)
        {
            var projectPath = GetAbsoluteProjectPath(simpleProject.RelativePath);
            if (File.Exists(projectPath)) return true;
            
            if (!Directory.Exists(projectPath)) _console.WriteWarning("Project: {0} was not found on disk", simpleProject.ProjectName);
            return false;
        }

        private string GetAbsoluteProjectPath(string relativePath)
        {
            return Path.Combine(_solutionFile.Directory.FullName, relativePath);
        }


        public void Dispose()
        {
            _projectCollection.Dispose();
        }

        public IVsProject GetProject(Guid projectGuid, string absoluteProjectPath)
        {
            IVsProject projectAdapter;
            if (_projectsByGuid.GetValue(true).TryGetValue(projectGuid, out projectAdapter)) return projectAdapter;

            projectAdapter = CreateProjectAdapter(absoluteProjectPath);
            _console.WriteLine("Potential authoring issue: Project {0} should have been referenced in the solution with guid {1}", Path.GetFileName(absoluteProjectPath), projectGuid);
            _projectsByGuid.GetValue(true).Add(projectGuid, projectAdapter);
            return projectAdapter;
        }

        private IVsProject CreateProjectAdapter(string absoluteProjectPath)
        {
            try
            {
                var msBuildProject = GetMsBuildProject(absoluteProjectPath, _projectCollection, _globalMsBuildProperties);
                return GetRealProjectAdapter(this, msBuildProject, _projectsByGuid.GetValue());
            }
            catch (Exception e)
            {
                var nullProjectAdapter = new NullProjectAdapter(absoluteProjectPath);
                _console.WriteWarning("Problem loading {0}, any future messages about modifications to it are speculative only:");
                _console.WriteWarning("  {0}", e.Message);
                return nullProjectAdapter;
            }
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