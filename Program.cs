using Microsoft.EntityFrameworkCore;
using SqlWebApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add these services for static files and future expansion
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Enable static file serving from wwwroot
app.UseStaticFiles();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // Creates database if it doesn't exist
}

// User API endpoints
app.MapGet("/api/users", async (AppDbContext db) =>
    { await db.Users.ToListAsync(); });

app.MapPost("/api/users", async (AppDbContext db, User user) =>
{
    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();
    return Results.Created($"/api/users/{user.Id}", user);
});

app.MapDelete("/api/users/{id}", async (AppDbContext db, int id) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound();

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/clear", async (AppDbContext db) =>
{
    // Delete all users
    db.Users.RemoveRange(db.Users);
    await db.SaveChangesAsync();

    // Reset identity counter using EF Core
    await db.Database.ExecuteSqlInterpolatedAsync($"DBCC CHECKIDENT ('Users', RESEED, 0)");

    return Results.Ok("Database cleared and ID counter reset");
});

// HTML endpoint (must come after static files middleware)
app.MapGet("/", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/index.html");
});

app.Run();