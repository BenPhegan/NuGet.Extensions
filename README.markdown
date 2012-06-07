# NuGet.Extensions

Provides a set of extensions to NuGet.  These include:

## GET
Targetted at the package restore use case, but does only what it says (it **get*s the packages, not install or update).  Simplified restore of full solution or directory structure NuGet package requirements.  Designed to be very fast for large numbers of package dependencies.  Provides caching, minimum package set identification including version ranges, resolution and download of only the smallest distinct set of packages per solution and many more advantages.  

## FINDASSEMBLY
Find a specific assembly within packages on a specific feed.

## GRAPH
Provides the ability to graph the package dependency for an entire feed.  Also lets you know if there are any cyclic dependencies on your feed.  Primarily aimed at large internal feed management.

## TEAMCITY
The **teamcity** command produces a dependency graph of all NuGet publish and subscribe relationships on a TeamCity build server.  With the ability to pack, publish and trigger directly from NuGet packages, as well as its ability to function as a NuGet OData feed, TeamCity provides the perfect platform to produce and consume NuGet packages on.  This command makes it very easy to graphically represent these publish/subscribe realationships.

## NUGETIFY
Modify a solution to use NuGet packages instead of existing references (INCOMPLETE)

## CLONE
Clone a feed from one location to another (INCOMPLETE)

## COPY
Copy a package from one feed to another (INCOMPLETE)