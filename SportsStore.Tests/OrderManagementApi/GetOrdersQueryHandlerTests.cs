using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Data;
using OrderManagement.Api.Data.Entities;
using OrderManagement.Api.Queries;
using Shared.Contracts.DTOs;
using Shared.Contracts.Enums;
using Xunit;

namespace SportsStore.Tests.OrderManagementApi;

public class GetOrdersQueryHandlerTests
{
    private OrderDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var dbContext = new OrderDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    [Fact]
    public async Task Handle_ReturnsAllOrders()
    {
        // Arrange
        var db = GetDbContext();
        
        db.Orders.Add(new Order { OrderId = Guid.NewGuid(), CustomerName = "Alice", Status = OrderStatus.Completed });
        db.Orders.Add(new Order { OrderId = Guid.NewGuid(), CustomerName = "Bob", Status = OrderStatus.Failed });
        await db.SaveChangesAsync();

        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<OrderManagement.Api.Mapping.OrderMappingProfile>();
        });
        var mapper = config.CreateMapper();

        var handler = new GetOrdersQueryHandler(db, mapper);

        // Act
        var result = await handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}
