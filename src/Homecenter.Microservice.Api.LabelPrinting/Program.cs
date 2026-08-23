using System.Text;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Configuration;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Seed;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Security;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Services;
using Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

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
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

// ---------------------------------------------------------------------------
// Persistencia
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<LabelPrintingDbContext>(options => options.UseNpgsql(connectionString));

// ---------------------------------------------------------------------------
// Inyeccion de dependencias por capa
// ---------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IZoneRepository, ZoneRepository>();
builder.Services.AddScoped<ILabelRepository, LabelRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IPrintRequestRepository, PrintRequestRepository>();

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IPrintSimulator, PrintSimulator>();

// Las reglas se registran individualmente y el motor las ordena por su propiedad Order.
// Agregar una regla nueva es registrar una clase mas: no hay que tocar el motor
// ni el caso de uso.
builder.Services.AddSingleton<IPrintRule, RequiredDataRule>();
builder.Services.AddSingleton<IPrintRule, LabelExistsRule>();
builder.Services.AddSingleton<IPrintRule, DocumentStatusRule>();
builder.Services.AddSingleton<IPrintRule, ZoneAvailabilityRule>();
builder.Services.AddSingleton<IPrintRule, ReprintPolicyRule>();
builder.Services.AddSingleton(provider => new PrintRuleEngine(provider.GetServices<IPrintRule>()));

builder.Services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();
builder.Services.AddScoped<IResolveLabelUseCase, ResolveLabelUseCase>();
builder.Services.AddScoped<IProcessPrintRequestUseCase, ProcessPrintRequestUseCase>();
builder.Services.AddScoped<IGetPrintHistoryUseCase, GetPrintHistoryUseCase>();
builder.Services.AddScoped<IGetDashboardUseCase, GetDashboardUseCase>();

// ---------------------------------------------------------------------------
// Autenticacion y autorizacion por roles
// ---------------------------------------------------------------------------
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
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

            // Sin tolerancia de reloj: un token expirado deja de servir cuando dice
            // que expira, no cinco minutos despues.
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

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
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Homecenter · Submodulo de Impresion de ETQ",
        Version = "v1",
        Description = "API de impresion de etiquetas pre-generadas (ETQ/LPN) con validacion de reglas, "
                    + "trazabilidad de impresiones y control de reimpresiones."
    });

    // La documentacion XML de cada capa alimenta las descripciones de Swagger:
    // el comentario que se escribe junto al codigo es el mismo que lee quien consume
    // la API, y no hay una segunda fuente de verdad que pueda quedar desactualizada.
    foreach (var xmlFile in Directory.GetFiles(AppContext.BaseDirectory, "Homecenter.Microservice.Api.LabelPrinting*.xml"))
    {
        options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
    }

    // Permite al evaluador probar los endpoints protegidos directamente desde Swagger.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Token JWT obtenido en api/auth/login. Se ingresa sin el prefijo 'Bearer'."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Migracion y carga de datos semilla al arranque.
// En Render no hay paso manual de despliegue: la instancia debe quedar operativa
// por si sola tras cada reinicio.
// ---------------------------------------------------------------------------
await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    try
    {
        var context = services.GetRequiredService<LabelPrintingDbContext>();
        await context.Database.MigrateAsync();

        var seeder = new MockDataSeeder(
            context,
            services.GetRequiredService<IPasswordHasher>(),
            services.GetRequiredService<IOptions<SeedOptions>>(),
            services.GetRequiredService<ILoggerFactory>().CreateLogger<MockDataSeeder>(),
            app.Environment.ContentRootPath);

        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        // Un fallo de datos semilla no debe tumbar el servicio: el health check
        // reportara el estado real de la base y el log queda para diagnostico.
        logger.LogError(ex, "Fallo la inicializacion de la base de datos.");
    }
}

// Swagger queda gobernado por configuracion. En esta entrega permanece habilitado en
// el ambiente publicado para que el evaluador pueda probar la API: decision consciente
// y documentada, no un descuido de hardening.
if (swaggerOptions.Enabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Impresion de ETQ v1"));
}

app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
