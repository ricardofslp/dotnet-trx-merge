using dotnet_trx_merge.Exceptions;
using FluentAssertions;
using Xunit;

namespace dotnet_test_rerun.UnitTests.Exceptions;

public class MergeExceptionTests
{
    [Fact]
    public void MergeException_DefaultConstructor_ShouldNotBeNull()
    {
        // Arrange
        // Act
        var mergeException = new MergeException();
        
        // Assert
        mergeException.Should().NotBeNull();
    }

    [Fact]
    public void MergeException_ConstructorWithMessage_ShouldSetMessage()
    {
        // Arrange
        string message = "Test message";
        
        // Act
        var mergeException = new MergeException(message);
        
        // Assert
        mergeException.Message.Should().Be(message);
    }

    [Fact]
    public void MergeException_ConstructorWithInnerException_ShouldSetMessageAndInnerException()
    {
        // Arrange
        string message = "Test message";
        var innerException = new Exception("Inner exception message");
        
        // Act
        var mergeException = new MergeException(message, innerException);
        
        // Assert
        mergeException.Message.Should().Be(message);
        mergeException.InnerException.Should().Be(innerException);
    }
}

