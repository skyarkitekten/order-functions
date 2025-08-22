using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Orders.Functions;

public class ReceiveOrderFunction
{
    private readonly ILogger<ReceiveOrderFunction> _logger;

    public ReceiveOrderFunction(ILogger<ReceiveOrderFunction> logger)
    {
        _logger = logger;
    }

    [Function("ReceiveOrderFunction")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        //TODO: Write Order to queue.

        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
