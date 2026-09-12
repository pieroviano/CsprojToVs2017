using System.IO;
using Xunit;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
    public class NuSpecReaderTest
    {
        [Fact]
        public void LoadsNuSpecWithNoNamespace()
        {
            var reader = new NuSpecReader(NoopLogger.Instance);
            var nuspec = reader.Read(new FileInfo(@"TestFiles\nuSpecs\dummy.csproj"));
            Assert.NotNull(nuspec);
        }
    }
}