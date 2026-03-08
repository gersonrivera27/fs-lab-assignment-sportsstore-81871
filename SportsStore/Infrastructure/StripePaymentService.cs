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
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // Stripe expects amount in cents
                    Currency = "usd",
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    },
                };

                var service = new PaymentIntentService();
                var paymentIntent = service.Create(options);

                return paymentIntent.ClientSecret;
            }
            catch (StripeException e)
            {
                Log.Error(e, "Stripe API error occurred while creating PaymentIntent");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred while creating PaymentIntent");
                return null;
            }
        }
    }
}
