<a id="tabla-de-contenidos"></a>
# Informe de Análisis y Diseño de Software – Sistema de Pago por Servicios Ambientales (PSA Costa Rica)

## Tabla de Contenidos
- [1. Introducción](#sec-1)
  - [1.1 Propósito](#sec-1-1)
  - [1.2 Antecedentes](#sec-1-2)
  - [1.3 Audiencia](#sec-1-3)
  - [1.4 Visión General](#sec-1-4)
- [2. Referencias](#sec-2)
- [3. Especificación de Diseño](#sec-3)
  - [3.1 Diseño Técnico](#sec-3-1)
    - [3.1.1 Arquitectura de la aplicación](#sec-3-1-1)
    - [3.1.2 Modelo de Base de Datos](#sec-3-1-2)
    - [3.1.3 Modelo de Objetos](#sec-3-1-3)
  - [3.2 Diseño Gráfico](#sec-3-2)
    - [3.2.1 Paleta de Colores](#sec-3-2-1)
    - [3.2.2 Tipografía](#sec-3-2-2)
    - [3.2.3 Imágenes y Recursos](#sec-3-2-3)
  - [3.3 Estructura de la Aplicación](#sec-3-3)
    - [3.3.1 Estructura de Carpetas](#sec-3-3-1)
    - [3.3.2 Convenciones Generales](#sec-3-3-2)
  - [3.4 Mapa de Navegación](#sec-3-4)
    - [3.4.1 Descripción](#sec-3-4-1)
    - [3.4.2 Representación del Mapa de Navegación](#sec-3-4-2)
  - [3.5 Wireframes](#sec-3-5)
    - [3.5.1 Definición](#sec-3-5-1)
    - [3.5.2 Consideraciones](#sec-3-5-2)
    - [3.5.3 Ejemplo Wireframe](#sec-3-5-3)
  - [3.6 Estándares](#sec-3-6)
    - [3.6.1 Estándares CSS](#sec-3-6-1)
    - [3.6.2 Estándares JavaScript](#sec-3-6-2)
    - [3.6.3 Estándares HTML](#sec-3-6-3)
- [4. Anexos](#sec-4)
  - [4.1 Hallazgos, deuda técnica y observaciones](#sec-4-1)
  - [4.2 Reglas de negocio consolidadas](#sec-4-2)
  - [4.3 Seguridad y control de acceso](#sec-4-3)
  - [4.4 Consideraciones no funcionales](#sec-4-4)
  - [4.5 Decisiones de diseño](#sec-4-5)
  - [4.6 Conclusión](#sec-4-6)

---

<a id="sec-1"></a>
## 1. Introducción

El presente documento regenera el **Informe de Análisis y Diseño de Software (IAS)** del sistema **PSA Costa Rica**, tomando como fuente principal el estado real del repositorio (WebApp, WebAPI, AppCore, DataAccess, DTOs/Entidades y scripts SQL).

<a id="sec-1-1"></a>
### 1.1 Propósito

Definir de forma trazable y actualizada el análisis y diseño final de la solución implementada, con formato listo para publicación en Wiki y alineado con la arquitectura y módulos efectivos del proyecto.

<a id="sec-1-2"></a>
### 1.2 Antecedentes

El sistema se desarrolló como una solución web por capas para gestionar:

- Registro de fincas.
- Evaluación técnica por rol de ingeniería.
- Configuración y cálculo de pagos.
- Seguimiento de planes, cuotas y estados.
- Seguridad de acceso y auditoría de acciones.

El IAS histórico se usa únicamente como referencia de estructura documental; el contenido de esta versión se deriva de artefactos de implementación actuales.

<a id="sec-1-3"></a>
### 1.3 Audiencia

- Equipo de desarrollo.
- Equipo de calidad.
- Coordinación/gestión de proyecto.
- Docentes o revisores técnicos.

<a id="sec-1-4"></a>
### 1.4 Visión General

Este documento conserva la estructura del IAS original, pero con contenido actualizado sobre:

- Diseño técnico real.
- Diseño de datos vigente.
- Navegación y módulos implementados.
- Seguridad, estándares y deuda técnica observable.

---

<a id="sec-2"></a>
## 2. Referencias

Fuentes utilizadas para esta regeneración:

- Código fuente actual del repositorio PSA Costa Rica.
- Solución `.slnx` y proyectos por capa (WebApp/WebAPI/AppCore/DataAccess/EntidadesDTO).
- Scripts SQL de `BaseDatos/` y `scripts/sql/`.
- Flujo CI de `.github/workflows/ci.yml`.
- Documento de despliegue `docs/deploy-azure-cicd.md`.

Referencias de marco teórico (vigentes para contexto académico):

- Pressman & Maxim (2020), *Software Engineering: A Practitioner's Approach*.
- Sommerville (2016), *Software Engineering*.

---

<a id="sec-3"></a>
## 3. Especificación de Diseño

<a id="sec-3-1"></a>
### 3.1 Diseño Técnico

<a id="sec-3-1-1"></a>
#### 3.1.1 Arquitectura de la aplicación

La solución usa arquitectura por capas con separación clara de responsabilidades:

- **PSA.WebApp (MVC)**: presentación, navegación, sesión web por cookies.
- **PSA.WebAPI**: endpoints HTTP, JWT, autorización por rol/política.
- **PSA.AppCore**: managers y servicios con reglas de negocio.
- **PSA.DataAccess**: DAOs y acceso a SQL Server.
- **SQL Server**: almacenamiento relacional, SPs, vistas, trazabilidad.

Decisiones arquitectónicas observadas:

- WebApp consume API; no hay acceso directo a base de datos desde UI.
- API delega reglas de negocio relevantes a AppCore.
- Seguridad híbrida por rol (`Authorize(Roles=...)`) y permisos (`Policy`, claim `perm`).

##### 3.1.1.1 Diagrama de arquitectura

[Insertar diagrama de arquitectura aquí]

##### 3.1.1.2 Diagrama de despliegue

[Insertar diagrama de despliegue aquí]

<a id="sec-3-1-2"></a>
#### 3.1.2 Modelo de Base de Datos

Modelo relacional principal (corroborado en scripts):

- Seguridad: `Roles`, `Permisos`, `RolesPermisos`, `Usuarios`, `TokensRecuperacion`.
- Núcleo: `Fincas`, `EvaluacionesTecnicas`, `FincaEvidencias`, `EvaluacionEvidencias`.
- Pagos: `ConfiguracionesPago`, `ConfiguracionPagoDetalle`, `PlanesPago`, `PlanesPagoDetalleCalculo`, `CuotasPago`, `TransaccionesPago`.
- Soporte: `CuentasBancarias`, `Notificaciones`, `AuditoriaLog`, `CatalogoFincaValores`.

Relaciones funcionales relevantes:

- Usuario -> Rol y Rol -> Permisos.
- Propietario -> Fincas -> Evaluaciones.
- Evaluación -> Plan -> Cuotas.
- Usuario -> Notificaciones y Auditoría.

Observaciones de consistencia:

- Existen scripts de normalización para variantes históricas de tabla (`RolesPermisos` / `RolPermisos`).
- Se refuerza una única configuración de pago activa con índice filtrado.
- `No se pudo corroborar en el código` una integración bancaria externa productiva, aunque existe tabla transaccional.

##### 3.1.2.1 Diagrama de base de datos

[Insertar modelo de datos aquí]

<a id="sec-3-1-3"></a>
#### 3.1.3 Modelo de Objetos

Objetos/DTOs de negocio más relevantes en la versión final:

- Autenticación y seguridad: `InicioSesionDTO`, `RegistrarUsuarioDTO`, `RespuestaInicioSesionDTO`, `TokenRecuperacion`.
- Fincas: `RegistrarFincaDTO`, `FincaResumenDTO`, `FincaDetalleDTO`.
- Evaluaciones: DTOs de flujo técnico y estados (`Pendiente`, `En proceso`, `Evaluada – Califica`, etc.).
- Pagos: `PlanPagoDTO`, `CuotaPlanPagoDTO`, DTOs de lectura Owner/Engineer/Admin y estados de plan/cuota.
- Perfil, administración, reportes y notificaciones con ViewModels/DTOs de soporte.

##### 3.1.3.1 Diagrama de clases

[Insertar diagrama de clases aquí]

---

<a id="sec-3-2"></a>
### 3.2 Diseño Gráfico

<a id="sec-3-2-1"></a>
#### 3.2.1 Paleta de Colores

`Pendiente de validación`: el repositorio no centraliza en un solo documento final una paleta oficial homologada. Existen vistas y estilos distribuidos en WebApp.

Recomendación para Wiki del proyecto:

- Mantener línea visual orientada a sostenibilidad.
- Definir tokens de color oficiales (modo claro/oscuro) en documento UI separado.

<a id="sec-3-2-2"></a>
#### 3.2.2 Tipografía

`No se pudo corroborar en el código` una especificación tipográfica formal única como artefacto de diseño final. Se recomienda estandarizar tipografía en guía visual del proyecto.

<a id="sec-3-2-3"></a>
#### 3.2.3 Imágenes y Recursos

Se identifican recursos gráficos en repositorio (`PSA.Images`) y vistas con componentes compartidos. Para Wiki:

- Usar imágenes optimizadas y versionadas.
- Mantener placeholders de diagramas en este IAS y ubicar arte final en carpeta de documentación.

---

<a id="sec-3-3"></a>
### 3.3 Estructura de la Aplicación

<a id="sec-3-3-1"></a>
#### 3.3.1 Estructura de Carpetas

Estructura base real de solución:

```text
/psa-costa-rica.slnx
/PSA.WebApp
/PSA.WebAPI
/PSA.AppCore
/PSA.DataAccess
/PSA.EntidadesDTO
/PSA.AppCore.Tests
/BaseDatos
/scripts/sql
/docs
```

<a id="sec-3-3-2"></a>
#### 3.3.2 Convenciones Generales

Convenciones efectivas observables:

- Separación por responsabilidad de capa/proyecto.
- Managers/Services para reglas de negocio en AppCore.
- DAO para persistencia y consultas SQL.
- DTOs para intercambio de datos y contratos API.
- Uso de auditoría para eventos sensibles.

---

<a id="sec-3-4"></a>
### 3.4 Mapa de Navegación

<a id="sec-3-4-1"></a>
#### 3.4.1 Descripción

La navegación se organiza por autenticación inicial y redirección por rol:

- Público: inicio, login, registro, recuperación/restablecimiento.
- Propietario: fincas, pagos, reportes propios, notificaciones, perfil.
- Ingeniero: bandeja/evaluación técnica, pagos pendientes de aprobación final, reportes técnicos.
- Administrador: usuarios, roles/permisos, configuración de pagos, validación de cuentas, auditoría, reportes.

<a id="sec-3-4-2"></a>
#### 3.4.2 Representación del Mapa de Navegación

```text
Inicio
├── Iniciar sesión
├── Registro
└── Recuperación de contraseña
    └── Validar token / restablecer

Autenticado
├── Dashboard por rol
│
├── Propietario
│   ├── Mis fincas / Registrar finca / Detalle
│   ├── Renovación anual
│   ├── Pagos (planes, detalle, historial, cuenta bancaria)
│   ├── Reportes dueño
│   ├── Notificaciones
│   └── Mi perfil
│
├── Ingeniero
│   ├── Fincas pendientes / evaluaciones
│   ├── Resultado técnico
│   ├── Planes pendientes de aprobación final
│   ├── Reportes ingeniero
│   ├── Notificaciones
│   └── Mi perfil
│
└── Administrador
    ├── Gestión de usuarios
    ├── Roles y permisos
    ├── Parámetros de pago
    ├── Validación de cuentas bancarias
    ├── Auditoría
    ├── Reportes administrativos
    └── Mi perfil
```

[Insertar mapa de navegación aquí]

---

<a id="sec-3-5"></a>
### 3.5 Wireframes

<a id="sec-3-5-1"></a>
#### 3.5.1 Definición

Los wireframes se mantienen como artefacto de referencia UX/UI para validar estructura de pantallas y flujo por rol.

<a id="sec-3-5-2"></a>
#### 3.5.2 Consideraciones

- Se corroboran vistas funcionales implementadas para autenticación, dashboards, fincas, evaluaciones, pagos, administración, reportes, notificaciones y perfil.
- `Pendiente de validación`: consolidación final de un paquete único de wireframes finales alineado 1:1 con cada pantalla productiva.

<a id="sec-3-5-3"></a>
#### 3.5.3 Ejemplo Wireframe

[Insertar ejemplo de wireframe aquí]

---

<a id="sec-3-6"></a>
### 3.6 Estándares

<a id="sec-3-6-1"></a>
#### 3.6.1 Estándares CSS

- Evitar estilos inline.
- Reutilizar componentes y estilos compartidos.
- Mantener consistencia visual por rol/módulo.

<a id="sec-3-6-2"></a>
#### 3.6.2 Estándares JavaScript

- Separar scripts por responsabilidad.
- Evitar lógica de negocio crítica en cliente.
- Mantener validaciones de servidor como fuente de verdad.

<a id="sec-3-6-3"></a>
#### 3.6.3 Estándares HTML

- Uso semántico de estructura en vistas.
- Formularios con validación clara y mensajes comprensibles.
- Consistencia de navegación y accesibilidad base.

---

<a id="sec-4"></a>
## 4. Anexos

<a id="sec-4-1"></a>
### 4.1 Hallazgos, deuda técnica y observaciones

1. Coexistencia de controladores con alcance similar (`FincaController` y `FincasController`) sugiere evolución incremental.
2. En `04_creacion_stored_procedures.sql` existen bloques repetidos de procedimientos en el histórico del archivo.
3. Persisten scripts de compatibilidad para normalizar roles/permisos por diferencias históricas de esquema.
4. Tabla `TransaccionesPago` existe, pero `No se pudo corroborar en el código` integración bancaria real de extremo a extremo.

<a id="sec-4-2"></a>
### 4.2 Reglas de negocio consolidadas

- Decisión técnica válida: `Califica` / `No Califica`.
- Si finca califica, se genera plan preliminar para ciclo siguiente.
- Tope de ajuste de pago limitado por configuración y tope institucional de 40%.
- No se recalcula/sobrescribe plan para misma finca-año cuando ya existe.
- Renovación anual restringida por elegibilidad de estado de plan y existencia de evaluación pendiente.
- Recuperación de contraseña: token expirable, un uso e invalidación de tokens previos.

<a id="sec-4-3"></a>
### 4.3 Seguridad y control de acceso

- JWT en API y cookie auth en WebApp.
- Autorización por rol + políticas por permisos.
- Throttle de intentos de login por email/IP.
- Auditoría de eventos de autenticación y operaciones sensibles.

<a id="sec-4-4"></a>
### 4.4 Consideraciones no funcionales

- **Mantenibilidad:** separación por capas y proyectos.
- **Escalabilidad:** modelo extensible de permisos y configuración de pagos.
- **Rendimiento:** uso de SPs/vistas para operaciones de reporte.
- **Disponibilidad:** pipeline CI activo; despliegue cloud real `Pendiente de validación`.
- **Trazabilidad:** bitácora en `AuditoriaLog`.

<a id="sec-4-5"></a>
### 4.5 Decisiones de diseño

- Arquitectura por capas para desacoplar UI/API/negocio/datos.
- Control de acceso granular por permisos.
- Motor de pagos basado en configuración activa versionable.
- Notificaciones in-app y correo para hitos de proceso.

<a id="sec-4-6"></a>
### 4.6 Conclusión

El sistema PSA Costa Rica, según su estado implementado en repositorio, evidencia una arquitectura y diseño funcionales coherentes para un entorno académico/profesional en fase madura de construcción. El documento queda actualizado al estado real del software, mantiene la estructura original del IAS y explicita vacíos de corroboración sin inventar comportamiento no sustentado en código.

