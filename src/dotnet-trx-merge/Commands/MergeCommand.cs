using System.CommandLine;
using System.Xml.Linq;
using dotnet_trx_merge.Commands.Configurations;
using dotnet_trx_merge.Extensions;
using dotnet_trx_merge.Logging;
using dotnet_trx_merge.Services;

namespace dotnet_trx_merge.Commands;

public class MergeCommand : RootCommand
{
    private readonly ILogger _log;
    private readonly MergeCommandConfiguration _config;
    private readonly ITrxFetcher _trxFetcher;

    public MergeCommand(ILogger logger,
        MergeCommandConfiguration config,
        ITrxFetcher trxFetcher) :
        base("allows to merge several trx files into a single one")
    {
        _log = logger;
        _config = config;
        _trxFetcher = trxFetcher;

        // Set Arguments and Options
        config.Set(this);

        this.SetHandler(async (context) =>
        {
            _config.GetValues(context);
            logger.SetLogLevel(_config.LogLevel);
            Run();
        });
    }

    public void Run()
    {
        (var trxFiles, var directoryPath) = GetTrxFiles();
        _log.Debug($"Found {trxFiles.Length} files to merge");

        if (trxFiles.Length > 0)
        {
            var mergedDocument = _trxFetcher.AddLatestTests(trxFiles);
            mergedDocument.ReplaceAllNamespaces(_config.Namespace);
            
            if (_config.CopyOriginalFiles && _config.IsDifferentOutputFolder())
                CopyFolder(directoryPath, Path.GetDirectoryName(_config.OutputPath)!);
            
            _log.Debug($"Document {_config.OutputPath} was saved");
            mergedDocument.Save(_config.OutputPath);
        }
    }

    private (string[] trxFiles, string directory) GetTrxFiles()
    {
        _log.Debug("Start fetching trx files");
        if (_config.IsDirectory())
        {
            string directoryPath = Path.GetDirectoryName(_config.Directory)!;
            return (Directory.GetFiles(directoryPath,
                "*.trx",
                _config.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly), directoryPath);
        }

        return (_config.TrxFiles.ToArray(), string.Empty);
    }

    private static void CopyFolder(string sourcePath, string destinationPath)
    {
        // Create new directory in destination
        Directory.CreateDirectory(destinationPath);

        // Copy files
        foreach (string sourceFile in Directory.GetFiles(sourcePath))
        {
            string destinationFile = Path.Combine(destinationPath, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, destinationFile, true); // Overwrite existing files
        }

        // Copy subdirectories recursively
        foreach (string subDirectory in Directory.GetDirectories(sourcePath))
        {
            CopyFolder(subDirectory, Path.Combine(destinationPath, Path.GetFileName(subDirectory)));
        }
    }
}