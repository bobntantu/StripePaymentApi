# Stripe Payment API

A simple ASP.NET Core Web API that demonstrates how to integrate Stripe Checkout with a backend application.

I built this project as a standalone payment integration example. The main goal is to show how a backend can create a Stripe Checkout Session, store the payment locally, and then use a Stripe webhook to confirm the payment.

The project does not include a custom frontend. Stripe hosts the checkout page, which keeps the example focused on the backend payment flow.

## What the project does

The API follows this basic process:

1. A client sends a payment request to the API.
2. The API creates a payment record with a `Pending` status.
3. The API creates a Stripe Checkout Session.
4. Stripe returns a hosted Checkout URL.
5. The customer completes the payment on Stripe.
6. Stripe sends a webhook to the API.
7. The API verifies the webhook signature.
8. The payment is found using the reference number.
9. The payment status is changed from `Pending` to `Paid`.
10. The Stripe Payment Intent ID and payment date are stored.

The important part is that the application does **not** rely on the browser's success page to decide whether a payment was completed.

The webhook is the source of truth for confirming the payment.

## Technologies

* .NET 8
* ASP.NET Core Web API
* C#
* Stripe.net
* Entity Framework Core
* SQLite
* Swagger / OpenAPI
* Stripe CLI

## Project structure

```text
StripePaymentApi/
│
├── Controllers/
│   └── StripeController.cs
│
├── Models/
│   ├── Payment.cs
│   └── PaymentRequest.cs
│
├── Data/
│   └── PaymentDbContext.cs
│
├── Configuration/
│   └── StripeSettings.cs
│
├── Services/
│   └── StripePaymentService.cs
│
├── Migrations/
│
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── StripePaymentApi.csproj
```

## API endpoints

### Create Checkout Session

```http
POST /api/stripe/create-checkout
```

Example request:

```json
{
  "amount": 49.99,
  "currency": "usd",
  "description": "Stripe Payment API Demo",
  "customerEmail": "test@example.com",
  "referenceNo": ""
}
```

The API creates the payment locally and returns a Stripe-hosted Checkout URL.

Example response:

```json
{
  "referenceNo": "PAY-20261006153012345",
  "sessionId": "cs_test_...",
  "checkoutUrl": "https://checkout.stripe.com/..."
}
```

The `checkoutUrl` can be opened directly in a browser.

There is no custom checkout page in this project because the purpose of the example is to demonstrate the backend Stripe integration.

## Webhook

```http
POST /api/stripe/webhook
```

The webhook handles Stripe events and verifies the Stripe signature before processing them.

The project currently listens for:

```text
checkout.session.completed
payment_intent.payment_failed
```

When a checkout session is successfully completed, the API changes the corresponding payment from:

```text
Pending
```

to:

```text
Paid
```

and stores:

* Stripe Checkout Session ID
* Stripe Payment Intent ID
* Payment date

## Configuration

Stripe credentials should not be stored directly in `appsettings.json`.

The project uses ASP.NET Core User Secrets during development.

The configuration is:

```text
Stripe:PublishableKey
Stripe:SecretKey
Stripe:WebhookSecret
```

For example:

```powershell
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
```

Do not commit real Stripe keys or webhook secrets to GitHub.

## Database

The project uses SQLite for simplicity.

The connection string is:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=payments.db"
}
```

The `Payment` table stores the local payment information.

Important fields include:

```text
ReferenceNo
Amount
Currency
CustomerEmail
PaymentProvider
Status
StripeSessionId
StripePaymentIntentId
CreatedAt
PaidAt
```

## Running the project

Clone the repository and open the project directory:

```powershell
cd StripePaymentApi
```

Restore the dependencies:

```powershell
dotnet restore
```

Apply the database migration:

```powershell
dotnet ef database update
```

Configure your Stripe test secret:

```powershell
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
```

Run the API:

```powershell
dotnet run
```

Swagger should then be available at the HTTPS URL shown by ASP.NET Core, for example:

```text
https://localhost:7278/swagger
```

## Testing the webhook locally

Stripe CLI can forward Stripe events to the local API.

First authenticate Stripe CLI:

```powershell
stripe login
```

Then start the webhook listener:

```powershell
stripe listen --events checkout.session.completed,payment_intent.payment_failed --forward-to https://localhost:7278/api/stripe/webhook
```

Stripe CLI will display a webhook signing secret:

```text
Ready! Your webhook signing secret is whsec_...
```

Save that value as a User Secret:

```powershell
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
```

Keep the Stripe CLI terminal running while testing.

## Testing a payment

Open Swagger:

```text
https://localhost:7278/swagger
```

Call:

```text
POST /api/stripe/create-checkout
```

with:

```json
{
  "amount": 49.99,
  "currency": "usd",
  "description": "Stripe Payment API Demo",
  "customerEmail": "test@example.com",
  "referenceNo": ""
}
```

Copy the `checkoutUrl` returned by the API and open it in a browser.

For Stripe test mode, a common successful test card is:

```text
4242 4242 4242 4242
```

Use any valid future expiration date and test CVC.

After the payment, Stripe sends:

```text
checkout.session.completed
```

to the local webhook.

The Stripe CLI terminal should show the request being forwarded to:

```text
POST /api/stripe/webhook
```

with a successful response.

The payment should then be updated in SQLite:

```text
Status = Paid
```

## Why use a webhook?

A payment success page is useful for sending the customer back to an application, but it should not be treated as the final confirmation of payment.

For example, a customer could close the browser before returning to the application.

The webhook provides a server-to-server notification from Stripe. The API can verify the Stripe signature and update its own database independently of what happens in the customer's browser.

For this reason, this project uses the following approach:

```text
Checkout
   ↓
Stripe processes payment
   ↓
Stripe webhook
   ↓
Verify signature
   ↓
Update database
```

rather than:

```text
Checkout
   ↓
Customer returns to success page
   ↓
Assume payment was successful
```

## Error handling

The API validates basic payment information before creating a Checkout Session.

If Stripe cannot create the Checkout Session, the local payment is marked as:

```text
Failed
```

Webhook requests with an invalid or missing Stripe signature are rejected.

The webhook handler also checks the payment status before updating it. This helps prevent the same successful webhook from changing the payment repeatedly.

## Security

This project is intended for demonstration and learning purposes.

For a production implementation, additional measures would normally be added, including:

* stronger request validation
* idempotency handling
* structured logging
* authentication and authorization
* database constraints
* more detailed payment state management
* production webhook configuration
* monitoring and alerting
* secret management through a dedicated secrets manager

Most importantly, Stripe secret keys and webhook signing secrets should never be committed to source control.

## Purpose of the project

This project is part of my backend development portfolio.

The purpose is not to build a complete e-commerce application. Instead, it demonstrates a practical payment integration using technologies commonly used in .NET backend applications.

The example covers several areas that are important in real applications:

* REST API development
* payment processing
* external API integration
* database persistence
* webhook processing
* signature verification
* asynchronous programming
* error handling
* secure configuration
* Entity Framework Core

## Author

**Bob Ntantu**

Software Engineer | .NET | C# | ASP.NET Core | SQL Server | AI & Computer Vision

This project is an independent technical demonstration created for learning, portfolio development, and demonstrating practical backend integration skills.
