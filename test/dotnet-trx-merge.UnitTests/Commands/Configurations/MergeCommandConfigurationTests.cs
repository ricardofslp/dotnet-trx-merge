using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using dotnet_trx_merge.Commands.Configurations;
using dotnet_trx_merge.Exceptions;
using dotnet_trx_merge.Logging;
using FluentAssertions;
using Xunit;

namespace dotnet_test_rerun.UnitTests.Commands.Configurations;

public class MergeCommandConfigurationTests
{
    private readonly MergeCommandConfiguration _configuration = new MergeCommandConfiguration();
    private Command Command = new Command("trx-merge");

    [Theory]
    [InlineData("--file", "Trx file to merge.", false)]
    [InlineData("-f", "Trx file to merge.", false)]
    [InlineData("--dir", "Folder to look for trx files.", false)]
    [InlineData("-d", "Folder to look for trx files.", false)]
    [InlineData("--loglevel", "Log Level", false)]
    [InlineData("--recursive", "Search recursively in folder.", false)]
    [InlineData("-r", "Search recursively in folder.", false)]
    [InlineData("--output", "Output file path.", false)]
    [InlineData("-o", "Output file path.", false)]
    public void RerunCommandConfiguration_Set_ShouldConfigureOptions(string optionName, string description, bool isRequired)
    {
        //Act
        _configuration.Set(Command);

        //Assert
        var option = Command.Children.FirstOrDefault(x => x is Option opt && opt.HasAlias(optionName)) as Option;
        option.Should().NotBeNull();
        option!.Description.Should().Be(description);
        option!.IsRequired.Should().Be(isRequired);
    }

    [Fact]
    public void RerunCommandConfiguration_GetValues_ShouldGetValuesFromInvocationContext()
    {
        //Arrange
        _configuration.Set(Command); 
        var result = new Parser(Command).Parse("--file file1 --file file2 --output output --loglevel Debug");
        var context = new InvocationContext(result);

        //Act
        _configuration.GetValues(context);

        //Assert
        _configuration.TrxFiles.Should().HaveCount(2);
        _configuration.TrxFiles.ElementAt(0).Should().Be("file1");
        _configuration.TrxFiles.ElementAt(1).Should().Be("file2");
        _configuration.OutputPath.Should().Be("output");
        _configuration.LogLevel.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public void RerunCommandConfiguration_GetValues_ShouldGetDefaultValues()
    {
        //Arrange
        _configuration.Set(Command); 
        var result = new Parser(Command).Parse("-f file");
        var context = new InvocationContext(result);

        //Act
        _configuration.GetValues(context);

        //Assert
        _configuration.TrxFiles.Should().HaveCount(1);
        _configuration.OutputPath.Should().StartWith("./mergedTrx_");
    }
    
    [Fact]
    public void RerunCommandConfiguration_NoFileOrDirectoryProvided_ShouldFail()
    {
        //Arrange
        _configuration.Set(Command); 
        var result = new Parser(Command).Parse("");
        var context = new InvocationContext(result);

        //Act
        var act = () =>_configuration.GetValues(context);

        //Assert
        act.Should().Throw<MergeException>().WithMessage("Need to have either -f or -d configured");
    }
    
    [Fact]
    public void RerunCommandConfiguration_WithFileAndDirectory_ShouldFail()
    {
        //Arrange
        _configuration.Set(Command); 
        var result = new Parser(Command).Parse("-f file -d directory");
        var context = new InvocationContext(result);

        //Act
        var act = () =>_configuration.GetValues(context);

        //Assert
        act.Should().Throw<MergeException>().WithMessage("Cannot have -f and -d configured at the same time");
    }
    
    [Fact]
    public void RerunCommandConfiguration_WithFileAndRecursive_ShouldFail()
    {
        //Arrange
        _configuration.Set(Command); 
        var result = new Parser(Command).Parse("-f file -r");
        var context = new InvocationContext(result);

        //Act
        var act = () =>_configuration.GetValues(context);

        //Assert
        act.Should().Throw<MergeException>().WithMessage("Cannot have -f and -r configured at the same time");
    }
}