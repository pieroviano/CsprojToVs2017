using System;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Xunit.Abstractions;

namespace Project2015To2017.Tests
{
    public class UnsupportedProjectTypesTest
    {
        public UnsupportedProjectTypesTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;
        /// <summary>
        /// Run test cases
        /// </summary>
        [InlineData("{8BB2217D-0F2D-49D1-97BC-3654ED321F3B};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "ASP.NET 5", UnsupportedProjectReason.NotSupportedProjectType)]
        [InlineData("{349C5851-65DF-11DA-9384-00065B846F21};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "ASP.NET MVC5", UnsupportedProjectReason.NotSupportedProjectType)]
        [InlineData("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "C# only", UnsupportedProjectReason.Supported)]
        [InlineData("", "No ProjectTypeGuids tag", UnsupportedProjectReason.Supported)]
        [Theory]
        public void CheckAnUnsupportedProjectTypeReturnsCorrectResult(string guidTypes, string testCase, UnsupportedProjectReason expected)
        {
            var xmlDocument = CreateTestProject("ProjectTypeGuids", guidTypes);
            var actual = UnsupportedProjectTypes.IsUnsupportedProjectType(xmlDocument);
            try
            {
                Assert.Equal(expected, actual);
            }
            catch
            {
                _output.WriteLine($"Failed for {testCase}: expected {expected} but returned {actual}");
                throw;
            }
        }

        /// <summary>
        /// Run test cases
        /// </summary>
        [InlineData("WindowsForms", "WinForms", UnsupportedProjectReason.Supported)]
        [InlineData("", "Other", UnsupportedProjectReason.Supported)]
        [Theory]
        public void CheckAnUnsupportedProjectOutputReturnsCorrectResult(string outputType, string testCase, UnsupportedProjectReason expected)
        {
            var xmlDocument = CreateTestProject("MyType", outputType);
            var actual = UnsupportedProjectTypes.IsUnsupportedProjectType(xmlDocument);
            try
            {
                Assert.Equal(expected, actual);
            }
            catch
            {
                _output.WriteLine($"Failed for {testCase}: expected {expected} but returned {actual}");
                throw;
            }
        }

        [Fact]
        public void IsUnsupportedProjectType_ThrowsExceptionIfXDocumentIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                UnsupportedProjectTypes.IsUnsupportedProjectType(null);
            });
        }

        /// <summary>
        /// Create a test case using the given element name + value
        /// </summary>
        /// <param name = "elementName">element name to add to PropertyGroup</param>
        /// <param name = "value">value to set</param>
        /// <returns></returns>
        private static Project CreateTestProject(string elementName, string value)
        {
            // parse empty template
            var xmlDocument = XDocument.Parse(template);
            if (!string.IsNullOrWhiteSpace(value))
            {
                var propertyGroup = xmlDocument.Descendants(Project.XmlLegacyNamespace + "PropertyGroup").First();
                propertyGroup.Add(new XElement(Project.XmlLegacyNamespace + elementName, value));
            }

            var testProject = new Project
            {
                ProjectDocument = xmlDocument
            };
            ProjectPropertiesReader.ReadPropertyGroups(testProject);
            return testProject;
        }

        /// <summary>
        /// Very basic proj template to create XDocument
        /// </summary>
        const string template = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + "<Project ToolsVersion = \"15.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">" + "  <PropertyGroup>" + "    <ProjectGuid>{104B8196-D5BC-4901-B00C-FA065F5CEAD1}</ProjectGuid>" + "  </PropertyGroup>" + "</Project>";
    }
}