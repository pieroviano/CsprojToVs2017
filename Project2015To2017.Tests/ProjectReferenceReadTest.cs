using System.IO;
using System.Linq;
using Xunit;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
    public class ProjectReferenceReadTest
    {
        [Fact]
        public void TransformsProjectReferences()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));
            Assert.Equal(2, project.ProjectReferences.Count);
            Assert.True(project.ProjectReferences.Any(x => x.Include == @"..\SomeOtherProject\SomeOtherProject.csproj" && x.Aliases == "global,one"));
        }
    }
}