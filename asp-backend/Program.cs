using JustOne.Auth;
using JustOne.Data;
using JustOne.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Read the "JwtSettings" section into a strongly-typed object.
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options => options.User.RequireUniqueEmail = true)
    .AddEntityFrameworkStores<AppDbContext>();

// 3. Authenticate requests using JWT bearer tokens.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Keep the claim names exactly as they appear in the token (no surprise remapping).
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Name,
            // RoleClaimType must match the claim name TokenService writes roles under.
            // If these two disagree, every role check fails with 403.
            RoleClaimType = "role"
        };
    });

// 4. Authorization. The "ManagerOnly" named policy is reused by any endpoint that needs it.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ManagerOnly", policy => policy.RequireRole(Roles.Manager));

builder.Services.AddScoped<ITokenService, TokenService>();

// Controllers instead of minimal API endpoints.
builder.Services.AddControllers();

// Swagger/Swashbuckle.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Adds the "Authorize" button in Swagger UI so a bearer token can be entered.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a JWT token, e.g.: eyJhbGciOi..."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

// CORS: allows the local Vue dev server (Vite) to call the backend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Seed the roles and the three test users so you can log in right away.
await DbSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Serves uploaded post images under /uploads/...
app.UseStaticFiles();

// Must come before Authentication/Authorization, otherwise the policy won't apply reliably.
app.UseCors("AllowVueDev");

// Order matters: authenticate first (who are you?), then authorize (are you allowed?).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();