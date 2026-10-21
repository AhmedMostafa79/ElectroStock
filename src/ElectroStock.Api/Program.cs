using ElectroStock.Application.Helpers;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Services;
using ElectroStock.Infrastructure;
using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using ElectroStock.Infrastructure.Services;
using ElectroStock.Infrastructure.Seed;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using ElectroStock.API.Filters;
using ElectroStock.API.Middleware;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();// uses appsettings 

}

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>          //parses json input strings into enums  
{
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(allowIntegerValues: false));
}); ;
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ElectroStock API", Version = "v1" });

    var xmlBasePath = AppContext.BaseDirectory;
    var xmlDocs = new[]
    {
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
        "ElectroStock.Application.xml",
        "ElectroStock.Domain.xml",
        "ElectroStock.Infrastructure.xml"
    };

    foreach (var xmlDoc in xmlDocs)
    {
        var xmlPath = Path.Combine(xmlBasePath, xmlDoc);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    //  register the authorize-filter (Auto-adds 401/403 to all authorized endpoints)
    c.OperationFilter<AuthorizeOperationFilter>();

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))

);// passes a new dbContext instance in each http request, so it is thread safe

builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Seeder Configuration

// Dependency Injection

    // Infrastructure Layer
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<ISequenceCounterRepository, SequenceCounterRepository>();
builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

    // Application Layer
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ISequenceNumberGenerator, SequenceNumberGenerator>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, JwtTokenGenerator>();


builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

// binding appsettings' AdminSeed section into .Infrastructure/Seed/AdminSeedSettings class
builder.Services.Configure<AdminSeedSettings>(builder.Configuration.GetSection("AdminSeed"));
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();


// binding appsettings' JWT section into .Application/Helpers/JWT class
builder.Services.Configure<JWT>(builder.Configuration.GetSection("JWT"));

// Authentication/Authorization
builder.Services.AddAuthentication(options =>
{
    //use jwt authentication scheme ..lets [Authenticated] work
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    //use jwt authentication scheme .. if not authorized throw 401
    options.DefaultChallengeScheme= JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.RequireHttpsMetadata = false;// not Https in development
    o.SaveToken = false;// don't save token
    o.TokenValidationParameters = new TokenValidationParameters // set token validation metrics
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"], //gets the Issuer from appsettings
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey=new SymmetricSecurityKey( 
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])), //pass Key in binary as for param type
        RoleClaimType=ClaimTypes.Role
    };
   
});


//System Building
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

using(var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();

        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync();
}
app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
