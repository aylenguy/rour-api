using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrbanStore.API.Data;
using UrbanStore.API.Models;

namespace UrbanStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/products
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _db.Products
            .Where(p => p.IsActive)
            .ToListAsync();
        return Ok(products);
    }

    // GET api/products/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null || !product.IsActive)
            return NotFound();
        return Ok(product);
    }

    // POST api/products
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // PUT api/products/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Product updated)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.Name = updated.Name;
        product.Price = updated.Price;
        product.OriginalPrice = updated.OriginalPrice;
        product.Category = updated.Category;
        product.Tag = updated.Tag;
        product.Description = updated.Description;
        product.Images = updated.Images;
        product.Sizes = updated.Sizes;
        product.Colors = updated.Colors;
        product.Details = updated.Details;

        await _db.SaveChangesAsync();
        return Ok(product);
    }

    // DELETE api/products/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.IsActive = false; // soft delete
        await _db.SaveChangesAsync();
        return NoContent();
    }
}