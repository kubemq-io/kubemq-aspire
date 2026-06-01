// KubeMQ Aspire Examples — Shared Orchestrator
//
// This AppHost provisions a KubeMQ container and registers all example projects.
// Use launch profiles to run specific categories:
//   dotnet run --launch-profile pubsub

var builder = DistributedApplication.CreateBuilder(args);

// KubeMQ server — auto-provisions Docker container
var licenseKey = builder.AddParameter("kubemq-license", secret: true);
var kubemq = builder.AddKubeMQ("messaging")
    .WithLicenseKey(licenseKey)
    .WithDataVolume();

// --- PubSub ---
#region PubSub
builder.AddProject<Projects.PubSub_BasicPublisher>("pubsub-basic-publisher")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.PubSub_BasicSubscriber>("pubsub-basic-subscriber")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.PubSub_WildcardSubscription>("pubsub-wildcard-subscription")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.PubSub_MultipleSubscribers>("pubsub-multiple-subscribers")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.PubSub_ConsumerGroup>("pubsub-consumer-group")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.PubSub_CancelSubscription>("pubsub-cancel-subscription")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.PubSub_StreamPublish>("pubsub-stream-publish")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- EventsStore ---
#region EventsStore
builder.AddProject<Projects.EventsStore_PersistentPubSub>("eventsstore-persistent-pubsub")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.EventsStore_StreamPublish>("eventsstore-stream-publish")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.EventsStore_StartFromFirst>("eventsstore-start-from-first")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.EventsStore_StartFromLast>("eventsstore-start-from-last")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.EventsStore_StartNewOnly>("eventsstore-start-new-only")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.EventsStore_StartAtTime>("eventsstore-start-at-time")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.EventsStore_StartAtTimeDelta>("eventsstore-start-at-time-delta")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.EventsStore_ReplayFromSequence>("eventsstore-replay-from-sequence")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.EventsStore_ReplayFromTime>("eventsstore-replay-from-time")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.EventsStore_ConsumerGroup>("eventsstore-consumer-group")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.EventsStore_CancelSubscription>("eventsstore-cancel-subscription")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- Queues ---
#region Queues
builder.AddProject<Projects.Queues_SendReceive>("queues-send-receive")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Queues_AckReject>("queues-ack-reject")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Queues_Batch>("queues-batch")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Queues_DeadLetterQueue>("queues-dead-letter-queue")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Queues_DelayedMessages>("queues-delayed-messages")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Queues_ExpirationPolicy>("queues-expiration-policy")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Queues_Peek>("queues-peek")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Queues_PollMode>("queues-poll-mode")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- QueuesStream ---
#region QueuesStream
builder.AddProject<Projects.QueuesStream_StreamSend>("queuesstream-stream-send")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.QueuesStream_StreamReceive>("queuesstream-stream-receive")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.QueuesStream_ReceiverBasic>("queuesstream-receiver-basic")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.QueuesStream_ReceiverPerMessage>("queuesstream-receiver-per-message")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.QueuesStream_ReceiverConcurrentAck>("queuesstream-receiver-concurrent-ack")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.QueuesStream_ReceiverErrorHandling>("queuesstream-receiver-error-handling")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.QueuesStream_AutoAck>("queuesstream-auto-ack")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.QueuesStream_AckAll>("queuesstream-ack-all")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.QueuesStream_NackAll>("queuesstream-nack-all")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.QueuesStream_RequeueAll>("queuesstream-requeue-all")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.QueuesStream_DeadLetterPolicy>("queuesstream-dead-letter-policy")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.QueuesStream_PollMode>("queuesstream-poll-mode")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- Commands ---
#region Commands
builder.AddProject<Projects.Commands_SendCommand>("commands-send-command")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Commands_HandleCommand>("commands-handle-command")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Commands_CommandTimeout>("commands-command-timeout")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Commands_ConsumerGroup>("commands-consumer-group")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- Queries ---
#region Queries
builder.AddProject<Projects.Queries_SendQuery>("queries-send-query")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Queries_HandleQuery>("queries-handle-query")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Queries_ConsumerGroup>("queries-consumer-group")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Queries_CachedResponse>("queries-cached-response")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- Config ---
#region Config
builder.AddProject<Projects.Config_BasicConnection>("config-basic-connection")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Config_AuthToken>("config-auth-token")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Config_TlsSetup>("config-tls-setup")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Config_MtlsSetup>("config-mtls-setup")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Config_CustomTimeouts>("config-custom-timeouts")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Config_GrpcTuning>("config-grpc-tuning")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Config_KeepaliveSettings>("config-keepalive-settings")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Config_ReconnectionPolicy>("config-reconnection-policy")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Config_EnvironmentOverrides>("config-environment-overrides")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Config_ChannelManagement>("config-channel-management")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Config_PurgeQueue>("config-purge-queue")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- Aspire-Specific ---
#region Aspire
builder.AddProject<Projects.Aspire_SingleInstance>("aspire-single-instance")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Aspire_KeyedMultiInstance>("aspire-keyed-multi-instance")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Aspire_HealthChecks>("aspire-health-checks")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Aspire_OpenTelemetryTracing>("aspire-opentelemetry-tracing")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Aspire_OpenTelemetryMetrics>("aspire-opentelemetry-metrics")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Aspire_GracefulShutdown>("aspire-graceful-shutdown")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Aspire_ContainerProvisioning>("aspire-container-provisioning")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Aspire_LicenseKeySecret>("aspire-license-key-secret")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Aspire_DataVolume>("aspire-data-volume")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Aspire_CustomImageTag>("aspire-custom-image-tag")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- Patterns ---
#region Patterns
builder.AddProject<Projects.Patterns_FanOut>("patterns-fan-out")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Patterns_RequestReply>("patterns-request-reply")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.Patterns_WorkQueue>("patterns-work-queue")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- ErrorHandling ---
#region ErrorHandling
builder.AddProject<Projects.ErrorHandling_ConnectionError>("errorhandling-connection-error")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.ErrorHandling_Reconnection>("errorhandling-reconnection")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.ErrorHandling_GracefulShutdown>("errorhandling-graceful-shutdown")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- Scenarios: OrderProcessing ---
#region OrderProcessing
builder.AddProject<Projects.OrderProcessing_Api>("orderprocessing-api")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.OrderProcessing_Processor>("orderprocessing-processor")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.OrderProcessing_Notifier>("orderprocessing-notifier")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.OrderProcessing_Dashboard>("orderprocessing-dashboard")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- Scenarios: RealtimeChat ---
#region RealtimeChat
builder.AddProject<Projects.RealtimeChat_Server>("realtimechat-server")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.RealtimeChat_Worker>("realtimechat-worker")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.RealtimeChat_Bot>("realtimechat-bot")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- Scenarios: IoTIngestion ---
#region IoTIngestion
builder.AddProject<Projects.IoTIngestion_Gateway>("iotingestion-gateway")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.IoTIngestion_Processor>("iotingestion-processor")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.IoTIngestion_Alerter>("iotingestion-alerter")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.IoTIngestion_Commander>("iotingestion-commander")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

// --- Scenarios: ApiGateway ---
#region ApiGateway
builder.AddProject<Projects.ApiGateway_Gateway>("apigateway-gateway")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.ApiGateway_UserService>("apigateway-user-service")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.ApiGateway_ProductService>("apigateway-product-service")
    .WithReference(kubemq).WaitFor(kubemq);
builder.AddProject<Projects.ApiGateway_AuditService>("apigateway-audit-service")
    .WithReference(kubemq).WaitFor(kubemq);
#endregion

builder.Build().Run();
