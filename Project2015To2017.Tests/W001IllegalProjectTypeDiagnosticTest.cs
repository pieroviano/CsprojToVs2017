using System.Xml.Linq;
using Xunit;
using Project2015To2017.Analysis.Diagnostics;
using Project2015To2017.Definition;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
    public class W001IllegalProjectTypeDiagnosticTest
    {
        [Fact]
        public void GeneratesResultForXamarinAndroid()
        {
            var project = new Project
            {
                ProjectDocument = new XDocument(new XElement("Project", new XElement(Project.XmlLegacyNamespace + "PropertyGroup", new XElement(XName.Get("ProjectTypeGuids", Project.XmlLegacyNamespace.NamespaceName), "{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}"))))
            };
            ProjectPropertiesReader.ReadPropertyGroups(project);
            var results = new W001IllegalProjectTypeDiagnostic().Analyze(project);
            Assert.Equal(1, results.Count);
        }

        [Fact]
        public void DoesNotGenerateResultForValidType()
        {
            var project = new Project
            {
                ProjectDocument = new XDocument(new XElement("Project", new XElement(Project.XmlLegacyNamespace + "PropertyGroup", new XElement(XName.Get("ProjectTypeGuids", Project.XmlLegacyNamespace.NamespaceName), "{318C4C53-4319-472D-A480-6540F3D375FD}"))))
            };
            ProjectPropertiesReader.ReadPropertyGroups(project);
            var results = new W001IllegalProjectTypeDiagnostic().Analyze(project);
            Assert.NotNull(results);
            Assert.Equal(0, results.Count);
        }
    }
}