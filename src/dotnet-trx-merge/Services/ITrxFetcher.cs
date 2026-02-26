using System.Xml.Linq;
using dotnet_trx_merge.Domain;

namespace dotnet_trx_merge.Services;

public interface ITrxFetcher
{
    public XDocument AddLatestTests(string[] filesToMerge);
    public MergeResult MergeWithAttachments(string[] filesToMerge);
}