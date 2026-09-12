using System.Collections.Generic;
using System.IO;
using Xunit;
using Project2015To2017.Definition;
using Project2015To2017.Migrate2017.Transforms;

namespace Project2015To2017.Tests
{
    public class AssemblyFilterDefaultTransformationTest
    {
        [Fact]
        public void PreventEmptyAssemblyReferences()
        {
            var project = new Project
            {
                AssemblyReferences = new List<AssemblyReference>
                {
                    new AssemblyReference
                    {
                        Include = "System"
                    }
                },
                FilePath = new FileInfo("test.cs")
            };
            new AssemblyFilterDefaultTransformation().Transform(project);
            Assert.Equal(0, project.AssemblyReferences.Count);
        }
    }
}