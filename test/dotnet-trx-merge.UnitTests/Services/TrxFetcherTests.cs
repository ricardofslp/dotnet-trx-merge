﻿using System.Xml.Linq;
using dotnet_trx_merge.Logging;
using dotnet_trx_merge.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Xunit;

namespace dotnet_test_rerun.UnitTests.Services;

public class TrxFetcherTests
{
    private const string TrxWithNoRunUser = "../../../Fixtures/Merge/TrxWithNoRunUser.trx";
    private const string TrxAllPass = "../../../Fixtures/Merge/TrxAllPass.trx";
    private const string TrxWithFailures = "../../../Fixtures/Merge/TrxWithFailures.trx";
    private const string TrxWithErrors = "../../../Fixtures/Merge/TrxWithErrors.trx";
    private const string TrxWithPendings = "../../../Fixtures/Merge/TrxWithPendings.trx";
    private const string TrxAllPassSecondRun = "../../../Fixtures/Merge/TrxAllPassSecondRun.trx";
    private const string TrxWithNamespaceAllPass = "../../../Fixtures/Merge/TrxWithNamespaceAllPass.trx";
    private const string TrxWithNamespaceWithFailures = "../../../Fixtures/Merge/TrxWithNamespaceWithFailures.trx";
    private const string TrxDuplicateTestIdAllPass = "../../../Fixtures/Merge/TrxDuplicateTestIdAllPass.trx";
    private const string TrxDuplicateTestIdWithFailures = "../../../Fixtures/Merge/TrxDuplicateTestIdWithFailures.trx";
    private const string TrxDuplicateTestIdAllPassSecondRun = "../../../Fixtures/Merge/TrxDuplicateTestIdAllPassSecondRun.trx";
    private const string TrxDuplicateTestIdSecondRunStillFails = "../../../Fixtures/Merge/TrxDuplicateTestIdSecondRunStillFails.trx";
    private const string TrxDuplicateTestIdSecondRunNewFailures = "../../../Fixtures/Merge/TrxDuplicateTestIdSecondRunNewFailures.trx";

    [Fact]
    public void AddLatestTests_WithOneFile_AllPass()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        
        // Act
        var mergedDocument =  trxFetcher.AddLatestTests(new []{TrxAllPass});

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
        ValidateTimes(mergedDocument.Descendants("Times").First(), 
            "2023-08-29T18:35:26.3589967+01:00", 
            "2023-08-29T18:35:26.3589971+01:00", 
            "2023-08-29T18:35:25.5259659+01:00", 
            "2023-08-29T18:35:26.3652254+01:00");
    }
    
    [Fact]
    public void AddLatestTests_WithOneFile_SomeFailures()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);

        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new []{TrxWithFailures});

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
        ValidateTimes(mergedDocument.Descendants("Times").First(),
            "2023-08-29T18:35:23.4708736+01:00",
            "2023-08-29T18:35:23.4708738+01:00",
            "2023-08-29T18:35:22.2327044+01:00",
            "2023-08-29T18:35:23.4786615+01:00");
    }
    
    [Fact]
    public void AddLatestTests_TwoFilesWithFailedAtEnd_AllPassInEnd()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        
        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new []{TrxAllPass, TrxWithFailures});

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
        ValidateTimes(mergedDocument.Descendants("Times").First(),
            "2023-08-29T18:35:23.4708736+01:00",
            "2023-08-29T18:35:23.4708738+01:00",
            "2023-08-29T18:35:22.2327044+01:00",
            "2023-08-29T18:35:26.3652254+01:00");
    }
    
    [Fact]
    public void AddLatestTests_TwoFilesWithFailedAtBeginning_AllPassInEnd()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        
        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new []{TrxWithFailures, TrxAllPass});

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
        
        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new []{TrxAllPass, TrxAllPassSecondRun});

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
        ValidateOutcome(mergedDocument, 1, 1, 0, "Completed");
        ValidateTestRunNode(mergedDocument.Root, "COMPUTER\\Utilizador");
    }

    [Fact]
    public void AddLatestTests_WithMultipleTestsWithSameId_SingleFileMerge_AllTestsAreOutput()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);

        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new[] { TrxDuplicateTestIdAllPass });

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

        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new[] { TrxDuplicateTestIdAllPass, TrxDuplicateTestIdAllPassSecondRun });

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

    [Fact]
    public void AddLatestTests_WithMultipleTestsWithSameId_ShouldUpdateTheFailuresToSuccess()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        
        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new[] { TrxDuplicateTestIdWithFailures, TrxDuplicateTestIdAllPassSecondRun });

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

    [Fact]
    public void AddLatestTests_WithMultipleTestsWithSameId_ShouldKeepAnyFailures()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        
        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new[] { TrxDuplicateTestIdWithFailures, TrxDuplicateTestIdSecondRunStillFails });

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
            "Failed");

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

        ValidateOutcome(mergedDocument, 3, 2, 1, "Failed");
    }

    [Fact]
    public void AddLatestTests_WithMultipleTestsWithSameId_ShouldUpdateWithNewFailures()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        
        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new[] { TrxDuplicateTestIdWithFailures, TrxDuplicateTestIdSecondRunNewFailures });

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
            "Failed");

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

        ValidateOutcome(mergedDocument, 3, 2, 1, "Failed");
    }
    
    [Fact]
    public void AddLatestTests_WithOneFile_SomeErrors()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        
        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new[] { TrxWithErrors });

        // Assert
        var results = mergedDocument.Descendants("UnitTestResult");
        results.Should().HaveCount(2);
        ValidateTestResult(results.ElementAt(0), 
            "0dda3bb0-bcfb-4990-a4ac-bbd1ccf4cf8d", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare", 
            "Error");
        
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
        
        ValidateOutcome(mergedDocument, 2, 1, 0, "Error", 1);
    }
    
    [Fact]
    public void AddLatestTests_WithOneFile_SomePendings()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);
        
        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new[] { TrxWithPendings });

        // Assert
        var results = mergedDocument.Descendants("UnitTestResult");
        results.Should().HaveCount(2);
        ValidateTestResult(results.ElementAt(0), 
            "0dda3bb0-bcfb-4990-a4ac-bbd1ccf4cf8d", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare", 
            "Pending");
        
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
        
        ValidateOutcome(mergedDocument, 2, 1, 0, "InProgress", pending: 1);
    }

    [Fact]
    public void AddLatestTests_WhenNamespaces_OutputShouldHaveNamespace()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);

        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new[] { TrxWithNamespaceWithFailures, TrxWithNamespaceAllPass });
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        // Assert
        ValidateTimes(mergedDocument.Descendants(ns + "Times").First(),
            "2023-08-29T18:35:23.4708736+01:00", 
            "2023-08-29T18:35:23.4708738+01:00", 
            "2023-08-29T18:35:22.2327044+01:00", 
            "2023-08-29T19:35:26.3652254+01:00");

        var results = mergedDocument.Descendants(ns + "UnitTestResult");
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
        
        var definitions = mergedDocument.Descendants(ns + "UnitTest");
        definitions.Should().HaveCount(2);
        ValidateTestDefinition(definitions.ElementAt(0),
            "e68ff2c7-8309-483b-a1fd-414967943cf0", 
            "86e2b6e4-df7a-e4fa-006e-c056c908e219", 
            "SecondSimpleNumberCompare",
            ns);
        
        ValidateTestDefinition(definitions.ElementAt(1), 
            "631e6252-66d2-49b8-9fb3-6b9fc02425db", 
            "3dacbae9-707e-1881-d63c-3573123dffc6", 
            "SimpleNumberCompare",
            ns);
        
        var entries = mergedDocument.Descendants(ns + "TestEntry");
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
    public void AddLatestTests_WithOneFileWithNoRunUser_AllPass()
    {
        // Arrange
        var logger = new Logger();
        var trxFetcher = new TrxFetcher(logger);

        // Act
        var mergedDocument = trxFetcher.AddLatestTests(new[] { TrxWithNoRunUser });

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
        ValidateTimes(mergedDocument.Descendants("Times").First(),
            "2023-08-29T18:35:26.3589967+01:00",
            "2023-08-29T18:35:26.3589971+01:00",
            "2023-08-29T18:35:25.5259659+01:00",
            "2023-08-29T18:35:26.3652254+01:00");
    }

    private void ValidateTimes(XElement times, string creation, string queued, string start, string finish)
    {
        times.Attribute("creation")!.Value.Should().Be(creation);
        times.Attribute("queuing")!.Value.Should().Be(queued);
        times.Attribute("start")!.Value.Should().Be(start);
        times.Attribute("finish")!.Value.Should().Be(finish);
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

    private static void ValidateTestDefinition(XElement testResult,
        string executionId,
        string testId,
        string testName,
        XNamespace ns)
    {
        testResult.Attribute("id")!.Value.Should().Be(testId);
        testResult.Attribute("name")!.Value.Should().Be(testName);
        var execution = testResult.Descendants(ns + "Execution");
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
        string outcome,
        int? error = null,
        int? pending = null)
    {
        var results = testResult.Descendants("ResultSummary").ElementAt(0);
        results.Attribute("outcome")!.Value.Should().Be(outcome);
        
        var counters = results.Descendants("Counters").ElementAt(0);
        counters.Attribute("total")!.Value.Should().Be(total.ToString());
        counters.Attribute("passed")!.Value.Should().Be(passed.ToString());
        counters.Attribute("failed")!.Value.Should().Be(failed.ToString());
        
        if(error.HasValue)
            counters.Attribute("error")!.Value.Should().Be(error.ToString());
        
        if(pending.HasValue)
            counters.Attribute("pending")!.Value.Should().Be(pending.ToString());
    }
    
    private static void ValidateTestRunNode(XElement testRun, string runUser)
    {
        Guid.TryParse(testRun.Attribute("id")!.Value, out _).Should().BeTrue();
        testRun.Attribute("name")!.Value.Should().Be("Generated by dotnet trx merge");
        testRun.Attribute("runUser")!.Value.Should().Be(runUser);
    }
}