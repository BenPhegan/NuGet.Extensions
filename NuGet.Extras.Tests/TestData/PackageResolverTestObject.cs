using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Xml.Linq;

namespace NuGet.Extras.Tests.TestData
{
    [Serializable]
    public class PackageResolverTestObject
    {
        public List<PackageReference> Input = new List<PackageReference>();
        public List<PackageReference> Output = new List<PackageReference>();
        public List<PackageReference> Error = new List<PackageReference>();
        public string Name;
        public string Description;

        Dictionary<string, List<PackageReference>> Data = new Dictionary<string, List<PackageReference>>();

        public PackageResolverTestObject(XElement test)
        {
            Name = test.Attribute("name").Value;
            Description = test.Attribute("description").Value;

            //TODO Not quite DRY....
            foreach (var input in test.Element("Input").Elements("Package"))
            {
                Input.Add(CreatePackageReferenceFromXElement(input));
            }

            foreach (var input in test.Element("Output").Elements("Package"))
            {
                Output.Add(CreatePackageReferenceFromXElement(input));
            }

            foreach (var input in test.Element("Error").Elements("Package"))
            {
                Error.Add(CreatePackageReferenceFromXElement(input));
            }
        }

        private PackageReference CreatePackageReferenceFromXElement(XElement input)
        {
            return new PackageReference(input.Attribute("id").Value, SemanticVersion.Parse(input.Attribute("version").Value), GetVersionSpec(input), new FrameworkName(".NET Framework, Version=4.0"));
        }

        private VersionSpec GetVersionSpec(XElement input)
        {
            if (input.Attribute("spec") != null)
            {
                VersionSpec val = input.Attribute("spec").Value == "" ? new VersionSpec() : VersionUtility.ParseVersionSpec(input.Attribute("spec").Value) as VersionSpec;
                return val;
            }
            return null;
        }
    }

}
