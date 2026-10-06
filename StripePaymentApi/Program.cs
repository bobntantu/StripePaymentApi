
using Microsoft.EntityFrameworkCore;
using StripePaymentApi.Configuration;
using StripePaymentApi.Data;
using StripePaymentApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Stripe configuration
builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));

// Stripe payment service
builder.Services.AddScoped<StripePaymentService>();

var app = builder.Build();

// Development tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

