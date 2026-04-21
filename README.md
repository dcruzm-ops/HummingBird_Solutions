# PSA Costa Rica

## Ejecución local rápida

1. Definir secretos/configuración fuera del repositorio:
   - `ConnectionStrings__PSAConnection`
   - `Jwt__Key` (**obligatoria**, si falta la API falla de forma controlada)
2. Restaurar/compilar:
   - `dotnet restore psa-costa-rica.slnx`
   - `dotnet build psa-costa-rica.slnx`
3. Ejecutar API/WebApp según perfil local.

## Seguridad de secretos JWT

- `appsettings*.json` contiene solo placeholder seguro (`set-via-env-or-user-secrets`).
- El valor real se debe inyectar con variable de entorno o User Secrets:

```bash
dotnet user-secrets set "Jwt:Key" "<clave-segura-larga>"
```

o

```bash
export Jwt__Key="<clave-segura-larga>"
```

## Evidencia técnica CI/CD y despliegue

Revisar `docs/deploy-azure-cicd.md` para:
- workflow CI incluido en repo,
- variables requeridas,
- pasos manuales para publicación en Azure,
- limitaciones reales no automatizadas en este entorno.
