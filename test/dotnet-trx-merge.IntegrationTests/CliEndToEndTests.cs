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
