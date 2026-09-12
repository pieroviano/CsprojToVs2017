using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
    public class SolutionReaderTests
    {
        [Fact]
        public void ReadsSolutionFileSuccessfully()
        {
            var testFile = @"TestFiles/Solutions/sampleSolution.testsln";
            var logger = new DummyLogger
            {
                MinimumLogLevel = LogLevel.Warning
            };
            SolutionReader.Instance.Read(testFile, logger);
            Assert.False(logger.LogEntries.Any());
        }
    }
}