using StrongDatabase.Api.Data;
using StrongDatabase.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongDatabase.Api.Services;

namespace StrongDatabase.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompraController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Repository _repo;
        public CompraController(AppDbContext context, Repository repo)
        {
            _context = context;
            _repo = repo;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Compra>>> GetAll()
        {
            return await _repo.GetComprasAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Compra>> GetById(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.Cliente)
                .Include(c => c.Produto)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (compra == null) return NotFound();
            return compra;
        }

        [HttpPost]
        public async Task<ActionResult<Compra>> Create(Compra compra)
        {
            var result = await _repo.AddCompraAsync(compra);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Compra compra)
        {
            if (id != compra.Id) return BadRequest();
            _context.Entry(compra).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var compra = await _context.Compras.FindAsync(id);
            if (compra == null) return NotFound();
            _context.Compras.Remove(compra);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
} 