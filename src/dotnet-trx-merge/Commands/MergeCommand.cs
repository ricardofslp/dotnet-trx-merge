using System.CommandLine;
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
        var trxFiles = GetTrxFiles();
        _log.Debug($"Found {trxFiles.Length} files to merge");

        if (trxFiles.Length > 0)
        {
            var mergedDocument = _trxFetcher.AddLatestTests(trxFiles); 
            mergedDocument.ReplaceAllNamespaces(_config.Namespace);

            _log.Debug($"Document {_config.OutputPath} was saved");
            mergedDocument.Save(_config.OutputPath);
        }
    }

    private string[] GetTrxFiles()
    {
        _log.Debug("Start fetching trx files");
        if (_config.IsDirectory())
        {
            string directoryPath = Path.GetDirectoryName(_config.Directory)!;
            return Directory.GetFiles(directoryPath,
                "*.trx",
                _config.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        return _config.TrxFiles.ToArray();
    }
}