# Despliegue a Azure (WebApp + WebAPI + Base de Datos)

Esta guía está pensada para llevar la versión estable de `main` (que ya corre local) a Azure con:

- `PSA.WebApp` (frontend MVC)
- `PSA.WebAPI` (backend API)
- SQL Database (base de datos)

---

## 1) Orden recomendado de creación en Azure

1. **Resource Group** (contenedor de todo)
2. **Azure SQL Server**
3. **Azure SQL Database**
4. **App Service Plan**
5. **Web App para API**
6. **Web App para Frontend**
7. (Opcional recomendado) **Application Insights**

> Orden clave: primero BD, luego API, luego WebApp.

---

## 2) Preparar Base de Datos SQL en Azure

1. Crear **Azure SQL Server** (usuario admin + contraseña fuerte).
2. Crear **Azure SQL Database** dentro de ese servidor.
3. En Networking del SQL Server:
   - permitir acceso desde servicios Azure,
   - agregar tu IP para migraciones/manual.
4. Ejecutar scripts del repo en este orden:
   - `BaseDatos/Tablas/psa_costa_rica_schema.sql`
   - `BaseDatos/Tablas/psa_auditoria_triggers.sql`
   - `BaseDatos/DatosSemilla/psa_datos_semilla.sql`
5. Validar que las tablas principales existan y que los datos semilla se cargaron.

---

## 3) Crear Web Apps (API y Frontend)

### 3.1 API (`PSA.WebAPI`)

- Runtime: **.NET (LTS)**
- OS: Linux o Windows (recomendado Linux por costo)
- Startup command: no requerido para publicación estándar .NET

### 3.2 Frontend (`PSA.WebApp`)

- Runtime: **.NET (LTS)**
- Mismo App Service Plan (si quieres optimizar costos)

---

## 4) Configuración de Application Settings (sin secretos en git)

Configurar en **Configuration > Application settings** de cada Web App.

### API (`PSA.WebAPI`)

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__PSAConnection=<cadena_sql_azure>`
- `Cors__AllowedOrigins=https://<tu-webapp-frontend>.azurewebsites.net`
- `AppSettings__WebAppBaseUrl=https://<tu-webapp-frontend>.azurewebsites.net`
- Variables SMTP necesarias para recuperación de contraseña

### Frontend (`PSA.WebApp`)

- `ASPNETCORE_ENVIRONMENT=Production`
- `ApiSettings__BaseUrl=https://<tu-webapi>.azurewebsites.net`
- `ConnectionStrings__PSAConnection=<cadena_sql_azure>`
- Variables `EmailSettings__*` si se usa envío de correo desde WebApp

> Nota: en Azure, `__` (doble guion bajo) representa `:` en configuración .NET.

---

## 5) Ajustes de código ya incluidos para Azure

Se dejaron estos ajustes para simplificar despliegue:

1. **CORS configurable por ambiente** en `PSA.WebAPI`:
   - lee `Cors:AllowedOrigins` (CSV),
   - mantiene `https://localhost:59664` como fallback local.
2. **Validación relajada de certificado solo en Development** en `PSA.WebApp`:
   - evita comportamiento inseguro en producción,
   - mantiene comodidad local para certificados de dev.

---

## 6) Publicación desde GitHub / Azure DevOps

Opciones válidas:

- **Deployment Center (GitHub Actions)**: recomendado.
- **Azure DevOps Pipeline**: si ya tienen organización/pipeline corporativo.

Configurar un pipeline por app:

- Pipeline API publica proyecto `PSA.WebAPI/PSA.WebAPI.csproj`.
- Pipeline WebApp publica proyecto `PSA.WebApp/PSA.WebApp.csproj`.

En ambos casos:

1. `dotnet restore`
2. `dotnet build -c Release`
3. `dotnet publish -c Release`
4. Deploy al App Service correspondiente.

---

## 7) Verificación post-deploy (checklist)

1. Abrir `https://<api>.azurewebsites.net/swagger` (si habilitado en el ambiente).
2. Probar endpoint de salud: `GET /api/health`.
3. Abrir `https://<webapp>.azurewebsites.net`.
4. Validar login y flujos críticos (fincas, evaluaciones, reportes).
5. Confirmar recuperación de contraseña (link debe apuntar al frontend en Azure).
6. Revisar logs en **Log stream** / Application Insights.

---

## 8) Errores comunes y solución rápida

- **CORS bloqueado**
  - Verificar `Cors__AllowedOrigins` en API.
- **500 por conexión SQL**
  - Verificar `ConnectionStrings__PSAConnection` y firewall SQL.
- **Frontend no encuentra API**
  - Verificar `ApiSettings__BaseUrl` en Frontend.
- **Link de recuperación apunta a localhost**
  - Verificar `AppSettings__WebAppBaseUrl` en API.

---

## 9) Siguiente paso recomendado

Antes de producción final, crear un entorno **staging** (2 Web Apps + 1 DB de pruebas) para validar:

- configuración,
- migraciones/scripts,
- smoke tests,
- performance básico.

Cuando staging esté estable, promover mismo pipeline a producción.
