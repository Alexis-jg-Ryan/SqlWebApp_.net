using Microsoft.EntityFrameworkCore;
using SqlWebApp.Models;

var builder = WebApplication.CreateBuilder(args);

// adding services to the container
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// builds app
var app = builder.Build();

// enables html file usage from wwwroot folder
app.UseStaticFiles();

// creating database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // Creates database if it doesn't exist
}

// user API endpoints
app.MapGet("/api/users", async (AppDbContext db) =>
{
    var users = await db.Users.ToListAsync();
    return Results.Json(users); // Explicitly return as JSON
});

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
    db.Users.RemoveRange(db.Users);
    await db.SaveChangesAsync();

    // sql line to reset users and reseed the id's to 0
    await db.Database.ExecuteSqlInterpolatedAsync($"DBCC CHECKIDENT ('Users', RESEED, 0)");

    return Results.Ok("Database cleared and ID counter reset");
});

// HTML endpoint
app.MapGet("/", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/index.html");
});

app.Run();