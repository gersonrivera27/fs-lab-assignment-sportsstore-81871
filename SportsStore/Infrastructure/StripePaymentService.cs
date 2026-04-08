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
        }

        public string? CreatePaymentIntent(decimal amount)
        {
            Log.Information("Stubbed payment intent creation for {Amount}", amount);
            return "pi_stub_" + Guid.NewGuid().ToString("N");
        }
    }
}
