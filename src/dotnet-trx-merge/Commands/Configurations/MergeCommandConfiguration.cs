using System.CommandLine;
using System.CommandLine.Parsing;
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
    public bool CopyOriginalFiles { get; internal set; }
    
    #endregion Properties

    #region Options

    private readonly Option<string[]> FilesOption = new("--file", "-f")
    {
        Description = "Trx file to merge. Can be set several times. Cannot be used with --dir.",
        Required = false
    };
    
    private readonly Option<string> DirectoryOption = new("--dir", "-d")
    {
        Description = "Folder to look for trx files. Cannot be used with --file.",
        Required = false,
        Arity = ArgumentArity.ZeroOrOne
    };

    private readonly Option<LogLevel> LogLevelOption = new("--loglevel")
    {
        Description = "Log Level",
        Required = false,
        DefaultValueFactory = _ => LogLevel.Verbose,
        CustomParser = Logger.ParseLogLevel
    };
    
    private readonly Option<bool> RecursiveOption = new("--recursive", "-r")
    {
        Description = "Search recursively in folder.",
        Required = false,
        Arity = ArgumentArity.Zero
    };

    private readonly Option<string> NamespaceOption = new("--namespace", "-n")
    {
        Description = "Namespace to add to output file.",
        Required = false
    };
    
    private readonly Option<string> OutputPathOption = new("--output", "-o")
    {
        Description = "Output file path. Must include the file name, not just a directory.",
        Required = false,
        DefaultValueFactory = _ => $"./mergedTrx_{DateTime.Now.ToFileTime()}.trx"
    };
    
    private readonly Option<bool> CopyOriginalFilesOption = new("--copyOriginalFiles", "-c")
    {
        Description = "Copy Original Files.",
        Required = false,
        Arity = ArgumentArity.Zero
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
        cmd.Add(CopyOriginalFilesOption);
    }

    public void GetValues(ParseResult parseResult)
    {
        TrxFiles = parseResult.GetValue(FilesOption) ?? Array.Empty<string>();
        LogLevel = parseResult.GetValue(LogLevelOption);
        Directory = parseResult.GetValue(DirectoryOption)!;
        Recursive = parseResult.GetValue(RecursiveOption);
        Namespace = parseResult.GetValue(NamespaceOption)!;
        OutputPath = parseResult.GetValue(OutputPathOption)!;
        CopyOriginalFiles = parseResult.GetValue(CopyOriginalFilesOption);
        
        ValidateInput();
    }

    public bool IsDirectory()
        => !string.IsNullOrWhiteSpace(Directory);

    public bool IsDifferentOutputFolder()
        => Path.GetDirectoryName(OutputPath)?.Equals(".") is false;
    

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