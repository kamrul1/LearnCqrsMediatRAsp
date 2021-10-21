using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApi.DataStore
{
    public class FakeDataStore
    {
        private static List<Product> products;

        public FakeDataStore()
        {
            products = new List<Product>
        {
            new Product { Id = 1, Name = "Test Product 1" },
            new Product { Id = 2, Name = "Test Product 2" },
            new Product { Id = 3, Name = "Test Product 3" }
        };
        }

        public async Task<Product> GetProductById(int id)
        {
            return await Task.FromResult(products.FirstOrDefault(x => x.Id == id));
        }

        public async Task<IEnumerable<Product>> GetAllProducts() => await Task.FromResult(products);

        public async Task AddProduct(Product product)
        {
            products.Add(product);
            await Task.CompletedTask;
        }

        public async Task EventOccured(Product product, string evt)
        {
            products.SingleOrDefault(p=>p.Id==product.Id).Name = $"{product.Name} evt: {evt}";

            await Task.CompletedTask;
        }


    }
}
