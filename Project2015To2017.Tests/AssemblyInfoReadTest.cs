using System.IO;
using Xunit;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
    public class AssemblyInfoReadTest
    {
        [Fact]
        public void FindsAttributes()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));
            Assert.NotNull(project.AssemblyAttributes.Company);
            Assert.NotNull(project.AssemblyAttributes.Copyright);
            Assert.NotNull(project.AssemblyAttributes.InformationalVersion);
            Assert.NotNull(project.AssemblyAttributes.Product);
            Assert.Equal("Title", project.AssemblyAttributes.Title);
            Assert.NotNull(project.AssemblyAttributes.Version);
        }
    }
}