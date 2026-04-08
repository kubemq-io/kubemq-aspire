using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Queues;
using Microsoft.AspNetCore.Mvc;

namespace KubeMQ.Aspire.Sample.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class QueuesController : ControllerBase
{
    private readonly IKubeMQClient _client;

    public QueuesController(IKubeMQClient client) => _client = client;

    /// <summary>
    /// Sends a message to the "queues.example" queue channel.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] string body)
    {
        var message = new QueueMessage
        {
            Channel = "queues.example",
            Body = Encoding.UTF8.GetBytes(body),
        };

        var result = await _client.SendQueueMessageAsync(message);
        return Ok(new { result.MessageId, result.IsError, result.Error });
    }

    /// <summary>
    /// Receives up to 5 messages from the "queues.example" queue channel.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ReceiveMessages()
    {
        var request = new QueuePollRequest
        {
            Channel = "queues.example",
            MaxMessages = 5,
            WaitTimeoutSeconds = 5,
            AutoAck = true,
        };

        var response = await _client.ReceiveQueueMessagesAsync(request);
        var messages = response.Messages.Select(m => new
        {
            m.MessageId,
            Body = Encoding.UTF8.GetString(m.Body.Span),
        }).ToList();

        return Ok(new { Count = messages.Count, Messages = messages });
    }
}
