# minimal-api

API minimal em .NET para gestão de administradores e veículos — projeto leve com testes de integração.

Resumo rápido
- Projeto: .NET Minimal API (Api/)
- Testes: MSTest (Test/)
- Banco: MySQL (migrations em Api/Migrations)
- Ambiente de teste: Setup em Test/Helpers/Setup.cs usa WebApplicationFactory e injeta um mock de IAdministradorServico

Como rodar local (Windows / PowerShell)
1. Restaurar e compilar:
   ```powershell
   dotnet restore
   dotnet build
   ```
2. Aplicar migrations (se usar DB real):
   ```powershell
   dotnet ef database update --project Api --startup-project Api
   ```
3. Rodar a API:
   ```powershell
   cd Api
   dotnet run
   ```
4. Rodar testes:
   ```powershell
   dotnet test
   ```

Pontos importantes para desenvolvimento e debugging
- Tests de integração usam Test/Helpers/Setup.cs:
  - Cria WebApplicationFactory<Startup>
  - Configura ambiente "Testing" e porta 5001
  - Injeta AdministradorServicoMock para IAdministradorServico
- Se um teste de login falhar (401), verifique:
  - Email/senha usados no seed vs no teste (espaços/trim e case)
  - Normalize entradas (Trim/ToLower) ou corrija o seed/mocks
- Use o mock quando quiser isolar lógica de serviços; altere Setup.cs para trocar implementações.

Endpoints principais
- POST /administradores/login
  - Body: { "Email": "...", "Senha": "..." }
  - Retorna: AdministradorLogado (Email, Perfil, Token)

Exemplo rápido (curl)
```bash
curl -X POST http://localhost:5000/administradores/login \
  -H "Content-Type: application/json" \
  -d '{"Email":"administrador@teste.com","Senha":"123456"}'
```
