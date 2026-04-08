using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using KubeMQ.Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KubeMQ.Aspire.Hosting.Tests;

public sealed class KubeMQHostingTests
{
    private static IDistributedApplicationBuilder CreateBuilder()
    {
        return DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            DisableDashboard = true,
            Args = [],
        });
    }

    [Fact]
    public void AddKubeMQ_CreatesResourceWithCorrectImage()
    {
        var builder = CreateBuilder();

        var kubemq = builder.AddKubeMQ("messaging");
        var resource = kubemq.Resource;

        var imageAnnotation = resource.Annotations
            .OfType<ContainerImageAnnotation>()
            .Single();

        Assert.Equal("kubemq/kubemq", imageAnnotation.Image);
        Assert.Equal("docker.io", imageAnnotation.Registry);
        Assert.Equal("2.5.0", imageAnnotation.Tag);
    }

    [Fact]
    public void AddKubeMQ_ExposesThreeEndpoints()
    {
        var builder = CreateBuilder();

        var kubemq = builder.AddKubeMQ("messaging");
        var resource = kubemq.Resource;

        var endpoints = resource.Annotations
            .OfType<EndpointAnnotation>()
            .ToList();

        Assert.Equal(3, endpoints.Count);

        var grpc = Assert.Single(endpoints, e => e.Name == KubeMQServerResource.GrpcEndpointName);
        Assert.Equal(50000, grpc.TargetPort);

        var rest = Assert.Single(endpoints, e => e.Name == KubeMQServerResource.RestEndpointName);
        Assert.Equal(9090, rest.TargetPort);

        var dashboard = Assert.Single(endpoints, e => e.Name == KubeMQServerResource.DashboardEndpointName);
        Assert.Equal(8080, dashboard.TargetPort);
    }

    [Fact]
    public void AddKubeMQ_ConnectionStringFormat()
    {
        var builder = CreateBuilder();

        var kubemq = builder.AddKubeMQ("messaging");
        var resource = kubemq.Resource;

        Assert.IsAssignableFrom<IResourceWithConnectionString>(resource);
        Assert.NotNull(resource.ConnectionStringExpression);

        var grpcEndpoint = resource.GrpcEndpoint;
        Assert.Equal(KubeMQServerResource.GrpcEndpointName, grpcEndpoint.EndpointName);
    }

    [Fact]
    public void AddKubeMQ_DefaultsPersistentLifetime()
    {
        var builder = CreateBuilder();

        var kubemq = builder.AddKubeMQ("messaging");
        var resource = kubemq.Resource;

        var lifetimeAnnotation = resource.Annotations
            .OfType<ContainerLifetimeAnnotation>()
            .SingleOrDefault();

        Assert.NotNull(lifetimeAnnotation);
        Assert.Equal(ContainerLifetime.Persistent, lifetimeAnnotation.Lifetime);
    }

    [Fact]
    public void WithLicenseKey_Parameter_SetsEnvVar()
    {
        var builder = CreateBuilder();

        var kubemq = builder.AddKubeMQ("messaging");
        var key = builder.AddParameter("kubemq-key", secret: true);

        var envCountBefore = kubemq.Resource.Annotations
            .OfType<EnvironmentCallbackAnnotation>()
            .Count();

        kubemq.WithLicenseKey(key);

        var envCountAfter = kubemq.Resource.Annotations
            .OfType<EnvironmentCallbackAnnotation>()
            .Count();

        Assert.True(envCountAfter > envCountBefore,
            "WithLicenseKey(ParameterResource) should add an environment annotation");
    }

    [Fact]
    public void WithDataVolume_MapsToStore()
    {
        var builder = CreateBuilder();

        var kubemq = builder.AddKubeMQ("messaging");
        kubemq.WithDataVolume();

        var volumeAnnotation = kubemq.Resource.Annotations
            .OfType<ContainerMountAnnotation>()
            .SingleOrDefault(m => m.Target == "/store");

        Assert.NotNull(volumeAnnotation);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
    }

    [Fact]
    public void WithImageTag_OverridesDefault()
    {
        var builder = CreateBuilder();

        var kubemq = builder.AddKubeMQ("messaging");
        kubemq.WithImageTag("2.5.0");

        var imageAnnotations = kubemq.Resource.Annotations
            .OfType<ContainerImageAnnotation>()
            .ToList();

        var latest = imageAnnotations.Last();
        Assert.Equal("kubemq/kubemq", latest.Image);
        Assert.Equal("2.5.0", latest.Tag);
    }

    [Fact]
    public void AddKubeMQ_FixedGrpcPort()
    {
        var builder = CreateBuilder();

        var kubemq = builder.AddKubeMQ("messaging", grpcPort: 55000);
        var resource = kubemq.Resource;

        var grpc = resource.Annotations
            .OfType<EndpointAnnotation>()
            .Single(e => e.Name == KubeMQServerResource.GrpcEndpointName);

        Assert.Equal(55000, grpc.Port);
        Assert.Equal(50000, grpc.TargetPort);
    }

    [DockerAvailable]
    [Trait("Category", "Integration")]
    public async Task Integration_ContainerStarts()
    {
        var builder = CreateBuilder();

        var licenseKeyParam = builder.AddParameter("kubemq-key", secret: true);
        var kubemq = builder.AddKubeMQ("messaging")
            .WithLicenseKey(licenseKeyParam);

        using var app = builder.Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await app.StartAsync(cts.Token);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource = Assert.Single(model.Resources.OfType<KubeMQServerResource>());
        Assert.Equal("messaging", resource.Name);

        await app.StopAsync(cts.Token);
    }

    [DockerAvailable]
    [Trait("Category", "Integration")]
    public async Task Integration_HealthCheckPasses()
    {
        var builder = CreateBuilder();

        var licenseKeyParam = builder.AddParameter("kubemq-key-health", secret: true);
        var kubemq = builder.AddKubeMQ("messaging")
            .WithLicenseKey(licenseKeyParam);

        using var app = builder.Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await app.StartAsync(cts.Token);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource = Assert.Single(model.Resources.OfType<KubeMQServerResource>());

        Assert.IsAssignableFrom<IResourceWithConnectionString>(resource);

        await app.StopAsync(cts.Token);
    }

    [Fact]
    public void HostingExtensions_NullBuilder_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => KubeMQHostingExtensions.AddKubeMQ(null!, "test"));
    }

    [Fact]
    public void HostingExtensions_EmptyName_Throws()
    {
        var builder = CreateBuilder();
        Assert.Throws<ArgumentException>(
            () => builder.AddKubeMQ(""));
        Assert.Throws<ArgumentException>(
            () => builder.AddKubeMQ("   "));
    }

    [Fact]
    public void WithLicenseKey_NullBuilder_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => KubeMQHostingExtensions.WithLicenseKey(null!, null!));
    }

    [Fact]
    public void WithImageTag_NullBuilder_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => KubeMQHostingExtensions.WithImageTag(null!, "2.5.0"));
    }

    [Fact]
    public void WithImageTag_EmptyTag_Throws()
    {
        var builder = CreateBuilder();
        var kubemq = builder.AddKubeMQ("messaging");
        Assert.Throws<ArgumentException>(
            () => kubemq.WithImageTag(""));
        Assert.Throws<ArgumentException>(
            () => kubemq.WithImageTag("   "));
    }

    // --- Phase 1: M-11 WithDataVolume empty string ---

    [Fact]
    public void WithDataVolume_EmptyStringName_UsesDefault()
    {
        var builder = CreateBuilder();
        var kubemq = builder.AddKubeMQ("messaging");
        kubemq.WithDataVolume("");

        var volumeAnnotation = kubemq.Resource.Annotations
            .OfType<ContainerMountAnnotation>()
            .SingleOrDefault(m => m.Target == "/store");

        Assert.NotNull(volumeAnnotation);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
    }

    [Fact]
    public void WithDataVolume_WhitespaceOnlyName_UsesDefault()
    {
        var builder = CreateBuilder();
        var kubemq = builder.AddKubeMQ("messaging");
        kubemq.WithDataVolume("   ");

        var volumeAnnotation = kubemq.Resource.Annotations
            .OfType<ContainerMountAnnotation>()
            .SingleOrDefault(m => m.Target == "/store");

        Assert.NotNull(volumeAnnotation);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
    }
}
