using MediatR;
using AutoMapper;
using OrderManagement.Api.Data;
using Shared.Contracts.DTOs;
using Shared.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace OrderManagement.Api.Queries;

// ─── Get All Orders ───────────────────────────────────────────────────────────

public record GetOrdersQuery(string? StatusFilter = null) : IRequest<List<OrderDto>>;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetOrdersQueryHandler(OrderDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var query = _db.Orders
            .Include(o => o.Items)
            .Include(o => o.InventoryRecord)
            .Include(o => o.PaymentRecord)
            .Include(o => o.ShipmentRecord)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.StatusFilter) &&
            Enum.TryParse<OrderStatus>(request.StatusFilter, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
        return _mapper.Map<List<OrderDto>>(orders);
    }
}

// ─── Get Order By Id ──────────────────────────────────────────────────────────

public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto?>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetOrderByIdQueryHandler(OrderDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.InventoryRecord)
            .Include(o => o.PaymentRecord)
            .Include(o => o.ShipmentRecord)
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, ct);

        return order == null ? null : _mapper.Map<OrderDto>(order);
    }
}

// ─── Get Order Status ─────────────────────────────────────────────────────────

public record GetOrderStatusQuery(Guid OrderId) : IRequest<OrderStatusDto?>;

public class GetOrderStatusQueryHandler : IRequestHandler<GetOrderStatusQuery, OrderStatusDto?>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetOrderStatusQueryHandler(OrderDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<OrderStatusDto?> Handle(GetOrderStatusQuery request, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.ShipmentRecord)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, ct);

        if (order == null) return null;

        return new OrderStatusDto(
            order.OrderId,
            order.Status.ToString(),
            order.UpdatedAt ?? order.CreatedAt,
            order.ShipmentRecord?.ShipmentReference,
            order.FailureReason,
            order.StatusHistory
                .OrderBy(h => h.Timestamp)
                .Select(h => new OrderStatusHistoryDto(h.Status, h.Timestamp, h.Notes))
                .ToList()
        );
    }
}

// ─── Get Customer Orders ──────────────────────────────────────────────────────

public record GetCustomerOrdersQuery(string CustomerId) : IRequest<List<OrderDto>>;

public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, List<OrderDto>>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetCustomerOrdersQueryHandler(OrderDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<List<OrderDto>> Handle(GetCustomerOrdersQuery request, CancellationToken ct)
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.InventoryRecord)
            .Include(o => o.PaymentRecord)
            .Include(o => o.ShipmentRecord)
            .Where(o => o.CustomerId == request.CustomerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return _mapper.Map<List<OrderDto>>(orders);
    }
}

// ─── Get Orders By Status ─────────────────────────────────────────────────────

public record GetOrdersByStatusQuery(string Status) : IRequest<List<OrderDto>>;

public class GetOrdersByStatusQueryHandler : IRequestHandler<GetOrdersByStatusQuery, List<OrderDto>>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetOrdersByStatusQueryHandler(OrderDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<List<OrderDto>> Handle(GetOrdersByStatusQuery request, CancellationToken ct)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, out var status))
            return new List<OrderDto>();

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.InventoryRecord)
            .Include(o => o.PaymentRecord)
            .Include(o => o.ShipmentRecord)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return _mapper.Map<List<OrderDto>>(orders);
    }
}

// ─── Get Dashboard Summary ────────────────────────────────────────────────────

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly OrderDbContext _db;

    public GetDashboardSummaryQueryHandler(OrderDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
    {
        var orders = await _db.Orders.ToListAsync(ct);

        var byStatus = orders
            .GroupBy(o => o.Status.ToString())
            .Select(g => new OrdersByStatusDto(g.Key, g.Count()))
            .ToList();

        var completedRevenue = orders
            .Where(o => o.Status == OrderStatus.Completed)
            .Sum(o => o.TotalAmount);

        return new DashboardSummaryDto(
            orders.Count,
            orders.Count(o => o.Status is OrderStatus.InventoryPending or OrderStatus.PaymentPending or OrderStatus.ShippingPending),
            orders.Count(o => o.Status == OrderStatus.Completed),
            orders.Count(o => o.Status == OrderStatus.Failed),
            completedRevenue,
            byStatus
        );
    }
}

// ─── Get All Products ─────────────────────────────────────────────────────────

public record GetProductsQuery : IRequest<List<ProductDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(OrderDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var products = await _db.Products.ToListAsync(ct);
        return _mapper.Map<List<ProductDto>>(products);
    }
}
