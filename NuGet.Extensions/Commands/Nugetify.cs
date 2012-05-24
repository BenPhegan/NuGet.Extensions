using System;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.IO;
using Microsoft.Build.Evaluation;
using NuGet.Commands;
using NuGet.Extensions.GetLatest.MSBuild;

namespace NuGet.Extensions.GetLatest.Commands
{
    public class Nugetify : Command
    {

        private readonly IPackageRepositoryFactory RepositoryFactory;
        private readonly IPackageSourceProvider SourceProvider;

        [ImportingConstructor]
        public Nugetify(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            Contract.Assert(packageRepositoryFactory != null);
            Contract.Assert(sourceProvider != null);

            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            if (!String.IsNullOrEmpty(Arguments[0]))
            {
                FileInfo solutionFile = new FileInfo(Arguments[0]);
                if (solutionFile.Exists && solutionFile.Extension == ".sln")
                {
                    var solution = new Solution(solutionFile.FullName);
                    var simpleProjectObjects = solution.Projects;

                    foreach (var simpleProject in simpleProjectObjects)
                    {
                        string projectPath = Path.Combine(solutionFile.Directory.FullName, simpleProject.RelativePath);
                        Project project = new Project(projectPath);
                        var references = project.GetItems("Reference");
                        foreach (var reference in references)
                        {
                            //TODO deal with GAC assemblies that we want to replace as well....
                            if (reference.HasMetadata("HintPath"))
                            {

                                var hintPath = reference.GetMetadataValue("HintPath");

                            }
                        }
                        Console.Write("");
                    }
                }
            }
        }
    }
}
