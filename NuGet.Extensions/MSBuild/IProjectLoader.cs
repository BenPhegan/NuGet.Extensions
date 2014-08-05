using System;

namespace NuGet.Extensions.MSBuild
{
    public interface IProjectLoader 
    {
        IVsProject GetProject(Guid? projectGuid, string absoluteProjectPath);
    }
}