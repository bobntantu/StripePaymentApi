
namespace StripePaymentApi.Models;

public class PaymentRequest
{
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "usd";

    public string Description { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string ReferenceNo { get; set; } = string.Empty;
}


