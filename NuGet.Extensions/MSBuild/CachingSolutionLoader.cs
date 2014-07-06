using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Common;

namespace NuGet.Extensions.MSBuild
{
    public sealed class CachingSolutionLoader : IDisposable
    {
        private readonly FileInfo _solutionFile;
        private readonly IConsole _console;
        private readonly CachingProjectLoader _projectLoader;
        private readonly Lazy<ICollection<IVsProject>> _projectsInSolution;

        public CachingSolutionLoader(FileInfo solutionFile, IDictionary<string, string> globalMsBuildProperties, IConsole console)
        {
            _solutionFile = solutionFile;
            _console = console;
            _projectLoader = new CachingProjectLoader(globalMsBuildProperties, console);
            _projectsInSolution = new Lazy<ICollection<IVsProject>>(LoadProjectsInSolutionByGuid);
        }

        public List<IVsProject> GetProjects()
        {
            return new List<IVsProject>(_projectsInSolution.Value);
        }

        public void Dispose()
        {
            _projectLoader.Dispose();
        }

        private ICollection<IVsProject> LoadProjectsInSolutionByGuid()
        {
            var solution = new Solution(_solutionFile.FullName);
            return solution.Projects.Where(ProjectExists).Select(CreateProjectAdapter).ToList();
        }

        private IVsProject CreateProjectAdapter(SolutionProject p)
        {
            string absoluteProjectPath = GetAbsoluteProjectPath(p.RelativePath);
            return _projectLoader.GetProject(ProjectGuid(p), absoluteProjectPath);
        }

        private bool ProjectExists(SolutionProject simpleProject)
        {
            var projectPath = GetAbsoluteProjectPath(simpleProject.RelativePath);
            if (File.Exists(projectPath)) return true;
            
            if (!Directory.Exists(projectPath)) _console.WriteWarning("Project: {0} was not found on disk", simpleProject.ProjectName);
            return false;
        }

        private static Guid ProjectGuid(SolutionProject p)
        {
            return Guid.Parse(p.ProjectGuid);
        }

        private string GetAbsoluteProjectPath(string relativePath)
        {
            return Path.Combine(_solutionFile.Directory.FullName, relativePath);
        }
    }

}