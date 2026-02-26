using System.Xml.Linq;

namespace dotnet_trx_merge.Domain;

public class MergeResult
{
    public XDocument MergedDocument { get; }
    public List<AttachmentDirectory> AttachmentDirectories { get; }

    public MergeResult(XDocument mergedDocument, List<AttachmentDirectory> attachmentDirectories)
    {
        MergedDocument = mergedDocument;
        AttachmentDirectories = attachmentDirectories;
    }
}

public record AttachmentDirectory(string SourceTrxDirectory, string RelativeResultsDirectory);
