using Play.Catalog.Service.Entities;
using Play.Common.MassTransit;
using Play.Common.MongoDb;
using Play.Common.Settings;

var builder = WebApplication.CreateBuilder(args);

var MyAllowedOrigins = "_myCORS";

ServiceSettings serviceSettings;

var configuration = builder.Configuration;

// Add services to the container.
serviceSettings = configuration.GetSection("ServiceSettings").Get<ServiceSettings>(); // "Catalog" db

builder.AddMongo()
    .AddMongoRepository<Item>("items")
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

//builder.Services.AddMassTransitHostedService();

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});

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
