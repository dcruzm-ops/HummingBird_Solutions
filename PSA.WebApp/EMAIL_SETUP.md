# Configuración SMTP (Unione)

Para que el correo de recuperación funcione en local:

1. Configure `PSA.WebApp/appsettings.json` (o preferiblemente `appsettings.Development.json`) en `EmailSettings`.
2. Complete credenciales reales:
   - `Username`: login SMTP (ej. `7133848`)
   - `Password`: password SMTP
   - Si no usa password SMTP, puede usar `ApiKey` como fallback.
3. Use un remitente válido del dominio sandbox:
   - `FromEmail`: `do-not-reply@sandbox-7133848-7db346.unionemailer.com`

> Nota: no suba secretos reales al repositorio.
