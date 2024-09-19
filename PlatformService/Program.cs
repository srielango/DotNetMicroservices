using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatformService.Data;
using PlatformService.Dtos;
using PlatformService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseInMemoryDatabase("InMem"));

builder.Services.AddScoped<IPlatformRepo, PlatformRepo>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

PrepDb.PrepPopulation(app);

app.MapGet("/api/Platforms", (IPlatformRepo repository, IMapper mapper) =>
{
    Console.WriteLine("--> Getting Platforms...");
    var platformItem = repository.GetAllPlatforms();
    return mapper.Map<IEnumerable<PlatformReadDto>>(platformItem);
})
.WithOpenApi();

app.MapGet("/api/Platforms/{id}", (IPlatformRepo repository, IMapper mapper, [FromRoute] int id) =>
{
    Console.WriteLine("--> Getting Platform by id...");
    var platformItem = repository.GetPlatformById(id);
    if (platformItem != null)
    {
        return Results.Ok(mapper.Map<PlatformReadDto>(platformItem));
    }
    return Results.NotFound();
})
.WithName("GetPlatformById")
.WithOpenApi();

app.MapPost("/api/Platforms", (IPlatformRepo repository, IMapper mapper, PlatformCreateDto platformCreateDto) =>
{
    var platformModel = mapper.Map<Platform>(platformCreateDto);
    repository.CreatePlatform(platformModel);
    repository.SaveChanges();

    var platformReadDto = mapper.Map<PlatformReadDto>(platformModel);
    return Results.CreatedAtRoute("GetPlatformById", new { Id = platformReadDto.Id }, platformReadDto);
})
.WithOpenApi();

// app.MapGet("/weatherforecast", () =>
// {
//     var forecast =  Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// })
// .WithName("GetWeatherForecast")
// .WithOpenApi();

app.Run();

