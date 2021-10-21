using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApi.Commands;
using WebApi.Queries;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator mediator;

        public ProductsController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult> GetProducts()
        {
            var products = await mediator.Send(new GetProductsQuery());

            return Ok(products);
        }

        [HttpGet("{id}",Name = nameof(GetProductById))]
        public async Task<ActionResult> GetProductById(int id)
        {
            var product =  await mediator.Send(new GetProductByIdQuery(id));
            return Ok(product);
        }


        [HttpPost]
        public async Task<ActionResult> AddProduct([FromBody] Product product)
        {
            var productToReturn = await mediator.Send(new AddProductCommand(product));

            var routeValues = new { id = productToReturn.Id};

            return CreatedAtRoute(nameof(GetProductById), routeValues, productToReturn);
        }
    }
}
