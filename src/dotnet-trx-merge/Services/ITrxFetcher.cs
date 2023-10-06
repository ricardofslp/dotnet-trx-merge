using System.Xml.Linq;

namespace dotnet_trx_merge.Services;

public interface ITrxFetcher
{
    public XDocument AddLatestTests(string[] filesToMerge);
}