using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace KubeMQ.Aspire.Hosting;

/// <summary>
/// Represents a KubeMQ server resource in the Aspire application model.
/// </summary>
public class KubeMQServerResource : ContainerResource, IResourceWithConnectionString
{
    internal const string GrpcEndpointName = "grpc";
    internal const string RestEndpointName = "rest";
    internal const string DashboardEndpointName = "dashboard";

    /// <summary>Initializes a new KubeMQ server resource with the specified name.</summary>
    /// <param name="name">The resource name.</param>
    public KubeMQServerResource(string name) : base(name) { }

    private EndpointReference? _grpcEndpoint;

    /// <summary>Gets the gRPC endpoint reference.</summary>
    public EndpointReference GrpcEndpoint =>
        _grpcEndpoint ??= new EndpointReference(this, GrpcEndpointName);

    /// <summary>Gets the connection string expression (host:port for gRPC).</summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"{GrpcEndpoint.Property(EndpointProperty.Host)}:{GrpcEndpoint.Property(EndpointProperty.Port)}");
}
