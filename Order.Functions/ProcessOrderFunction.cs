using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net;
using Orders.Functions.Models;
using Orders.Functions.Data;
using Orders.Functions.Services;

namespace Orders.Functions;

public static class ProcessOrderFunction
{
    [Function(nameof(ProcessOrderFunction))]
    public static async Task<string> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(ProcessOrderFunction));
        logger.LogInformation("Starting order processing orchestration.");

        var order = context.GetInput<Order>();

        try
        {
            // Step 1: Validate order
            var validationResult = await context.CallActivityAsync<bool>(nameof(ValidateOrderActivity), order);
            if (!validationResult)
            {
                logger.LogWarning("Order validation failed for order {orderId}.", order?.Id);
                return "Order validation failed";
            }

            // Step 2: Save order to database
            await context.CallActivityAsync(nameof(SaveOrderActivity), order);
            logger.LogInformation("Order {orderId} saved successfully.", order.Id);

            // Step 3: Send notification
            await context.CallActivityAsync(nameof(NotifyOrderActivity), order);
            logger.LogInformation("Notification sent for order {orderId}.", order.Id);

            return $"Order {order.Id} processed successfully";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing order {orderId}.", order?.Id);
            return $"Error processing order: {ex.Message}";
        }
    }

    [Function(nameof(ValidateOrderActivity))]
    public static bool ValidateOrderActivity([ActivityTrigger] Order order, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("ValidateOrderActivity");
        logger.LogInformation("Validating order {orderId}.", order.Id);

        return order.IsValid(out var errors);
    }

    [Function(nameof(SaveOrderActivity))]
    public static Task SaveOrderActivity([ActivityTrigger] Order order, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("SaveOrderActivity");
        logger.LogInformation("Saving order {orderId}.", order.Id);

        // Note: In a real implementation, you'd inject dependencies here
        // For now, this is a placeholder that would need dependency injection setup
        logger.LogInformation("Order {orderId} would be saved to database.", order.Id);

        return Task.CompletedTask;
    }

    [Function(nameof(NotifyOrderActivity))]
    public static Task NotifyOrderActivity([ActivityTrigger] Order order, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("NotifyOrderActivity");
        logger.LogInformation("Sending notification for order {orderId}.", order.Id);

        // Note: In a real implementation, you'd inject dependencies here
        // For now, this is a placeholder that would need dependency injection setup
        logger.LogInformation("Notification sent for order {orderId}.", order.Id);

        return Task.CompletedTask;
    }

    [Function("ProcessOrderFunction_HttpStart")]
    public static async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("ProcessOrderFunction_HttpStart");

        try
        {
            // Read and parse order from request body
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Request body cannot be empty.");
                return badRequest;
            }

            var order = JsonSerializer.Deserialize<Order>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (order == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid order data.");
                return badRequest;
            }

            // Set defaults if needed
            if (order.Id == Guid.Empty)
                order.Id = Guid.NewGuid();
            if (order.CreatedAt == DateTime.MinValue)
                order.CreatedAt = DateTime.UtcNow;

            // Start orchestration with order data
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(ProcessOrderFunction), order);

            logger.LogInformation("Started order processing orchestration with ID = '{instanceId}' for order {orderId}.", instanceId, order.Id);

            // Returns an HTTP 202 response with an instance management payload.
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse JSON from request body.");
            var jsonError = req.CreateResponse(HttpStatusCode.BadRequest);
            await jsonError.WriteStringAsync("Invalid JSON format.");
            return jsonError;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred.");
            var serverError = req.CreateResponse(HttpStatusCode.InternalServerError);
            await serverError.WriteStringAsync("An internal error occurred.");
            return serverError;
        }
    }
}