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
            var cartItems = cart.Lines.Select(l => new { l.Product.ProductID, l.Product.Name, l.Quantity, Subtotal = l.Product.Price * l.Quantity });
            Log.Information("Checkout page accessed. Cart contains {ItemCount} items with total {CartTotal}. Items: {@CartItems}",
                cart.Lines.Count(), cart.ComputeTotalValue(), cartItems);
            return View(new Order());
        }

        [HttpPost]
        [Route("Order/CreatePaymentIntent")]
        public IActionResult CreatePaymentIntent()
        {
            var total = cart.ComputeTotalValue();
            var cartItems = cart.Lines.Select(l => new { l.Product.ProductID, l.Product.Name, l.Quantity });
            Log.Information("Creating PaymentIntent for {ItemCount} items, total amount: {Amount}. Products: {@CartItems}",
                cart.Lines.Count(), total, cartItems);

            var clientSecret = paymentService.CreatePaymentIntent(total);

            if (string.IsNullOrEmpty(clientSecret))
            {
                Log.Error("Failed to create PaymentIntent for amount {Amount}. Stripe returned no client secret", total);
                return BadRequest(new { error = "Failed to create payment intent. Please try again." });
            }

            Log.Information("PaymentIntent created successfully for amount {Amount}", total);
            return Json(new { clientSecret });
        }

        [HttpPost]
        [Route("Order/PaymentFailed")]
        public IActionResult PaymentFailed([FromBody] PaymentFailedRequest request)
        {
            Log.Error("Stripe payment FAILED. Error: {StripeError}, PaymentIntentId: {PaymentIntentId}, DeclineCode: {DeclineCode}, Cart total: {CartTotal}, Customer: {CustomerName}",
                request.ErrorMessage, request.PaymentIntentId, request.DeclineCode, cart.ComputeTotalValue(), request.CustomerName);
            return Ok();
        }

        [HttpPost]
        [Route("Order/PaymentCancelled")]
        public IActionResult PaymentCancelled([FromBody] PaymentCancelledRequest request)
        {
            Log.Warning("Stripe payment CANCELLED by user. PaymentIntentId: {PaymentIntentId}, Cart total: {CartTotal}, Customer: {CustomerName}",
                request.PaymentIntentId, cart.ComputeTotalValue(), request.CustomerName);
            return Ok();
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
                Log.Information("Order {OrderId} created successfully. PaymentIntentId: {PaymentIntentId}, Items: {ItemCount}, Total: {OrderTotal}, Customer: {CustomerName}",
                    order.OrderID, order.PaymentIntentId, order.Lines.Count, cart.ComputeTotalValue(), order.Name);
                cart.Clear();
                Log.Information("Cart cleared after successful order {OrderId}", order.OrderID);
                return RedirectToPage("/Completed", new { orderId = order.OrderID });
            } else {
                Log.Warning("Checkout validation failed for customer {CustomerName}. Errors: {Errors}",
                    order.Name, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                ViewBag.StripePublishableKey = configuration["Stripe:PublishableKey"];
                return View(order);
            }
        }
    }

    public class PaymentFailedRequest
    {
        public string? ErrorMessage { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? DeclineCode { get; set; }
        public string? CustomerName { get; set; }
    }

    public class PaymentCancelledRequest
    {
        public string? PaymentIntentId { get; set; }
        public string? CustomerName { get; set; }
    }
}
