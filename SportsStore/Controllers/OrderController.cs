using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;
using SportsStore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace SportsStore.Controllers {

    public class OrderController : Controller {
        private IOrderRepository repository;
        private Cart cart;
        private readonly IPaymentService paymentService;
        private readonly IConfiguration configuration;

        public OrderController(IOrderRepository repoService, Cart cartService, IPaymentService payService, IConfiguration config) {
            repository = repoService;
            cart = cartService;
            paymentService = payService;
            configuration = config;
        }

        public ViewResult Checkout()
        {
            ViewBag.StripePublishableKey = configuration["Stripe:PublishableKey"];
            Log.Information("Stripe Publishable Key configured: {KeyPresent}", !string.IsNullOrEmpty(configuration["Stripe:PublishableKey"]));
            return View(new Order());
        }

        [HttpPost]
        [Route("Order/CreatePaymentIntent")]
        public IActionResult CreatePaymentIntent()
        {
            var total = cart.ComputeTotalValue();
            var clientSecret = paymentService.CreatePaymentIntent(total);
            
            if (string.IsNullOrEmpty(clientSecret))
            {
                return BadRequest(new { error = "Failed to create payment intent" });
            }

            return Json(new { clientSecret });
        }

        [HttpPost]
        public IActionResult Checkout(Order order, string paymentIntentId) {
            if (cart.Lines.Count() == 0) {
                ModelState.AddModelError("", "Sorry, your cart is empty!");
            }

            if (string.IsNullOrEmpty(paymentIntentId))
            {
                ModelState.AddModelError("", "Payment processing failed. Please try again.");
                Log.Warning("Checkout failed: Missing PaymentIntentId");
            }

            if (ModelState.IsValid) {
                order.PaymentIntentId = paymentIntentId;
                order.Lines = cart.Lines.ToArray();
                repository.SaveOrder(order);
                cart.Clear();
                Log.Information("Order {OrderId} successfully created with PaymentIntent {PaymentIntentId}", order.OrderID, order.PaymentIntentId);
                return RedirectToPage("/Completed", new { orderId = order.OrderID });
            } else {
                ViewBag.StripePublishableKey = configuration["Stripe:PublishableKey"];
                return View(order);
            }
        }
    }
}
