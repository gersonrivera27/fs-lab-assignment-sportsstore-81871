using Microsoft.AspNetCore.Mvc;
using Moq;
using SportsStore.Controllers;
using SportsStore.Models;
using SportsStore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace SportsStore.Tests {

    public class OrderControllerTests {

        [Fact]
        public void Cannot_Checkout_Empty_Cart() {
            // Arrange - create a mock repository
            Mock<IOrderRepository> mockRepo = new Mock<IOrderRepository>();
            Mock<IPaymentService> mockPayment = new Mock<IPaymentService>();
            Mock<IConfiguration> mockConfig = new Mock<IConfiguration>();
            // Arrange - create an empty cart
            Cart cart = new Cart();
            // Arrange - create the order
            Order order = new Order();
            // Arrange - create an instance of the controller
            OrderController target = new OrderController(mockRepo.Object, cart, mockPayment.Object, mockConfig.Object);

            // Act
            ViewResult? result = target.Checkout(order, "") as ViewResult;

            // Assert - check that the order hasn't been stored 
            mockRepo.Verify(m => m.SaveOrder(It.IsAny<Order>()), Times.Never);
            // Assert - check that the method is returning the default view
            Assert.True(string.IsNullOrEmpty(result?.ViewName));
            // Assert - check that I am passing an invalid model to the view
            Assert.False(result?.ViewData.ModelState.IsValid);
        }

        [Fact]
        public void Cannot_Checkout_Invalid_ShippingDetails() {

            // Arrange - create a mock order repository
            Mock<IOrderRepository> mockRepo = new Mock<IOrderRepository>();
            Mock<IPaymentService> mockPayment = new Mock<IPaymentService>();
            Mock<IConfiguration> mockConfig = new Mock<IConfiguration>();
            // Arrange - create a cart with one item
            Cart cart = new Cart();
            cart.AddItem(new Product(), 1);
            // Arrange - create an instance of the controller
            OrderController target = new OrderController(mockRepo.Object, cart, mockPayment.Object, mockConfig.Object);
            // Arrange - add an error to the model
            target.ModelState.AddModelError("error", "error");

            // Act - try to checkout
            ViewResult? result = target.Checkout(new Order(), "pi_123") as ViewResult;

            // Assert - check that the order hasn't been passed stored
            mockRepo.Verify(m => m.SaveOrder(It.IsAny<Order>()), Times.Never);
            // Assert - check that the method is returning the default view
            Assert.True(string.IsNullOrEmpty(result?.ViewName));
            // Assert - check that I am passing an invalid model to the view
            Assert.False(result?.ViewData.ModelState.IsValid);
        }

        [Fact]
        public void Can_Checkout_And_Submit_Order() {
            // Arrange - create a mock order repository
            Mock<IOrderRepository> mockRepo = new Mock<IOrderRepository>();
            Mock<IPaymentService> mockPayment = new Mock<IPaymentService>();
            Mock<IConfiguration> mockConfig = new Mock<IConfiguration>();
            // Arrange - create a cart with one item
            Cart cart = new Cart();
            cart.AddItem(new Product(), 1);
            // Arrange - create an instance of the controller
            OrderController target = new OrderController(mockRepo.Object, cart, mockPayment.Object, mockConfig.Object);

            // Act - try to checkout
            RedirectToPageResult? result =
                    target.Checkout(new Order(), "pi_123") as RedirectToPageResult;

            // Assert - check that the order has been stored
            mockRepo.Verify(m => m.SaveOrder(It.IsAny<Order>()), Times.Once);
            // Assert - check that the method is redirecting to the Completed action
            Assert.Equal("/Completed", result?.PageName);
        }
    }
}
