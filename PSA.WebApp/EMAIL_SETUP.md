# Configuración SMTP (Unione)

Para que el correo de recuperación funcione en local, configure credenciales por variables de entorno (recomendado):

- `PSA_EMAIL_SMTP_HOST` (ej. `smtp.us1.unione.io`)
- `PSA_EMAIL_SMTP_PORT` (ej. `587`)
- `PSA_EMAIL_SMTP_USERNAME` (ej. `7133848`)
- `PSA_EMAIL_SMTP_PASSWORD` (password SMTP) **o** `PSA_EMAIL_API_KEY`
- `PSA_EMAIL_FROM` (ej. `do-not-reply@sandbox-7133848-7db346.unionemailer.com`)
- `PSA_EMAIL_FROM_NAME` (ej. `PSA Costa Rica - Do Not Reply`)
- `PSA_EMAIL_ENABLE_SSL` (`true`)

Opcional:
- `PSA_EMAIL_SENDER_DOMAIN` para construir remitente `do-not-reply@<dominio>` si no define `PSA_EMAIL_FROM`.

> No suba secretos reales al repositorio. Use `appsettings.Development.json`, User Secrets o variables de entorno.
