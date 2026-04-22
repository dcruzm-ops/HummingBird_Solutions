# Guion de demo (45 minutos) basado en código real — PSA Costa Rica

> Fuente de verdad: rama actual del repositorio. Este guion prioriza rutas estables y marca explícitamente lo parcial/riesgoso.

## SECCIÓN 1 — RESUMEN EJECUTIVO DE HALLAZGOS

### Rubros **sí implementados**
- Landing del equipo y landing del producto (Home/Index, Home/Equipo, Home/Producto).
- Autenticación completa base: iniciar sesión, cerrar sesión, registro, recuperación/restablecimiento por token.
- Registro de finca con mapa (Leaflet + reverse geocoding), catálogos ambientales, carga de evidencias.
- Flujo técnico de evaluaciones: bandeja, asignación, resultado, ajustes técnicos, carga de evidencia.
- Administración: gestión de usuarios, roles/permisos, configuración de pagos, validación de cuentas bancarias, auditoría.
- Pagos: generación de plan desde evaluación calificada, detalle de cálculo, asociación de cuenta, aprobación final por ingeniero.
- Reportería por rol (dueño, ingeniero, administrador), incluyendo pagos por ubicación y resumen de actividad.
- Arquitectura por capas y CI básico en GitHub Actions.

### Rubros **parciales**
- Simulación de planes mensuales: existe bandera `Simular` en API/SP, pero en WebApp el flujo principal usa generación real (`Simular=false`).
- Configuración de “roles y permisos” depende del bootstrap SQL de permisos/relaciones (si no se ejecuta, la UI queda limitada).
- Estándar “no utiliza alertas”: no hay `window.alert`, pero sí hay componentes visuales Bootstrap tipo `alert`.
- Azure: hay guía y pipeline CI, pero el propio documento de despliegue indica que no se validó un despliegue cloud real desde este entorno.

### Rubros **ausentes o no demostrables de forma segura**
- Evidencia verificable de publicación productiva real en Azure dentro del repo (solo guía y pasos).
- Automatización completa de CD (solo CI + pasos manuales documentados).

### Partes **seguras para demo en vivo**
1. Landing equipo + landing producto.
2. Login por rol y dashboards.
3. Registro de finca con mapa y evidencias.
4. Cola de evaluación + evaluación con ajustes del ingeniero.
5. Flujo de plan de pagos, desglose y tope.
6. Reportes por rol.
7. Administración (usuarios, configuración pagos, auditoría).

### Partes con **riesgo de demo**
- Reverse geocoding de mapa depende de servicio externo (Nominatim).
- Recuperación por correo depende de SMTP configurado.
- Módulos de permisos/roles si DB no tiene scripts de bootstrap aplicados.
- Cualquier afirmación de “ya está publicado en Azure” sin evidencia externa en vivo.

---

## SECCIÓN 2 — MATRIZ DE COBERTURA DEMO VS RUBRO

| Rubro | ¿Existe en código? | ¿Se puede demostrar en vivo? | Módulo / archivo / pantalla / endpoint relacionado | Riesgo | Recomendación para demo |
|---|---|---|---|---|---|
| 1) Landing equipo | Sí | Sí | `HomeController.Equipo`, `Views/Home/Equipo.cshtml` | Bajo | Abrir al inicio (1 min). |
| 2) Landing producto | Sí | Sí | `HomeController.Producto`, `Views/Home/Producto.cshtml` | Bajo | Mostrar propuesta de valor y CTA a login. |
| 3) Inicio/cierre de sesión | Sí | Sí | `AutenticacionController` (WebApp/API), `api/Autenticacion/iniciar-sesion`, `CerrarSesion` | Bajo | Entrar y salir rápido por rol. |
| 3) Recuperación contraseña | Sí | Con cuidado | `api/RecuperacionContrasena/*`, vistas recuperación | Medio (SMTP) | Probar solicitud+token; correo real solo si SMTP listo. |
| 3) Registro y usuarios | Sí | Sí | Registro público + `AdministracionController` usuarios | Bajo | Crear usuario en admin, editar rol. |
| 3) Asignación de roles | Sí | Sí | Editar usuario (`IdRol`), `api/Administracion/usuarios/{id}` | Medio | Confirmar roles seed antes de demo. |
| 4) Registro de propiedad | Sí | Sí | `Views/Fincas/RegistrarFinca.cshtml`, `registro-finca.js`, `api/Fincas` | Medio (mapa externo) | Tener finca backup precargada. |
| 4) Mapa geográfico | Sí | Con cuidado | Leaflet + Nominatim (`registro-finca.js`) | Medio/alto (internet) | Click rápido en CR; fallback con finca semilla. |
| 4) Fotografías/evidencia finca | Sí | Sí | `api/FincaEvidencias/subir`, formulario archivos | Medio (peso/formatos) | Usar imágenes pequeñas testadas. |
| 4) Catálogo ambiental requerido | Sí | Sí | `RegistrarFincaDTO` + selects en vista | Bajo | Cargar exactamente los valores de catálogo. |
| 5) Gestión usuarios | Sí | Sí | `Administracion/GestionUsuarios`, crear/editar/eliminar | Bajo | Demostrar crear + editar + inactivar. |
| 5) Mover cliente a otro asesor | Sí | Sí | `ReasignarCliente`, endpoint `usuarios/reasignar-cliente` | Medio | Validar existencia de ingenieros/propietarios. |
| 5) Roles y permisos | Sí | Con cuidado | `RolesPermisosSimple`, `api/Administracion/roles-permisos` | Medio | Confirmar bootstrap permisos antes. |
| 5) Precio por hectárea y % ajustes | Sí | Sí | `ParametrosPago`, `ConfiguracionPagoDAO` | Bajo | Mostrar creación de versión con tope. |
| 5) Auditoría | Sí | Sí | `AuditoriaLogs`, `api/Administracion/auditoria` | Bajo | Filtrar por módulo Pagos/Evaluaciones. |
| 6) Cola pendientes visita | Sí | Sí | `Evaluaciones/FincasPendientes`, `bandeja-pendientes` | Bajo | Usar estado Pendiente/En proceso. |
| 6) Registro resultados evaluación | Sí | Sí | `NuevaEvaluacion`, `resultado` | Bajo | Guardar “Califica” con observaciones. |
| 6) Ingeniero edita datos finca | Sí | Sí | Ajustes técnicos + DAO actualiza `Fincas` | Bajo | Mostrar antes/después en detalle. |
| 6) Evidencia evaluación | Sí | Sí | `api/FincaEvidencias/evaluacion/{id}/subir` | Medio | Subir 1 imagen para minimizar riesgo. |
| 7) Reglas configurables cálculo | Sí | Sí | `PaymentCalculationService`, config pago admin | Bajo | Mostrar factores en detalle de plan. |
| 7) Plan mensual simulado | Parcial | Con cuidado | API/SP tienen `Simular`; UI principal no expone simulación clara | Medio | Mencionar como capacidad backend; demo principal real. |
| 7) Historial pagos por propiedad | Sí | Sí | `Pagos/HistorialPagos`, `Reportes/DuenoPagos` | Bajo | Abrir historial + detalle cálculo. |
| 7) Tope 40% | Parcial controlado | Sí | DAO valida <=40; cálculo aplica tope config | Bajo | Configurar ejemplo >30 y explicar recorte. |
| 8) Reportes dueño | Sí | Sí | `DuenoFincas`, `DuenoPagos` | Bajo | Mostrar cuotas + ajustes + transacciones. |
| 8) Reportes ingeniero | Sí | Sí | `IngenieroEvaluaciones`, `IngenieroTecnico` | Bajo | Filtrar mensual/anual. |
| 8) Reportes admin ubicación/actividad | Sí | Sí | `AdminPagos`, `AdminResumenActividad` | Bajo | Filtrar provincia/cantón/distrito. |
| 9) Fallos cosméticos | Sí (detectables) | Sí | Vistas CSS/UX | Medio | Preparar explicación breve. |
| 11) Estándar validación/no alertas | Parcial | Con cuidado | Validaciones DataAnnotations + mensajes JS; hay componentes `alert` | Medio | Aclarar “sin alertas nativas JS”. |
| 12) Arquitectura/capas/BD | Sí | Sí | solución + Program + DAO/SP/scripts | Bajo | Cierre técnico final. |
| 12) Repositorio/Publicación Azure | Parcial | Con cuidado | `ci.yml` + `deploy-azure-cicd.md` | Medio | Hablar de evidencia CI y pasos manuales. |

---

## SECCIÓN 3 — CASOS DE PRUEBA / ESCENARIOS DE DEMO

### CP-01 — Entrada institucional y navegación inicial
- **Objetivo:** abrir landing del equipo y del producto.
- **Rol:** cualquiera.
- **Precondiciones:** WebApp arriba.
- **Pasos:** abrir `/Home/Index` -> navegar a `/Home/Producto`.
- **Resultado esperado:** se ven ambos contextos y CTA a login.
- **Tiempo demo:** 2 min.
- **Notas:** usar discurso fijo de alcance.
- **Riesgo/fallback:** si falla imagen externa, continuar con texto funcional.

### CP-02 — Login multirol y cierre de sesión
- **Objetivo:** validar seguridad por roles y dashboards.
- **Rol:** administrador, dueño, ingeniero.
- **Precondiciones:** usuarios demo listos.
- **Pasos:** login admin -> dashboard admin -> logout -> login ingeniero -> logout -> login dueño.
- **Resultado esperado:** redirección por rol correcta.
- **Tiempo demo:** 4 min.
- **Notas:** no navegar profundo aún.
- **Riesgo/fallback:** si un usuario falla, usar otro seed equivalente.

### CP-03 — Registro de usuario y asignación de rol
- **Objetivo:** crear usuario y ajustar rol desde administración.
- **Rol:** administrador.
- **Precondiciones:** sesión admin activa.
- **Pasos:** crear usuario -> editar rol -> verificar listado.
- **Resultado esperado:** usuario visible con nuevo rol.
- **Tiempo demo:** 4 min.
- **Notas:** usar correo demo único.
- **Riesgo/fallback:** si falla crear, usar usuario seed y solo editar.

### CP-04 — Registro de finca con mapa y evidencia
- **Objetivo:** registrar finca completa con atributos ambientales.
- **Rol:** dueño.
- **Precondiciones:** dueño autenticado.
- **Pasos:** abrir RegistrarFinca -> pin mapa en CR -> completar atributos -> subir foto -> guardar.
- **Resultado esperado:** finca aparece en MisFincas.
- **Tiempo demo:** 6 min.
- **Notas:** usar archivo liviano.
- **Riesgo/fallback:** si geocoding falla, mostrar finca seed ya registrada.

### CP-05 — Cola técnica y evaluación con ajustes
- **Objetivo:** tomar evaluación pendiente y registrar resultado.
- **Rol:** ingeniero.
- **Precondiciones:** existe evaluación pendiente.
- **Pasos:** abrir FincasPendientes -> NuevaEvaluacion -> ajustar hectáreas/vegetación/pendiente -> decisión Califica -> guardar + evidencia.
- **Resultado esperado:** evaluación finalizada; datos técnicos ajustados persisten.
- **Tiempo demo:** 7 min.
- **Notas:** explicar trazabilidad de original vs ajustado.
- **Riesgo/fallback:** si carga evidencia falla, continuar con evaluación guardada sin adjuntos.

### CP-06 — Flujo de plan de pago y tope
- **Objetivo:** mostrar cálculo y regla de tope.
- **Rol:** ingeniero + dueño + admin.
- **Precondiciones:** evaluación calificada + configuración vigente.
- **Pasos:** generar plan (si aplica) -> dueño asocia cuenta -> ingeniero aprueba activación -> dueño ve detalle.
- **Resultado esperado:** plan activo y desglose visible.
- **Tiempo demo:** 7 min.
- **Notas:** enfatizar base + ajustes + tope.
- **Riesgo/fallback:** usar plan seed existente y abrir detalle.

### CP-07 — Reportes por rol
- **Objetivo:** demostrar analítica diferenciada.
- **Rol:** dueño, ingeniero, administrador.
- **Precondiciones:** datos seed cargados.
- **Pasos:** dueño (pagos/transacciones) -> ingeniero (evaluaciones mensual/anual) -> admin (pagos por ubicación + resumen actividad).
- **Resultado esperado:** filtros aplican y listados responden.
- **Tiempo demo:** 7 min.
- **Notas:** filtrar solo un criterio por pantalla para rapidez.
- **Riesgo/fallback:** si filtro falla, mostrar reporte sin filtro y explicar endpoint disponible.

### CP-08 — Auditoría y cierre técnico
- **Objetivo:** cerrar con trazabilidad, capas y despliegue.
- **Rol:** administrador / técnico del equipo.
- **Precondiciones:** acciones previas generaron logs.
- **Pasos:** abrir AuditoriaLogs -> filtrar módulo Pagos/Evaluaciones -> mostrar CI y doc Azure.
- **Resultado esperado:** evidencia de trazabilidad y madurez técnica.
- **Tiempo demo:** 4 min.
- **Notas:** usar respuestas cortas.
- **Riesgo/fallback:** si no hay eventos, mostrar histórico seed/auditoría crítica.

---

## SECCIÓN 4 — GUIÓN MAESTRO DE PRESENTACIÓN (45 MINUTOS)

> Estilo unificado: frases breves, secuenciales y retomables por cualquier integrante.

### Bloque 1 (0:00–2:00) — Apertura
- **Quién:** cualquiera.
- **Narración literal:** “Buenos días. Vamos a demostrar PSA Costa Rica sobre el código funcional actual. Mostraremos flujo real por rol: administrador, dueño e ingeniero, desde registro hasta pagos y reportes.”
- **Acción en pantalla:** Home/Index.
- **Transición:** “Ahora pasamos a la landing del producto.”
- **Objetivo:** contexto.
- **Valor:** enmarca alcance.
- **Pregunta probable:** “¿Esto es maqueta?”
- **Respuesta corta:** “No, es flujo conectado a API y base de datos.”

### Bloque 2 (2:00–4:00) — Landing producto
- **Narración:** “Esta página resume capacidades: fincas, evaluación técnica, pagos y trazabilidad por rol.”
- **Acción:** Home/Producto, scroll corto.
- **Transición:** “Entramos al sistema por autenticación.”
- **Objetivo:** mapa funcional.
- **Valor:** hilo narrativo.
- **Pregunta:** “¿Qué roles maneja?”
- **Respuesta:** “Administrador, propietario e ingeniero forestal.”

### Bloque 3 (4:00–8:00) — Login y control por rol
- **Narración:** “Iniciaremos sesión y verificaremos redirección por rol.”
- **Acción:** login admin, ver dashboard, logout; login ingeniero/logout; login dueño.
- **Transición:** “Con el dueño activo, registramos una finca.”
- **Objetivo:** seguridad y acceso.
- **Valor:** control de permisos.
- **Pregunta:** “¿Qué pasa si credenciales fallan?”
- **Respuesta:** “Se bloquea acceso y se devuelve validación controlada.”

### Bloque 4 (8:00–14:00) — Registro de finca (dueño)
- **Narración:** “Vamos a registrar una finca con datos generales, ubicación geográfica y atributos ambientales.”
- **Acción:** Fincas/RegistrarFinca, pin en mapa, completar campos, subir 1 foto, guardar.
- **Transición:** “Ahora vemos cómo esta finca entra al flujo técnico.”
- **Objetivo:** captura operativa.
- **Valor:** trazabilidad desde origen.
- **Pregunta:** “¿Valida que sea Costa Rica?”
- **Respuesta:** “Sí, el mapa y geocodificación validan ubicación en CR.”

### Bloque 5 (14:00–21:00) — Cola y evaluación técnica (ingeniero)
- **Narración:** “El ingeniero visualiza la bandeja pendiente, toma el caso y registra la visita.”
- **Acción:** login ingeniero -> FincasPendientes -> NuevaEvaluacion -> decisión + observaciones + ajustes + evidencia -> guardar.
- **Transición:** “Con evaluación calificada, vamos a pagos.”
- **Objetivo:** validación técnica.
- **Valor:** edición controlada de datos.
- **Pregunta:** “¿Puede modificar datos del dueño?”
- **Respuesta:** “Sí, mediante campos ajustados y con trazabilidad en auditoría.”

### Bloque 6 (21:00–29:00) — Plan de pagos y cálculo
- **Narración:** “El cálculo usa base por hectárea y ajustes configurables de vegetación, hídrico y pendiente, con tope administrativo.”
- **Acción:** generar/ver plan, asociar cuenta (dueño), aprobar activación (ingeniero), abrir detalle del plan (dueño/admin).
- **Transición:** “Ahora validamos reportes por cada rol.”
- **Objetivo:** reglas económicas.
- **Valor:** transparencia del cálculo.
- **Pregunta:** “¿Cómo aplican el tope?”
- **Respuesta:** “Se suma ajuste bruto y se recorta al tope de configuración vigente.”

### Bloque 7 (29:00–36:00) — Reportes por rol
- **Narración:** “Mostramos reportes específicos para cada actor.”
- **Acción:**
  1. Dueño: Reporte pagos (cuotas + ajustes + transacciones).
  2. Ingeniero: Evaluaciones mensual/anual.
  3. Admin: Pagos por ubicación + resumen actividad.
- **Transición:** “Cerramos con administración y auditoría.”
- **Objetivo:** análisis.
- **Valor:** toma de decisiones.
- **Pregunta:** “¿Se puede filtrar por ubicación?”
- **Respuesta:** “Sí, provincia, cantón y distrito en reportes administrativos.”

### Bloque 8 (36:00–41:00) — Administración y auditoría
- **Narración:** “Aquí gestionamos usuarios, roles/permisos, configuración de pagos y evidencia de auditoría.”
- **Acción:** GestionUsuarios, RolesPermisos, ParametrosPago, AuditoriaLogs.
- **Transición:** “Finalizamos con arquitectura y despliegue.”
- **Objetivo:** gobierno del sistema.
- **Valor:** control operativo.
- **Pregunta:** “¿Quién puede cambiar parámetros de pago?”
- **Respuesta:** “Solo administración con permisos específicos.”

### Bloque 9 (41:00–45:00) — Cierre técnico y preguntas
- **Narración:** “La solución está separada por capas: WebApp, WebAPI, AppCore, DataAccess y SQL. Tenemos CI activo y guía de publicación en Azure.”
- **Acción:** mostrar estructura del repo, `ci.yml`, y documento de deploy.
- **Objetivo:** defensa técnica.
- **Valor:** madurez de ingeniería.
- **Pregunta:** “¿Está publicado ya en Azure?”
- **Respuesta:** “Hay guía y pasos listos; despliegue productivo no está evidenciado en este repo.”

---

## SECCIÓN 5 — GUIÓN POR ROL

### A) Administrador
- **Pantallas:** Dashboard Administrador, GestiónUsuarios, ReasignarCliente, RolesPermisos, ParametrosPago, AuditoriaLogs, Reportes admin.
- **Flujo:**
  1. Ingresar.
  2. Mostrar usuarios y edición de rol.
  3. Mostrar configuración de pagos (precio base, tope, ajustes).
  4. Mostrar auditoría filtrada.
  5. Reporte pagos por ubicación y resumen de actividad.
- **Explicación literal:** “El administrador gobierna usuarios, permisos, parámetros de pago y trazabilidad.”
- **Preguntas probables:** “¿Tiene granularidad de permisos?”
- **Respuesta breve:** “Sí, además del rol, se evalúan políticas por permiso.”

### B) Dueño de finca
- **Pantallas:** Login, Dashboard Dueño, RegistrarFinca, MisFincas, DetalleFinca, CuentaBancaria, PlanesDueno, HistorialPagos, Reportes dueño.
- **Flujo:**
  1. Registrar finca con mapa y atributos.
  2. Adjuntar evidencia.
  3. Consultar estado.
  4. Asociar cuenta bancaria a plan.
  5. Revisar cuotas, detalle y transacciones.
- **Explicación literal:** “El dueño registra, consulta y da continuidad financiera de su expediente.”
- **Pregunta:** “¿Ve datos de otros dueños?”
- **Respuesta:** “No. Solo consulta recursos ligados a su identidad autenticada.”

### C) Ingeniero forestal
- **Pantallas:** Dashboard Ingeniero, FincasPendientes, NuevaEvaluacion, DetalleEvaluacion, HistorialEvaluaciones, reportes ingeniero.
- **Flujo:**
  1. Tomar evaluación pendiente.
  2. Registrar visita y decisión.
  3. Ajustar variables técnicas cuando aplica.
  4. Adjuntar evidencia técnica.
  5. Aprobar activación final de plan.
- **Explicación literal:** “El ingeniero valida técnicamente la finca y habilita continuidad del pago.”
- **Pregunta:** “¿Puede alterar sin trazabilidad?”
- **Respuesta:** “No. Los cambios técnicos generan registro auditable.”

---

## SECCIÓN 6 — CHECKLIST OPERATIVO PRE-DEMO

### Identidades y acceso
- [ ] Usuario admin operativo (ejemplo: `admin@psa.local`).
- [ ] Usuario ingeniero operativo (ejemplo: `ingeniero01@psa.local`).
- [ ] Usuario dueño operativo (ejemplo: `dueno01@psa.local`).
- [ ] Contraseñas verificadas en ambiente de demo.
- [ ] Sesión limpia de navegador antes de iniciar.

### Datos demo
- [ ] Al menos 1 finca pendiente de evaluación.
- [ ] Al menos 1 finca calificada con plan generado.
- [ ] Al menos 1 cuenta bancaria en estado Validada.
- [ ] Al menos 1 plan en estado PendienteDatosBancarios/PendienteAprobacionFinal/Activo.
- [ ] Evidencias de imagen/PDF listas en carpeta local.

### Flujo funcional
- [ ] Registro finca probado (mapa + guardado).
- [ ] Evaluación técnica probada (guardar decisión).
- [ ] Asociación de cuenta a plan probada.
- [ ] Aprobación final por ingeniero probada.
- [ ] Reportes por rol con datos visibles.
- [ ] Auditoría con eventos recientes.

### Infra y enlaces
- [ ] WebApp levantada.
- [ ] WebAPI levantada.
- [ ] DB con scripts aplicados (base + seeds + roles/permisos).
- [ ] Link repo Azure DevOps/GitHub disponible.
- [ ] Evidencia CI disponible (`ci.yml`).
- [ ] Documento de despliegue Azure listo para mostrar.
- [ ] (Opcional) Swagger accesible.

### Equipo de presentación
- [ ] Navegador único y resolución fija.
- [ ] Internet estable (por mapa/geocoding).
- [ ] Orden de presentadores practicado con traspaso en cualquier minuto.
- [ ] Cronómetro visible para cumplir 45 min.

---

## SECCIÓN 7 — HALLAZGOS COSMÉTICOS Y DE UX QUE AFECTAN LA PRESENTACIÓN

### Debe corregirse antes del demo
1. **Dependencia visual de servicios externos** en mapas/imágenes; si falla internet la experiencia se degrada.
2. **Inconsistencia textual de estados y etiquetas** (por ejemplo variaciones “No Califica” / “No califica”, estados largos con guion especial).
3. **Mensajes de error en modales administrativos** con estilos `alert` Bootstrap mezclados con estilo propio del sistema.
4. **Flujos con demasiadas acciones en una sola pantalla** (Planes dueño: detalle + asociación de cuenta), puede confundir en vivo.

### Puede tolerarse si se explica
1. Uso de componentes visuales tipo `alert` (no son alertas nativas JS bloqueantes).
2. Tarjetas y tablas con estilos diferentes entre módulos (deuda de uniformidad visual).
3. Vistas antiguas/duplicadas presentes en repo (no afectan demo si se usa la ruta principal).

---

## SECCIÓN 8 — PREGUNTAS TÉCNICAS PROBABLES Y RESPUESTAS CORTAS

1. **¿Cómo está dividida la arquitectura?**  
   WebApp MVC (UI), WebAPI (endpoints + auth), AppCore (reglas), DataAccess (DAO SQL), SQL Server (tablas/SP/vistas).

2. **¿Cómo controlan acceso por rol y permiso?**  
   Se combina `Authorize(Roles=...)` con políticas por permiso (`claim perm`).

3. **¿Dónde viven las reglas de pago?**  
   En AppCore (`PaymentCalculationService`) y configuración vigente en DB (`ConfiguracionesPago` + detalle).

4. **¿Aplican tope de ajuste?**  
   Sí. El cálculo recorta el porcentaje bruto al tope configurado; además el admin valida tope <= 40.

5. **¿El ingeniero modifica la finca final?**  
   Sí, mediante campos ajustados en evaluación; la DAO actualiza la finca y conserva trazabilidad.

6. **¿Cómo manejan errores?**  
   Middleware de excepciones en API y manejo controlado por `try/catch` en controladores críticos.

7. **¿Cómo se audita?**  
   Eventos a `AuditoriaLog` desde operaciones sensibles: pagos, evaluaciones, administración.

8. **¿Qué evidencia de CI/CD tienen?**  
   Workflow CI con restore/build/test y documento con pasos de publicación manual a Azure.

9. **¿Está desplegado en Azure hoy?**  
   No hay evidencia de despliegue productivo ejecutado en este entorno; sí están los pasos y requisitos.

10. **¿Cómo garantizan separación de responsabilidades?**  
   WebApp no toca DB directa; consume API. API delega negocio a AppCore y persistencia a DataAccess.

---

## SECCIÓN 9 — PLAN DE CONTINGENCIA

### Si falla el mapa/geocoding en registro de finca
- **Cómo continuar:** abrir `MisFincas` y usar finca semilla ya registrada.
- **Evidencia a mostrar:** detalle de finca con ubicación y atributos cargados.
- **Qué decir literal:** “La validación geográfica depende del servicio externo; para continuidad mostramos un caso registrado en base.”
- **Cómo retomar:** pasar directo a evaluación técnica.

### Si falla envío de correo en recuperación de contraseña
- **Cómo continuar:** demostrar solicitud y validación de token en interfaz sin depender del inbox.
- **Evidencia:** endpoints y flujo de token en UI.
- **Qué decir:** “La lógica de recuperación está implementada; el correo depende de SMTP del ambiente.”
- **Retomar:** volver a login con cuenta demo preparada.

### Si falla carga de evidencia
- **Cómo continuar:** guardar evaluación/finca sin adjunto.
- **Evidencia:** mensaje de guardado exitoso y estado actualizado.
- **Qué decir:** “La evidencia es complementaria; el flujo principal de negocio continúa.”
- **Retomar:** continuar a pagos.

### Si falla generación de plan en vivo
- **Cómo continuar:** abrir plan semilla existente en detalle.
- **Evidencia:** desglose completo de cálculo y cuotas.
- **Qué decir:** “Mostramos un plan generado previamente con la misma lógica para evitar romper el hilo.”
- **Retomar:** continuar con asociación de cuenta/reportes.

### Si falla reportería por filtro puntual
- **Cómo continuar:** limpiar filtros y mostrar reporte base.
- **Evidencia:** datos cargados + columnas esperadas.
- **Qué decir:** “El endpoint está disponible; usamos vista base para continuidad.”
- **Retomar:** pasar a bloque técnico final.

### Si preguntan por Azure productivo y no está listo
- **Cómo continuar:** mostrar `ci.yml` y guía de despliegue.
- **Evidencia:** pipeline y documento en repo.
- **Qué decir:** “Tenemos CI ejecutable y despliegue manual documentado; publicación productiva queda como siguiente paso operativo.”
- **Retomar:** cierre de arquitectura y Q&A.

---

## Dataset demo recomendado (mínimo)

### Usuarios sugeridos
- Administrador: `admin@psa.local`
- Ingeniero: `ingeniero01@psa.local`
- Dueño: `dueno01@psa.local`

### Finca demo sugerida (nueva)
- Nombre: `Finca Demo Exposición 2026`
- Ubicación: San José (pin dentro de CR)
- Hectáreas: `14.50`
- Pendiente: `Inclinada`
- Ríos/quebradas: `Sí`
- Nacientes: `Sí` (cantidad `2`)
- Vegetación: `Bosque secundario`
- Uso suelo: `Conservación`

### Escenario de pago demo
- Precio base hectárea: `₡25,000`
- Ajustes visibles: vegetación + hídrico + pendiente
- Tope: usar configuración activa (ej. 30%) y explicar límite institucional 40%.

