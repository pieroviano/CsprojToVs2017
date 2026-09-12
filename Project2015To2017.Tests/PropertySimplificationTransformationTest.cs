using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;

namespace Project2015To2017.Tests
{
    public class PropertySimplificationTransformationTest
    {
        [Fact]
        public void SimplifiesProperties1()
        {
            const string xml = @"
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{5C9DE16E-C69A-4182-9C0C-30FF7CC944CD}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>Dopamine.Tests</RootNamespace>
    <AssemblyName>Dopamine.Tests</AssemblyName>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <ProjectTypeGuids>{3AC096D0-A1C2-E12C-1390-A8335801FDAB};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
    <VisualStudioVersion Condition=""'$(VisualStudioVersion)' == ''"">10.0</VisualStudioVersion>
    <VSToolsPath Condition=""'$(VSToolsPath)' == ''"">$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)</VSToolsPath>
    <ReferencePath>$(ProgramFiles)\Common Files\microsoft shared\VSTT\$(VisualStudioVersion)\UITestExtensionPackages</ReferencePath>
    <IsCodedUITest>False</IsCodedUITest>
    <TestProjectType>UnitTest</TestProjectType>
    <SccProjectName>
    </SccProjectName>
    <SccLocalPath>
    </SccLocalPath>
    <SccAuxPath>
    </SccAuxPath>
    <SccProvider>
    </SccProvider>
    <NuGetPackageImportStamp>
    </NuGetPackageImportStamp>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    <AssemblyVersion>1.0.0</AssemblyVersion>
    <FileVersion>1.0.0</FileVersion>
    <AssemblyVersion>1.0</AssemblyVersion>
    <FileVersion>1.0</FileVersion>
    <AssemblyVersion>1</AssemblyVersion>
    <FileVersion>1</FileVersion>
    <Version>1.0.0</Version>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
</Project>";
            var project = ParseAndTransform(xml, projectName: "Dopamine.Tests");
            Assert.Equal(3, project.PropertyGroups.Count);
            Assert.Null(project.PropertyGroups[0].Attribute("Condition"));
            Assert.NotNull(project.PropertyGroups[1].Attribute("Condition"));
            Assert.NotNull(project.PropertyGroups[2].Attribute("Condition"));
            var childrenGlobal = project.PrimaryPropertyGroup().Elements().ToImmutableArray();
            Assert.Equal(7, childrenGlobal.Length);
            Assert.True(Extensions.ValidateChildren(childrenGlobal, "ProjectGuid", "ProjectTypeGuids", "VSToolsPath", "ReferencePath", "IsCodedUITest", "TestProjectType", "TargetFrameworkVersion"));
            var childrenDebug = project.PropertyGroups[1].Elements().ToImmutableArray();
            Assert.Equal(2, childrenDebug.Length);
            Assert.True(Extensions.ValidateChildren(childrenDebug, "DebugType", "OutputPath"));
            var childrenRelease = project.PropertyGroups[2].Elements().ToImmutableArray();
            Assert.Equal(2, childrenRelease.Length);
            Assert.True(Extensions.ValidateChildren(childrenRelease, "DebugType", "OutputPath"));
        }

        [Fact]
        public void SimplifiesProperties2()
        {
            const string xml = @"
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <AssemblyName>Dopamine</AssemblyName>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
    <ProjectTypeGuids>{60dc8134-eba5-43b8-bcc9-bb4bc16c2548};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DefineConstants>TRACE;DEBUG;WINDOWS_DESKTOP</DefineConstants>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DefineConstants>TRACE</DefineConstants>
  </PropertyGroup>
</Project>";
            var project = ParseAndTransform(xml, projectName: "Dopamine");
            Assert.True(project.IsWindowsPresentationFoundationProject());
            Assert.False(project.IsWindowsFormsProject());
            Assert.Equal(1, project.TargetFrameworks.Count);
            Assert.Equal(1, project.TargetFrameworks.Count(x => x == "net461"));
            Assert.Equal(2, project.Configurations.Count);
            Assert.Equal(1, project.Configurations.Count(x => x == "Debug"));
            Assert.Equal(1, project.Configurations.Count(x => x == "Release"));
            Assert.Equal(1, project.Platforms.Count);
            Assert.Equal(1, project.Platforms.Count(x => x == "AnyCPU"));
            Assert.Equal(3, project.PropertyGroups.Count);
            Assert.Null(project.PropertyGroups[0].Attribute("Condition"));
            Assert.NotNull(project.PropertyGroups[1].Attribute("Condition"));
            Assert.NotNull(project.PropertyGroups[2].Attribute("Condition"));
            var childrenGlobal = project.PrimaryPropertyGroup().Elements().ToImmutableArray();
            Assert.Equal(3, childrenGlobal.Length);
            Assert.True(Extensions.ValidateChildren(childrenGlobal, "OutputType", "TargetFrameworkVersion", "ProjectTypeGuids"));
            var childrenDebug = project.PropertyGroups[1].Elements().ToImmutableArray();
            Assert.Equal(1, childrenDebug.Length);
            Assert.True(Extensions.ValidateChildren(childrenDebug, "DefineConstants"));
            var childrenRelease = project.PropertyGroups[2].Elements().ToImmutableArray();
            Assert.Equal(0, childrenRelease.Length);
        }

        [Fact]
        public void HandlesComplexConditions()
        {
            const string xml = @"
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <AssemblyName>Dopamine.Tests</AssemblyName>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)' == 'Debug' And '$(Platform)' == 'AnyCPU' "">
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Platform)|$(Configuration)' == 'AnyCPU|Release' "">
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
  </PropertyGroup>
</Project>";
            var project = ParseAndTransform(xml, projectName: "Dopamine.Tests");
            Assert.Equal(2, project.Configurations.Count);
            Assert.Equal(1, project.Configurations.Count(x => x == "Debug"));
            Assert.Equal(1, project.Configurations.Count(x => x == "Release"));
            Assert.Equal(1, project.Platforms.Count);
            Assert.Equal(1, project.Platforms.Count(x => x == "AnyCPU"));
            Assert.Equal(3, project.PropertyGroups.Count);
            Assert.Null(project.PropertyGroups[0].Attribute("Condition"));
            Assert.NotNull(project.PropertyGroups[1].Attribute("Condition"));
            Assert.NotNull(project.PropertyGroups[2].Attribute("Condition"));
            var childrenGlobal = project.PrimaryPropertyGroup().Elements().ToImmutableArray();
            Assert.Equal(1, childrenGlobal.Length);
            Assert.True(Extensions.ValidateChildren(childrenGlobal, "TargetFrameworkVersion"));
            var childrenDebug = project.PropertyGroups[1].Elements().ToImmutableArray();
            Assert.Equal(2, childrenDebug.Length);
            Assert.True(Extensions.ValidateChildren(childrenDebug, "DebugType", "OutputPath"));
            var childrenRelease = project.PropertyGroups[2].Elements().ToImmutableArray();
            Assert.Equal(1, childrenRelease.Length);
            Assert.True(Extensions.ValidateChildren(childrenRelease, "OutputPath"));
        }

        [Fact]
        public void HandlesUnknownConfigurationSimplifications()
        {
            const string xml = @"
<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""4.0"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
    <Configurations>Debug;Release</Configurations>
  </PropertyGroup>
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <AssemblyName>Class1</AssemblyName>
    <TargetFrameworkVersion>v4.7</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <Optimize>false</Optimize>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <Optimize>true</Optimize>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release_CI|AnyCPU'"">
    <OutputPath>bin/Release_CI\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <Optimize>true</Optimize>
    <CodeAnalysisRuleSet>..\FxCop.Rules.ruleset</CodeAnalysisRuleSet>
    <DocumentationFile>bin\Release_CI\Class1.xml</DocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RunCodeAnalysis>true</RunCodeAnalysis>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
 </Project>";
            var project = ParseAndTransform(xml, projectName: "Class1");
            Assert.Equal(2, project.Configurations.Count);
            Assert.Equal(1, project.Configurations.Count(x => x == "Debug"));
            Assert.Equal(1, project.Configurations.Count(x => x == "Release"));
            Assert.Equal(5, project.PropertyGroups.Count);
            Assert.Null(project.PropertyGroups[0].Attribute("Condition"));
            Assert.Null(project.PropertyGroups[1].Attribute("Condition"));
            Assert.NotNull(project.PropertyGroups[2].Attribute("Condition"));
            Assert.NotNull(project.PropertyGroups[3].Attribute("Condition"));
            Assert.NotNull(project.PropertyGroups[4].Attribute("Condition"));
            var childrenGlobal = project.UnconditionalGroups().Elements().ToImmutableArray();
            Assert.Equal(2, childrenGlobal.Length);
            Assert.True(Extensions.ValidateChildren(childrenGlobal, "TargetFrameworkVersion"));
            var childrenDebug = project.PropertyGroups[2].Elements().ToImmutableArray();
            Assert.Equal(0, childrenDebug.Length);
            var childrenRelease = project.PropertyGroups[3].Elements().ToImmutableArray();
            Assert.Equal(0, childrenRelease.Length);
            var childrenReleaseCI = project.PropertyGroups[4].Elements().ToImmutableArray();
            Assert.Equal(7, childrenReleaseCI.Length);
            Assert.True(Extensions.ValidateChildren(childrenReleaseCI, "DefineConstants", "OutputPath", "Optimize", "CodeAnalysisRuleSet", "DocumentationFile", "TreatWarningsAsErrors", "RunCodeAnalysis"));
            Assert.Equal(@"bin/$(Configuration)\", childrenReleaseCI.First(x => x.Name.LocalName == "OutputPath").Value);
        }

        [Fact]
        public void RemovesProjectGuidWhenMatchesSolution()
        {
            var guid = Guid.NewGuid();
            var xml = @"
<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""4.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{" + guid.ToString() + @"}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>ClassLibrary1</RootNamespace>
    <AssemblyName>ClassLibrary1</AssemblyName>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <SccProjectName>SAK</SccProjectName>
    <SccLocalPath>SAK</SccLocalPath>
    <SccAuxPath>SAK</SccAuxPath>
    <SccProvider>SAK</SccProvider>
  </PropertyGroup>
 </Project>";
            var project = ParseAndTransform(xml, projectName: "Class1");
            var name = "someproject";
            project.ProjectName = name;
            project.Solution = new Solution
            {
                ProjectPaths = new[]
                {
                    new ProjectReference
                    {
                        ProjectName = name,
                        ProjectGuid = guid
                    }
                }
            };
            new PropertySimplificationTransformation().Transform(project);
            Assert.True(!project.ProjectDocument.Descendants().Any(x => x.Name.LocalName == "ProjectGuid"));
        }

        [Fact]
        public void RemovesVisualStudioVersion()
        {
            var guid = Guid.NewGuid();
            var xml = @"
<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""4.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{" + guid.ToString() + @"}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>ClassLibrary1</RootNamespace>
    <AssemblyName>ClassLibrary1</AssemblyName>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <SccProjectName>SAK</SccProjectName>
    <SccLocalPath>SAK</SccLocalPath>
    <SccAuxPath>SAK</SccAuxPath>
    <SccProvider>SAK</SccProvider>
	<VisualStudioVersion Condition=""'$(VisualStudioVersion)' == ''"">10.0</VisualStudioVersion>
  </PropertyGroup>
 </Project>";
            var project = ParseAndTransform(xml, projectName: "Class1");
            var name = "someproject";
            project.ProjectName = name;
            project.Solution = new Solution
            {
                ProjectPaths = new[]
                {
                    new ProjectReference
                    {
                        ProjectName = name,
                        ProjectGuid = guid
                    }
                }
            };
            new PropertySimplificationTransformation().Transform(project);
            Assert.True(!project.ProjectDocument.Descendants().Any(x => x.Name.LocalName == "VisualStudioVersion"));
        }

        private static Project ParseAndTransform(string xml, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "", string projectName = null)
        {
            var testCsProjFile = $"{memberName}_test.csproj";
            File.WriteAllText(testCsProjFile, xml, Encoding.UTF8);
            var project = new ProjectReader().Read(testCsProjFile);
            project.ProjectName = projectName;
            project.FilePath = null;
            new PropertySimplificationTransformation().Transform(project);
            return project;
        }
    }
}