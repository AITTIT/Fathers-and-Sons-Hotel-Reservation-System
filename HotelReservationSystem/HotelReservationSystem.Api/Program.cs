using HotelReservationSystem.Api.Features.Rooms.Abstractions;
using HotelReservationSystem.Api.Features.Rooms.Endpoints;
using HotelReservationSystem.Api.Features.Rooms.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IRoomReadService, InMemoryRoomReadService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
    .WithName("Health")
    .WithTags("Health");

app.MapRoomEndpoints();

app.Run();
