using System.Net.Mail;
using System.Text;
using AutoMapper;
using CommandsService.AsyncDataServices;
using CommandsService.Data;
using CommandsService.Dtos;
using CommandsService.EventProcessing;
using CommandsService.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMem"));
builder.Services.AddScoped<ICommandRepo, CommandRepo>();
builder.Services.AddHostedService<MessageBusSubscriber>();

builder.Services.AddSingleton<IEventProcessor, EventProcessor>();

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

// app.UseHttpsRedirection();

app.MapPost("/api/c/Platforms", () =>
{
    Console.WriteLine("--> Inbound post command service");
    return Results.Ok("Inbound test from Platforms");
});

app.MapGet("/api/c/Platforms", (ICommandRepo repository, IMapper mapper) =>
{
    Console.WriteLine("--> Getting platforms from CommandsService");
    var platformItems = repository.GetAllPlatforms();

    return Results.Ok(mapper.Map<IEnumerable<PlatformReadDto>>(platformItems));
});

app.MapGet("/api/c/Platforms/{platformId}/Commands", (ICommandRepo repository, IMapper mapper, [FromRoute] int platformId) =>
{
    Console.WriteLine($"--> Hit Get commands for platform {platformId}");

    if (!repository.PlatformExists(platformId))
    {
        return Results.NotFound();
    }

    var commands = repository.GetCommandsForPlatform(platformId);

    return Results.Ok(mapper.Map<IEnumerable<CommandReadDto>>(commands));
});

app.MapGet("/api/c/Platforms/{platformId}/Commands/{commandId}", (ICommandRepo repository, IMapper mapper, [FromRoute] int platformId, int commandId) =>
{
    Console.WriteLine($"--> Hit Get command for platform {platformId} and command {commandId}");

    if (!repository.PlatformExists(platformId))
    {
        return Results.NotFound();
    }

    var command = repository.GetCommand(platformId, commandId);

    if (command == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(mapper.Map<CommandReadDto>(command));
})
.WithName("GetCommandForPlatform");

app.MapPost("/api/c/Platforms/{platformId}/Commands/CreateCommandForPlatform", (ICommandRepo repository, IMapper mapper, [FromRoute] int platformId, [FromBody] CommandCreateDto commandDto) =>
{
    Console.WriteLine("--> Create platform");

    if (!repository.PlatformExists(platformId))
    {
        return Results.NotFound();
    }

    var command = mapper.Map<Command>(commandDto);

    repository.CreateCommand(platformId, command);

    repository.SaveChanges();

    var commandReadDto = mapper.Map<CommandReadDto>(command);

    return Results.CreatedAtRoute("GetCommandForPlatform",
        new { platformId = platformId, commandId = commandReadDto.Id }, commandReadDto);
});

app.Run();
