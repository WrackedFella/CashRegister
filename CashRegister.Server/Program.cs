using CashRegister.Server.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure CashRegister service with configuration
builder.Services.Configure<CashRegisterConfiguration>(
    builder.Configuration.GetSection("CashRegister"));
builder.Services.AddSingleton<CashRegisterService>(sp =>
{
    var config = sp.GetRequiredService<IOptions<CashRegisterConfiguration>>();
    return new CashRegisterService(config.Value);
});

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
