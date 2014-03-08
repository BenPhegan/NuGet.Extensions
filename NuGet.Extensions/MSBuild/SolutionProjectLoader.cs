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
        private readonly Lazy<IDictionary<Guid, ProjectAdapter>> _projectsInSolutionByGuid;

        public SolutionProjectLoader(FileInfo solutionFile, IConsole console)
        {
            _solutionFile = solutionFile;
            _console = console;
            _projectCollection = new ProjectCollection();
            _projectsInSolutionByGuid = new Lazy<IDictionary<Guid, ProjectAdapter>>(LoadProjectsInSolutionByGuid);
        }

        public List<ProjectAdapter> GetProjects()
        {
            return ProjectsByGuid.Values.ToList();
        }

        private IDictionary<Guid, ProjectAdapter> ProjectsByGuid
        {
            get { return _projectsInSolutionByGuid.Value; }
        }

        private IDictionary<Guid, ProjectAdapter> LoadProjectsInSolutionByGuid()
        {
            var solution = new Solution(_solutionFile.FullName);
            return solution.Projects.Where(ProjectExists).ToDictionary(ProjectGuid, ProjectAdapter);
        }

        public void Dispose()
        {
            _projectCollection.Dispose();
        }

        public IVsProject GetProject(Guid projectGuid, string projectPath)
        {
            ProjectAdapter projectAdapter;
            if (!ProjectsByGuid.TryGetValue(projectGuid, out projectAdapter))
            {
                _console.WriteWarning("Project {0} should have been referenced in the solution with guid {1}", Path.GetFileName(projectPath), projectGuid);
                projectAdapter = CreateProjectAdapter(projectPath);
                ProjectsByGuid.Add(projectGuid, projectAdapter);
            }
            return projectAdapter;
        }

        private ProjectAdapter ProjectAdapter(SolutionProject p)
        {
            return CreateProjectAdapter(p.RelativePath);
        }

        private ProjectAdapter CreateProjectAdapter(string relativePath)
        {
            var projectLoader = (IProjectLoader) this;
            return new ProjectAdapter(GetAbsoluteProjectPath(relativePath), _projectCollection, projectLoader);
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