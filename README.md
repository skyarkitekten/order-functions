# Order Processing Azure Functions

This project contains Azure Durable Functions for processing orders with validation, persistence, and notification capabilities.

## Architecture Overview

The solution uses Azure Durable Functions to orchestrate order processing through multiple activities:

1. **Order Validation** - Validates incoming order data
2. **Order Persistence** - Saves orders to Azure Cosmos DB
3. **Order Notification** - Sends events to Azure Event Grid

## Components

### Models

#### `Order`

Represents an order with the following properties:

- `Id` (`Guid`) - Unique order identifier
- `CustomerName` (`string`) - Name of the customer
- `Items` (`List<OrderItem>`) - List of items in the order
- `TotalAmount` (`decimal`) - Total amount of the order
- `CreatedAt` (`DateTime`) - When the order was created

#### `OrderItem`

Represents an individual item in an order:

- `ProductId` (string) - Unique product identifier
- `ProductName` (string) - Name of the product
- `Quantity` (int) - Quantity ordered
- `UnitPrice` (decimal) - Price per unit

### Services

#### `OrderRepository`

Handles persistence of orders to Azure Cosmos DB.

- **Method**: `SaveOrderAsync(Order order)` - Saves an order to the database

#### `OrderNotificationService`

Handles sending notifications via Azure Event Grid.

- **Method**: `NotifyOrderProcessedAsync(Order order)` - Sends order processed event

### Functions

#### `ProcessOrderFunction` (Orchestrator)

**Endpoint**: `POST /api/ProcessOrderFunction_HttpStart`

**Description**: Main orchestrator that coordinates the order processing workflow.

**Input**: JSON order object in the request body

```json
{
  "customerName": "John Doe",
  "items": [
    {
      "productId": "P001",
      "productName": "Widget",
      "quantity": 2,
      "unitPrice": 10.0
    }
  ],
  "totalAmount": 20.0
}
```

**Process Flow**:

1. Accepts HTTP POST request with order JSON
2. Validates and deserializes order data
3. Starts durable function orchestration
4. Returns HTTP 202 with status URLs for monitoring

**Response**: Standard Durable Functions response with status URLs

```json
{
  "id": "orchestration-instance-id",
  "statusQueryGetUri": "https://...",
  "sendEventPostUri": "https://...",
  "terminatePostUri": "https://...",
  "rewindPostUri": "https://..."
}
```

#### Orchestration Activities

##### `ValidateOrderActivity`

**Purpose**: Validates order data according to business rules

**Validation Rules**:

- CustomerName is required (not null/empty)
- At least one order item is required
- Each item must have quantity > 0
- Each item must have non-negative unit price
- Total amount must be non-negative

**Returns**: `true` if valid, `false` if validation fails

##### `SaveOrderActivity`

**Purpose**: Persists the validated order to Azure Cosmos DB

**Behavior**:

- Saves order using OrderRepository
- Logs successful save operation
- Currently implements placeholder logic (ready for dependency injection)

##### `NotifyOrderActivity`

**Purpose**: Sends order processed notification via Event Grid

**Behavior**:

- Publishes "OrderProcessed" event to Event Grid
- Includes order details in event payload
- Currently implements placeholder logic (ready for dependency injection)

## Usage Examples

### Submit Order for Processing

```bash
curl -X POST "https://your-function-app.azurewebsites.net/api/ProcessOrderFunction_HttpStart" \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Jane Smith",
    "items": [
      {
        "productId": "P002",
        "productName": "Gadget",
        "quantity": 1,
        "unitPrice": 25.99
      }
    ],
    "totalAmount": 25.99
  }'
```

### Check Processing Status

Use the `statusQueryGetUri` from the initial response:

```bash
curl "https://your-function-app.azurewebsites.net/runtime/webhooks/durabletask/instances/{instanceId}"
```

## Error Handling

### HTTP Errors

- **400 Bad Request**: Invalid JSON format or missing required fields
- **500 Internal Server Error**: Unexpected errors during processing

### Validation Errors

If order validation fails, the orchestration will complete with a failure message indicating validation issues.

### Retry Logic

Durable Functions provide built-in retry capabilities for transient failures in activities.

## Configuration Requirements

### Azure Cosmos DB

- Connection string configured in application settings
- Database and container created for order storage
- Partition key: order ID

### Azure Event Grid

- Event Grid topic endpoint and access key
- Topic configured to receive "OrderProcessed" events

### Dependencies

- Microsoft.Azure.Cosmos (for Cosmos DB integration)
- Azure.Messaging.EventGrid (for Event Grid notifications)
- Microsoft.Azure.Functions.Worker.Extensions.DurableTask (for orchestration)

## Monitoring

### Application Insights

- All activities log information for monitoring
- Error details captured for troubleshooting
- Performance metrics available

### Durable Functions Dashboard

- View orchestration instances and their status
- Track activity execution and timing
- Debug failed orchestrations

## Future Enhancements

1. **Dependency Injection**: Configure proper DI for OrderRepository and OrderNotificationService
2. **Unit Testing**: Add comprehensive test coverage
3. **Enhanced Validation**: Add more sophisticated business rules
4. **Error Handling**: Implement compensation logic for failed activities
5. **Monitoring**: Add custom metrics and alerts
