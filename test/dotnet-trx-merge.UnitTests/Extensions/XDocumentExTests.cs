using System.Xml.Linq;
using dotnet_trx_merge.Extensions;
using FluentAssertions;
using Xunit;

namespace dotnet_test_rerun.UnitTests.Extensions;

public class XDocumentExTests
{
    [Fact]
    public void ReplaceAllNamespaces_NullXDocument_ReturnsNull()
    {
        // Arrange
        XDocument doc = null;
    
        // Act
        var result = doc.ReplaceAllNamespaces("http://newnamespace.com");
    
        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ReplaceAllNamespaces_NullOrEmptyNewNsValue_ReturnsOriginalXDocument()
    {
        // Arrange
        XDocument doc = new XDocument(new XElement("root"));
    
        // Act
        var result1 = doc.ReplaceAllNamespaces(null);
        var result2 = doc.ReplaceAllNamespaces("");
    
        // Assert
        result1.ToString().Should().Be(doc.ToString());
        result2.ToString().Should().Be(doc.ToString());
    }

    [Fact]
    public void ReplaceAllNamespaces_ValidNewNsValue_UpdatesNamespaces()
    {
        // Arrange
        XDocument doc = new XDocument(new XElement("root"));
    
        // Act
        var result = doc.ReplaceAllNamespaces("http://newnamespace.com");
    
        // Assert
        result.Root.Name.Namespace.NamespaceName.Should().Be("http://newnamespace.com");
    }

    [Fact]
    public void ReplaceAllNamespaces_DifferentNamespaces_UpdatesAllNamespaces()
    {
        // Arrange
        XNamespace ns1 = "http://namespace1.com";
        XNamespace ns2 = "http://namespace2.com";
        var newNamespace = "http://newnamespace.com";
        XDocument doc = new XDocument(
            new XElement(ns1 + "root",
                new XElement(ns2 + "child1"),
                new XElement(ns1 + "child2")
            )
        );
    
        // Act
        var result = doc.ReplaceAllNamespaces(newNamespace);
    
        // Assert
        result.Root.Name.Namespace.NamespaceName.Should().Be(newNamespace);
        result.Root.Element($"{{{newNamespace}}}child1").Name.Namespace.NamespaceName.Should().Be(newNamespace);
        result.Root.Element($"{{{newNamespace}}}child2").Name.Namespace.NamespaceName.Should().Be(newNamespace);
    }
}