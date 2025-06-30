using Microsoft.AspNetCore.Mvc;
using StrongDatabase.Api.Models;
using StrongDatabase.Api.Services;

namespace StrongDatabase.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly Repository _repository;

        public CustomerController(Repository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Customer>>> GetCustomers()
        {
            var customers = await _repository.GetCustomersAsync();
            return Ok(customers);
        }

        [HttpPost]
        public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
        {
            var createdCustomer = await _repository.AddCustomerAsync(customer);
            return CreatedAtAction(nameof(GetCustomers), new { id = createdCustomer.Id }, createdCustomer);
        }
    }
} 