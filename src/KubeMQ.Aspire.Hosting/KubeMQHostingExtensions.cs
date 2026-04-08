using Aspire.Hosting.ApplicationModel;
using KubeMQ.Aspire.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding KubeMQ server resources to the Aspire application model.
/// </summary>
public static class KubeMQHostingExtensions
{
    /// <summary>
    /// Adds a KubeMQ server resource to the application model.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The resource name.</param>
    /// <param name="grpcPort">Optional fixed gRPC port (default: auto-assigned).</param>
    /// <returns>A resource builder for further configuration.</returns>
    public static IResourceBuilder<KubeMQServerResource> AddKubeMQ(
        this IDistributedApplicationBuilder builder,
        string name,
        int? grpcPort = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var resource = new KubeMQServerResource(name);

        return builder.AddResource(resource)
            .WithImage(KubeMQContainerImageTags.Image, KubeMQContainerImageTags.Tag)
            .WithImageRegistry(KubeMQContainerImageTags.Registry)
            .WithEndpoint(port: grpcPort, targetPort: 50000, name: KubeMQServerResource.GrpcEndpointName)
            .WithEndpoint(targetPort: 9090, name: KubeMQServerResource.RestEndpointName)
            .WithHttpEndpoint(targetPort: 8080, name: KubeMQServerResource.DashboardEndpointName)
            .WithLifetime(ContainerLifetime.Persistent);
    }

    /// <summary>Sets the KubeMQ license key from a parameter resource (secrets).</summary>
    public static IResourceBuilder<KubeMQServerResource> WithLicenseKey(
        this IResourceBuilder<KubeMQServerResource> builder,
        IResourceBuilder<ParameterResource> licenseKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(licenseKey);

        return builder.WithEnvironment("KUBEMQ_TOKEN", licenseKey);
    }

    /// <summary>Adds a data volume for persistent message storage at /store.</summary>
    public static IResourceBuilder<KubeMQServerResource> WithDataVolume(
        this IResourceBuilder<KubeMQServerResource> builder,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var volumeName = string.IsNullOrWhiteSpace(name)
            ? $"{builder.Resource.Name}-data"
            : name;
        return builder.WithVolume(volumeName, "/store");
    }

    /// <summary>Overrides the default Docker image tag.</summary>
    public static IResourceBuilder<KubeMQServerResource> WithImageTag(
        this IResourceBuilder<KubeMQServerResource> builder,
        string tag)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        return builder.WithImage(KubeMQContainerImageTags.Image, tag);
    }
}
