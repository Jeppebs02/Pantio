using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PantioAPI;
using PantioAPI.EntityFramework;
using PantioAPI.Filters;
using PantioAPI.Services;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.EntityFramework;
using PantioRepository.EntityFramework.Repositories;

var builder = WebApplication.CreateBuilder(args);
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["https://pantio.thisisalegitwebsite.qzz.io", "http://localhost:5173", "http://localhost:3000"];

builder.Services.AddDbContext<PantioDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure()
    )
);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth0:Authority"];
        options.Audience = builder.Configuration["Auth0:Audience"];
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
builder.Services.AddScoped<IInventoryItemService, InventoryItemService>();
builder.Services.AddScoped<IStoreConnectionRepository, StoreConnectionRepository>();
builder.Services.AddScoped<IStoreConnectionService, StoreConnectionService>();
builder.Services.AddScoped<IAuth0ManagementService, Auth0ManagementService>();
builder.Services.AddHttpClient<INettoAuthClient, NettoAuthClient>();
builder.Services.AddHttpClient<Auth0ManagementService>();
builder.Services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
builder.Services.AddScoped<IExpiryDateRepository, ExpiryDateRepository>();
builder.Services.AddScoped<IExpiryDateService, ExpiryDateService>();
builder.Services.AddScoped<IExpiryNotificationRepository, ExpiryNotificationRepository>();
builder.Services.AddScoped<IExpiryCheckService, ExpiryCheckService>();
builder.Services.Configure<ExpiryCheckOptions>(builder.Configuration.GetSection("ExpiryCheck"));
builder.Services.AddHostedService<ExpiryCheckBackgroundService>();
builder.Services.AddScoped<IProductCacheService, ProductCacheService>();
builder.Services.AddScoped<IInventoryItemCacheService, InventoryItemCacheService>();
builder.Services.AddHttpClient<IOpenFoodFactsService, OpenFoodFactsService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OpenFoodFacts:BaseUrl"]!);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Pantio/1.0");
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection(GeminiOptions.Section));
builder.Services.AddHttpClient("Gemini");
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IRecipeSuggestionService, RecipeSuggestionService>();
builder.Services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
builder.Services.AddScoped<IShoppingListService, ShoppingListService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddScoped<Auth0OwnershipFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Auth0OwnershipFilter>();
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PantioDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
