# Evidencia técnica de CI/CD y despliegue (Azure)

Este repositorio **no afirma** una publicación productiva ya ejecutada en Azure.  
Sí deja preparada una base verificable para build/test continuo y publicación manual controlada.

## 1) CI automatizado en repositorio

Se agregó el workflow `.github/workflows/ci.yml` con:

1. `dotnet restore`
2. `dotnet build` (Release)
3. `dotnet test` del proyecto `PSA.AppCore.Tests`

## 2) Secretos y configuración requerida

Para ejecutar local o en pipeline, definir:

- `ConnectionStrings__PSAConnection`
- `Jwt__Key` (**obligatoria**, no viene en repo)
- Credenciales SMTP si se desea envío real de correos:
  - `SmtpSettings__Host`
  - `SmtpSettings__Port`
  - `SmtpSettings__Username`
  - `SmtpSettings__Password`
  - `SmtpSettings__FromEmail`

## 3) Publicación manual mínima a Azure App Service (pendiente operativo)

Pasos esperados para el equipo:

1. Crear App Service + SQL Server/Database en Azure.
2. Configurar variables de aplicación anteriores en App Service (Configuration).
3. Ejecutar scripts SQL de `BaseDatos/` y `scripts/sql/` en el entorno destino.
4. Publicar API:
   - `dotnet publish PSA.WebAPI/PSA.WebAPI.csproj -c Release -o ./artifacts/webapi`
5. Desplegar artefacto publicado con `az webapp deploy` o perfil de publicación.

## 4) Limitaciones reales actuales

- No se ejecutó despliegue real en Azure desde este entorno.
- No se validó conectividad contra recursos cloud externos.
- Queda lista la evidencia de pipeline CI y los pasos explícitos que faltan de forma manual.
