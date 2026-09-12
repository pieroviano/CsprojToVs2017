using System.Linq;
using Xunit;
using Project2015To2017.Migrate2017.Transforms;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
    public class ImportsTargetsFilterPackageReferencesTransformationTest
    {
        [Fact]
        public void DedupeImportsFromPackagesAlternativePackagesFolder()
        {
            var projFile = @"TestFiles\AltNugetConfig\ProjectFolder\net46console.testcsproj";
            var project = new ProjectReader().Read(projFile);
            var transformation = new ImportsTargetsFilterPackageReferencesTransformation();
            //Then attempt to clear any referencing the nuget packages folder
            transformation.Transform(project);
            var expectedRemaining = new[]
            {
                @"<Import Project=""C:\SomeTargets.props"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" />",
                @"<Import Project=""C:\SomeTargets.targets"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" />"
            };
            var remainingImports = project.Imports.Select(x => x.ToString()).ToList();
            Assert.Equal(2, remainingImports.Count);
            Assert.Equal(expectedRemaining, remainingImports);
            Assert.False(project.Targets.Any());
        }
    }
}