using Test1.Middlewares;
using Test1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IDBService, DBService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();