using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Xml.Linq;
using dotnet_trx_merge.Commands;
using dotnet_trx_merge.Commands.Configurations;
using dotnet_trx_merge.Logging;
using dotnet_trx_merge.Services;
using NSubstitute;
using Xunit;

namespace dotnet_test_rerun.UnitTests.Commands;

public class MergeCommandTests
{
    private const string FirstFilePath = "../../../Fixtures/TrxAllPass.trx";
    private const string SecondFilePath = "../../../Fixtures/TrxWithFailures.trx";
    private const string Directory = "../../../Fixtures/";

    [Fact]
    public async Task Run_TestsOnce_WithFile_NoFailures()
    {
        // Arrange
        var logger = new Logger();
        var config = new MergeCommandConfiguration();
        InitialConfigurationSetup(config, $"-f {FirstFilePath} -f {SecondFilePath}");
        var trxFetcher = Substitute.For<ITrxFetcher>();
        var command = new MergeCommand(logger, config, trxFetcher);

        // Act
        command.Run();

        // Assert
        trxFetcher.Received(1).AddLatestTests(Arg.Any<XDocument>(), 
            Arg.Is<string[]>(files => files.Count() == 2));
    }
    
    [Fact]
    public async Task Run_TestsOnce_WithDirectory_NoFailures()
    {
        // Arrange
        var logger = new Logger();
        var config = new MergeCommandConfiguration();
        InitialConfigurationSetup(config, $"-d {Directory} -r");
        var trxFetcher = Substitute.For<ITrxFetcher>();
        var command = new MergeCommand(logger, config, trxFetcher);

        // Act
        command.Run();

        // Assert
        trxFetcher.Received(1).AddLatestTests(Arg.Any<XDocument>(), 
            Arg.Is<string[]>(files => files.Count() == 3));
    }
    
    [Fact]
    public async Task InvokeCommand_TestsOnce_NoFailures()
    {
        // Arrange
        var logger = new Logger();
        var config = new MergeCommandConfiguration();
        var cmd = new Command("trx-merge");
        config.Set(cmd);
        var trxFetcher = Substitute.For<ITrxFetcher>();
        var command = new MergeCommand(logger, config, trxFetcher);

        // Act
        await command.InvokeAsync($"-f {FirstFilePath} -f {SecondFilePath}");

        // Assert
        trxFetcher.Received(1).AddLatestTests(Arg.Any<XDocument>(), 
            Arg.Is<string[]>(files => 
                files.ElementAt(0).Equals(FirstFilePath) &&
                files.ElementAt(1).Equals(SecondFilePath)));
    }
    
    private void InitialConfigurationSetup(MergeCommandConfiguration configuration, string options)
    {
        var command = new Command("trx-merge");
        configuration.Set(command);
        var result = new Parser(command).Parse(options);
        var context = new InvocationContext(result);
        configuration.GetValues(context);
    }
}