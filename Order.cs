using System;
using System.Collections.Generic;

namespace Orders.Functions.Models;

public class Order
{
    public Guid Id { get; set; }
    public string? CustomerName { get; set; }
    public List<OrderItem>? Items { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();
        if (string.IsNullOrWhiteSpace(CustomerName))
            errors.Add("CustomerName is required.");
        if (Items == null || Items.Count == 0)
            errors.Add("At least one order item is required.");
        else
        {
            foreach (var item in Items)
            {
                if (item.Quantity <= 0)
                    errors.Add($"Item '{item.ProductName}' must have quantity greater than zero.");
                if (item.UnitPrice < 0)
                    errors.Add($"Item '{item.ProductName}' must have a non-negative unit price.");
            }
        }
        if (TotalAmount < 0)
            errors.Add("TotalAmount must be non-negative.");
        return errors.Count == 0;
    }
}

public class OrderItem //a record is probably better here.
{
    public string? ProductId { get; set; } //FIX: this will never be null
    public string? ProductName { get; set; } //FIX: this will never be null
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

