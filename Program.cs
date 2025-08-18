using Microsoft.EntityFrameworkCore;
using BarnCaseAPI.Data;
using BarnCaseAPI.Services;
using BarnCaseAPI.Workers;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllers();
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // crashes without this
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BarnCase API",
        Version = "v1",
        Description = "BarnCase API documentation"
    });

    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
    c.EnableAnnotations();
});

builder.Services.AddScoped<FarmService>();
builder.Services.AddScoped<AnimalService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductionService>();

builder.Services.AddHostedService<ProductionWorker>(); // automatically starts producing products of animals

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BarnCase API v1");
    c.RoutePrefix = string.Empty;
});

// optional: app.UseHttpsRedirection();

app.MapControllers();

app.Run();  
