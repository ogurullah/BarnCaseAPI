using Microsoft.EntityFrameworkCore;
using BarnCaseAPI.Data;
using BarnCaseAPI.Services;
using BarnCaseAPI.Workers;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;
using Serilog;
using BarnCaseAPI.Logging;
using Serilog.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using BarnCaseAPI.Options;
using BarnCaseAPI.Security;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

//builder.Services.AddControllers();
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // crashes without this
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection.GetValue<string>("Issuer") ?? throw new InvalidOperationException("JWT Issuer is not configured.");
var audience = jwtSection.GetValue<string>("Audience") ?? throw new InvalidOperationException("JWT Audience is not configured.");
var keyB64 = jwtSection.GetValue<string>("SigningKeyB64");
if (string.IsNullOrWhiteSpace(keyB64))
{
    throw new InvalidOperationException("JWT SigningKeyB64 is not configured.");
}
var signingKey = new SymmetricSecurityKey(Convert.FromBase64String(keyB64));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = signingKey,

            ClockSkew = TimeSpan.FromSeconds(15)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.NoResult();
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, AdminOrSelfHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, AdminOrOwnerHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("AdminOrSelf", p => p.AddRequirements(new AdminOrSelfRequirement()));
    options.AddPolicy("AdminOrOwner", p => p.AddRequirements(new AdminOrOwnerRequirement()));
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

    c.CustomSchemaIds(t => t.FullName);

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

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<FarmService>();
builder.Services.AddScoped<AnimalService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductionService>();

builder.Services.AddHostedService<ProductionWorker>(); // automatically starts producing products of animals

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (context, elapsed, exemption) => LogEventLevel.Information;
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} -> {StatusCode} in {Elapsed:0.0000} ms";
});

app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BarnCase API v1");
    c.RoutePrefix = string.Empty;
});

app.UseAuthentication();
app.UseAuthorization();
// optional: app.UseHttpsRedirection();

app.MapControllers();

try
{
    Log.Information("Starting BarnCase API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
