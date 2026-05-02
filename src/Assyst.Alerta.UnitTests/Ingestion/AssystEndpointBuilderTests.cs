using Assyst.Alerta.Ingestion;
using Assyst.Alerta.UnitTests.TestData;

namespace Assyst.Alerta.UnitTests.Ingestion;

[TestSubject(typeof(AssystEndpointBuilder))]
public sealed class AssystEndpointBuilderTests
{
    private static readonly string[] ExpectedFields =
    [
        "id",
        "affectedUserName",
        "alertStatusEnum",
        "shortDescription",
        "formattedReference",
        "assignedServDeptId",
        "dateOfLastAssignment",
        "lastSlaClockStop",
        "originalAssignedServDeptSC",
        "lastActionTypeId",
        "lastActionServDeptSC"
    ];

    [Theory]
    [InlineData("https://assyst.example.com")]
    [InlineData("https://assyst.example.com/")]
    public void BuildNonAssignedOpenEventsEndpoint_PathIsCanonicalRegardlessOfTrailingSlash(string baseUrl)
    {
        // Arrange
        var builder = new AssystEndpointBuilder(TestOptions.Ingestion(baseUrl: baseUrl));

        // Act
        var uri = builder.BuildNonAssignedOpenEventsEndpoint(547);

        // Assert
        uri.AbsolutePath.Should().Be("/assystREST/v2/events");
        uri.Host.Should().Be("assyst.example.com");
        uri.Scheme.Should().Be("https");
    }

    [Fact]
    public void BuildNonAssignedOpenEventsEndpoint_QueryIncludesDepartmentIdAndOpenStatusAndNoAssignedUserId()
    {
        // Arrange
        var builder = new AssystEndpointBuilder(TestOptions.Ingestion());

        // Act
        var uri = builder.BuildNonAssignedOpenEventsEndpoint(547);

        // Assert
        uri.Query.Should()
            .Contain("assignedServDeptId=547")
            .And.Contain("assignedUserId=0")
            .And.Contain("eventStatus=open");
    }

    [Fact]
    public void BuildNonAssignedOpenEventsEndpoint_QueryIncludesAllRequestedFields()
    {
        // Arrange
        var builder = new AssystEndpointBuilder(TestOptions.Ingestion());

        // Act
        var uri = builder.BuildNonAssignedOpenEventsEndpoint(547);

        // Assert
        foreach (var field in ExpectedFields)
        {
            uri.Query.Should().Contain(field);
        }
    }

    [Fact]
    public void BuildNonAssignedOpenEventsEndpoint_DifferentDepartmentIdsProduceDifferentUris()
    {
        // Arrange
        var builder = new AssystEndpointBuilder(TestOptions.Ingestion());

        // Act
        var first = builder.BuildNonAssignedOpenEventsEndpoint(547);
        var second = builder.BuildNonAssignedOpenEventsEndpoint(553);

        // Assert
        first.Should().NotBe(second);
        first.Query.Should().Contain("assignedServDeptId=547");
        second.Query.Should().Contain("assignedServDeptId=553");
    }
}