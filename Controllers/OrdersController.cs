using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using UrbanStore.API.Data;
using UrbanStore.API.Models;

namespace UrbanStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
    {
        _db = db;
    }

    // ─── DTO de entrada ───────────────────────────────────────────────────────
    // Nunca recibir el modelo de BD directo: el cliente no debe poder
    // mandar Id, Status, CreatedAt ni Total manipulados.

    public record OrderItemDto(
        [Required] int ProductId,
        [Range(1, 100)] int Quantity,
        [Required] string Size,
        [Required] string Color
    );

    public record CreateOrderDto(
        [Required][StringLength(100, MinimumLength = 3)] string CustomerName,
        [Required][EmailAddress] string CustomerEmail,
        [Required][StringLength(200, MinimumLength = 5)] string Address,
        [Required][StringLength(100, MinimumLength = 2)] string City,
        [Required][StringLength(100, MinimumLength = 2)] string Province,
        [StringLength(8)] string? Zip,
        [Required] string PaymentMethod,
        [Required][MinLength(1)] List<OrderItemDto> Items
    );

    // ─── POST api/orders ──────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        // 1. Data Annotations ya validaron campos vacíos y formatos básicos.
        //    ModelState.IsValid lo captura automáticamente ASP.NET Core,
        //    pero lo dejamos explícito para mayor claridad.
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // 2. Validar método de pago contra lista blanca
        var validPaymentMethods = new[] { "mp", "card", "transfer" };
        if (!validPaymentMethods.Contains(dto.PaymentMethod))
            return BadRequest(new { message = "Método de pago no válido." });

        // 3. Recalcular el total desde la base de datos
        //    NUNCA confiar en el precio que manda el cliente.
        decimal recalculatedTotal = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in dto.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);

            if (product == null)
                return BadRequest(new { message = $"El producto con ID {item.ProductId} no existe." });

            // Opcional: validar stock si tu modelo lo tiene
            // if (product.Stock < item.Quantity)
            //     return BadRequest(new { message = $"Stock insuficiente para {product.Name}." });

            recalculatedTotal += product.Price * item.Quantity;

            orderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = product.Name,
                Price = product.Price,   // precio real, no el del cliente
                Size = item.Size,
                Color = item.Color,
                Quantity = item.Quantity,
            });
        }

        // 4. Calcular envío en el servidor (misma lógica que el front)
        const decimal freeShippingThreshold = 50000;
        const decimal shippingCost = 2500;
        var shipping = recalculatedTotal >= freeShippingThreshold ? 0 : shippingCost;

        // 5. Crear la orden con datos limpios
        var order = new Order
        {
            CustomerName = dto.CustomerName.Trim(),
            CustomerEmail = dto.CustomerEmail.Trim().ToLowerInvariant(),
            Address = dto.Address.Trim(),
            City = dto.City.Trim(),
            Province = dto.Province.Trim(),
            Zip = dto.Zip?.Trim(),
            PaymentMethod = dto.PaymentMethod,
            Total = recalculatedTotal + shipping,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            Items = orderItems,
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, new { order.Id, order.Total, order.Status });
    }

    // ─── GET api/orders/5 ─────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return Ok(order);
    }

    // ─── GET api/orders (admin) ───────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders);
    }

    // ─── PATCH api/orders/5/status ────────────────────────────────────────────
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var validStatuses = new[] { "pending", "confirmed", "shipped", "delivered", "cancelled" };
        if (!validStatuses.Contains(status))
            return BadRequest(new { message = "Estado no válido." });

        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        await _db.SaveChangesAsync();
        return Ok(order);
    }

    // ─── DELETE api/orders/5 ─────────────────────────────────────────────────
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
