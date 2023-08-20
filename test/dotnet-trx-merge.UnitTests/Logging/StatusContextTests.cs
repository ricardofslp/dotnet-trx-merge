using FluentAssertions;
using Xunit;
using StatusContext = dotnet_trx_merge.Logging.StatusContext;

namespace dotnet_test_rerun.UnitTests.Logging;

public class StatusContextTests
{
    [Fact]
    public void StatusContextTests_WithNullContext_Status_ShouldThrowException()
    {
        // Arrange
        var statusContext = new StatusContext(null!);
        
        // Act
        var act = () => statusContext.Status("test");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null. (Parameter 'context')");
    }
}