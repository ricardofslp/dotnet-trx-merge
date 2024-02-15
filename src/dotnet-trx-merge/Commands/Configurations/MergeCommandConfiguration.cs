using System.CommandLine;
using System.CommandLine.Invocation;
using dotnet_trx_merge.Exceptions;
using dotnet_trx_merge.Logging;

namespace dotnet_trx_merge.Commands.Configurations;

public class MergeCommandConfiguration
{
    #region Properties

    public IEnumerable<string> TrxFiles { get; internal set; }
    public LogLevel LogLevel { get; internal set; }
    public string Directory { get; internal set; }
    public bool Recursive { get; internal set; }
    public string Namespace { get; internal set; }
    public string OutputPath { get; internal set; }
    
    #endregion Properties

    #region Options

    private readonly Option<IEnumerable<string>> FilesOption = new(new[] { "--file", "-f" })
    {
        Description = "Trx file to merge. Can be set several times. Cannot be used with --dir.",
        IsRequired = false
    };
    
    private readonly Option<string> DirectoryOption = new(new[] { "--dir", "-d" })
    {
        Description = "Folder to look for trx files. Cannot be used with --file.",
        IsRequired = false,
        Arity = ArgumentArity.ZeroOrOne
    };

    private readonly Option<LogLevel> LogLevelOption = new(new[] { "--loglevel" }, parseArgument: Logger.ParseLogLevel, isDefault: true)
    {
        Description = "Log Level",
        IsRequired = false
    };
    
    private readonly Option<string> RecursiveOption = new(new[] { "--recursive", "-r" })
    {
        Description = "Search recursively in folder.",
        IsRequired = false,
        Arity = ArgumentArity.Zero
    };

    private readonly Option<string> NamespaceOption = new(new[] { "--namespace", "-n" })
    {
        Description = "Namespace to add to output file.",
        IsRequired = false
    };
    
    private readonly Option<string> OutputPathOption =
        new(new[] { "--output", "-o" }, getDefaultValue: () => $"./mergedTrx_{DateTime.Now.ToFileTime()}.trx")
    {
        Description = "Output file path. Must include the file name, not just a directory.",
        IsRequired = false
    };

    #endregion Options

    public void Set(Command cmd)
    {
        cmd.Add(FilesOption);
        cmd.Add(LogLevelOption);
        cmd.Add(DirectoryOption);
        cmd.Add(RecursiveOption);
        cmd.Add(NamespaceOption);
        cmd.Add(OutputPathOption);
    }

    public void GetValues(InvocationContext context)
    {
        TrxFiles = context.ParseResult.GetValueForOption(FilesOption)!;
        LogLevel = context.ParseResult.GetValueForOption(LogLevelOption);
        Directory = context.ParseResult.GetValueForOption(DirectoryOption)!;
        Recursive = context.ParseResult.FindResultFor(RecursiveOption) is not null;
        Namespace = context.ParseResult.GetValueForOption(NamespaceOption)!;
        OutputPath = context.ParseResult.GetValueForOption(OutputPathOption)!;
        
        ValidateInput();
    }

    public bool IsDirectory()
        => !string.IsNullOrWhiteSpace(Directory);
    

    private void ValidateInput()
    {
        if (!string.IsNullOrWhiteSpace(Directory) &&
            TrxFiles != null &&
            TrxFiles.Any())
        {
            Environment.ExitCode = 1;
            throw new MergeException("Cannot have -f and -d configured at the same time");
        }
        
        if (TrxFiles != null &&
            TrxFiles.Any() &&
            Recursive)
        {
            Environment.ExitCode = 1;
            throw new MergeException("Cannot have -f and -r configured at the same time");
        }

        if (string.IsNullOrWhiteSpace(Directory) &&
            (TrxFiles == null || TrxFiles.Count() == 0))
        {
            Environment.ExitCode = 1;
            throw new MergeException("Need to have either -f or -d configured");
        }
    }
}