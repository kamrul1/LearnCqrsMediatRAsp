using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApi.Queries
{
    public record GetProductsQuery() : IRequest<IEnumerable<Product>>;

}
