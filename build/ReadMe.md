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

## MediatR Notifications

This is useful when we need multiple handlers for a single request. Some use cases
are:
- Sending an email
- Invalidating a cache

To demo feature, add method to be called in the repo by the event
```csharp
public async Task EventOccured(Product product, string evt)
{
    products.SingleOrDefault(p=>p.Id==product.Id).Name = $"{product.Name} evt: {evt}";

    await Task.CompletedTask;
}
```

Create the create a INotification with a single project
```csharp
public record ProductAddedNotification(Product Product) : INotification;
```
Create Notification Handler1
```csharp
public class EmailHandler : INotificationHandler<ProductAddedNotification>
{
    private readonly FakeDataStore _fakeDataStore;

    public EmailHandler(FakeDataStore fakeDataStore) => _fakeDataStore = fakeDataStore;

    public async Task Handle(ProductAddedNotification notification, CancellationToken cancellationToken)
    {
        await _fakeDataStore.EventOccured(notification.Product, "Email sent");
        await Task.CompletedTask;
    }
}

```
Create Notification Handler2
```csharp
public class CacheInvalidationHandler : INotificationHandler<ProductAddedNotification>
{
    private readonly FakeDataStore _fakeDataStore;

    public CacheInvalidationHandler(FakeDataStore fakeDataStore) => _fakeDataStore = fakeDataStore;

    public async Task Handle(ProductAddedNotification notification, CancellationToken cancellationToken)
    {
        await _fakeDataStore.EventOccured(notification.Product, "Cache Invalidated");
        await Task.CompletedTask;
    }
}
```

With these two classes, we create two handlers called EmailHandler and CacheInvalidationHandler that essentially do the same thing:

Implement INotificationHandler\<ProductAddedNotification>, signifying it can handle that event
Call the EventOccured method on FakeDataStore, specifying the event that occurred.

## Trigger the Notifications by Publish

Add extra line in AddProduct method for controller:
```csharp
await mediator.Publish(new ProductAddedNotification(productToReturn));
```
This could have also been done inside the AddProductCommand, instead of the controller

To trigger the notification. Call the AddProduct method on the controller using
the Swagger UI.

Here is an example response:
```js
{
  "id": 7,
  "name": "string by kam evt: Cache Invalidated evt: Email sent"
}
```

# Building MediatR Behaviors
Often applications have cross cutting concerns.  These might include authorization, validating, and logging

Instead of repeating this logic throughout our handlers, we can make use of Behaviors. Behaviors are very 
similar to ASP.NET Core middleware, in that they accept a request, perform some action, then (optionally) 
pass along the request.

## Creating Behaviours

Firstly, we define a behaviour class, such as logging for example:
```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        this.logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        logger.LogInformation($"Handling {typeof(TRequest).Name}");

        var response = await next();

        logger.LogInformation($"Handled {typeof(TResponse).Name}");

        return response;
    }
}
```
What this code does:
> 1. We first define a ===LoggingBehavior=== class, taking two type parameters TRequest and TResponse, and implementing the  ===IPipelineBehavior<TRequest, TResponse>=== interface. Simply put, this behavior can operate on any request.
> 2. We then implement the Handle method, logging before and after we call the ===next()=== delegate.

This logging handler can then be applied to any request, and will log output before and after it is handled.

## register the behavior

Add it in ConfigureServices in Startup:
```csharp
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

Running a web api call to any end point produces the following the console window:
```json
info: WebApi.Behavior.LoggingBehavior[0]
      Handling GetProductsQuery
info: WebApi.Behavior.LoggingBehavior[0]
      Handled IEnumerable`1
```

>This shows the logging output before and after our GetProducts query handler was invoked.