using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportsStore.Infrastructure;
using SportsStore.Models;
using Serilog;

namespace SportsStore.Pages {

    public class CartModel : PageModel {
        private IStoreRepository repository;

        public CartModel(IStoreRepository repo, Cart cartService) {
            repository = repo;
            Cart = cartService;
        }

        public Cart Cart { get; set; }
        public string ReturnUrl { get; set; } = "/";

        public void OnGet(string returnUrl) {
            ReturnUrl = returnUrl ?? "/";
            Log.Information("Cart page viewed. Items in cart: {CartItemCount}, Cart total: {CartTotal}",
                Cart.Lines.Count, Cart.ComputeTotalValue());
        }

        public IActionResult OnPost(long productId, string returnUrl) {
            Product? product = repository.Products
                .FirstOrDefault(p => p.ProductID == productId);
            if (product != null) {
                Cart.AddItem(product, 1);
                Log.Information("Product added to cart. ProductId: {ProductId}, ProductName: {ProductName}, Price: {ProductPrice}, Cart items: {CartItemCount}, Cart total: {CartTotal}",
                    product.ProductID, product.Name, product.Price, Cart.Lines.Count, Cart.ComputeTotalValue());
            } else {
                Log.Warning("Attempted to add non-existent product to cart. ProductId: {ProductId}", productId);
            }
            return RedirectToPage(new { returnUrl = returnUrl });
        }

        public IActionResult OnPostRemove(long productId, string returnUrl) {
            var line = Cart.Lines.First(cl => cl.Product.ProductID == productId);
            Log.Information("Product removed from cart. ProductId: {ProductId}, ProductName: {ProductName}, Quantity removed: {Quantity}, Cart items: {CartItemCount}",
                line.Product.ProductID, line.Product.Name, line.Quantity, Cart.Lines.Count - 1);
            Cart.RemoveLine(line.Product);
            return RedirectToPage(new { returnUrl = returnUrl });
        }
    }
}
