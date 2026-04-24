# Reactivar servicio de correos en Azure (PSA Costa Rica)

Esta guía está orientada a un escenario donde la app ya está publicada en Azure App Service, pero faltó configurar SMTP.

## 1) Confirmar qué componente envía correos

En este repositorio, el envío de recuperación de contraseña ocurre en **PSA.WebAPI** usando `SmtpSettings`.

- Si faltan `Host`, `FromEmail`, `Username` o `Password`, la API lanza error y no envía correos.

## 2) Recolectar datos SMTP del proveedor

Necesitas estos valores antes de tocar Azure:

- Host SMTP
- Puerto (`587` recomendado con TLS)
- SSL/TLS habilitado (`true`)
- Correo remitente (`FromEmail`)
- Nombre remitente (`FromName`)
- Usuario SMTP
- Contraseña SMTP

> Recomendación: usar un remitente del dominio verificado en tu proveedor SMTP.

## 3) Configurar App Settings en Azure App Service de la API

En el portal de Azure:

1. Ir a **App Services**.
2. Abrir la app de la API (por ejemplo `app-psa-api-prod` o equivalente).
3. Entrar a **Settings > Environment variables** (o **Configuration** según UI).
4. En **Application settings**, crear/actualizar:

- `SmtpSettings__Host`
- `SmtpSettings__Port`
- `SmtpSettings__EnableSsl`
- `SmtpSettings__FromName`
- `SmtpSettings__FromEmail`
- `SmtpSettings__Username`
- `SmtpSettings__Password`

5. Guardar cambios (Azure solicitará reinicio).

## 4) Reiniciar la API

Aunque Azure suele reiniciar automáticamente, haz un **Restart** manual del App Service para asegurar carga de variables.

## 5) Validar que la API esté arriba

Validaciones rápidas:

- `GET https://<tu-api>/health`
- `GET https://<tu-api>/swagger`

Si esto falla, resuelve disponibilidad primero antes de probar correos.

## 6) Probar flujo real de recuperación

1. Desde WebApp, ejecutar “Olvidé mi contraseña”.
2. Ingresar un correo de usuario existente.
3. Confirmar recepción del correo.
4. Validar token y restablecer contraseña.

## 7) Diagnóstico si sigue sin llegar correo

Revisar en **Log stream** de la API:

- Error de SMTP no configurado (faltan claves obligatorias).
- Error de autenticación SMTP (usuario/clave inválidos).
- Error TLS/SSL (puerto o política TLS incorrecta).

Checklist rápido:

- `SmtpSettings__Host` correcto.
- Puerto compatible con tu proveedor (`587` TLS o el que indique).
- Credenciales vigentes.
- Remitente autorizado/verificado por el proveedor.
- No hay reglas anti-spam bloqueando el envío.

## 8) Endurecimiento recomendado (post-reactivación)

- Guardar secretos SMTP en **Azure Key Vault** y referenciarlos desde App Settings.
- Definir alertas de error de envío (Application Insights).
- Evitar usar sandbox de correo en producción.
- Crear un correo técnico de pruebas para smoke test después de cada despliegue.

## 9) Nota de arquitectura de este repositorio

Actualmente los correos salen desde **PSA.WebAPI** (no desde PSA.WebApp en Azure para recuperación de contraseña). Por eso la configuración SMTP crítica debe vivir en el App Service de la API.
