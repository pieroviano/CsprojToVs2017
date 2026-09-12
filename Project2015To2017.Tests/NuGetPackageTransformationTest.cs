using System.IO;
using System.Linq;
using Xunit;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;

namespace Project2015To2017.Tests
{
    public class NuGetPackageTransformationTest
    {
        [Fact]
        public void ConvertsNuspec()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));
            project.AssemblyAttributes = new AssemblyAttributes
            {
                InformationalVersion = "7.0",
                Version = "8.0",
                FileVersion = "9.0",
                Copyright = "copyright from assembly",
                Description = "description from assembly",
                Company = "assembly author"
            };
            new NuGetPackageTransformation().Transform(project);
            var transformedPackageConfig = project.PackageConfiguration;
            Assert.Null(transformedPackageConfig.Id);
            Assert.Equal("7.0", transformedPackageConfig.Version);
            Assert.Equal("some author", transformedPackageConfig.Authors);
            Assert.Equal("copyright from assembly", transformedPackageConfig.Copyright);
            Assert.True(transformedPackageConfig.RequiresLicenseAcceptance);
            Assert.Equal("a nice description.", transformedPackageConfig.Description);
            Assert.Equal("some tags API", transformedPackageConfig.Tags);
            Assert.Equal("someurl", transformedPackageConfig.LicenseUrl);
            Assert.Equal("Some long\n        text\n        with newlines", transformedPackageConfig.ReleaseNotes.Trim());
        }

        [Fact]
        public void ConvertsNuspecWithNoInformationalVersion()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));
            project.AssemblyAttributes = new AssemblyAttributes
            {
                Version = "8.0",
                FileVersion = "9.0",
                Copyright = "copyright from assembly",
                Description = "description from assembly",
                Company = "assembly author"
            };
            new NuGetPackageTransformation().Transform(project);
            var transformedPackageConfig = project.PackageConfiguration;
            Assert.Equal("8.0", transformedPackageConfig.Version);
        }

        [Fact]
        public void ConvertsDependencies()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));
            project.PackageReferences = new[]
            {
                new PackageReference
                {
                    Id = "Newtonsoft.Json",
                    Version = "10.0.2"
                },
                new PackageReference
                {
                    Id = "Other.Package",
                    Version = "1.0.2"
                }
            };
            new NuGetPackageTransformation().Transform(project);
            Assert.Equal("[10.0.2,11)", project.PackageReferences.Single(x => x.Id == "Newtonsoft.Json").Version);
            Assert.Equal("1.0.2", project.PackageReferences.Single(x => x.Id == "Other.Package").Version);
        }
    }
}