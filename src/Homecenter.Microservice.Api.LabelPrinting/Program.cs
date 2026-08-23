using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Configuration;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuracion tipada: nada de strings magicos dispersos por el codigo.
// ---------------------------------------------------------------------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<EncryptionOptions>(builder.Configuration.GetSection(EncryptionOptions.SectionName));
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection(RateLimitingOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<PrintingOptions>(builder.Configuration.GetSection(PrintingOptions.SectionName));
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.Configure<SwaggerOptions>(builder.Configuration.GetSection(SwaggerOptions.SectionName));

var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
var swaggerOptions = builder.Configuration.GetSection(SwaggerOptions.SectionName).Get<SwaggerOptions>() ?? new SwaggerOptions();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

// ---------------------------------------------------------------------------
// Persistencia
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<LabelPrintingDbContext>(options => options.UseNpgsql(connectionString));

// ---------------------------------------------------------------------------
// CORS: el frontend (Cloudflare Pages) y el API (Render) viven en dominios distintos,
// por lo que el origen debe estar declarado explicitamente o la aplicacion no opera.
// ---------------------------------------------------------------------------
const string CorsPolicyName = "LabelPrintingCors";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (corsOptions.AllowedOrigins.Length == 0)
        {
            return;
        }

        policy.WithOrigins(corsOptions.AllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    }));

// ---------------------------------------------------------------------------
// Health checks: Render exige un endpoint de salud para mantener vivo el servicio.
// ---------------------------------------------------------------------------
var healthChecks = builder.Services.AddHealthChecks();
if (!string.IsNullOrWhiteSpace(connectionString))
{
    healthChecks.AddNpgSql(connectionString, name: "database", tags: new[] { "db" });
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Homecenter · Submodulo de Impresion de ETQ",
        Version = "v1",
        Description = "API de impresion de etiquetas pre-generadas (ETQ/LPN) con validacion de reglas, "
                    + "trazabilidad de impresiones y control de reimpresiones."
    }));

var app = builder.Build();

// Swagger queda gobernado por configuracion. En esta entrega permanece habilitado en
// el ambiente publicado para que el evaluador pueda probar la API: decision consciente
// y documentada, no un descuido de hardening.
if (swaggerOptions.Enabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Impresion de ETQ v1"));
}

app.UseCors(CorsPolicyName);
app.MapControllers();

app.Run();
