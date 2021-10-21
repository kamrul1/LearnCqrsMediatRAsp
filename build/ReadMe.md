# Markdown File

Following example course:
 
https://code-maze.com/cqrs-mediatr-in-aspnet-core/


## Gotcha about Folders
A small concequence of the way visual studio manages folders is the folder view
has to be used to move project to src folder.  Remove from solution and then add
existing project to get it working again.


# In Startup add mediatR and repo dependency

- services.AddMediatR(typeof(Startup));
- services.AddSingleton\<FakeDataStore>();

## Requests with MediatR

There are two types of requests in MediatR. One that returns a value, 
and one that doesn’t. Often this corresponds to reads/queries (returning a value) and writes/commands (usually doesn’t return a value).

First inject MediatR via the constructor and use it to call the query

### Queries and Handlers

Create Query:
```csharp
public record GetProductsQuery() : IRequest<IEnumerable<Product>>;
```

Create Query Handler:
```csharp
public class GetProductsHandler : IRequestHandler<GetProductsQuery, IEnumerable<Product>>
{
    private readonly FakeDataStore _fakeDataStore;

    public GetProductsHandler(FakeDataStore fakeDataStore) => _fakeDataStore = fakeDataStore;

    public async Task<IEnumerable<Product>> Handle(GetProductsQuery request,
        CancellationToken cancellationToken) => await _fakeDataStore.GetAllProducts();

}
```

Test with Swagger UI for the project:
https://localhost:5001/swagger/index.html


 ### MediatR Commands that don't return a value

Create a Command:
```csharp
public record AddProductCommand(Product Product) : IRequest;
```
Create a Command Handler:
```csharp
public class AddProductHandler : IRequestHandler<AddProductCommand, Unit>
{
    private readonly FakeDataStore _fakeDataStore;

    public AddProductHandler(FakeDataStore fakeDataStore) => _fakeDataStore = fakeDataStore;

    public async Task<Unit> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        await _fakeDataStore.AddProduct(request.Product);
            
        return Unit.Value;
    }
}
```
When using MediatR, instead of void, we use the 
Unit struct that represents a void type.



Test with Swagger UI for the project:
https://localhost:5001/swagger/index.html

### MediatR Commands that return a value

Modify the add command to use to return a product:
```csharp
public record AddProductCommand(Product Product) : IRequest<Product>;
```

Modify the add command handler:

```csharp
public class AddProductHandler : IRequestHandler<AddProductCommand, Product>
{
    private readonly FakeDataStore fakeDataStore;

    public AddProductHandler(FakeDataStore fakeDataStore)
    {
        this.fakeDataStore = fakeDataStore;
    }

    public async Task<Product> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        await fakeDataStore.AddProduct(request.Product);
        return request.Product;
    }
}
```


## Gotacha CreatedAtRoute

The HttpGet need to have the nameof attribute in the GetProductById: 
```csharp
[HttpGet("{id}",Name = nameof(GetProductById))]
public async Task<ActionResult> GetProductById(int id)
{
    var product =  await mediator.Send(new GetProductByIdQuery(id));
    return Ok(product);
}
```

Otherwise the CreatedAtRoute returns error no route matched:
```csharp
[HttpPost]
public async Task<ActionResult> AddProduct([FromBody] Product product)
{
    var productToReturn = await mediator.Send(new AddProductCommand(product));

    var routeValues = new { id = productToReturn.Id};

    return CreatedAtRoute(nameof(GetProductById), routeValues, productToReturn);
}

```

The ***CreatedAtRoute** adds a location parameter to the response header, so the route
of the created object is available.
```csharp
location: https://localhost:5001/api/Products/6 
```


