using Microsoft.EntityFrameworkCore;
using SqlWebApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // Creates database if it doesn't exist
}

// Configure the HTTP request pipeline
app.MapGet("/", () => "Hello from SQL Web App!");

app.MapGet("/users", async (AppDbContext db) =>
    await db.Users.ToListAsync());

app.MapPost("/users", async (AppDbContext db, User user) =>
{
    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", user);
});

app.Run();