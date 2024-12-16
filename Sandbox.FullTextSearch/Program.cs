using Microsoft.EntityFrameworkCore;
using Sandbox.FullTextSearch;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContextFactory<AppDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("Context")));

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

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    using (var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>())
    {
        dbContext.Database.Migrate();
    }
}

app.Run();