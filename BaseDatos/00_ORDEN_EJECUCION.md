# Orden de ejecución de scripts SQL

## Ruta SQL Server local / laboratorio (mantiene comportamiento actual)

Ejecutar en este orden dentro de `BaseDatos`:

1. `Tablas/01_creacion_bd_y_tablas.sql`
2. `Seeds/01_insercion_datos_semilla_extensos.sql`
3. `Views/01_creacion_vistas_reportes.sql`
4. `StoredProcedures/01_creacion_stored_procedures.sql`
5. `Triggers/01_creacion_triggers.sql`
6. `Seeds/02_admin_config_pagos_y_roles_seed.sql`
7. `StoredProcedures/02_admin_y_perfil_sprocs_consolidado.sql`
8. `Alters/01_roles_permisos_bootstrap_y_normalizacion.sql`
9. `Alters/02_incrementales_pagos_seguridad_cobertura.sql`

## Ruta Azure SQL Database (cloud-safe)

> Ejecutar conectado directamente a la **base de datos Azure SQL** destino.
> No ejecutar `CREATE DATABASE` ni `USE` en Azure SQL Query Editor.

1. `Azure/01_creacion_tablas_azure_safe.sql`
2. `Seeds/01_insercion_datos_semilla_extensos.sql`
3. `Azure/02_creacion_vistas_reportes_azure_safe.sql`
4. `StoredProcedures/01_creacion_stored_procedures.sql`
5. `Triggers/01_creacion_triggers.sql`
6. `Seeds/02_admin_config_pagos_y_roles_seed.sql`
7. `StoredProcedures/02_admin_y_perfil_sprocs_consolidado.sql`
8. `Alters/01_roles_permisos_bootstrap_y_normalizacion.sql`
9. `Alters/02_incrementales_pagos_seguridad_cobertura.sql`

## Notas

- `Azure/01_creacion_tablas_azure_safe.sql` elimina operaciones destructivas (`DROP`) y dependencias de cambio de base (`USE`).
- `Azure/01_creacion_tablas_azure_safe.sql` ahora es idempotente: crea solo objetos faltantes y permite re-ejecución segura en Azure SQL.
- Si aparece `Invalid object name 'dbo.Permisos'`, validar que primero corrió el script de tablas correspondiente al entorno (local o Azure).

- `Azure/02_creacion_vistas_reportes_azure_safe.sql` crea/actualiza vistas sin `USE` y valida que las tablas de pagos existan antes de compilar vistas.
