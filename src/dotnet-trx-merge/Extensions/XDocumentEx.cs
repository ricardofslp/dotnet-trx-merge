using System.Xml.Linq;

namespace dotnet_trx_merge.Extensions;

public static class XDocumentEx
{
    public static XDocument ReplaceAllNamespaces(this XDocument doc, string? newNsValue)
    {
        if (string.IsNullOrWhiteSpace(newNsValue))
            return doc;
        
        XNamespace newNs = XNamespace.Get(newNsValue);

        // Recursively update the namespace of all elements and attributes
        void UpdateNamespaces(XElement element)
        {
            // Update the namespace of the current element
            element.Name = newNs.GetName(element.Name.LocalName);

            // Remove the old namespace declaration if present
            XAttribute? oldNsAttr = element.Attributes().Where(attr => attr.IsNamespaceDeclaration).FirstOrDefault();

            // Replace the namespace declaration
            if (oldNsAttr is not null)
            {
                oldNsAttr.Remove();
                element.SetAttributeValue(XNamespace.Xmlns + "ns", newNs);
            }

            // Recurse into child elements
            foreach (var child in element.Elements())
                UpdateNamespaces(child);
        }

        // Start updating namespaces from the root
        UpdateNamespaces(doc.Root);

        return doc;
    }
}