using System;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Azure;
using Orders.Functions.Models;


namespace Orders.Functions.Services
{
    public class OrderNotificationService
    {
        private readonly EventGridPublisherClient _client;
        public OrderNotificationService(string topicEndpoint, string topicKey)
        {
            _client = new EventGridPublisherClient(
                new Uri(topicEndpoint),
                new AzureKeyCredential(topicKey)
            );
        }

        public async Task NotifyOrderProcessedAsync(Order order)
        {
            var eventData = new
            {
                OrderId = order.Id,
                CustomerName = order.CustomerName,
                TotalAmount = order.TotalAmount
            };

            var eventGridEvent = new EventGridEvent(
                subject: $"Order/{order.Id}",
                eventType: "OrderProcessed",
                dataVersion: "1.0",
                data: eventData
            );

            await _client.SendEventAsync(eventGridEvent);
        }
    }
}
