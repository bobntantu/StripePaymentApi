
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using StripePaymentApi.Configuration;
using StripePaymentApi.Models;

namespace StripePaymentApi.Services;

public class StripePaymentService
{
    private readonly StripeSettings _settings;

    public StripePaymentService(
        IOptions<StripeSettings> settings)
    {
        _settings = settings.Value;

        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<Session> CreateCheckoutSessionAsync(
        Payment payment)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",

            CustomerEmail = payment.CustomerEmail,

            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,

                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = payment.Currency.ToLower(),

                        UnitAmount = (long)(payment.Amount * 100),

                        ProductData =
                            new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = payment.Description
                            }
                    }
                }
            },

            SuccessUrl =
                $"https://localhost:5001/payment/success?reference={payment.ReferenceNo}",

            CancelUrl =
                $"https://localhost:5001/payment/cancelled?reference={payment.ReferenceNo}",

            Metadata = new Dictionary<string, string>
            {
                ["referenceNo"] = payment.ReferenceNo
            }
        };

        var service = new SessionService();

        return await service.CreateAsync(options);
    }
}

