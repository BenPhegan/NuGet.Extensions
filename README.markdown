# NuGet.Extensions
[![Build status](https://ci.appveyor.com/api/projects/status/cd6eu7k8l30sk4kp)](https://ci.appveyor.com/project/BenPhegan/nuget-extensions)

Provides a set of extensions to NuGet.  Many of these are aimed at large corporate Development Operations teams, and the management of large numbers of feeds, packages, relationships, and a different level of complexity than might be normally found in the consumption of NuGet packages for open source projects.

#Commands

## GET
Targetted at the package restore use case, but does only what it says (it **get*s the packages, not install or update).  Simplified restore of full solution or directory structure NuGet package requirements.  Designed to be very fast for large numbers of package dependencies.  Provides caching, minimum package set identification including version ranges, resolution and download of only the smallest distinct set of packages per solution and many more advantages.  

## FINDASSEMBLY
Find a specific assembly within packages on a specific feed.

## DETAILS
Allows you to query details about a specific package ID from the commandline.  Provides information such as the file paths, dependencies, authors etc.

## GRAPH
Provides the ability to graph the package dependency for an entire feed.  Also lets you know if there are any cyclic dependencies on your feed.  Primarily aimed at large internal feed management.

For an example of the output, the graph below was produced by pointing it at http://nuget.org.  Turns out, their graph is currently acyclic (this is a GOOD thing!).  There are quite a lot of disconnected graphs, and some quite interesting clusters of usage.  Worth a browse.

<img src="https://github.com/BenPhegan/NuGet.Extensions/raw/master/images/NuGetOrgDependencyGraph.png" alt="NuGet TeamCity Package Dependency Graph" height="500" width="700" />

To have a play with the data yourself, you can load the DGML graph directly into Visual Studio 2010 or greater from [here.](https://raw.githubusercontent.com/BenPhegan/NuGet.Extensions/master/images/NuGetOrgDependencyGraph.dgml)

## TEAMCITY
The **teamcity** command produces a dependency graph of all NuGet publish and subscribe relationships on a TeamCity build server.  With the ability to pack, publish and trigger directly from NuGet packages, as well as its ability to function as a NuGet OData feed, TeamCity provides the perfect platform to produce and consume NuGet packages on.  This command makes it very easy to graphically represent these publish/subscribe realationships.

Using the option -packageasvertex, you will get a graph that represents each build configuration as a solid node, and each package as a label with edges representing dependencies.  See below for an example.  This graph was built with packages being published via both NuGet Package build steps, and directly from the artifacts.

<img src="https://github.com/BenPhegan/NuGet.Extensions/raw/master/images/TeamCityPackageTriggers.png" alt="NuGet TeamCity Package Dependency Graph" height="500" width="700" />

To have a play with the data yourself, you can load the DGML graph directly into Visual Studio 2010 or greater from [here.](https://raw.githubusercontent.com/BenPhegan/NuGet.Extensions/master/images/TeamCityPackageTriggers.dgml)

## NUGETIFY
The "nugetify" command allows a simple conversion to using simple file references to using NuGet package references.  What it does:

1. Provided a feed and a solution, try and find a package on the feed that contains a file matching each file reference for each project.
1. Write packages.config files for each project with resolved package to reference mappings.
1. Update all hintpaths within projects.
1. Create packages directory and repositories.config file.
1. Optionally create a nuspec file for each project output, including adding dependencies and output files.

It works best if the feed you want to satisfy the file references with is a local directory.  To get a useful local directory, look at the "clone" command here as well.  Clone the feed locally, prune it if required, then run nugetify.  Using an SSD you can get pretty snappy results.

Note that by design, it will only provide a mapping between your file references and packages that will provide them. In order to pull in dependencies, after running `nuget nugetify` you can run `nuget update -reinstall`.

## CLONE
Clone a feed from one location to another.  Also provides the ability to refresh a feed (update any packages that already exist from another feed).

## COPY
Copy a package from one feed to another (INCOMPLETE)


[1]: http://github.com/BenPhegan/NuGet.Extensions/raw/master/images/NuGetOrgDependencyGraph.png  "Optional title attribute"
