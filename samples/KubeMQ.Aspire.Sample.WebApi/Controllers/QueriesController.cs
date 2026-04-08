using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KubeMQ.Aspire.Sample.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class QueriesController : ControllerBase
{
    private readonly IKubeMQClient _client;

    public QueriesController(IKubeMQClient client) => _client = client;

    /// <summary>
    /// Sends a query to the "queries.example" channel.
    /// A responder must be subscribed for this to succeed.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendQuery([FromBody] string body)
    {
        var query = new QueryMessage
        {
            Channel = "queries.example",
            Body = Encoding.UTF8.GetBytes(body),
            TimeoutInSeconds = 10,
        };

        var response = await _client.SendQueryAsync(query);
        var responseBody = response.Body.Length > 0
            ? Encoding.UTF8.GetString(response.Body.Span)
            : "(empty)";

        return Ok(new { response.Executed, response.Error, Body = responseBody });
    }
}
