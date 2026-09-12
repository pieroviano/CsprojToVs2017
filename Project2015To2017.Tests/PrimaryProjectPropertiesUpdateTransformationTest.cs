using System.IO;
using System.Xml.Linq;
using Xunit;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Tests
{
    public class PrimaryProjectPropertiesUpdateTransformationTest
    {
        [Fact]
        public void OutputAppendTargetFrameworkToOutputPathNull()
        {
            var project = new Project
            {
                IsModernProject = true,
                AppendTargetFrameworkToOutputPath = null,
                PropertyGroups = new[]
                {
                    new XElement("PropertyGroup")
                },
                FilePath = new FileInfo("test.cs")
            };
            new PrimaryProjectPropertiesUpdateTransformation().Transform(project);
            var appendTargetFrameworkToOutputPath = project.Property("AppendTargetFrameworkToOutputPath");
            Assert.Null(appendTargetFrameworkToOutputPath);
        }

        [Fact]
        public void OutputAppendTargetFrameworkToOutputPathTrue()
        {
            var project = new Project
            {
                IsModernProject = true,
                AppendTargetFrameworkToOutputPath = true,
                PropertyGroups = new[]
                {
                    new XElement("PropertyGroup")
                },
                FilePath = new FileInfo("test.cs")
            };
            new PrimaryProjectPropertiesUpdateTransformation().Transform(project);
            var appendTargetFrameworkToOutputPath = project.Property("AppendTargetFrameworkToOutputPath");
            Assert.NotNull(appendTargetFrameworkToOutputPath);
            Assert.Equal("true", appendTargetFrameworkToOutputPath.Value);
        }

        [Fact]
        public void OutputAppendTargetFrameworkToOutputPathFalse()
        {
            var project = new Project
            {
                IsModernProject = true,
                AppendTargetFrameworkToOutputPath = false,
                PropertyGroups = new[]
                {
                    new XElement("PropertyGroup")
                },
                FilePath = new FileInfo("test.cs")
            };
            new PrimaryProjectPropertiesUpdateTransformation().Transform(project);
            var appendTargetFrameworkToOutputPath = project.Property("AppendTargetFrameworkToOutputPath");
            Assert.NotNull(appendTargetFrameworkToOutputPath);
            Assert.Equal("false", appendTargetFrameworkToOutputPath.Value);
        }
    }
}