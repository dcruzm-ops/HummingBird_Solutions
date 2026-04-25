# Validación de cierre técnico - Milestone 2 (PSA Costa Rica)

## Objetivo
Consolidar la verificación funcional del Milestone 2 con énfasis en integración **frontend (WebApp)** ↔ **backend (WebAPI)**, usando como base los criterios del ERS e IAS.

## Issues cubiertos en esta rama

### ISSUE-11 - Inicio de sesión
- WebApp consume `POST /api/Autenticacion/iniciar-sesion`.
- Se mantiene fallback local vía `AutenticacionManager` si el API no está disponible.
- Se crea sesión cookie con `IdUsuario`, `Email` e `IdRol` para navegación por rol.

### ISSUE-13 - Registro de usuario
- WebApp consume `POST /api/Autenticacion/registrar`.
- Se conserva fallback local para persistencia mediante AppCore/DataAccess.
- Manejo de errores de negocio devueltos por API.

### Integración de recuperación de contraseña (RF-AUT-04/05/06)
- WebApp consume `POST /api/RecuperacionContrasena/solicitar`.
- WebApp consume `POST /api/RecuperacionContrasena/restablecer`.
- Formularios de recuperación y restablecimiento pasaron de flujo visual a flujo funcional contra backend.

### Integración de registro de finca (RF-FIN-01/03/05)
- Formulario `RegistrarFinca` ahora envía datos reales a backend con `POST /api/Finca/Create`.
- Se aplica validación básica de hectáreas en cliente y validaciones de DTO en servidor.
- Se mantiene fallback local con `FincaDAO.Create` para resiliencia en entorno de desarrollo.

## Alineación con IAS
- Respeta arquitectura por capas del IAS: WebApp no accede a SQL en primera opción, usa WebAPI y mantiene fallback controlado para desarrollo.
- Se elimina estado "solo maqueta" en formularios críticos al conectarlos con endpoints existentes.
- Se mantiene separación de responsabilidades: controladores ligeros y consumo de servicios API.

## Riesgos o alcance pendiente para siguientes issues
- Evaluaciones técnicas, pagos, auditoría y notificaciones aún requieren endpoints de escritura dedicados en WebAPI para cerrar completamente todos los RF del ERS.
- Este milestone deja operativo el bloque de autenticación y registro de finca con integración real FE/BE.
