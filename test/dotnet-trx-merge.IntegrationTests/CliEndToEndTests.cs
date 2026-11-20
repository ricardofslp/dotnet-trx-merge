using System.Diagnostics;
using System.Xml.Linq;
using AwesomeAssertions;
using dotnet_test_rerun.IntegrationTests.Utilities;
using Xunit;

namespace dotnet_test_rerun.IntegrationTests;

/// <summary>
/// Runs the real tool (Program.cs included) in a separate process, so that argument parsing,
/// exception handling and exit codes are covered end to end.
/// Requires the tool to be built already, which is the case through the project reference.
/// </summary>
public class CliEndToEndTests
{
    private static readonly string Dir = TestUtilities.GetTmpDirectory();

    public CliEndToEndTests()
    {
        TestUtilities.CopyFixture(string.Empty, new DirectoryInfo(Dir));
    }

    [Fact]
    public void Help_ShowsAllOptions_ExitCodeZero()
    {
        var (exitCode, output) = RunCli("--help");

        exitCode.Should().Be(0);
        foreach (var option in new[]
                 {
                     "--file", "--dir", "--loglevel", "--recursive", "--namespace", "--output", "--copyOriginalFiles"
                 })
        {
            output.Should().Contain(option);
        }
    }

    [Fact]
    public void UnknownOption_ExitCodeIsNotZero_AndShowsError()
    {
        var (exitCode, output) = RunCli("--this-option-does-not-exist");

        exitCode.Should().NotBe(0);
        output.Should().Contain("--this-option-does-not-exist");
    }

    [Fact]
    public void NoArguments_ExitCodeOne_AndExplainsWhatIsNeeded()
    {
        var (exitCode, output) = RunCli();

        exitCode.Should().Be(1);
        output.Should().Contain("Need to have either -f or -d configured");
    }

    [Fact]
    public void FileAndDirectory_ExitCodeOne_AndExplainsConflict()
    {
        var (exitCode, output) = RunCli("-f", "a.trx", "-d", Dir);

        exitCode.Should().Be(1);
        output.Should().Contain("Cannot have -f and -d configured at the same time");
    }

    [Fact]
    public void FileAndRecursive_ExitCodeOne_AndExplainsConflict()
    {
        var (exitCode, output) = RunCli("-f", "a.trx", "-r");

        exitCode.Should().Be(1);
        output.Should().Contain("Cannot have -f and -r configured at the same time");
    }

    [Fact]
    public void MissingFile_ExitCodeIsNotZero()
    {
        var (exitCode, _) = RunCli("-f", Path.Combine(Dir, "does-not-exist.trx"));

        exitCode.Should().NotBe(0);
    }

    [Fact]
    public void MergeTwoFiles_WritesValidMergedFile_ExitCodeZero()
    {
        var outputFile = Path.Combine(Dir, "merged-files.trx");

        var (exitCode, _) = RunCli(
            "-f", Path.Combine(Dir, "MergeOnePassOneFailed", "TrxAllPass.trx"),
            "-f", Path.Combine(Dir, "MergeOnePassOneFailed", "TrxWithFailures.trx"),
            "-o", outputFile);

        exitCode.Should().Be(0);
        File.Exists(outputFile).Should().BeTrue();
        XDocument.Load(outputFile).Root!.Name.LocalName.Should().Be("TestRun");
    }

    [Fact]
    public void MergeDirectory_WithoutRecursiveFlag_OnlyLooksAtTopLevel()
    {
        var (exitCode, output) = RunCli(
            "-d", Path.Combine(Dir, "MergeWithRecursiveOption") + Path.DirectorySeparatorChar,
            "-o", Path.Combine(Dir, "merged-top-level.trx"),
            "--loglevel", "Debug");

        exitCode.Should().Be(0);
        output.Should().Contain("Found 0 files to merge");
    }

    [Fact]
    public void MergeDirectory_WithRecursiveFlag_FindsFilesInSubfolders()
    {
        var outputFile = Path.Combine(Dir, "merged-recursive.trx");

        var (exitCode, output) = RunCli(
            "-d", Path.Combine(Dir, "MergeWithRecursiveOption") + Path.DirectorySeparatorChar,
            "-r",
            "-o", outputFile,
            "--loglevel", "Debug");

        exitCode.Should().Be(0);
        output.Should().Contain("Found 2 files to merge");
        File.Exists(outputFile).Should().BeTrue();
    }

    [Fact]
    public void MergeWithNamespace_ReplacesNamespaceInOutput()
    {
        var outputFile = Path.Combine(Dir, "merged-namespace.trx");
        const string ns = "http://newNamespace/";

        var (exitCode, _) = RunCli(
            "-f", Path.Combine(Dir, "FilesWithNamespaces", "TrxWithNamespaceAllPass.trx"),
            "-n", ns,
            "-o", outputFile);

        exitCode.Should().Be(0);
        XDocument.Load(outputFile).Root!.GetDefaultNamespace().NamespaceName.Should().Be(ns);
    }

    // Issue #222: --dir without a trailing separator silently searched the parent folder
    [Fact]
    public void MergeDirectory_WithoutTrailingSeparator_FindsFilesInThatDirectory()
    {
        var outputFile = Path.Combine(Dir, "merged-no-separator.trx");

        var (exitCode, output) = RunCli(
            "-d", Path.Combine(Dir, "MergeWithTwoFilesAllPass"),
            "-o", outputFile,
            "--loglevel", "Debug");

        exitCode.Should().Be(0);
        output.Should().Contain("Found 2 files to merge");
        File.Exists(outputFile).Should().BeTrue();
    }

    [Fact]
    public void MergeDirectory_ThatDoesNotExist_ExitCodeOne_AndSaysSo()
    {
        var (exitCode, output) = RunCli("-d", Path.Combine(Dir, "no-such-folder"));

        exitCode.Should().Be(1);
        output.Should().Contain("does not exist");
    }

    // Issue #149: counters must keep the camelCase names of the TRX schema (notExecuted, not notexecuted)
    [Fact]
    public void MergeFile_WithNotExecutedTest_WritesCamelCaseCounter()
    {
        var input = Path.Combine(Dir, "not-executed.trx");
        File.WriteAllText(input, TrxWithOutcome("NotExecuted"));
        var outputFile = Path.Combine(Dir, "merged-not-executed.trx");

        var (exitCode, _) = RunCli("-f", input, "-o", outputFile);

        exitCode.Should().Be(0);
        var counters = XDocument.Load(outputFile).Descendants().Single(e => e.Name.LocalName == "Counters");
        counters.Attribute("notExecuted")!.Value.Should().Be("1");
        counters.Attribute("notexecuted").Should().BeNull();
        counters.Attribute("total")!.Value.Should().Be("1");
    }

    // Issue #150: merged output must stay in the namespace of the input, with no xmlns="" elements
    [Fact]
    public void MergeFiles_WithDefaultNamespace_AndNoNamespaceOption_KeepsNamespaceOnEveryElement()
    {
        var input = Path.Combine(Dir, "namespaced.trx");
        File.WriteAllText(input, TrxWithOutcome("Passed"));
        var outputFile = Path.Combine(Dir, "merged-namespaced.trx");

        var (exitCode, _) = RunCli("-f", input, "-o", outputFile);

        exitCode.Should().Be(0);
        File.ReadAllText(outputFile).Should().NotContain("xmlns=\"\"");
        var document = XDocument.Load(outputFile);
        document.Descendants().Select(e => e.Name.NamespaceName).Distinct()
            .Should().BeEquivalentTo(new[] { "http://microsoft.com/schemas/VisualStudio/TeamTest/2010" });
    }

    private static string TrxWithOutcome(string outcome) => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <TestRun id="4fbfe72b-df6b-413e-bb31-d2f6389884ef" name="user@host 2026-01-01 10:00:00" runUser="user" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <Times creation="2026-01-01T10:00:00.0000000+00:00" queuing="2026-01-01T10:00:00.0000000+00:00" start="2026-01-01T10:00:00.0000000+00:00" finish="2026-01-01T10:00:01.0000000+00:00" />
          <Results>
            <UnitTestResult executionId="e68ff2c7-8309-483b-a1fd-414967943cf0" testId="86e2b6e4-df7a-e4fa-006e-c056c908e219" testName="SomeTest" computerName="host" duration="00:00:00.0010000" startTime="2026-01-01T10:00:00.0000000+00:00" endTime="2026-01-01T10:00:01.0000000+00:00" testType="13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b" outcome="{outcome}" testListId="8c84fa94-04c1-424b-9868-57a2d4851a1d" />
          </Results>
          <TestDefinitions>
            <UnitTest name="SomeTest" storage="tests.dll" id="86e2b6e4-df7a-e4fa-006e-c056c908e219">
              <Execution id="e68ff2c7-8309-483b-a1fd-414967943cf0" />
              <TestMethod codeBase="tests.dll" adapterTypeName="executor://x/" className="Tests" name="SomeTest" />
            </UnitTest>
          </TestDefinitions>
          <TestEntries>
            <TestEntry testId="86e2b6e4-df7a-e4fa-006e-c056c908e219" executionId="e68ff2c7-8309-483b-a1fd-414967943cf0" testListId="8c84fa94-04c1-424b-9868-57a2d4851a1d" />
          </TestEntries>
          <TestLists>
            <TestList name="Results Not in a List" id="8c84fa94-04c1-424b-9868-57a2d4851a1d" />
          </TestLists>
          <ResultSummary outcome="Completed">
            <Counters total="1" executed="1" passed="0" failed="0" />
          </ResultSummary>
        </TestRun>
        """;

    private static (int ExitCode, string Output) RunCli(params string[] args)
    {
        var repoRoot = FindRepoRoot();
        var configuration = AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}")
            ? "Release"
            : "Debug";

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in new[]
                 {
                     "run", "--no-build", "--configuration", configuration,
                     "--project", Path.Combine("src", "dotnet-trx-merge", "dotnet-trx-merge.csproj"), "--"
                 }.Concat(args))
        {
            startInfo.ArgumentList.Add(arg);
        }

        startInfo.Environment["NO_COLOR"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(TimeSpan.FromSeconds(90)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"trx-merge did not finish in time. Args: {string.Join(' ', args)}");
        }

        process.WaitForExit();
        return (process.ExitCode, stdout.Result + Environment.NewLine + stderr.Result);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "dotnet-trx-merge.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new InvalidOperationException("Could not find dotnet-trx-merge.sln above the test output folder.");
    }
}
