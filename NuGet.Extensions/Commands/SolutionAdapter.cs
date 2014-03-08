using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using NuGet.Common;
using NuGet.Extensions.MSBuild;

namespace NuGet.Extensions.Commands
{
    public class SolutionAdapter {
        private readonly IConsole _console;

        public SolutionAdapter(IConsole console)
        {
            _console = console;
        }

        public List<ProjectAdapter> GetProjectsFromSolution(FileInfo solutionFile, DirectoryInfo solutionRoot, ProjectCollection projectCollection)
        {
            var solution = new Solution(solutionFile.FullName);
            var simpleProjectObjects = solution.Projects;
            var projectAdapters = simpleProjectObjects.Select(p => GetProjectAdapterOrDefault(solutionRoot, p, projectCollection)).Where(p => p != null).ToList();
            return projectAdapters;
        }

        private ProjectAdapter GetProjectAdapterOrDefault(DirectoryInfo solutionRoot, SolutionProject simpleProject, ProjectCollection projectCollection)
        {
            var projectPath = Path.Combine(solutionRoot.FullName, simpleProject.RelativePath);
            if (File.Exists(projectPath))
            {
                return new ProjectAdapter(projectPath, projectCollection: projectCollection);
            }
            else
            {
                _console.WriteWarning("Project: {0} was not found on disk", simpleProject.ProjectName);
                return null;
            }
        }
    }
}