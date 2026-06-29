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

        var resourceBuilder = builder.AddResource(resource)
            .WithImage(KubeMQContainerImageTags.Image, KubeMQContainerImageTags.Tag)
            .WithEndpoint(port: grpcPort, targetPort: 50000, name: KubeMQServerResource.GrpcEndpointName)
            .WithEndpoint(targetPort: 9090, name: KubeMQServerResource.RestEndpointName)
            .WithHttpEndpoint(targetPort: 8080, name: KubeMQServerResource.DashboardEndpointName)
            .WithLifetime(ContainerLifetime.Persistent);

        // Call the framework's registry extension explicitly so it is unambiguous next to the
        // resource-specific WithImageRegistry overload defined below in this class.
        return ContainerResourceBuilderExtensions.WithImageRegistry(
            resourceBuilder, KubeMQContainerImageTags.Registry);
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

    /// <summary>
    /// Overrides the container image registry, so private or air-gapped users can repoint the
    /// KubeMQ pull to their own registry. Optionally overrides the image path and tag as well.
    /// </summary>
    /// <param name="builder">The KubeMQ server resource builder.</param>
    /// <param name="registry">The container registry host, e.g. <c>my-registry.example.com</c>.</param>
    /// <param name="image">Optional image path. When omitted, the previously configured image is kept.</param>
    /// <param name="tag">Optional image tag. When omitted, the previously configured tag is kept.</param>
    /// <remarks>
    /// Passing only <paramref name="registry"/> preserves any image/tag already set (e.g. via
    /// <see cref="WithImageTag"/>); the image and tag are only replaced when supplied here.
    /// </remarks>
    public static IResourceBuilder<KubeMQServerResource> WithImageRegistry(
        this IResourceBuilder<KubeMQServerResource> builder,
        string registry,
        string? image = null,
        string? tag = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(registry);

        if (image is not null || tag is not null)
        {
            builder = builder.WithImage(
                image ?? KubeMQContainerImageTags.Image,
                tag ?? KubeMQContainerImageTags.Tag);
        }

        // Invoke the framework extension explicitly (rather than the same-named overload here).
        return ContainerResourceBuilderExtensions.WithImageRegistry(builder, registry);
    }
}
