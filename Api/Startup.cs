using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Infraestrutura.Db;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.DTOs;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Enuns;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Servicos;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
        key = Configuration?.GetSection("Jwt")?.ToString()?? "";
        

    }
    private string key = "";
    public IConfiguration Configuration { get; set; } = default!;
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IAdministradorServico, AdministradorServico>();
        services.AddScoped<IVeiculoServico, VeiculoServico>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "Minimal API", Version = "v1" });
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "Insira seu token JWT aqui: "
	});

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

services.AddCors(options =>
{
  options.AddPolicy("Local",
    p => p.WithOrigins("http://localhost:7000", "https://localhost:7108")
          .AllowAnyHeader()
          .AllowAnyMethod());
});

// Autenticação e Autorização (JWT)
var jwtIssuer = Configuration["Jwt:Issuer"] ?? "minimal-api";
var jwtAudience = Configuration["Jwt:Audience"] ?? "minimal-api-clients";
var jwtSecret = Configuration["Jwt:Secret"] ?? "dev-secret-change"; // altere em produção

services.AddAuthentication(option =>
{
  option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
  var tvp = new TokenValidationParameters
  {
    ValidateLifetime = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
    ValidateIssuer = false,
    ValidateAudience = false
  };
  option.TokenValidationParameters = tvp;
});
services.AddAuthorization();

services.AddDbContext<DbContexto>(options =>
{
  options.UseMySql(
   Configuration.GetConnectionString("mysql"),
   ServerVersion.AutoDetect(Configuration.GetConnectionString("mysql"))
  );
 });
}
     public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Minimal API v1");
        });
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        
        app. UseEndpoints(endpoints =>
        {
            #region Home
            // Habilita CORS antes dos endpoints
            app.UseCors("Local");


            endpoints.MapGet("/", () => Results.Json(new Home())).WithTags("Home");
            #endregion

            #region Administradores
            string GerarTokenJwt(Administrador administrador)
            {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            

            var securitykey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securitykey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>()
            {
            new Claim("email", administrador.Email),
            new Claim("Perfil", administrador.Perfil),
            new Claim(ClaimTypes.Role, administrador.Perfil),
            };

            var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
            }
            endpoints.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
            {
            var adm = administradorServico.Login(loginDTO);
            if (adm != null)
            {
                string token = GerarTokenJwt(adm);
                return Results.Ok(new AdministradorLogado
                {
                Email = adm.Email,
                Perfil = adm.Perfil,
                Token = token
                });
            }
            else
                return Results.Unauthorized();
                

            }).AllowAnonymous().WithTags("Administradores");

            endpoints.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
            {
            var adms = new List<AdministradorModelView>();
            var administradores = administradorServico.Todos(pagina);
            foreach(var adm in administradores)
            {
                adms.Add(new AdministradorModelView
                {
                Id = adm.Id,
                Email = adm.Email,
                Perfil = adm.Perfil

                });
                    
                }
            return Results.Ok(adms);
            }).RequireAuthorization().RequireAuthorization(policy => policy.RequireRole("Adm")).WithTags("Administradores");

            endpoints.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) =>
            {
            var administrador = administradorServico.BuscaPorId(id);
            if (administrador == null) return Results.NotFound();
            return Results.Ok(new AdministradorModelView
            {
                Id = administrador.Id,
                Email = administrador.Email,
                Perfil = administrador.Perfil
            });
            }).RequireAuthorization().WithTags("Administradores");


            endpoints.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
            {
            var validacao = new ErrosDeValidacao { Mensagens = new List<string>() };

            if (string.IsNullOrEmpty(administradorDTO.Email))
                validacao.Mensagens.Add("Email não pode ser vazio");

            if (string.IsNullOrEmpty(administradorDTO.Senha))
                validacao.Mensagens.Add("Senha não pode ser vazia");
            
            if(administradorDTO.Perfil == null)
            validacao.Mensagens.Add("Perfil não pode ser vazio");
            
            if(validacao.Mensagens.Count > 0)
                return Results.BadRequest(validacao);

            var administrador = new Administrador
            {
                Email = administradorDTO.Email,
                Senha = administradorDTO.Senha,
                Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
            };
            administradorServico.Incluir(administrador);
            
            return Results.Created($"/administrador/{administrador.Id}", new AdministradorModelView
            {
                Id = administrador.Id,
                Email = administrador.Email,
                Perfil = administrador.Perfil
            });
            }).RequireAuthorization().RequireAuthorization(policy => policy.RequireRole("Adm")).WithTags("Administradores");
            #endregion

            #region Veiculos
            ErrosDeValidacao ValidaDTO(VeiculoDTO veiculoDTO)
            {
                var validacao = new ErrosDeValidacao{Mensagens = new List<string>()};
            if (string.IsNullOrEmpty(veiculoDTO.Nome))
                validacao.Mensagens.Add("O nome não pode ser vazio");

            if (string.IsNullOrEmpty(veiculoDTO.Marca))
                validacao.Mensagens.Add("A marca não pode ficar em branco");

            if (veiculoDTO.Ano < 1950)
                validacao.Mensagens.Add("Veiculo muito antigo, aceito apenas anos superiores a 1950");
            return validacao;
            }
            endpoints.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
            {
            
            var validacao = ValidaDTO(veiculoDTO);
            if(validacao.Mensagens.Count > 0)
                return Results.BadRequest(validacao);
            
            var veiculo = new Veiculo
            {
                Nome = veiculoDTO.Nome,
                Marca = veiculoDTO.Marca,
                Ano = veiculoDTO.Ano
            };
            veiculoServico.Incluir(veiculo);
            return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
            }).RequireAuthorization().RequireAuthorization(policy => policy.RequireRole("Adm", "Editor")).WithTags("Veiculos");

            endpoints.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) =>
            {
            var veiculos = veiculoServico.Todos(pagina);

            return Results.Ok(veiculos);
            }).RequireAuthorization().RequireAuthorization(policy => policy.RequireRole("Adm")).WithTags("Veiculos");

            endpoints.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
            {
            var veiculo = veiculoServico.BuscaPorId(id);
            if (veiculo == null) return Results.NotFound();
            return Results.Ok(veiculo);
            }).RequireAuthorization().RequireAuthorization(policy => policy.RequireRole("Adm", "Editor")).WithTags("Veiculos");

            endpoints.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
            {
            var veiculo = veiculoServico.BuscaPorId(id);
            if (veiculo == null) return Results.NotFound();

            var validacao = ValidaDTO(veiculoDTO);
            if(validacao.Mensagens.Count > 0)
                return Results.BadRequest(validacao);
            

            veiculo.Nome = veiculoDTO.Nome;
            veiculo.Marca = veiculoDTO.Marca;
            veiculo.Ano = veiculoDTO.Ano;

            veiculoServico.Atualizar(veiculo);

            return Results.Ok(veiculo);
            }).RequireAuthorization().RequireAuthorization(policy => policy.RequireRole("Adm")).WithTags("Veiculos");

            endpoints.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
            {
            var veiculo = veiculoServico.BuscaPorId(id);
            if (veiculo == null) return Results.NotFound();
            

            veiculoServico.Apagar(veiculo);

            return Results.NoContent();
            }).RequireAuthorization().RequireAuthorization(policy => policy.RequireRole("Adm")).WithTags("Veiculos");
            #endregion

        });
     }

}