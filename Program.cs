using Microsoft.AspNetCore.Identity;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Infraestrutura.Db;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.DTOs;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Servicos;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services. AddDbContext<DbContexto>(options =>
{
  options.UseMySql(
   builder.Configuration.GetConnectionString("mysql"),
   ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql"))
  );
});
var app = builder.Build();


app.MapGet("/", () => Results.Json(new Home()));
app.MapPost("/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
{
  if (administradorServico.Login(loginDTO) != null)
    return Results.Ok("Login com sucesso");
  else
    return Results.Unauthorized();
});

app.UseSwagger();
app.UseSwaggerUI();
app.Run();

