using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Commands;
using Microsoft.AspNetCore.Mvc;

namespace KubeMQ.Aspire.Sample.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CommandsController : ControllerBase
{
    private readonly IKubeMQClient _client;

    public CommandsController(IKubeMQClient client) => _client = client;

    /// <summary>
    /// Sends a command to the "commands.example" channel.
    /// A responder must be subscribed for this to succeed.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendCommand([FromBody] string body)
    {
        var command = new CommandMessage
        {
            Channel = "commands.example",
            Body = Encoding.UTF8.GetBytes(body),
            TimeoutInSeconds = 10,
        };

        var response = await _client.SendCommandAsync(command);
        return Ok(new { response.Executed, response.Error });
    }
}
