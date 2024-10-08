using System.Windows.Input;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatformService;
using PlatformService.AsyncDataServices;
using PlatformService.Data;
using PlatformService.Dtos;
using PlatformService.Models;
using PlatformService.SyncDataServices.Grpc;
using PlatformService.SyncDataServices.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseInMemoryDatabase("InMem"));

builder.Services.AddScoped<IPlatformRepo, PlatformRepo>();
builder.Services.AddHttpClient<ICommandDataClient, CommandDataClient>();
builder.Services.AddSingleton<IMessageBusClient, MessageBusClient>();
builder.Services.AddGrpc();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

app.MapGrpcService<GrpcPlatformService>();
app.MapGet("/protos/platforms.proto", async context =>
{
    await context.Response.WriteAsync(File.ReadAllText("Protos/platforms.proto"));
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

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

app.MapPost("/api/Platforms", async (IPlatformRepo repository,
    IMapper mapper,
    ICommandDataClient commandDataClient,
    IMessageBusClient messageBusClient,
PlatformCreateDto platformCreateDto) =>
{
    var platformModel = mapper.Map<Platform>(platformCreateDto);
    repository.CreatePlatform(platformModel);
    repository.SaveChanges();

    var platformReadDto = mapper.Map<PlatformReadDto>(platformModel);

    //Send sync message
    try
    {
        await commandDataClient.SendPlatformToCommand(platformReadDto);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> Could not send synchronously: {ex.Message}");
    }

    //Send async message
    try
    {
        var platformPublishedDto = mapper.Map<PlatformPublishedDto>(platformReadDto);
        platformPublishedDto.Event = "Platform_Published";
        messageBusClient.PublishPlatform(platformPublishedDto);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> Could not send asynchronously: {ex.Message}");
    }

    return Results.CreatedAtRoute("GetPlatformById", new { Id = platformReadDto.Id }, platformReadDto);
})
.WithOpenApi();

Console.WriteLine($"--> CommandService Endpoint {builder.Configuration["CommandService"]}");

app.Run();
