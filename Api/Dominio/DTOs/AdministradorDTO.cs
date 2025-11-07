namespace MinimalApi.DTOs;
using MinimalApi.Dominio.Enuns;
public class AdministradorDTO
{

    public string Email { get; set; } = default!;
    public string Senha { get; set; } = default!;
    public Perfil? Perfil { get; set; } = default!;
}