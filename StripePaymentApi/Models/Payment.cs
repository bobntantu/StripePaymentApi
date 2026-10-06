
namespace StripePaymentApi.Models;

public class Payment
{
    public int Id { get; set; }

    public string ReferenceNo { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "usd";

    public string Description { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string PaymentProvider { get; set; } = "Stripe";

    public string Status { get; set; } = "Pending";

    public string? StripeSessionId { get; set; }

    public string? StripePaymentIntentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }
}

