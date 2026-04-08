using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Commands;
using OrderManagement.Api.Queries;
using Shared.Contracts.DTOs;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Gets all orders, optionally filtered by status.</summary>
    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetOrders([FromQuery] string? status = null)
    {
        var result = await _mediator.Send(new GetOrdersQuery(status));
        return Ok(result);
    }

    /// <summary>Gets an order by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Gets the live status of an order including full history.</summary>
    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<Shared.Contracts.DTOs.OrderStatusDto>> GetOrderStatus(Guid id)
    {
        var result = await _mediator.Send(new GetOrderStatusQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Submits a new order from the customer cart.</summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResponseDto>> Checkout([FromBody] CheckoutRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _logger.LogInformation(
            "Checkout request received for customer {CustomerName} (Id={CustomerId}) with {ItemCount} items",
            request.CustomerName, request.CustomerId, request.Items.Count);

        var result = await _mediator.Send(new CheckoutOrderCommand(request));
        return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, result);
    }

    /// <summary>Cancels an order (only if still in early stages).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        var success = await _mediator.Send(new CancelOrderCommand(id));
        if (!success) return BadRequest("Order cannot be cancelled at this stage.");
        return NoContent();
    }
}

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator) => _mediator = mediator;

    /// <summary>Gets all orders for a specific customer.</summary>
    [HttpGet("{customerId}/orders")]
    public async Task<ActionResult<List<OrderDto>>> GetCustomerOrders(string customerId)
    {
        var result = await _mediator.Send(new GetCustomerOrdersQuery(customerId));
        return Ok(result);
    }
}

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Gets all available products.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetProducts()
    {
        var result = await _mediator.Send(new GetProductsQuery());
        return Ok(result);
    }
}

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    /// <summary>Gets summary statistics for the admin dashboard.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var result = await _mediator.Send(new GetDashboardSummaryQuery());
        return Ok(result);
    }

    /// <summary>Gets orders filtered by status for the admin dashboard.</summary>
    [HttpGet("orders/by-status/{status}")]
    public async Task<ActionResult<List<OrderDto>>> GetOrdersByStatus(string status)
    {
        var result = await _mediator.Send(new GetOrdersByStatusQuery(status));
        return Ok(result);
    }
}
