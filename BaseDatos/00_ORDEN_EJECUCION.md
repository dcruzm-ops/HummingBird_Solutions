# Orden de ejecución de scripts SQL

Ejecutar en este orden:

1. `01_creacion_bd_y_tablas.sql`
2. `02_insercion_datos_semilla_extensos.sql`
3. `03_creacion_vistas_reportes.sql`
4. `04_creacion_stored_procedures.sql`
5. `05_creacion_triggers.sql`
6. `06_admin_roles_permisos_sprocs.sql`
7. `07_admin_config_pagos_y_roles_seed.sql`
8. `08_admin_roles_permisos_bootstrap.sql` *(opcional para reforzar compatibilidad en ambientes existentes)*

> Nota: los scripts de administración (06-08) soportan las variantes de tabla
> `dbo.RolesPermisos` y `dbo.RolPermisos`.
