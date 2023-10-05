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
    private const string FirstDuplicateFilePath = "../../../Fixtures/Merge/DuplicateTestIdAllPass.trx";
    private const string SecondDuplicateFilePath = "../../../Fixtures/Merge/TrxWithFailures.trx";
    private const string ThirdDupliacteFilePath = "../../../Fixtures/Merge/DuplicateTestIdAllPassSecondRun.trx";

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
        
        var definitions = mergedDocument.Descendants("UnitTest");
        definitions.Should().HaveCount(1);
        ValidateTestDefinition(definitions.ElementAt(0), 
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare");
        
        var entries = mergedDocument.Descendants("TestEntry");
        entries.Should().HaveCount(1);
        ValidateTestEntry(entries.ElementAt(0), 
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219");
        
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
        
        var definitions = mergedDocument.Descendants("UnitTest");
        definitions.Should().HaveCount(2);
        ValidateTestDefinition(definitions.ElementAt(0), 
            "0dda3bb0-bcfb-4990-a4ac-bbd1ccf4cf8d", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare");
        
        ValidateTestDefinition(definitions.ElementAt(1), 
            "631e6252-66d2-49b8-9fb3-6b9fc02425db", 
            "3dacbae9-707e-1881-d63c-3573123dffc6", 
            "SimpleNumberCompare");
        
        var entries = mergedDocument.Descendants("TestEntry");
        entries.Should().HaveCount(2);
        ValidateTestEntry(entries.ElementAt(0), 
            "0dda3bb0-bcfb-4990-a4ac-bbd1ccf4cf8d", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219");
        
        ValidateTestEntry(entries.ElementAt(1), 
            "631e6252-66d2-49b8-9fb3-6b9fc02425db", 
            "3dacbae9-707e-1881-d63c-3573123dffc6");
        
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
        
        var definitions = mergedDocument.Descendants("UnitTest");
        definitions.Should().HaveCount(2);
        ValidateTestDefinition(definitions.ElementAt(0), 
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare");
        
        ValidateTestDefinition(definitions.ElementAt(1), 
            "631e6252-66d2-49b8-9fb3-6b9fc02425db", 
            "3dacbae9-707e-1881-d63c-3573123dffc6", 
            "SimpleNumberCompare");
        
        var entries = mergedDocument.Descendants("TestEntry");
        entries.Should().HaveCount(2);
        ValidateTestEntry(entries.ElementAt(0), 
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219");
        
        ValidateTestEntry(entries.ElementAt(1), 
            "631e6252-66d2-49b8-9fb3-6b9fc02425db", 
            "3dacbae9-707e-1881-d63c-3573123dffc6");
        
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
        
        var definitions = mergedDocument.Descendants("UnitTest");
        definitions.Should().HaveCount(2);
        ValidateTestDefinition(definitions.ElementAt(0), 
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare");
        
        ValidateTestDefinition(definitions.ElementAt(1), 
            "631e6252-66d2-49b8-9fb3-6b9fc02425db", 
            "3dacbae9-707e-1881-d63c-3573123dffc6", 
            "SimpleNumberCompare");
        
        var entries = mergedDocument.Descendants("TestEntry");
        entries.Should().HaveCount(2);
        ValidateTestEntry(entries.ElementAt(0), 
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219");
        
        ValidateTestEntry(entries.ElementAt(1), 
            "631e6252-66d2-49b8-9fb3-6b9fc02425db", 
            "3dacbae9-707e-1881-d63c-3573123dffc6");
        
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
        
        var definitions = mergedDocument.Descendants("UnitTest");
        definitions.Should().HaveCount(1);
        ValidateTestDefinition(definitions.ElementAt(0), 
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare");
        
        var entries = mergedDocument.Descendants("TestEntry");
        entries.Should().HaveCount(1);
        ValidateTestEntry(entries.ElementAt(0), 
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219");
        
        ValidateOutcome(mergedDocument, 1, 1, 0, "Completed");
    }

    [Fact]
    public void AddLatestTests_WithMultipleTestsWithSameId_SingleFileMerge_AllTestsAreOutput()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        var mergedDocument = new XDocument(new XElement("TestRun"));

        // Act
        trxFetcher.AddLatestTests(mergedDocument, new[] { FirstDuplicateFilePath });

        // Assert
        var results = mergedDocument.Descendants("UnitTestResult");
        results.Should().HaveCount(3);
        ValidateTestResult(results.ElementAt(0),
            "ac62796d-afd7-40db-a5a2-14798d68813f",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5",
            "Test1",
            "Passed");

        ValidateTestResult(results.ElementAt(1),
            "0213c00a-c8f7-4787-a5a0-179d076d5c85",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5",
            "Test2",
            "Passed");

        ValidateTestResult(results.ElementAt(2),
            "f72ee21c-4b63-4e22-b839-444caab919cd",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5",
            "Test3",
            "Passed");

        var definitions = mergedDocument.Descendants("UnitTest");
        definitions.Should().HaveCount(1);
        ValidateTestDefinition(definitions.ElementAt(0),
            "ac62796d-afd7-40db-a5a2-14798d68813f",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5",
            "ShouldCheckoutEachScenarioSuccessfully");

        var entries = mergedDocument.Descendants("TestEntry");
        entries.Should().HaveCount(3);
        ValidateTestEntry(entries.ElementAt(0),
            "ac62796d-afd7-40db-a5a2-14798d68813f",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5");

        ValidateTestEntry(entries.ElementAt(1),
            "0213c00a-c8f7-4787-a5a0-179d076d5c85",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5");

        ValidateTestEntry(entries.ElementAt(2),
            "f72ee21c-4b63-4e22-b839-444caab919cd",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5");

        ValidateOutcome(mergedDocument, 3, 3, 0, "Completed");
    }

    [Fact]
    public void AddLatestTests_WithMultipleTestsWithSameId_ShouldUseTheLatestExecutionId()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        var mergedDocument = new XDocument(new XElement("TestRun"));

        // Act
        trxFetcher.AddLatestTests(mergedDocument, new[] { FirstDuplicateFilePath, ThirdDupliacteFilePath });

        // Assert
        var results = mergedDocument.Descendants("UnitTestResult");
        results.Should().HaveCount(3);
        ValidateTestResult(results.ElementAt(0),
            "ac62796d-afd7-40db-a5a2-14798d688131",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5",
            "Test1",
            "Passed");

        ValidateTestResult(results.ElementAt(1),
            "0213c00a-c8f7-4787-a5a0-179d076d5c81",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5",
            "Test2",
            "Passed");

        ValidateTestResult(results.ElementAt(2),
            "f72ee21c-4b63-4e22-b839-444caab919c1",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5",
            "Test3",
            "Passed");

        var definitions = mergedDocument.Descendants("UnitTest");
        definitions.Should().HaveCount(1);
        ValidateTestDefinition(definitions.ElementAt(0),
            "ac62796d-afd7-40db-a5a2-14798d68813f",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5",
            "ShouldCheckoutEachScenarioSuccessfully");

        var entries = mergedDocument.Descendants("TestEntry");
        entries.Should().HaveCount(3);
        ValidateTestEntry(entries.ElementAt(0),
            "ac62796d-afd7-40db-a5a2-14798d688131",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5");

        ValidateTestEntry(entries.ElementAt(1),
            "0213c00a-c8f7-4787-a5a0-179d076d5c81",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5");

        ValidateTestEntry(entries.ElementAt(2),
            "f72ee21c-4b63-4e22-b839-444caab919c1",
            "bd0f3e87-2d49-7223-2c00-e0924f9c83f5");

        ValidateOutcome(mergedDocument, 3, 3, 0, "Completed");
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

    private static void ValidateTestDefinition(XElement testResult,
        string executionId, 
        string testId, 
        string testName)
    {
        testResult.Attribute("id")!.Value.Should().Be(testId);
        testResult.Attribute("name")!.Value.Should().Be(testName);
        var execution = testResult.Descendants("Execution");
        execution.Should().HaveCount(1);
        execution.ElementAt(0).Attribute("id")!.Value.Should().Be(executionId);
    }

    private static void ValidateTestEntry(XElement testResult,
        string executionId, 
        string testId)
    {
        testResult.Attribute("executionId")!.Value.Should().Be(executionId);
        testResult.Attribute("testId")!.Value.Should().Be(testId);
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