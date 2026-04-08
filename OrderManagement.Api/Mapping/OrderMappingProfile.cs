using AutoMapper;
using OrderManagement.Api.Data.Entities;
using Shared.Contracts.DTOs;

namespace OrderManagement.Api.Mapping;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderDto>()
            .ConstructUsing((src, ctx) => new OrderDto(
                src.OrderId,
                src.CustomerId,
                src.CustomerName,
                src.Status.ToString(),
                src.TotalAmount,
                src.CreatedAt,
                src.UpdatedAt,
                ctx.Mapper.Map<List<OrderItemDto>>(src.Items),
                src.ShipmentRecord != null ? src.ShipmentRecord.ShipmentReference : null,
                src.ShipmentRecord != null ? $"Estimated: {src.ShipmentRecord.EstimatedDispatchDate:dd MMM yyyy}" : null,
                src.FailureReason,
                src.InventoryRecord != null ? ctx.Mapper.Map<InventoryStatusDto>(src.InventoryRecord) : null,
                src.PaymentRecord != null ? ctx.Mapper.Map<PaymentStatusDto>(src.PaymentRecord) : null,
                src.ShipmentRecord != null ? ctx.Mapper.Map<ShipmentStatusDto>(src.ShipmentRecord) : null,
                src.CorrelationId
            ));

        CreateMap<OrderItem, OrderItemDto>()
            .ConstructUsing(src => new OrderItemDto(
                src.ProductId,
                src.ProductName,
                src.Quantity,
                src.UnitPrice,
                src.Quantity * src.UnitPrice
            ));

        CreateMap<Product, ProductDto>()
            .ConstructUsing(src => new ProductDto(
                src.ProductId,
                src.Name,
                src.Description,
                src.Price,
                src.Category,
                src.StockQuantity
            ));

        CreateMap<InventoryRecord, InventoryStatusDto>()
            .ConstructUsing(src => new InventoryStatusDto(
                src.Success,
                src.Success ? "InventoryConfirmed" : "InventoryFailed",
                src.FailureReason,
                src.ProcessedAt
            ));

        CreateMap<PaymentRecord, PaymentStatusDto>()
            .ConstructUsing(src => new PaymentStatusDto(
                src.Approved,
                src.Approved ? "PaymentApproved" : "PaymentRejected",
                src.TransactionId,
                src.RejectionReason,
                src.Amount,
                src.ProcessedAt
            ));

        CreateMap<ShipmentRecord, ShipmentStatusDto>()
            .ConstructUsing(src => new ShipmentStatusDto(
                src.Success,
                src.Success ? "ShippingCreated" : "ShippingFailed",
                src.ShipmentReference,
                src.EstimatedDispatchDate,
                src.FailureReason,
                src.CreatedAt
            ));
    }
}
