using System.Xml.Linq;

namespace dotnet_trx_merge.Services;

public interface ITrxFetcher
{
    public void AddLatestTests(XDocument mergedDocument, string[] filesToMerge);
}