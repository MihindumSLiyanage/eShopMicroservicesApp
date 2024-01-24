using Play.Common.MassTransit;
using Play.Common.MongoDb;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;
using Polly;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

var MyAllowedOrigins = "_myCORS";

// Add services to the container.
builder.AddMongo()
    .AddMongoRepository<InventoryItem>("inventoryitems")
    .AddMongoRepository<CatalogItem>("catalogitems")
    .AddMassTransitWithRabbitMq();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowedOrigins,
                 policy =>
                 {
                     policy.WithOrigins("http://localhost:3000")
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials();
                 });
});

AddCatalogClients(builder);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(MyAllowedOrigins);

app.UseAuthorization();

app.MapControllers();

app.Run();

static void AddCatalogClients(WebApplicationBuilder builder)
{
    Random jitterer = new Random();

    builder.Services.AddHttpClient<CatalogClient>(client =>
    {
        client.BaseAddress = new Uri("https://localhost:7001");
    })
        .AddTransientHttpErrorPolicy(builderA => builderA.Or<TimeoutRejectedException>().WaitAndRetryAsync(
            5,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)),
            onRetry: (outcome, timespan, retryAttempt) =>
            {
                var serviceProvider = builder.Services.BuildServiceProvider();
                serviceProvider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
            }
            ))
                .AddTransientHttpErrorPolicy(builderB => builderB.Or<TimeoutRejectedException>().CircuitBreakerAsync(
            3,
            TimeSpan.FromSeconds(15),
            onBreak: (outcome, timespan) =>
            {
                var serviceProvider = builder.Services.BuildServiceProvider();
                serviceProvider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Opening the circuit for {timespan.TotalSeconds} seconds...");
            },
            onReset: () =>
            {
                var serviceProvider = builder.Services.BuildServiceProvider();
                serviceProvider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Closing the circuit...");
            }
            ))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
}