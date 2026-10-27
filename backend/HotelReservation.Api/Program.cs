using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotelReservation.Api.Data;
using HotelReservation.Api.Features.Common;
using HotelReservation.Api.Features.Hotels;
using HotelReservation.Api.Features.Reservations;
using HotelReservation.Api.Features.Rooms;
using HotelReservation.Api.Features.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (jwtOptions.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be configured and at least 32 characters long.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<TokenService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = jwtOptions.SigningKey,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = JwtRegisteredClaimNames.Sub
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Admin, p => p.RequireRole("Admin"));
    options.AddPolicy(Policies.StaffOrAdmin, p => p.RequireRole("Staff", "Admin"));
});

// Browser clients on other origins must be listed in Cors:AllowedOrigins. Native clients (MAUI) don't use CORS.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => "Hotel Reservation API");

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapHotelEndpoints();
app.MapRoomEndpoints();
app.MapReservationEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}

app.Run();