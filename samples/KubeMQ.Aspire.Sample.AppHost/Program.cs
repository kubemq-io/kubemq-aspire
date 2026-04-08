var builder = DistributedApplication.CreateBuilder(args);

var kubemqKey = builder.AddParameter("kubemq-key", secret: true);

var messaging = builder.AddKubeMQ("messaging")
    .WithLicenseKey(kubemqKey)
    .WithDataVolume();

builder.AddProject<Projects.KubeMQ_Aspire_Sample_WebApi>("webapi")
    .WithReference(messaging)
    .WaitFor(messaging);

builder.Build().Run();
