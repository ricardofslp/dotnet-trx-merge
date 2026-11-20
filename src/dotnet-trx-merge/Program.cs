using System.CommandLine;
using System.IO.Abstractions;
using System.Reflection;
using dotnet_trx_merge.Commands;
using dotnet_trx_merge.Commands.Configurations;
using dotnet_trx_merge.Exceptions;
using dotnet_trx_merge.Logging;
using dotnet_trx_merge.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup Dependency Injection
var serviceProvider = new ServiceCollection()
    .AddSingleton<ILogger, Logger>()
    .AddSingleton<MergeCommand>()
    .AddSingleton<MergeCommandConfiguration>()
    .AddSingleton<ITrxFetcher, TrxFetcher>()
    .BuildServiceProvider();
    
var Log = serviceProvider.GetRequiredService<ILogger>();
var cmd = serviceProvider.GetService<MergeCommand>();

var invocationConfiguration = new InvocationConfiguration { EnableDefaultExceptionHandler = false };

try
{
    var exitCode = await cmd!.Parse(args).InvokeAsync(invocationConfiguration);
    if (exitCode != 0 && Environment.ExitCode == 0)
    {
        Environment.ExitCode = exitCode;
    }
}
catch (Exception exception)
{
    if (exception is MergeException
        || (exception is TargetInvocationException
            && exception.InnerException is MergeException))
    {
        Log.Error(exception.Message);
        Log.Debug(exception.StackTrace ?? string.Empty);
    }
    else
    {
        Log.Exception(exception);
    }

    Environment.ExitCode = 1;
}

return Environment.ExitCode;
