using DropShippingService.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSupplierAdapters();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/inventory-sync", (string supplier, IServiceProvider serviceProvider) =>
{
    var inventorySync = serviceProvider.GetRequiredKeyedService<IInventorySync>(supplier);
});
app.MapGet("/product-sync", (string supplier, IServiceProvider serviceProvider) =>
{
    var productSync = serviceProvider.GetRequiredKeyedService<IProductSync>(supplier);
});
app.MapGet("/shipping-probe", (string supplier, IServiceProvider serviceProvider) =>
{
    var shippingProbe = serviceProvider.GetRequiredKeyedService<IShippingProbe>(supplier);
});

app.Run();