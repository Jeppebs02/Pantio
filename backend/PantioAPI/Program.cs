using Microsoft.EntityFrameworkCore;
using PantioAPI;
using PantioAPI.EntityFramework;
using PantioAPI.Services;
using PantioClassLibrary.Interfaces.Services;
using PantioClassLibrary.Interfaces.Repository;
using PantioRepository.EntityFramework;
using PantioRepository.EntityFramework.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PantioDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure()
    )
);

builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
builder.Services.AddScoped<IInventoryItemService, InventoryItemService>();
builder.Services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
builder.Services.AddScoped<IExpiryDateRepository, ExpiryDateRepository>();
builder.Services.AddScoped<IExpiryDateService, ExpiryDateService>();
builder.Services.AddScoped<IExpiryNotificationRepository, ExpiryNotificationRepository>();
builder.Services.AddScoped<IExpiryCheckService, ExpiryCheckService>();
builder.Services.Configure<ExpiryCheckOptions>(builder.Configuration.GetSection("ExpiryCheck"));
builder.Services.AddHostedService<ExpiryCheckBackgroundService>();
builder.Services.AddScoped<IProductCacheService, ProductCacheService>();
builder.Services.AddHttpClient<IOpenFoodFactsService, OpenFoodFactsService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OpenFoodFacts:BaseUrl"]!);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Pantio/1.0");
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
