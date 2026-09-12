using System.Collections.Generic;
using Xunit;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Tests
{
    public class TargetFrameworkReplaceTransformationTest
    {
        [Fact]
        public void HandlesProjectNull()
        {
            Project project = null;
            var targetFrameworks = new List<string>
            {
                "netstandard2.0"
            };
            var transformation = new TargetFrameworkReplaceTransformation(targetFrameworks);
            transformation.Transform(project);
            Assert.Null(project);
        }

        [Fact]
        public void HandlesProjectTargetFrameworksEmpty()
        {
            var project = new Project();
            var targetFrameworks = new List<string>
            {
                "netstandard2.0"
            };
            var transformation = new TargetFrameworkReplaceTransformation(targetFrameworks);
            transformation.Transform(project);
            Assert.Equal(1, project.TargetFrameworks.Count);
            Assert.Equal("netstandard2.0", project.TargetFrameworks[0]);
        }

        [Fact]
        public void HandlesOptionTargetFrameworksNull()
        {
            var project = new Project
            {
                TargetFrameworks =
                {
                    "net46"
                }
            };
            var transformation = new TargetFrameworkReplaceTransformation(null);
            transformation.Transform(project);
            Assert.Equal(1, project.TargetFrameworks.Count);
            Assert.Equal("net46", project.TargetFrameworks[0]);
        }

        [Fact]
        public void HandlesOptionTargetFrameworksEmpty()
        {
            var project = new Project
            {
                TargetFrameworks =
                {
                    "net46"
                }
            };
            var transformation = new TargetFrameworkReplaceTransformation(new List<string>());
            transformation.Transform(project);
            Assert.Equal(1, project.TargetFrameworks.Count);
            Assert.Equal("net46", project.TargetFrameworks[0]);
        }

        [Fact]
        public void HandlesOptionTargetFrameworks()
        {
            var project = new Project
            {
                TargetFrameworks =
                {
                    "net46"
                }
            };
            var targetFrameworks = new List<string>
            {
                "netstandard2.0"
            };
            var transformation = new TargetFrameworkReplaceTransformation(targetFrameworks);
            transformation.Transform(project);
            Assert.Equal(1, project.TargetFrameworks.Count);
            Assert.Equal("netstandard2.0", project.TargetFrameworks[0]);
        }

        [Fact]
        public void HandlesOptionTargetFrameworksMulti()
        {
            var project = new Project
            {
                TargetFrameworks =
                {
                    "net46"
                }
            };
            var targetFrameworks = new List<string>
            {
                "netstandard2.0",
                "net47"
            };
            var transformation = new TargetFrameworkReplaceTransformation(targetFrameworks);
            transformation.Transform(project);
            Assert.Equal(2, project.TargetFrameworks.Count);
            Assert.Equal("netstandard2.0", project.TargetFrameworks[0]);
            Assert.Equal("net47", project.TargetFrameworks[1]);
        }

        [Fact]
        public void HandlesOptionAppendTargetFrameworkToOutputPathNull()
        {
            var project = new Project();
            var transformation = new TargetFrameworkReplaceTransformation(null, true);
            transformation.Transform(project);
            Assert.False(project.AppendTargetFrameworkToOutputPath.HasValue);
        }

        [Fact]
        public void HandlesOptionAppendTargetFrameworkToOutputPathFalse()
        {
            var project = new Project();
            var transformation = new TargetFrameworkReplaceTransformation(null, false);
            transformation.Transform(project);
            Assert.True(project.AppendTargetFrameworkToOutputPath.HasValue);
            Assert.Equal(false, project.AppendTargetFrameworkToOutputPath.Value);
        }
    }
}