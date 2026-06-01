using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PantioAPI;
using PantioAPI.EntityFramework;
using PantioAPI.Filters;
using PantioAPI.Services;
using PantioClassLibrary.Interfaces;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.EntityFramework;
using PantioRepository;
using PantioRepository.EntityFramework.Repositories;
using PantioRepository.Security;

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

builder.Services.Configure<StoreConnectionTokenEncryptionOptions>(
    builder.Configuration.GetSection(StoreConnectionTokenEncryptionOptions.Section));
builder.Services.AddSingleton(serviceProvider =>
    new StoreConnectionTokenProtector(
        serviceProvider.GetRequiredService<IOptions<StoreConnectionTokenEncryptionOptions>>().Value));
builder.Services.AddHostedService<StoreConnectionTokenMigrationService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
builder.Services.AddScoped<IInventoryItemService, InventoryItemService>();
builder.Services.AddScoped<IStoreConnectionRepository, StoreConnectionRepository>();
builder.Services.AddScoped<IStoreConnectionService, StoreConnectionService>();
builder.Services.Configure<StoreConnectionAutoSyncOptions>(
    builder.Configuration.GetSection(StoreConnectionAutoSyncOptions.Section));
builder.Services.AddHostedService<StoreConnectionAutoSyncBackgroundService>();
builder.Services.AddScoped<IAuth0ManagementService, Auth0ManagementService>();
builder.Services.AddHttpClient<INettoAuthClient, NettoAuthClient>();
builder.Services.AddHttpClient<Auth0ManagementService>();
builder.Services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
builder.Services.AddScoped<IExpiryDateRepository, ExpiryDateRepository>();
builder.Services.AddScoped<IExpiryDateService, ExpiryDateService>();
builder.Services.AddScoped<IExpiryNotificationRepository, ExpiryNotificationRepository>();
builder.Services.AddScoped<IExpiryCheckService, ExpiryCheckService>();
builder.Services.Configure<FcmOptions>(builder.Configuration.GetSection(FcmOptions.Section));
builder.Services.AddHttpClient<IFcmService, FcmService>();
builder.Services.Configure<ExpiryCheckOptions>(builder.Configuration.GetSection("ExpiryCheck"));
builder.Services.AddHostedService<ExpiryCheckBackgroundService>();
builder.Services.AddScoped<IInactiveUserService, InactiveUserService>();
builder.Services.AddHostedService<InactiveUserBackgroundService>();
builder.Services.AddScoped<IProductCacheService, ProductCacheService>();
builder.Services.AddScoped<IProductCacheDbRepository, ProductCacheDbRepository>();
builder.Services.AddScoped<IInventoryItemCacheService, InventoryItemCacheService>();
builder.Services.Configure<OpenFoodFactsOptions>(builder.Configuration.GetSection(OpenFoodFactsOptions.Section));
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
builder.Services.AddScoped<IUnitOfWork, EFUnitOfWork>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IRecipeSuggestionService, RecipeSuggestionService>();
builder.Services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
builder.Services.AddScoped<IShoppingListService, ShoppingListService>();

builder.Services.AddHealthChecks();

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
app.UseExceptionHandler(errorApp =>
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync("{\"status\":500,\"title\":\"Internal Server Error\"}");
    }));
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();
