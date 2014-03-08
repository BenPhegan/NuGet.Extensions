using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using NuGet.Common;

namespace NuGet.Extensions.MSBuild
{
    public class SolutionAdapter : IDisposable {
        private readonly FileInfo _solutionFile;
        private readonly IConsole _console;
        private readonly ProjectCollection _projectCollection;
        private readonly Lazy<IDictionary<Guid, ProjectAdapter>> _projectsInSolutionByGuid;

        public SolutionAdapter(FileInfo solutionFile, IConsole console)
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

        public static List<string> GetAssemblyNamesForProjectReferences(ProjectAdapter project)
        {
            var assembliesReferenced = new List<string>();
            var projectReferences = project.GetProjectReferences();
            using (var projectCollection = new ProjectCollection())
            {
                foreach (var reference in projectReferences)
                {
                    var newProjectPath = Path.Combine(project.ProjectDirectory.FullName, reference.IncludeName);
                    var refProjectAdapter = new ProjectAdapter(newProjectPath, projectCollection: projectCollection);
                    assembliesReferenced.Add(refProjectAdapter.AssemblyName);
                }
            }
            return assembliesReferenced;
        }

        private ProjectAdapter ProjectAdapter(SolutionProject p)
        {
            return new ProjectAdapter(GetAbsoluteProjectPath(p), _projectCollection);
        }

        private static Guid ProjectGuid(SolutionProject p)
        {
            return Guid.Parse(p.ProjectGuid);
        }

        private bool ProjectExists(SolutionProject simpleProject)
        {
            var projectPath = GetAbsoluteProjectPath(simpleProject);
            if (File.Exists(projectPath)) return true;

            _console.WriteWarning("Project: {0} was not found on disk", simpleProject.ProjectName);
            return false;
        }

        private string GetAbsoluteProjectPath(SolutionProject simpleProject)
        {
            return Path.Combine(_solutionFile.Directory.FullName, simpleProject.RelativePath);
        }

    }
}