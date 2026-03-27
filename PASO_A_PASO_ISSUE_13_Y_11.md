# Paso a paso para cerrar ISSUE-13 y ISSUE-11

> Estado actual: Swagger ya funciona y el endpoint `POST /api/Autenticacion/registrar` ya crea usuarios.

## 1) ISSUE-13 (Registro + hash + persistencia)

### Objetivo funcional
Completar y dejar estable el flujo de **registro** con contraseña hasheada y rol por defecto Propietario (`IdRol = 2`).

### Checklist técnico
1. Validar base de datos inicial:
   - Ejecutar `BaseDatos/Tablas/psa_costa_rica_schema.sql`.
   - Ejecutar `BaseDatos/DatosSemilla/psa_datos_semilla.sql`.
2. Confirmar que exista el rol Propietario:
   - `SELECT IdRol, Nombre FROM Roles;` (debe existir `IdRol = 2`, `Nombre = 'Propietario'`).
3. Probar registro en Swagger:
   - `POST /api/Autenticacion/registrar`
   - Enviar `nombreCompleto`, `email`, `contrasena`, `confirmacionContrasena`.
4. Verificar en BD:
   - Usuario insertado en `Usuarios`.
   - `PasswordHash` no esté en texto plano.
   - `IdRol` guardado como `2`.
5. Casos de validación mínimos:
   - Correo duplicado -> error controlado.
   - Contraseñas no coinciden -> error controlado.
   - Rol por defecto faltante en BD -> error claro del API.

### Criterio de cierre sugerido ISSUE-13
- Registro exitoso.
- Hash aplicado.
- Usuario persistido con `IdRol = 2`.
- Errores de negocio controlados en respuestas 400.

---

## 2) ISSUE-11 (Inicio de sesión)

### Objetivo funcional
Implementar el flujo de **inicio de sesión** reutilizando el hash ya definido en AppCore.

### Paso a paso de implementación
1. En `AutenticacionManager`, crear método `IniciarSesionAsync(InicioSesionDTO dto)`.
2. Buscar usuario por correo con `UsuarioDAO.ObtenerPorEmailAsync(dto.Email.Trim())`.
3. Si no existe usuario -> retornar error de credenciales inválidas.
4. Verificar contraseña con `IServicioHashContrasena.VerificarHash(...)`.
5. Si hash inválido -> retornar error de credenciales inválidas.
6. Si válido:
   - actualizar `UltimoAcceso` en BD (nuevo método DAO recomendado: `ActualizarUltimoAccesoAsync`).
   - responder DTO de sesión (mínimo: `IdUsuario`, `NombreCompleto`, `Email`, `IdRol`).
7. Exponer endpoint `POST /api/Autenticacion/iniciar-sesion`.
8. Documentar respuestas HTTP esperadas (200, 400/401).

### Pruebas mínimas ISSUE-11
1. Login correcto con usuario recién registrado.
2. Login con correo inexistente -> falla controlada.
3. Login con contraseña incorrecta -> falla controlada.
4. Confirmar actualización de `UltimoAcceso` cuando login es exitoso.

### Criterio de cierre sugerido ISSUE-11
- Endpoint de login funcional.
- Verificación de hash operativa.
- Actualización de último acceso.
- Respuestas controladas para éxito y fallos.

---

## Orden recomendado de trabajo
1. Cerrar ISSUE-13 completamente (estabilizar registro y evidencias de prueba).
2. Luego implementar ISSUE-11 (login), reutilizando las piezas de ISSUE-13.
3. Finalmente, adjuntar evidencia en PR:
   - Capturas de Swagger (request/response)
   - Query de verificación en BD
   - Lista corta de casos probados
