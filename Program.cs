using Microsoft.EntityFrameworkCore;
using BarnCaseAPI.Data;
using BarnCaseAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));


var app = builder.Build();

// app.UseHttpsRedirection();

app.MapGet("/", () => "API is alive");
app.MapGet("/users", (AppDbContext db) => db.Users.ToList());

app.MapPost("/users", async (AppDbContext db, User user) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", user);
});

app.MapGet("/users/{id:int}", async (AppDbContext db, int id) =>
{
    var u = await db.Users.FindAsync(id);
    return u is null ? Results.NotFound() : Results.Ok(u);
});

app.Run();
