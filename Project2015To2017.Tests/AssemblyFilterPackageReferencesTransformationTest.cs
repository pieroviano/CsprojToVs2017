using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Project2015To2017.Definition;
using Project2015To2017.Migrate2017.Transforms;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
    public class AssemblyFilterPackageReferencesTransformationTest
    {
        [Fact]
        public void TransformsAssemblyReferences()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));
            var transformation = new AssemblyFilterPackageReferencesTransformation();
            transformation.Transform(project);
            Assert.Equal(12, project.AssemblyReferences.Count);
            Assert.True(project.AssemblyReferences.Any(x => x.Include == @"System.Xml.Linq"));
            Assert.True(project.AssemblyReferences.Any(x => x.Include == @"Microsoft.CSharp"));
        }

        [Fact]
        public void RemoveExtraAssemblyReferences()
        {
            var project = new Project
            {
                AssemblyReferences = new List<AssemblyReference>
                {
                    new AssemblyReference
                    {
                        Include = "Test.Package",
                        EmbedInteropTypes = "false",
                        HintPath = @"..\packages\Test.Package.dll",
                        Private = "false",
                        SpecificVersion = "false"
                    },
                    new AssemblyReference
                    {
                        Include = "Other.Package",
                        EmbedInteropTypes = "false",
                        HintPath = @"..\packages\Other.Package.dll",
                        Private = "false",
                        SpecificVersion = "false"
                    }
                },
                PackageReferences = new List<PackageReference>
                {
                    new PackageReference
                    {
                        Id = "Test.Package",
                        IsDevelopmentDependency = false,
                        Version = "1.2.3"
                    },
                    new PackageReference
                    {
                        Id = "Another.Package",
                        IsDevelopmentDependency = false,
                        Version = "3.2.1"
                    }
                }
            };
            var transformation = new AssemblyFilterPackageReferencesTransformation();
            transformation.Transform(project);
            Assert.Equal(1, project.AssemblyReferences.Count);
            Assert.Equal(2, project.PackageReferences.Count);
        }
    }
}