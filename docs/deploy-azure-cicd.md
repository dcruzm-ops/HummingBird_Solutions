# Deploy Azure + CI/CD (PSA Costa Rica)

Guía operativa alineada con este repositorio para desplegar:

1. **Azure SQL Database**
2. **PSA.WebAPI** (Azure App Service)
3. **PSA.WebApp** (Azure App Service)

---

## 1) Recursos Azure a crear

Sugerencia de nombres (ajústelos a su convención):

- **Resource Group**: `rg-psa-cr-{env}`
- **SQL Server lógico**: `sql-psa-cr-{env}`
- **Azure SQL Database**: `sqldb-psa-cr-{env}`
- **App Service Plan**: `asp-psa-cr-{env}`
- **Web App API**: `app-psa-api-{env}`
- **Web App Frontend**: `app-psa-web-{env}`

Donde `{env}` típicamente es `dev` (rama `development`) o `prod` (rama `main`).

---

## 2) Estrategia de ramas y workflows

Ramas estandarizadas:

- `development` => sandbox / preproducción
- `main` => producción

Workflows del repo:

- **CI**: `.github/workflows/ci.yml`
  - `restore`, `build Release`, `test`
- **Deploy API**: `.github/workflows/deploy-api.yml`
  - publica `PSA.WebAPI` en `development` y `main`
- **Deploy WebApp**: `.github/workflows/deploy.yml`
  - publica `PSA.WebApp` en `development` y `main`

---

## 3) App Settings requeridos

> Definir en Azure App Service > Configuration.
> Nunca versionar secretos en el repositorio.

### 3.1 PSA.WebAPI

Requeridos:

- `ConnectionStrings__PSAConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__Key` (**obligatoria en Production**)
- `Cors__AllowedOrigins__0` (ejemplo: URL pública de WebApp)
  - agregar más índices según necesidad (`__1`, `__2`, ...)

Opcionales (si usan recuperación de contraseña / notificaciones SMTP):

- `SmtpSettings__Host`
- `SmtpSettings__Port`
- `SmtpSettings__EnableSsl`
- `SmtpSettings__FromName`
- `SmtpSettings__FromEmail`
- `SmtpSettings__Username`
- `SmtpSettings__Password`

### 3.2 PSA.WebApp

Requerido:

- `ApiSettings__BaseUrl` (URL pública de la API, sin slash final opcional)

---

## 4) Secretos GitHub requeridos (Actions)

### Development

- `AZURE_WEBAPP_NAME_API_DEV`
- `AZURE_WEBAPP_PUBLISH_PROFILE_API_DEV`
- `AZURE_WEBAPP_NAME_WEBAPP_DEV`
- `AZURE_WEBAPP_PUBLISH_PROFILE_WEBAPP_DEV`

### Production

- `AZURE_WEBAPP_NAME_API_PROD`
- `AZURE_WEBAPP_PUBLISH_PROFILE_API_PROD`
- `AZURE_WEBAPP_NAME_WEBAPP_PROD`
- `AZURE_WEBAPP_PUBLISH_PROFILE_WEBAPP_PROD`

---

## 5) Orden exacto de publicación

### Paso 1: Azure SQL

1. Crear `sqldb-psa-cr-{env}`.
2. Ejecutar scripts en orden de `BaseDatos/00_ORDEN_EJECUCION.md` usando la sección **Azure SQL Database** (incluye `Azure/02_creacion_vistas_reportes_azure_safe.sql`).
3. Probar conectividad con la cadena `ConnectionStrings__PSAConnection` que usará la API.


> Si durante creación de vistas aparece `Invalid object name 'dbo.PlanesPago'` o `dbo.CuotasPago`, significa que el script de tablas no terminó completo en esa base de datos. Re-ejecute `Azure/01_creacion_tablas_azure_safe.sql` en una base limpia o complete las tablas faltantes antes de continuar.

### Paso 2: WebAPI

1. Configurar App Settings de API.
2. Verificar que `Cors__AllowedOrigins__*` incluya el dominio de WebApp.
3. Hacer push a `development` (dev) o `main` (prod), o correr workflow manualmente.
4. Confirmar que `/swagger` y `/health` respondan.

### Paso 3: WebApp

1. Configurar `ApiSettings__BaseUrl` apuntando al dominio de la API desplegada.
2. Hacer push a `development`/`main` o ejecutar workflow manual.
3. Validar login y flujo principal web->api.

---

## 6) Manejo actual de archivos/evidencias

- La API guarda evidencias en `wwwroot/uploads/...`.
- Al iniciar, la API asegura existencia de `wwwroot/uploads`.
- En cada guardado de evidencia también se crea la carpeta objetivo si no existe.
- En App Service esto persiste en almacenamiento del sitio (no Blob Storage). Si requiere alta durabilidad/escala, planificar migración posterior a Blob Storage.

---

## 7) Smoke tests post-deploy

1. `GET https://<api-app-service>/health`
2. `GET https://<api-app-service>/swagger`
3. Abrir `https://<web-app-service>/`.
4. Iniciar sesión en WebApp.
5. Ejecutar una operación que consuma API autenticada.
6. Subir una evidencia y confirmar que no falla por carpetas inexistentes.

---

## 8) Errores comunes a revisar

- **401/500 en login API**: `Jwt__Key` faltante o inválida.
- **CORS bloqueado**: faltan orígenes en `Cors__AllowedOrigins__*`.
- **WebApp no consume API**: `ApiSettings__BaseUrl` incorrecta.
- **Error SQL al arrancar**: cadena `ConnectionStrings__PSAConnection` inválida o firewall SQL.
- **Deploy falla en Actions**: publish profile vencido o secreto mal nombrado.
