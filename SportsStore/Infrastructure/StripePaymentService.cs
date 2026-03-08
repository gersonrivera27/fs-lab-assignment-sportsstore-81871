using Stripe;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace SportsStore.Infrastructure
{
    public interface IPaymentService
    {
        string? CreatePaymentIntent(decimal amount);
    }

    public class StripePaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;

        public StripePaymentService(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public string? CreatePaymentIntent(decimal amount)
        {
            try
            {
                Log.Information("Initiating Stripe PaymentIntent creation for {Amount} cents ({Currency})", (long)(amount * 100), "usd");

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // Stripe expects amount in cents
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" },
                };

                var service = new PaymentIntentService();
                var paymentIntent = service.Create(options);

                Log.Information("Stripe PaymentIntent {PaymentIntentId} created successfully for {Amount} cents", paymentIntent.Id, paymentIntent.Amount);
                return paymentIntent.ClientSecret;
            }
            catch (StripeException e)
            {
                Log.Error(e, "Stripe API error: {StripeError}, Code: {ErrorCode}", e.Message, e.StripeError?.Code);
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error creating PaymentIntent for amount {Amount}", amount);
                return null;
            }
        }
    }
}
