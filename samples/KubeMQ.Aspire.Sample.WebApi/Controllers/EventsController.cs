using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Events;
using Microsoft.AspNetCore.Mvc;

namespace KubeMQ.Aspire.Sample.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EventsController : ControllerBase
{
    private readonly IKubeMQClient _client;

    public EventsController(IKubeMQClient client) => _client = client;

    /// <summary>
    /// Publishes an event to the "events.example" channel.
    /// Fire-and-forget -- no response expected.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PublishEvent([FromBody] string body)
    {
        var message = new EventMessage
        {
            Channel = "events.example",
            Body = Encoding.UTF8.GetBytes(body),
            Tags = new Dictionary<string, string> { ["source"] = "aspire-sample" },
        };

        await _client.SendEventAsync(message);
        return Ok(new { Status = "published" });
    }
}
