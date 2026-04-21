# Informe de Análisis y Diseño de Software (IAS)
## PSA Costa Rica - Versión Final Regenerada desde Implementación

> **Estado del documento:** Regenerado desde el estado real del repositorio y scripts de base de datos.
>
> **Base de verdad usada:** implementación actual (WebApp, WebAPI, AppCore, DataAccess, Entidades/DTOs y SQL).
>
> **Nota de alcance:** Este IAS sustituye la lectura operativa del documento histórico. El documento histórico se usa únicamente como referencia de estructura, no como fuente de verdad funcional.

---

## 1. Introducción

### 1.1 Propósito
Documentar de forma técnica y actualizada el análisis y diseño del sistema **PSA Costa Rica** conforme a su estado implementado, para facilitar mantenimiento, revisión académica/profesional y publicación en Wiki.

### 1.2 Alcance
Este IAS cubre:
- Arquitectura real por capas de la solución.
- Módulos funcionales disponibles en WebApp/WebAPI.
- Reglas de negocio identificables en servicios, managers y scripts SQL.
- Modelo de datos relacional vigente y artefactos de persistencia.
- Seguridad, control de acceso, navegación y consideraciones no funcionales.

### 1.3 Contexto del proyecto
PSA Costa Rica es una solución web para gestionar el ciclo de registro de fincas, evaluación técnica, configuración de pagos, planes/cuotas y trazabilidad administrativa, con operación multirol (Administrador, Propietario y perfil técnico de ingeniería).

### 1.4 Objetivos del sistema
- Digitalizar el flujo de vida de una finca en PSA.
- Permitir evaluación técnica con decisión y ajustes.
- Generar y administrar planes de pago bajo configuración activa.
- Registrar evidencia y auditoría de acciones críticas.
- Controlar acceso por autenticación + autorización por rol/permisos.

---

## 2. Descripción general de la solución

### 2.1 Problema que resuelve
Centraliza procesos que históricamente tienden a fragmentarse: registro de finca, evaluación técnica, parametrización económica, seguimiento de pagos y control administrativo de seguridad/permisos.

### 2.2 Visión general del sistema
La solución está compuesta por:
- **PSA.WebApp** (UI MVC).
- **PSA.WebAPI** (servicios HTTP, JWT, políticas de autorización).
- **PSA.AppCore** (managers y servicios de reglas de negocio).
- **PSA.DataAccess** (DAO + acceso SQL).
- **PSA.EntidadesDTO** (entidades/DTOs compartidos).
- **BaseDatos + scripts/sql** (modelo relacional, vistas, SPs, normalizaciones).

### 2.3 Tipos de usuario / roles
- **Administrador** (rol lógico 1): gestión de usuarios, roles/permisos, parámetros de pago, validación de cuentas, auditoría, reportes administrativos.
- **Propietario / Dueño de finca** (rol lógico 2): registro/gestión de fincas, consulta de planes/cuotas, registro de cuenta bancaria, reportes propios, renovación anual.
- **Ingeniero** (rol lógico 3): bandeja técnica, asignación/ejecución de evaluación, decisión técnica, acciones sobre continuidad de pagos.

### 2.4 Resumen funcional de alto nivel
Flujo dominante:
1. Usuario propietario registra finca.
2. Se crea evaluación pendiente.
3. Ingeniero asigna/ejecuta evaluación y decide **Califica / No Califica**.
4. Si califica, se genera plan preliminar de pago para ciclo siguiente.
5. Propietario registra/asocia cuenta bancaria.
6. Flujo pasa a aprobación final y activación del plan.
7. Sistema mantiene cuotas, reportes y auditoría.

---

## 3. Arquitectura del sistema

### 3.1 Arquitectura por capas realmente usada

```text
PSA.WebApp (MVC / Cookie Auth)
        |
        v
PSA.WebAPI (JWT + Policies + Controllers)
        |
        v
PSA.AppCore (Managers / Services / reglas)
        |
        v
PSA.DataAccess (DAO / SPs / consultas SQL)
        |
        v
SQL Server (tablas, vistas, procedimientos, triggers)
```

[Insertar diagrama de arquitectura aquí]

### 3.2 Responsabilidad por capa
- **WebApp:** presentación, navegación, renderizado de vistas, manejo de sesión cookie e invocación HTTP a API.
- **WebAPI:** contratos HTTP, validaciones de entrada, autorización por rol y permisos, traducción de errores.
- **AppCore:** reglas funcionales de negocio, orquestación de procesos multi-módulo, notificaciones y auditoría funcional.
- **DataAccess:** acceso a datos (DAO), ejecución de SPs y consultas para persistencia/lectura.
- **BD:** integridad relacional, estado del dominio, catálogo/configuración y trazabilidad.

### 3.3 Flujo de comunicación entre capas
- WebApp consume endpoints API con `HttpClientService`.
- API delega en managers/servicios AppCore.
- AppCore usa DAOs para operaciones SQL.
- DAOs leen/escriben en SQL Server (tablas, vistas y SPs).

### 3.4 Buenas prácticas observadas y esperadas
- Separación de responsabilidades por proyectos.
- Uso de DTOs para contratos entre capas.
- Controles de autorización por rol y por política de permisos.
- Registro de eventos en bitácora (`AuditoriaLog`) para acciones sensibles.

### 3.5 Exclusión de lógica indebida
- La WebApp no accede directamente a SQL.
- La API no concentra toda la lógica de negocio compleja (delegación a AppCore).
- Persistencia concentrada en DAOs/SPs.

---

## 4. Stack tecnológico

| Categoría | Implementación corroborada |
|---|---|
| Lenguaje principal | C# |
| Framework backend | ASP.NET Core Web API |
| Framework frontend | ASP.NET Core MVC (Razor Views) |
| Autenticación WebApp | Cookie Authentication |
| Autenticación API | JWT Bearer |
| Autorización | Roles + policies por permisos (`perm`) |
| Persistencia | SQL Server |
| Pruebas | xUnit (PSA.AppCore.Tests) |
| CI | GitHub Actions (`dotnet restore/build/test`) |
| Despliegue | Guía de publicación manual a Azure App Service (`Pendiente de validación` de despliegue real) |

---

## 5. Módulos funcionales del sistema

> Se listan módulos con evidencia en controladores, vistas, managers y scripts.

### 5.1 Autenticación (inicio/cierre/registro)
- **Propósito:** alta de usuarios, inicio de sesión y salida del sistema.
- **Actores:** todos.
- **Flujo general:** registro -> login -> emisión de token API + sesión web.
- **Reglas relevantes:** normalización de rol lógico 1/2/3, control de intentos con throttle.
- **Dependencias:** Usuarios, Roles, RolesPermisos, JWT, Cookie auth.

### 5.2 Recuperación de contraseña
- **Propósito:** recuperación segura mediante token temporal.
- **Actores:** todos.
- **Flujo general:** solicitar -> generar token -> validar token -> restablecer.
- **Reglas relevantes:** token de 6 dígitos, invalida tokens activos previos, uso único, expiración configurable.
- **Dependencias:** TokensRecuperacion, SMTP, PasswordPolicy, auditoría.

### 5.3 Gestión de fincas
- **Propósito:** registro, edición, consulta de fincas del propietario.
- **Actores:** propietario.
- **Flujo general:** registrar finca -> crear evaluación pendiente -> consulta detalle/historial.
- **Reglas relevantes:** validación de provincia/cantón/distrito; validaciones de propiedad.
- **Dependencias:** EvaluacionesTecnicas, FincaEvidencias, reportes.

### 5.4 Evaluación técnica
- **Propósito:** ejecución técnica de evaluación y decisión.
- **Actores:** ingeniero.
- **Flujo general:** tomar evaluación pendiente -> registrar visita/ajustes/decisión -> finalizar.
- **Reglas relevantes:** decisión sólo `Califica` o `No Califica`; estados técnicos controlados.
- **Dependencias:** generación de plan preliminar si califica, auditoría y notificaciones.

### 5.5 Pagos y planes
- **Propósito:** ciclo de planes/cuotas de pago por finca.
- **Actores:** ingeniero, propietario, administrador.
- **Flujo general:** generar plan preliminar -> asociar cuenta bancaria -> aprobación final -> activación -> seguimiento de cuotas.
- **Reglas relevantes:** no recalcular/sobrescribir plan existente por finca-año; control de estados del plan.
- **Dependencias:** ConfiguracionesPago, CuentasBancarias, CuotasPago, PlanesPagoDetalleCalculo.

### 5.6 Gestión de cuentas bancarias
- **Propósito:** registro por dueño y validación administrativa.
- **Actores:** propietario, administrador.
- **Flujo general:** dueño registra cuenta (pendiente) -> administración valida/rechaza -> asociación a plan.
- **Reglas relevantes:** tipo de cuenta permitido; validación previa a activaciones.
- **Dependencias:** CuentasBancarias, módulo Pagos, auditoría.

### 5.7 Administración (usuarios, roles, permisos, pagos)
- **Propósito:** gobierno del sistema.
- **Actores:** administrador.
- **Flujo general:** CRUD usuarios, roles/permisos, configuración de pagos, cuentas pendientes, auditoría.
- **Reglas relevantes:** autorización por políticas específicas (no solo rol).
- **Dependencias:** Permisos/RolesPermisos, ConfiguracionesPago, AuditoriaLog.

### 5.8 Notificaciones
- **Propósito:** informar hitos funcionales del flujo.
- **Actores:** todos (según evento).
- **Flujo general:** emisión in-app y, en eventos definidos, correo electrónico.
- **Reglas relevantes:** catálogos de severidad `info/success/warning`; marca de leídas.
- **Dependencias:** Notificaciones, servicios de correo.

### 5.9 Reportes
- **Propósito:** vistas operativas y administrativas para seguimiento.
- **Actores:** propietario, ingeniero, administrador.
- **Flujo general:** consumo de vistas/SPs/reportes API por rol.
- **Reglas relevantes:** endpoints segmentados por rol y/o permisos.
- **Dependencias:** vistas SQL de reporte, procedimientos y DAOs.

### 5.10 Perfil de usuario
- **Propósito:** consulta/edición de datos propios, cambio de contraseña e inactivación.
- **Actores:** usuario autenticado.
- **Flujo general:** ver perfil -> editar -> acciones de seguridad personal.
- **Reglas relevantes:** autorización obligatoria.
- **Dependencias:** Usuarios, autenticación, auditoría.

---

## 6. Reglas de negocio identificadas

> Solo se incluyen reglas corroboradas. Cuando aplica se marca explícitamente incertidumbre.

1. **Estados de evaluación técnica controlados** por catálogo de estados permitidos en DTO de flujo.
2. **Decisión técnica restringida** a `Califica` o `No Califica`.
3. **Si evaluación califica, se genera plan preliminar** para el año siguiente (`DateTime.UtcNow.Year + 1`).
4. **Tope de ajuste de pago:** se aplica tope de configuración y además tope institucional máximo de 40%.
5. **No se sobrescribe plan por finca/año:** existe validación de existencia previa.
6. **Renovación anual restringida:** se habilita cuando plan está finalizado/cancelado o queda una cuota, y no existe renovación pendiente activa del mismo ciclo.
7. **Recuperación de contraseña con token:** un uso, expirable, invalida tokens previos activos.
8. **Política de contraseña:** mínimo 10 caracteres con mayúscula, minúscula, número y símbolo.
9. **Throttle de login:** bloqueo temporal por intentos fallidos repetidos (parámetros configurables con valores por defecto).
10. **Una sola configuración de pago activa:** reforzada por script e índice único filtrado.
11. **Trazabilidad:** eventos relevantes registran auditoría (módulo, acción, detalle, actor, IP cuando aplica).

**Elementos con validación incompleta desde código:**
- `Pendiente de validación`: operación completa de transacciones bancarias reales; existe tabla `TransaccionesPago`, pero el flujo principal observable está centrado en plan/cuotas/estados.

---

## 7. Diseño de datos

### 7.1 Entidades/tablas principales
- Seguridad: `Roles`, `Permisos`, `RolesPermisos`, `Usuarios`, `TokensRecuperacion`.
- Núcleo PSA: `Fincas`, `EvaluacionesTecnicas`, `FincaEvidencias`, `EvaluacionEvidencias`.
- Pagos: `ConfiguracionesPago`, `ConfiguracionPagoDetalle`, `PlanesPago`, `PlanesPagoDetalleCalculo`, `CuotasPago`, `TransaccionesPago`.
- Soporte: `CuentasBancarias`, `Notificaciones`, `AuditoriaLog`, `CatalogoFincaValores`.

### 7.2 Relaciones clave (funcionales)
- Usuario-rol y rol-permiso para seguridad.
- Propietario-fincas y finca-evaluaciones para ciclo técnico.
- Evaluación-plan-cuentas/cuotas para ciclo de pagos.
- Usuario/auditoría y usuario/notificaciones para trazabilidad y comunicación.

### 7.3 Tablas maestras/configurables
- `CatalogoFincaValores` (valores de pendiente/vegetación/uso suelo).
- `ConfiguracionesPago` + `ConfiguracionPagoDetalle` (motor de cálculo y topes).
- `Permisos`/`RolesPermisos` (control de acceso granular).

### 7.4 Tablas transaccionales
- `PlanesPago`, `PlanesPagoDetalleCalculo`, `CuotasPago`, `TransaccionesPago`.
- `EvaluacionesTecnicas` y evidencias como traza operativa del proceso técnico.

### 7.5 Seguridad y auditoría en datos
- `AuditoriaLog` para acciones críticas.
- `TokensRecuperacion` con fechas y uso.
- `Notificaciones` con estado de lectura y timestamps.

### 7.6 Observaciones de consistencia
- Existen scripts de normalización para resolver diferencias históricas (`RolesPermisos` vs `RolPermisos`) y permisos de login/autorización.
- `No se pudo corroborar en el código` una política única de versionado documental del MER; sí hay scripts evolutivos con enfoque correctivo.

[Insertar modelo de datos aquí]

---

## 8. Navegación y experiencia funcional

### 8.1 Mapa de navegación textual

```text
Público
- Inicio
- Iniciar sesión
- Registro
- Recuperar contraseña / validar token / restablecer

Autenticado
- Dashboard (redirección por rol)

Propietario
- Mis fincas / registrar finca / detalle finca
- Renovación anual (según elegibilidad)
- Pagos: planes, detalle, historial, cuenta bancaria
- Reportes del dueño
- Notificaciones
- Mi perfil

Ingeniero
- Bandeja técnica pendiente
- Evaluaciones: detalle, registrar resultado, historial/proceso
- Pagos: planes pendientes y aprobación final
- Reportes técnicos
- Notificaciones
- Mi perfil

Administrador
- Dashboard admin
- Gestión de usuarios
- Roles y permisos
- Parámetros de pago
- Validación de cuentas bancarias
- Auditoría
- Reportes administrativos
- Mi perfil
```

[Insertar mapa de navegación aquí]

### 8.2 Accesos por rol
El acceso se protege por:
- Atributos `[Authorize(Roles="...")]`.
- Policies por permiso (`ADMIN_*`, `ING_*`, `DUENO_*`).

### 8.3 Pantallas clave corroboradas
Se identifican vistas MVC para autenticación, dashboard por rol, fincas, evaluaciones, pagos, administración, reportes, notificaciones y perfil.

---

## 9. Seguridad y control de acceso

### 9.1 Autenticación
- API: JWT Bearer con validación de issuer/audience/signing key y lifetime.
- WebApp: Cookie Authentication con expiración deslizante (8 horas).

### 9.2 Autorización
- Modelo híbrido de **rol + permiso granular** (claim `perm`).
- Policies explícitas para administración, reportes, auditoría, aprobación técnica y renovación de finca.

### 9.3 Recuperación de contraseña
- Generación de token de 6 dígitos.
- Vigencia configurable (por defecto 1 minuto en configuración actual).
- Invalida tokens previos y marca token como usado al restablecer.

### 9.4 Protección adicional
- Throttle de intentos de login por clave compuesta email/IP.
- Auditoría de eventos de autenticación y recuperación.

### 9.5 Protección de datos
- Password hash en almacenamiento.
- Manejo de secretos por variables de entorno/user secrets (no hardcodeados en repositorio).

---

## 10. Consideraciones no funcionales

### 10.1 Mantenibilidad
- Separación por proyectos/capas y contratos DTO.
- Presencia de pruebas unitarias en reglas críticas (cálculo y política de contraseña).

### 10.2 Escalabilidad
- API desacoplada de UI.
- Modelo de permisos extensible por tabla `Permisos`.
- Configuración de pagos versionada y activable.

### 10.3 Rendimiento
- Uso de vistas y SPs para reportes y operaciones frecuentes.
- Caching en memoria para throttle de seguridad.

### 10.4 Disponibilidad
- CI existente para validación continua de build/test.
- `Pendiente de validación` despliegue cloud ejecutado extremo a extremo.

### 10.5 Usabilidad
- Navegación por dashboard y menús por rol.
- Mensajes de validación en español desde WebApp.

### 10.6 Seguridad
- JWT + cookies + authorization policies + auditoría + throttle.

### 10.7 Trazabilidad
- Registro sistemático de eventos sensibles en `AuditoriaLog`.

### 10.8 Compatibilidad
- Stack .NET y SQL Server estandarizado.
- Scripts de compatibilidad para variaciones históricas en esquema de permisos.

---

## 11. Decisiones de diseño

1. **Arquitectura por capas (WebApp/WebAPI/AppCore/DataAccess/SQL).**
   - Ventaja: separación clara, mantenibilidad.
   - Riesgo: mayor coordinación intercapa.

2. **Autorización por permisos además de rol.**
   - Ventaja: control fino de acciones administrativas.
   - Riesgo: requiere mantenimiento de catálogo y claims.

3. **Motor de pagos con configuración activa versionada.**
   - Ventaja: cambios funcionales sin recompilar lógica de cálculo.
   - Riesgo: dependencia de calidad de datos de configuración.

4. **Generación automática de plan preliminar al calificar evaluación.**
   - Ventaja: continuidad operativa del flujo.
   - Riesgo: acoplamiento temporal (año siguiente) debe revisarse según política institucional real.

5. **Estrategia de notificaciones in-app + email.**
   - Ventaja: mejor comunicación de hitos.
   - Riesgo: dependencia de SMTP configurado.

---

## 12. Hallazgos, deuda técnica y observaciones

1. **Evidencia de evolución histórica del esquema SQL** con scripts de normalización y compatibilidad (por ejemplo, variantes de tabla de relación de roles/permisos).
2. **Duplicidad/solapamiento funcional en algunos controladores** (por ejemplo coexistencia de `FincaController` y `FincasController`) que sugiere transición incremental.
3. **Script de procedimientos con bloques repetidos** (`SP_Fincas_Registrar`/`SP_Fincas_Actualizar` aparecen más de una vez en el archivo), lo cual eleva riesgo de mantenimiento.
4. **Tabla `TransaccionesPago` existe en modelo**, pero su uso operativo de extremo a extremo no queda completamente evidente en los flujos principales actuales (`No se pudo corroborar en el código` para integración bancaria real).
5. **Token lifetime de recuperación muy corto por defecto (1 min)**: útil para seguridad, pero puede impactar usabilidad en entornos reales.
6. **Despliegue cloud declarado como pendiente operativo**: hay guía y CI, pero sin confirmación de release productivo ejecutado desde este entorno.

---

## 13. Conclusión

El sistema PSA Costa Rica presenta una base arquitectónica sólida por capas, con implementación funcional verificable para autenticación, gestión de fincas, evaluación técnica, administración de pagos, seguridad por permisos y trazabilidad por auditoría. La versión actual refleja una solución en estado de **madurez intermedia-alta para entorno académico/prototipo profesional**, con componentes críticos implementados y pruebas unitarias en reglas sensibles.

La alineación entre análisis, diseño e implementación es mayor que en el documento histórico cuando se observa el estado real del repositorio. Persisten oportunidades de mejora en homogeneización de artefactos heredados, consolidación de controladores y validación operativa final de despliegue/flujo bancario real.

---

## Apéndice A - Estado de corroboración

- **Corroborado en código**: arquitectura por capas, módulos funcionales principales, seguridad, reportes, notificaciones, auditoría, motor de cálculo, políticas.
- **Pendiente de validación**: ejecución real de despliegue cloud y operación bancaria externa integral.
- **No se pudo corroborar en el código**: evidencia de integración bancaria en producción y documentación única final del MER fuera de scripts.

