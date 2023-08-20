using System.Xml.Linq;
using dotnet_trx_merge.Commands;
using dotnet_trx_merge.Commands.Configurations;
using dotnet_trx_merge.Logging;
using dotnet_trx_merge.Services;
using FluentAssertions;
using Xunit;

namespace dotnet_test_rerun.UnitTests.Services;

public class TrxFetcherTests
{
    private const string FirstFilePath = "../../../Fixtures/Merge/TrxAllPass.trx";
    private const string SecondFilePath = "../../../Fixtures/Merge/TrxWithFailures.trx";
    private const string ThirdFilePath = "../../../Fixtures/Merge/TrxAllPassSecondRun.trx";
    
    [Fact]
    public void AddLatestTests_WithOneFile_AllPass()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        var mergedDocument = new XDocument(new XElement("TestRun"));

        // Act
        trxFetcher.AddLatestTests(mergedDocument, new []{FirstFilePath});

        // Assert
        var results = mergedDocument.Descendants("UnitTestResult");
        results.Should().HaveCount(1);
        ValidateTestResult(results.ElementAt(0), 
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare", 
            "Passed");
        
        ValidateOutcome(mergedDocument, 1, 1, 0, "Completed");
    }
    
    [Fact]
    public void AddLatestTests_WithOneFile_SomeFailures()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        var mergedDocument = new XDocument(new XElement("TestRun"));

        // Act
        trxFetcher.AddLatestTests(mergedDocument, new []{SecondFilePath});

        // Assert
        var results = mergedDocument.Descendants("UnitTestResult");
        results.Should().HaveCount(2);
        ValidateTestResult(results.ElementAt(0), 
            "0dda3bb0-bcfb-4990-a4ac-bbd1ccf4cf8d", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare", 
            "Failed");
        
        ValidateTestResult(results.ElementAt(1), 
            "631e6252-66d2-49b8-9fb3-6b9fc02425db", 
            "3dacbae9-707e-1881-d63c-3573123dffc6", 
            "SimpleNumberCompare", 
            "Passed");
        
        ValidateOutcome(mergedDocument, 2, 1, 1, "Failed");
    }
    
    [Fact]
    public void AddLatestTests_TwoFilesWithFailedAtEnd_AllPassInEnd()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        var mergedDocument = new XDocument(new XElement("TestRun"));

        // Act
        trxFetcher.AddLatestTests(mergedDocument, new []{FirstFilePath, SecondFilePath});

        // Assert
        var results = mergedDocument.Descendants("UnitTestResult");
        results.Should().HaveCount(2);
        ValidateTestResult(results.ElementAt(0), 
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare", 
            "Passed");
        
        ValidateTestResult(results.ElementAt(1), 
            "631e6252-66d2-49b8-9fb3-6b9fc02425db", 
            "3dacbae9-707e-1881-d63c-3573123dffc6", 
            "SimpleNumberCompare", 
            "Passed");
        
        ValidateOutcome(mergedDocument, 2, 2, 0, "Completed");
    }
    
    [Fact]
    public void AddLatestTests_TwoFilesWithFailedAtBeginning_AllPassInEnd()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        var mergedDocument = new XDocument(new XElement("TestRun"));

        // Act
        trxFetcher.AddLatestTests(mergedDocument, new []{SecondFilePath, FirstFilePath});

        // Assert
        var results = mergedDocument.Descendants("UnitTestResult");
        results.Should().HaveCount(2);
        ValidateTestResult(results.ElementAt(0), 
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare", 
            "Passed");
        
        ValidateTestResult(results.ElementAt(1), 
            "631e6252-66d2-49b8-9fb3-6b9fc02425db", 
            "3dacbae9-707e-1881-d63c-3573123dffc6", 
            "SimpleNumberCompare", 
            "Passed");
        
        ValidateOutcome(mergedDocument, 2, 2, 0, "Completed");
    }
    
    [Fact]
    public void AddLatestTests_WithTwoFiles_SameTest()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        var mergedDocument = new XDocument(new XElement("TestRun"));

        // Act
        trxFetcher.AddLatestTests(mergedDocument, new []{FirstFilePath, ThirdFilePath});

        // Assert
        var results = mergedDocument.Descendants("UnitTestResult");
        results.Should().HaveCount(1);
        ValidateTestResult(results.ElementAt(0), 
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare", 
            "Passed");
        
        ValidateOutcome(mergedDocument, 1, 1, 0, "Completed");
    }

    private static void ValidateTestResult(XElement testResult,
        string executionId, 
        string testId, 
        string testName, 
        string outcome)
    {
        testResult.Attribute("executionId")!.Value.Should().Be(executionId);
        testResult.Attribute("testId")!.Value.Should().Be(testId);
        testResult.Attribute("testName")!.Value.Should().Be(testName);
        testResult.Attribute("outcome")!.Value.Should().Be(outcome);
    }
    
    private static void ValidateOutcome(XDocument testResult,
        int total, 
        int passed, 
        int failed, 
        string outcome)
    {
        var results = testResult.Descendants("ResultSummary").ElementAt(0);
        results.Attribute("outcome")!.Value.Should().Be(outcome);
        
        var counters = results.Descendants("Counters").ElementAt(0);
        counters.Attribute("total")!.Value.Should().Be(total.ToString());
        counters.Attribute("passed")!.Value.Should().Be(passed.ToString());
        counters.Attribute("failed")!.Value.Should().Be(failed.ToString());
    }
}