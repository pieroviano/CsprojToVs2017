using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using Project2015To2017.Migrate2017.Transforms;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
    public class FileTransformationTest
    {
        [Fact]
        public void TransformsFilesExcludeByIntermediateOutputPath()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "FileInclusion", "intermediatePathExclusion.testcsproj"));
            project.CodeFileExtension = "cs";
            var transformation = new FileTransformation();
            transformation.Transform(project);
            var includeItems = project.ItemGroups.SelectMany(x => x.Elements()).ToImmutableList();
            Assert.Equal(0, includeItems.Count(x => x.Name.LocalName == "Compile" && x.Attribute("Remove")?.Value == "Folder\\FileIncludedByWildcard.cs"));
        }

        [Fact]
        public void TransformsFilesExclude()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "FileInclusion", "fileExclusion.testcsproj"));
            project.CodeFileExtension = "cs";
            var transformation = new FileTransformation();
            transformation.Transform(project);
            var includeItems = project.ItemGroups.SelectMany(x => x.Elements()).ToImmutableList();
            Assert.Equal(1, includeItems.Count(x => x.Name.LocalName == "Compile" && x.Attribute("Remove")?.Value == "Program.cs"));
        }

        [Fact]
        public void TransformsFiles()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "FileInclusion", "fileinclusion.testcsproj"));
            project.CodeFileExtension = "cs";
            var transformation = new FileTransformation();
            transformation.Transform(project);
            var includeItems = project.ItemGroups.SelectMany(x => x.Elements()).ToImmutableList();
            Assert.Equal(39, includeItems.Count);
            Assert.Equal(12, includeItems.Count(x => x.Name.LocalName == "Reference"));
            Assert.Equal(7, includeItems.Count(x => x.Name.LocalName == "Import"));
            Assert.Equal(0, includeItems.Count(x => x.Name.LocalName == "Import" && x.Attribute("Include") == null));
            Assert.Equal(2, includeItems.Count(x => x.Name.LocalName == "ProjectReference"));
            Assert.Equal(2, includeItems.Count(x => x.Name.LocalName.Equals("Antlr4")));
            Assert.Equal(1, includeItems.Count(x => x.Name.LocalName.Equals("Antlr3")));
            Assert.Equal(11, includeItems.Count(x => x.Name.LocalName == "Compile"));
            Assert.Equal(5, includeItems.Count(x => x.Name.LocalName == "Compile" && x.Attribute("Update") != null));
            Assert.Equal(4, includeItems.Count(x => x.Name.LocalName == "Compile" && x.Attribute("Include") != null));
            Assert.Equal(2, includeItems.Count(x => x.Name.LocalName == "Compile" && x.Attribute("Remove") != null));
            Assert.Equal(3, includeItems.Count(x => x.Name.LocalName == "EmbeddedResource")); // #73 include things that are not ending in .resx
            Assert.Equal(0, includeItems.Count(x => x.Name.LocalName == "Content"));
            Assert.Equal(1, includeItems.Count(x => x.Name.LocalName == "None"));
            Assert.Equal(0, includeItems.Count(x => x.Name.LocalName == "Analyzer"));
            var resourceDesigner = includeItems.Single(x => x.Name.LocalName == "Compile" && x.Attribute("Update")?.Value == @"Properties\Resources.Designer.cs");
            Assert.Equal(3, resourceDesigner.Elements().Count());
            var dependentUponElement = resourceDesigner.Elements().Single(x => x.Name.LocalName == "DependentUpon");
            Assert.Equal("Resources.resx", dependentUponElement.Value);
            var linkedFile = includeItems.Single(x => x.Name.LocalName == "Compile" && x.Attribute("Include")?.Value == @"..\OtherTestProjects\OtherTestClass.cs");
            var linkAttribute = linkedFile.Attributes().FirstOrDefault(a => a.Name.LocalName == "Link");
            Assert.NotNull(linkAttribute);
            Assert.Equal("OtherTestClass.cs", linkAttribute.Value);
            var sourceWithDesigner = includeItems.Single(x => x.Name.LocalName == "Compile" && x.Attribute("Update")?.Value == @"SourceFileWithDesigner.cs");
            var subTypeElement = sourceWithDesigner.Elements().Single();
            Assert.Equal("SubType", subTypeElement.Name.LocalName);
            Assert.Equal("Component", subTypeElement.Value);
            var designerForSource = includeItems.Single(x => x.Name.LocalName == "Compile" && x.Attribute("Update")?.Value == @"SourceFileWithDesigner.Designer.cs");
            var dependentUponElement2 = designerForSource.Elements().Single();
            Assert.Equal("DependentUpon", dependentUponElement2.Name.LocalName);
            Assert.Equal("SourceFileWithDesigner.cs", dependentUponElement2.Value);
            var fileWithAnotherAttribute = includeItems.Single(x => x.Name.LocalName == "Compile" && x.Attribute("Update")?.Value == @"AnotherFile.cs");
            Assert.Equal(2, fileWithAnotherAttribute.Attributes().Count());
            Assert.Equal("AttrValue", fileWithAnotherAttribute.Attribute("AnotherAttribute")?.Value);
            var removeMatchingWildcard = includeItems.Where(x => x.Name.LocalName == "Compile" && x.Attribute("Remove")?.Value != null).ToImmutableList();
            Assert.NotNull(removeMatchingWildcard);
            Assert.Equal(2, removeMatchingWildcard.Count);
            Assert.True(removeMatchingWildcard.Any(x => x.Attribute("Remove")?.Value == "SourceFileAsResource.cs"));
            Assert.True(removeMatchingWildcard.Any(x => x.Attribute("Remove")?.Value == "Class1.cs"));
        }

        [Fact]
        public void TransformsFilesPreserveCOMReferences()
        {
            var project = new ProjectReader().Read(Path.Combine("TestFiles", "FileInclusion", "projectWithCOMRefs.testcsproj"));
            project.CodeFileExtension = "cs";
            var transformation = new FileTransformation();
            var comReferencesBefore = project.ProjectDocument.Root.DescendantNodes().Where(node => node.NodeType == System.Xml.XmlNodeType.Element).Where(node => (node as XElement).Name.LocalName.Equals("COMReference")).ToList();
            Assert.Equal(2, comReferencesBefore.Count);
            transformation.Transform(project);
            var comReferencesAfter = project.ProjectDocument.Root.DescendantNodes().Where(node => node.NodeType == System.Xml.XmlNodeType.Element).Where(node => (node as XElement).Name.LocalName.Equals("COMReference")).ToList();
            Assert.Equal(2, comReferencesAfter.Count);
            Assert.Equal((comReferencesBefore[0] as XElement).Value, (comReferencesAfter[0] as XElement).Value);
            Assert.Equal((comReferencesBefore[1] as XElement).Value, (comReferencesAfter[1] as XElement).Value);
        }
    }
}