# Integración realizada sobre la base `dennis-feature-dev`

## Criterio aplicado
- La estructura que prevalece es la de `HummingBird_Solutions-dennis-feature-dev(base)`.
- Se integraron cambios útiles de `HummingBird_Solutions-developSprint2` dentro de esa estructura.
- Se excluyeron carpetas duplicadas o legacy que no debían sobrevivir como proyectos paralelos:
  - `AppCore/`
  - `DataAccess/`
  - `PSA-Costa-Rica/`
  - `.vs/`
  - archivos `bin/`, `obj/`, `*.user`, `*.http`, solution duplicadas anidadas

## Archivos integrados o ajustados
### PSA.EntidadesDTO
- Se copiaron DTOs y entidades nuevas desde la versión entrante.
- Se normalizaron rutas problemáticas para evitar arrastre de nombres corruptos o poco portables.

### PSA.DataAccess
- Se agregó `DbContextHelper.cs`.
- Se agregó `DAO/RecuperacionContrasenaDAO.cs` adaptado para usar la cadena `PSAConnection` del proyecto base.
- Se actualizó `DAO/FincaDAO.cs` para conservar la lógica existente de `dennis-feature-dev` y además incorporar los métodos CRUD de la otra versión.

### PSA.AppCore
- Se agregó `FincaService.cs`.
- Se agregó `Managers/RecuperacionContrasenaManager.cs` adaptado a la estrategia de hash existente en la base.

### PSA.WebAPI
- Se agregaron controladores auxiliares y de recuperación de contraseña.
- Se actualizó `Program.cs` para registrar dependencias nuevas.
- Se agregó `appsettings.Development.json` con placeholders para `AppSettings:WebAppBaseUrl` y `SmtpSettings`.

### PSA.WebApp
- Se agregaron modelos de view model para recuperación/restablecimiento de contraseña.
- No se integraron las carpetas `Pages/` ni los assets de template (`wwwroot/lib`, `site.css`, `site.js`) para no contaminar la base MVC funcional.

## Correcciones aplicadas durante la integración
1. Se eliminó el connection string quemado a `JARED\SQLEXPRESS` que venía en la versión entrante.
2. Se evitó usar el hash SHA-256 de la versión entrante para recuperación de contraseña, porque era incompatible con el hash de la base funcional.
3. Se evitó reemplazar la solución raíz y los proyectos correctos por copias duplicadas/anidadas.

## Pendientes manuales recomendados
1. Configurar `PSA.WebAPI/appsettings.Development.json` con valores reales de SMTP y `WebAppBaseUrl` si se quiere usar envío de correo.
2. Probar build en Visual Studio / .NET SDK local, porque en este entorno no había `dotnet` instalado para compilar y validar.
3. Verificar si el flujo WebApp de recuperación de contraseña debe invocar el nuevo API, porque en la base original la UI ya existía pero estaba solo como flujo visual.
