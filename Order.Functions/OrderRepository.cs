using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Orders.Functions.Models;

namespace Orders.Functions.Data;

public class OrderRepository
{
    private readonly Container _container;

    public OrderRepository(CosmosClient client, string databaseId, string containerId)
    {
        _container = client.GetContainer(databaseId, containerId);
    }

    public async Task SaveOrderAsync(Order order)
    {
        await _container.CreateItemAsync(order, new PartitionKey(order.Id.ToString()));
    }
}

