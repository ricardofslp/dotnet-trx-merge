using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO.Abstractions;
using System.Xml.Linq;
using dotnet_test_rerun.IntegrationTests.Utilities;
using dotnet_trx_merge.Commands;
using dotnet_trx_merge.Commands.Configurations;
using dotnet_trx_merge.Logging;
using dotnet_trx_merge.Services;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_test_rerun.IntegrationTests;

public class DotNetTrxMergeTests
{
    private readonly ITestOutputHelper TestOutputHelper;
    private static string _dir = TestUtilities.GetTmpDirectory();
    private static readonly IFileSystem FileSystem = new FileSystem();

    public DotNetTrxMergeTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        TestUtilities.CopyFixture(string.Empty, new DirectoryInfo(_dir));
    }

    [Fact]
    public void DotnetTrxMerge_WithSelectingFiles_Success()
    {
        // Arrange
        Environment.ExitCode = 0;

        // Act
        var output = RunDotNetTestRerunAndCollectOutputMessage("MergeWithTwoFilesAllPass",
            $"-f {_dir}/MergeOnePassOneFailed/TrxAllPass.trx -f {_dir}/MergeOnePassOneFailed/TrxWithFailures.trx");

        // Assert
        Environment.ExitCode.Should().Be(0);
        output.Should().Contain("Found 2 files to merge");
        output.Should().Contain("Found 1 tests in file");
        output.Should().Contain("Found 2 tests in file");
        output.Should().Contain("New result of test 3dacbae9-707e-1881-d63c-3573123dffc6 - 631e6252-66d2-49b8-9fb3-6b9fc02425db was found in file ");
    }

    [Fact]
    public void DotnetTrxMerge_WithSelectingDir_Success()
    {
        // Arrange
        Environment.ExitCode = 0;

        // Act
        var output = RunDotNetTestRerunAndCollectOutputMessage("MergeWithTwoFilesAllPass",
            $"-d {_dir}/MergeWithTwoFilesAllPass/");

        // Assert
        Environment.ExitCode.Should().Be(0);
        output.Should().Contain("Found 2 files to merge");
        output.Should().Contain("Found 1 tests in file");
        output.Should().Contain("Found 1 tests in file");
    }

    [Fact]
    public void DotnetTrxMerge_WithNoFileFound_Success()
    {
        // Arrange
        Environment.ExitCode = 0;

        // Act
        var output = RunDotNetTestRerunAndCollectOutputMessage("MergeWithRecursiveOption",
            $"-d {_dir}/MergeWithRecursiveOption/");

        // Assert
        Environment.ExitCode.Should().Be(0);
        output.Should().Contain("Found 0 files to merge");
        output.Should().NotContain(".trx was saved");
    }

    [Fact]
    public void DotnetTrxMerge_WithRecursiveOption_Success()
    {
        // Arrange
        Environment.ExitCode = 0;

        // Act
        var output = RunDotNetTestRerunAndCollectOutputMessage("MergeWithRecursiveOption",
            $"-d {_dir}/MergeWithRecursiveOption/ -r");

        // Assert
        Environment.ExitCode.Should().Be(0);
        output.Should().Contain("Found 2 files to merge");
        output.Should().Contain("Found 1 tests in file");
        output.Should().Contain("Found 1 tests in file");
    }
    
    [Fact]
    public async Task DotnetTrxMerge_WithOutputFolder_Success()
    {
        // Arrange
        Environment.ExitCode = 0;
        var outputFile = $"{_dir}/MergeWithRecursiveOption/mergeDocument.trx";

        // Act
        var _ = RunDotNetTestRerunAndCollectOutputMessage("MergeWithRecursiveOption",
            $"-d {_dir}/MergeWithRecursiveOption/ -r -o {outputFile}");

        // Assert
        Environment.ExitCode.Should().Be(0);
        File.Exists(outputFile).Should().BeTrue();
        var text = await File.ReadAllTextAsync(outputFile);
        text.Should().Contain("<Counters total=\"1\" passed=\"1\" failed=\"0\" />");
        text.Should().Contain("testId=\"86e2b6e4-df7a-e4fa-006e-c056c908e219\"");
        text.Should().Contain("testName=\"SecondSimpleNumberCompare\"");
    }

    [Fact]
    public async Task DotnetTrxMerge_MergeWithDuplicatedIds_Success()
    {
        // Arrange
        Environment.ExitCode = 0;
        var outputFile = $"{_dir}/MergeDuplicatedIds/mergeDocument.trx";

        // Act
        var _ = RunDotNetTestRerunAndCollectOutputMessage("MergeDuplicatedIds",
            $"-d {_dir}/MergeDuplicatedIds/ -r -o {outputFile}");

        // Assert
        Environment.ExitCode.Should().Be(0);
        File.Exists(outputFile).Should().BeTrue();
        var text = await File.ReadAllTextAsync(outputFile);
        text.Should().Contain("<Counters total=\"2\" passed=\"2\" failed=\"0\" />");
        text.Should().Contain("testId=\"86e2b6e4-df7a-e4fa-006e-c056c908e219\"");
        text.Should().Contain("testName=\"SecondSimpleNumberCompare\"");
        text.Should().Contain("testName=\"SimpleNumberCompare\"");
    }

    [Fact]
    public async Task DotnetTrxMerge_MergeWithFilesWithNamespaces_SuccessAndShouldIncludeNamespaces()
    {
        // Arrange
        Environment.ExitCode = 0;
        var outputFile = $"{_dir}/FilesWithNamespaces/mergeDocument.trx";

        // Act
        var _ = RunDotNetTestRerunAndCollectOutputMessage("MergeFilesWithNamespaces",
            $"-d {_dir}/FilesWithNamespaces/ -r -o {outputFile}");

        // Assert
        Environment.ExitCode.Should().Be(0);
        File.Exists(outputFile).Should().BeTrue();
        var text = await File.ReadAllTextAsync(outputFile);
        text.Should().Contain("<Counters total=\"2\" passed=\"2\" failed=\"0\" />");
        text.Should().Contain("testId=\"86e2b6e4-df7a-e4fa-006e-c056c908e219\"");
        text.Should().Contain("testName=\"SecondSimpleNumberCompare\"");
        text.Should().Contain("testName=\"SimpleNumberCompare\"");
        XDocument doc = XDocument.Load(outputFile);
        var ns = doc.Root.GetDefaultNamespace();
        ns.NamespaceName.Should().Be("http://microsoft.com/schemas/VisualStudio/TeamTest/2010");
    }

    private string RunDotNetTestRerunAndCollectOutputMessage(string proj, string args = "")
    {
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var logger = new Logger();
        logger.SetLogLevel(LogLevel.Debug);

        Command command = new Command("trx-merge");
        MergeCommandConfiguration mergeCommandConfiguration = new MergeCommandConfiguration();
        mergeCommandConfiguration.Set(command);

        ParseResult result =
            new Parser(command).Parse($"{args}");
        InvocationContext context = new(result);

        mergeCommandConfiguration.GetValues(context);

        MergeCommand mergeCommand = new MergeCommand(logger,
            mergeCommandConfiguration,
            new TrxFetcher(logger));

        mergeCommand.Run();

        return stringWriter.ToString().Trim();
    }
    
    

    [Fact]
    public async Task DotnetTrxMerge_MergeWithFilesGivingANamespace_SuccessAndShouldIncludeNamespaces()
    {
        // Arrange
        Environment.ExitCode = 0;
        var outputFile = $"{_dir}/FilesWithNamespaces/mergeDocument.trx";
        var newNamespace = "http://newNamespace/"; 

        // Act
        var _ = RunDotNetTestRerunAndCollectOutputMessage("MergeFilesWithNamespaces",
            $"-d {_dir}/FilesWithNamespaces/ -r -o {outputFile} -n {newNamespace}");

        // Assert
        Environment.ExitCode.Should().Be(0);
        File.Exists(outputFile).Should().BeTrue();
        var text = await File.ReadAllTextAsync(outputFile);
        text.Should().Contain("<Counters total=\"2\" passed=\"2\" failed=\"0\" />");
        text.Should().Contain("testId=\"86e2b6e4-df7a-e4fa-006e-c056c908e219\"");
        text.Should().Contain("testName=\"SecondSimpleNumberCompare\"");
        text.Should().Contain("testName=\"SimpleNumberCompare\"");
        XDocument doc = XDocument.Load(outputFile);
        var ns = doc.Root.GetDefaultNamespace();
        ns.NamespaceName.Should().Be("http://newNamespace/");
    }
}