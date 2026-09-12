using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Project2015To2017.Reading;
using Project2015To2017.Writing;

namespace Project2015To2017.Tests
{
    public class ProgramTest
    {
        [Fact]
        public void ValidatesFileIsWritable()
        {
            var projectFile = Path.Combine("TestFiles", "OtherTestProjects", "readonly.testcsproj");
            var copiedProjectFile = Path.Combine("TestFiles", "OtherTestProjects", $"{nameof(ValidatesFileIsWritable)}.readonly");
            if (File.Exists(copiedProjectFile))
            {
                File.SetAttributes(copiedProjectFile, FileAttributes.Normal);
                File.Delete(copiedProjectFile);
            }

            try
            {
                File.Copy(projectFile, copiedProjectFile);
                File.SetAttributes(copiedProjectFile, FileAttributes.ReadOnly);
                var logger = new DummyLogger();
                var project = new ProjectReader(logger).Read(copiedProjectFile);
                Assert.False(logger.LogEntries.Any(x => x.Contains("Aborting as could not write to project file")));
                var writer = new ProjectWriter(logger);
                Assert.False(writer.TryWrite(project));
                Assert.True(logger.LogEntries.Any(x => x.Contains("Aborting as could not write to project file")));
            }
            finally
            {
                if (File.Exists(copiedProjectFile))
                {
                    File.SetAttributes(copiedProjectFile, FileAttributes.Normal);
                    File.Delete(copiedProjectFile);
                }
            }
        }

        [Fact]
        public void ValidatesFileIsWritableAfterCheckout()
        {
            var logs = new List<string>();
            var projectFile = Path.Combine("TestFiles", "OtherTestProjects", "readonly.testcsproj");
            var copiedProjectFile = Path.Combine("TestFiles", "OtherTestProjects", $"{nameof(ValidatesFileIsWritableAfterCheckout)}.readonly");
            if (File.Exists(copiedProjectFile))
            {
                File.SetAttributes(copiedProjectFile, FileAttributes.Normal);
                File.Delete(copiedProjectFile);
            }

            try
            {
                File.Copy(projectFile, copiedProjectFile);
                File.SetAttributes(copiedProjectFile, FileAttributes.ReadOnly);
                var project = new ProjectReader().Read(copiedProjectFile);
                var projectWriter = new ProjectWriter(new ProjectWriteOptions { CheckoutOperation = file => File.SetAttributes(file.FullName, FileAttributes.Normal), DeleteFileOperation = _ =>
                {
                } });
                Assert.True(projectWriter.TryWrite(project));
                Assert.False(logs.Any(x => x.Contains("Aborting as could not write to project file")));
            }
            finally
            {
                if (File.Exists(copiedProjectFile))
                {
                    File.SetAttributes(copiedProjectFile, FileAttributes.Normal);
                    File.Delete(copiedProjectFile);
                }
            }
        }

        [Fact]
        public void ValidatesFileExists()
        {
            Assert.False(ProjectConverter.Validate(new FileInfo(Path.Combine("TestFiles", "OtherTestProjects", "nonexistent.testcsproj")), NoopLogger.Instance));
        }
    }
}