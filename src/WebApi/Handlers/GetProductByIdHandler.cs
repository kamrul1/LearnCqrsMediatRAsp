using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebApi.DataStore;
using WebApi.Queries;

namespace WebApi.Handlers
{
    public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Product>
    {
        private readonly FakeDataStore fakeDataStore;

        public GetProductByIdHandler(FakeDataStore fakeDataStore)
        {
            this.fakeDataStore = fakeDataStore;
        }
        public Task<Product> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            return fakeDataStore.GetProductById(request.Id);
        }
    }
}
