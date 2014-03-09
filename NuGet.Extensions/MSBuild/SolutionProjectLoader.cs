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
        private readonly Lazy<IDictionary<Guid, IVsProject>> _projectsInSolutionByGuid;
        private readonly IDictionary<string, string> _globalMsBuildProperties = new Dictionary<string, string>();

        public SolutionProjectLoader(FileInfo solutionFile, IConsole console)
        {
            _solutionFile = solutionFile;
            _console = console;
            _projectCollection = new ProjectCollection();
            _projectsInSolutionByGuid = new Lazy<IDictionary<Guid, IVsProject>>(LoadProjectsInSolutionByGuid);
        }

        public List<IVsProject> GetProjects()
        {
            return ProjectsByGuid.Values.ToList();
        }

        private IDictionary<Guid, IVsProject> ProjectsByGuid
        {
            get { return _projectsInSolutionByGuid.Value; }
        }

        private IDictionary<Guid, IVsProject> LoadProjectsInSolutionByGuid()
        {
            var solution = new Solution(_solutionFile.FullName);
            return solution.Projects.Where(ProjectExists).ToDictionary(ProjectGuid, CreateProjectAdapter);
        }

        public void Dispose()
        {
            _projectCollection.Dispose();
        }

        public IVsProject GetProject(Guid projectGuid, string absoluteProjectPath)
        {
            IVsProject projectAdapter;
            if (!ProjectsByGuid.TryGetValue(projectGuid, out projectAdapter))
            {
                _console.WriteLine("Potential authoring issue: Project {0} should have been referenced in the solution with guid {1}", Path.GetFileName(absoluteProjectPath), projectGuid);
                projectAdapter = CreateProjectAdapter(absoluteProjectPath);
                ProjectsByGuid.Add(projectGuid, projectAdapter);
            }
            return projectAdapter;
        }

        private IVsProject CreateProjectAdapter(SolutionProject p)
        {
            return CreateProjectAdapter(GetAbsoluteProjectPath(p.RelativePath));
        }

        private IVsProject CreateProjectAdapter(string absoluteProjectPath)
        {
            var projectLoader = (IProjectLoader) this;
            try
            {
                var msBuildProject = CreateMsBuildProject(absoluteProjectPath, _projectCollection, _globalMsBuildProperties);
                return new ProjectAdapter(msBuildProject, projectLoader);
            }
            catch (Exception e)
            {
                var nullProjectAdapter = new NullProjectAdapter(absoluteProjectPath);
                _console.WriteWarning("Problem loading {0}, any future messages about modifications to it are speculative only:");
                _console.WriteWarning("  {0}", e.Message);
                return nullProjectAdapter;
            }
        }
        

        private static Project CreateMsBuildProject(string projectPath, ProjectCollection projectCollection, IDictionary<string, string> globalMsBuildProperties)
        {
            var canonicalProjectPath = Path.GetFullPath(projectPath).ToLowerInvariant();
            var existing = projectCollection.GetLoadedProjects(canonicalProjectPath).SingleOrDefault();
            return existing ?? new Project(canonicalProjectPath, globalMsBuildProperties, null, projectCollection);
        }

        private static Guid ProjectGuid(SolutionProject p)
        {
            return Guid.Parse(p.ProjectGuid);
        }

        private bool ProjectExists(SolutionProject simpleProject)
        {
            var projectPath = GetAbsoluteProjectPath(simpleProject.RelativePath);
            if (File.Exists(projectPath)) return true;

            _console.WriteWarning("Project: {0} was not found on disk", simpleProject.ProjectName);
            return false;
        }

        private string GetAbsoluteProjectPath(string relativePath)
        {
            return Path.Combine(_solutionFile.Directory.FullName, relativePath);
        }

    }
}