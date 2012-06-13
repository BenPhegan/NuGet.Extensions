# NuGet.Extensions

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

For an example of the output, the graph below was produced by pointing it at http://nuget.org.  Turns out, their graph is currently cyclic (this is a GOOD thing!).  There are quite a lot of disconnected graphs, and some quite interesting clusters of usage.  Worth a browse.

<img src="http://github.com/BenPhegan/NuGet.Extensions/raw/master/images/NuGetOrgDependencyGraph.png" alt="NuGet TeamCity Package Dependency Graph" height="500" width="700" />

To have a play with the data yourself, you can load the DGML graph directly into Visual Studio 2010 or greater from [here.](https://raw.github.com/BenPhegan/NuGet.Extensions/master/images/NuGetOrgDependencyGraph.dgml)

## TEAMCITY
The **teamcity** command produces a dependency graph of all NuGet publish and subscribe relationships on a TeamCity build server.  With the ability to pack, publish and trigger directly from NuGet packages, as well as its ability to function as a NuGet OData feed, TeamCity provides the perfect platform to produce and consume NuGet packages on.  This command makes it very easy to graphically represent these publish/subscribe realationships.

Using the option -packageasvertex, you will get a graph that represents each build configuration as a solid node, and each package as a label with edges representing dependencies.  See below for an example.  This graph was built with packages being published via both NuGet Package build steps, and directly from the artifacts.

<img src="http://github.com/BenPhegan/NuGet.Extensions/raw/master/images/TeamCityPackageTriggers.png" alt="NuGet TeamCity Package Dependency Graph" height="500" width="700" />

To have a play with the data yourself, you can load the DGML graph directly into Visual Studio 2010 or greater from [here.](https://raw.github.com/BenPhegan/NuGet.Extensions/master/images/TeamCityPackageTriggers.dgml)

## NUGETIFY
Modify a solution to use NuGet packages instead of existing references (INCOMPLETE)

## CLONE
Clone a feed from one location to another (INCOMPLETE)

## COPY
Copy a package from one feed to another (INCOMPLETE)


[1]: http://github.com/BenPhegan/NuGet.Extensions/raw/master/images/NuGetOrgDependencyGraph.png  "Optional title attribute"