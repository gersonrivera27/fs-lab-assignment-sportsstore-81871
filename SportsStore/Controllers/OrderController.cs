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
            Log.Information("Checkout page accessed. Cart contains {ItemCount} items with total {CartTotal}", cart.Lines.Count(), cart.ComputeTotalValue());
            return View(new Order());
        }

        [HttpPost]
        [Route("Order/CreatePaymentIntent")]
        public IActionResult CreatePaymentIntent()
        {
            Log.Information("Creating PaymentIntent for {ItemCount} items, total amount: {Amount}", cart.Lines.Count(), cart.ComputeTotalValue());
            var total = cart.ComputeTotalValue();
            var clientSecret = paymentService.CreatePaymentIntent(total);
            
            if (string.IsNullOrEmpty(clientSecret))
            {
                Log.Warning("Failed to create PaymentIntent for amount {Amount}", cart.ComputeTotalValue());
                return BadRequest(new { error = "Failed to create payment intent" });
            }

            Log.Information("PaymentIntent created successfully. ClientSecret obtained for amount {Amount}", cart.ComputeTotalValue());
            return Json(new { clientSecret });
        }

        [HttpPost]
        public IActionResult Checkout(Order order, string? paymentIntentId) {
            if (cart.Lines.Count() == 0) {
                ModelState.AddModelError("", "Sorry, your cart is empty!");
                Log.Warning("Checkout attempted with empty cart");
            }

            if (string.IsNullOrEmpty(paymentIntentId))
            {
                ModelState.AddModelError("", "Payment processing failed. Please try again.");
                Log.Warning("Checkout attempted without PaymentIntentId. Cart has {ItemCount} items", cart.Lines.Count());
            }

            if (ModelState.IsValid) {
                order.PaymentIntentId = paymentIntentId;
                order.Lines = cart.Lines.ToArray();
                repository.SaveOrder(order);
                Log.Information("Order {OrderId} created successfully. PaymentIntentId: {PaymentIntentId}, Items: {ItemCount}, Total: {OrderTotal}, Customer: {CustomerName}", order.OrderID, order.PaymentIntentId, order.Lines.Count, cart.ComputeTotalValue(), order.Name);
                cart.Clear();
                return RedirectToPage("/Completed", new { orderId = order.OrderID });
            } else {
                Log.Warning("Checkout validation failed for customer {CustomerName}. Errors: {Errors}", order.Name, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                ViewBag.StripePublishableKey = configuration["Stripe:PublishableKey"];
                return View(order);
            }
        }
    }
}
