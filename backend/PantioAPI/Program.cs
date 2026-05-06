using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
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
