using System.IO;
using System.Linq;
using Xunit;
using Project2015To2017.Definition;
using Project2015To2017.Migrate2017.Transforms;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
    public class TestProjectPackageReferenceTransformationTest
    {
        [Fact]
        public void AddsTestPackages()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));
            project.Type = ApplicationType.TestProject;
            project.TargetFrameworks.Add("net45");
            var transformation = new TestProjectPackageReferenceTransformation();
            transformation.Transform(project);
            Assert.Equal(10, project.PackageReferences.Count);
            Assert.Equal(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin.Host.HttpListener" && x.Version == "3.1.0"));
            Assert.Equal(1, project.PackageReferences.Count(x => x.Id == "Microsoft.NET.Test.Sdk" && !string.IsNullOrWhiteSpace(x.Version)));
            Assert.Equal(1, project.PackageReferences.Count(x => x.Id == "AutoMapper" && x.Version == "6.1.1" && x.IsDevelopmentDependency));
        }

        [Fact]
        public void AcceptsNetStandardFramework()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));
            project.Type = ApplicationType.TestProject;
            project.TargetFrameworks.Add("netstandard2.0");
            var transformation = new TestProjectPackageReferenceTransformation();
            transformation.Transform(project);
            Assert.Equal(10, project.PackageReferences.Count);
            Assert.Equal(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin.Host.HttpListener" && x.Version == "3.1.0"));
            Assert.Equal(1, project.PackageReferences.Count(x => x.Id == "Microsoft.NET.Test.Sdk" && !string.IsNullOrWhiteSpace(x.Version)));
            Assert.Equal(1, project.PackageReferences.Count(x => x.Id == "AutoMapper" && x.Version == "6.1.1" && x.IsDevelopmentDependency));
        }

        [Fact]
        public void DoesNotAddTestPackagesIfExists()
        {
            var transformation = new TestProjectPackageReferenceTransformation();
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "containsTestSDK.testcsproj"));
            project.TargetFrameworks.Add("net45");
            transformation.Transform(project);
            Assert.Equal(6, project.PackageReferences.Count);
            Assert.Equal(0, project.PackageReferences.Count(x => x.Id == "MSTest.TestAdapter"));
        }

        [Fact]
        public void TransformsPackages()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));
            var transformation = new TestProjectPackageReferenceTransformation();
            transformation.Transform(project);
            Assert.Equal(7, project.PackageReferences.Count);
            Assert.Equal(2, project.PackageReferences.Count(x => x.IsDevelopmentDependency));
            Assert.Equal(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin" && x.Version == "3.1.0"));
        }

        [Fact]
        public void HandlesNonXml()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));
            var transformation = new TestProjectPackageReferenceTransformation();
            transformation.Transform(project);
        }
    }
}