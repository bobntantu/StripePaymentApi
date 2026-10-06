using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using StripePaymentApi.Configuration;
using StripePaymentApi.Data;
using StripePaymentApi.Models;
using StripePaymentApi.Services;

namespace StripePaymentApi.Controllers;

[ApiController]
[Route("api/stripe")]
public class StripeController : ControllerBase
{
    private readonly PaymentDbContext _db;
    private readonly StripePaymentService _stripeService;
    private readonly StripeSettings _settings;

    public StripeController(
        PaymentDbContext db,
        StripePaymentService stripeService,
        IOptions<StripeSettings> settings)
    {
        _db = db;
        _stripeService = stripeService;
        _settings = settings.Value;
    }

    [HttpPost("create-checkout")]
    public async Task<IActionResult> CreateCheckout(
        [FromBody] PaymentRequest request)
    {
        if (request == null)
            return BadRequest("Payment request is required.");

        if (request.Amount <= 0)
            return BadRequest("Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(request.Currency))
            return BadRequest("Currency is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Description is required.");

        var referenceNo =
            string.IsNullOrWhiteSpace(request.ReferenceNo)
                ? GenerateReferenceNumber()
                : request.ReferenceNo;

        var payment = new Payment
        {
            ReferenceNo = referenceNo,
            Amount = request.Amount,
            Currency = request.Currency.ToLowerInvariant(),
            Description = request.Description,
            CustomerEmail = request.CustomerEmail,
            PaymentProvider = "Stripe",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);

        await _db.SaveChangesAsync();

        try
        {
            var session =
                await _stripeService.CreateCheckoutSessionAsync(payment);

            payment.StripeSessionId = session.Id;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                referenceNo = payment.ReferenceNo,
                sessionId = session.Id,
                checkoutUrl = session.Url
            });
        }
        catch (StripeException)
        {
            payment.Status = "Failed";

            await _db.SaveChangesAsync();

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "Unable to create Stripe Checkout Session."
                });
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(Request.Body)
            .ReadToEndAsync();

        var signature =
            Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrWhiteSpace(signature))
            return BadRequest("Missing Stripe webhook signature.");

        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                _settings.WebhookSecret);
        }
        catch (StripeException)
        {
            return BadRequest(
                "Invalid Stripe webhook signature.");
        }

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:

                var session =
                    stripeEvent.Data.Object as Session;

                if (session == null)
                    return BadRequest(
                        "Invalid Checkout Session payload.");

                await HandleCheckoutCompleted(session);

                break;

            case EventTypes.PaymentIntentPaymentFailed:

                var paymentIntent =
                    stripeEvent.Data.Object as PaymentIntent;

                if (paymentIntent == null)
                    return BadRequest(
                        "Invalid PaymentIntent payload.");

                await HandlePaymentFailed(paymentIntent);

                break;
        }

        return Ok();
    }

    private async Task HandleCheckoutCompleted(
        Session session)
    {
        if (session.Metadata == null ||
            !session.Metadata.TryGetValue(
                "referenceNo",
                out var referenceNo))
        {
            return;
        }

        var payment = await _db.Payments
            .FirstOrDefaultAsync(
                p => p.ReferenceNo == referenceNo);

        if (payment == null)
            return;

        // Idempotency:
        // Stripe can send the same webhook more than once.
        if (payment.Status == "Paid")
            return;

        payment.Status = "Paid";

        payment.PaidAt = DateTime.UtcNow;

        payment.StripeSessionId = session.Id;

        payment.StripePaymentIntentId =
            session.PaymentIntentId;

        await _db.SaveChangesAsync();
    }

    private async Task HandlePaymentFailed(
        PaymentIntent paymentIntent)
    {
        // First try to find the payment by PaymentIntent ID.
        var payment = await _db.Payments
            .FirstOrDefaultAsync(
                p => p.StripePaymentIntentId ==
                     paymentIntent.Id);

        if (payment == null)
            return;

        // Don't overwrite a successful payment.
        if (payment.Status == "Paid")
            return;

        payment.Status = "Failed";

        await _db.SaveChangesAsync();
    }

    private static string GenerateReferenceNumber()
    {
        return $"PAY-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }
}