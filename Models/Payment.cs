namespace MidtransQrisApi.Models;

public class Payment
{
    public int Id { get; set; }

    public string OrderId { get; set; } = string.Empty;

    public string TransactionId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Status { get; set; } = "pending";

    public string? QrCodeUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}