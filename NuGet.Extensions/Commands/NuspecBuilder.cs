using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NuGet.Common;

namespace NuGet.Extensions.Commands
{
    public class NuspecBuilder {
        public NuspecBuilder()
        {
        }

        public Manifest CreateNuspecManifest(INuspecDataSource nuspecData, string assemblyOutput, List<ManifestDependency> manifestDependencies, string targetFramework = ".NET Framework, Version=4.0")
        {
            var manifest = new Manifest
                           {
                               Metadata =
                               {
                                   //TODO need to revisit and get the TargetFramework from the assembly...
                                   DependencySets = new List<ManifestDependencySet>
                                                    {
                                                        new ManifestDependencySet{Dependencies = manifestDependencies,TargetFramework = targetFramework}
                                                    },
                                   Id = nuspecData.Id ?? assemblyOutput,
                                   Title = nuspecData.Title ?? assemblyOutput,
                                   Version = "$version$",
                                   Description = nuspecData.Description ?? assemblyOutput,
                                   Authors = nuspecData.Author ?? "$author$",
                                   Tags = nuspecData.Tags ?? "$tags$",
                                   LicenseUrl = nuspecData.LicenseUrl ?? "$licenseurl$",
                                   RequireLicenseAcceptance = nuspecData.RequireLicenseAcceptance,
                                   Copyright = nuspecData.Copyright ?? "$copyright$",
                                   IconUrl = nuspecData.IconUrl ?? "$iconurl$",
                                   ProjectUrl = nuspecData.ProjectUrl ?? "$projrcturl$",
                                   Owners = nuspecData.Owners ?? nuspecData.Author ?? "$author$"                                          
                               },
                               Files = new List<ManifestFile>
                                       {
                                           new ManifestFile
                                           {
                                               Source = assemblyOutput + ".dll",
                                               Target = "lib"
                                           }
                                       }
                           };

            //Dont add a releasenotes node if we dont have any to add...
            if (!String.IsNullOrEmpty(nuspecData.ReleaseNotes)) manifest.Metadata.ReleaseNotes = nuspecData.ReleaseNotes;

            return manifest;
        }

        public void Save(IConsole console, Manifest manifest, string nuspecFile)
        {
            try
            {
                console.WriteLine("Saving new NuSpec: {0}", nuspecFile);
                using (var stream = new MemoryStream())
                {
                    manifest.Save(stream, validate: false);
                    stream.Seek(0, SeekOrigin.Begin);
                    var content = stream.ReadToEnd();
                    File.WriteAllText(nuspecFile, RemoveSchemaNamespace(content));
                }
            }
            catch (Exception)
            {
                console.WriteError("Could not save file: {0}", nuspecFile);
                throw;
            }
        }

        private static string RemoveSchemaNamespace(string content)
        {
            // This seems to be the only way to clear out xml namespaces.
            return Regex.Replace(content, @"(xmlns:?[^=]*=[""][^""]*[""])", String.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }
    }
}