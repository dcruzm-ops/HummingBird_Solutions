# Orden de ejecución de scripts SQL

Ejecutar en este orden (rutas actuales dentro de `BaseDatos`):

1. `Tablas/01_creacion_bd_y_tablas.sql`
2. `Seeds/01_insercion_datos_semilla_extensos.sql`
3. `Views/01_creacion_vistas_reportes.sql`
4. `StoredProcedures/01_creacion_stored_procedures.sql`
5. `Triggers/01_creacion_triggers.sql`
6. `Seeds/02_admin_config_pagos_y_roles_seed.sql`
7. `StoredProcedures/02_admin_y_perfil_sprocs_consolidado.sql`
8. `Alters/01_roles_permisos_bootstrap_y_normalizacion.sql` *(recomendado para bootstrap/normalización de roles y permisos)*
9. `Alters/02_incr
ementales_pagos_seguridad_cobertura.sql` *(ajustes incrementales de pagos, seguridad, cobertura y snapshots)*

> Nota: Todos los scripts `.sql` quedaron consolidados dentro de `BaseDatos`.
> Los scripts de administración/roles soportan las variantes de tabla
> `dbo.RolesPermisos` y `dbo.RolPermisos`.