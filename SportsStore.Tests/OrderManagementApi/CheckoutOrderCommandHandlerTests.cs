using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrderManagement.Api.Commands;
using OrderManagement.Api.Data;
using OrderManagement.Api.Data.Entities;
using OrderManagement.Api.Messaging;
using Shared.Contracts.DTOs;
using Shared.Contracts.Enums;
using Xunit;

namespace SportsStore.Tests.OrderManagementApi;

public class CheckoutOrderCommandHandlerTests
{
    private OrderDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var dbContext = new OrderDbContext(options);
        dbContext.Database.EnsureCreated(); // Seeds the products from OnModelCreating
        return dbContext;
    }

    [Fact]
    public async Task Handle_ValidCheckout_CreatesOrderAndPublishesEvents()
    {
        // Arrange
        var db = GetDbContext();
        var publisherMock = new Mock<IMessagePublisher>();
        var loggerMock = new Mock<ILogger<CheckoutOrderCommandHandler>>();
        var handler = new CheckoutOrderCommandHandler(db, publisherMock.Object, loggerMock.Object);

        var request = new CheckoutRequestDto(
            "cust123",
            "John Doe",
            "123 Street",
            new List<CheckoutItemDto>
            {
                new CheckoutItemDto(1, 2) // Product 1 is Kayak ($275)
            }
        );
        var command = new CheckoutOrderCommand(request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.OrderId);
        Assert.Equal(OrderStatus.InventoryPending.ToString(), result.Status);
        
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderId == result.OrderId);
        Assert.NotNull(order);
        Assert.Equal(550m, order.TotalAmount); // 2 * 275
        Assert.Single(order.Items);
        Assert.Equal(1, order.Items.First().ProductId);
        
        // Verify events were published
        publisherMock.Verify(
            p => p.PublishAsync(
                "order.submitted", 
                It.IsAny<string>(), 
                It.IsAny<global::Shared.Contracts.Events.OrderSubmittedEvent>()), 
            Times.Once);
            
        publisherMock.Verify(
            p => p.PublishAsync(
                "inventory.check", 
                It.IsAny<string>(), 
                It.IsAny<global::Shared.Contracts.Events.InventoryCheckRequested>()), 
            Times.Once);
    }
}
