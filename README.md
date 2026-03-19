# 📌 Estrategia de trabajo del repositorio

## 🎯 Objetivo
Definir las reglas de trabajo del repositorio para asegurar consistencia entre los 4 integrantes del equipo.

---

## 📂 Alcance

- Documentar estrategia de ramas:
  - `main` como rama estable
  - `develop` como rama de integración
  - ramas personales por integrante
- Definir convención de nombres para ramas, commits y pull requests
- Definir regla de integración hacia `develop`
- Definir checklist mínimo de revisión antes de aprobar PRs

---

## 🌿 Estrategia de ramas

### 🔹 main
- Rama estable (producción)
- Solo contiene código probado y aprobado
- No se permite hacer push directo

### 🔹 develop
- Rama de integración
- Aquí se unen todas las funcionalidades antes de pasar a `main`

### 🔹 Ramas personales
- Cada integrante trabaja en su propia rama
- Se crean a partir de `develop`

Ejemplo:
```bash
git checkout -b feature/login-jared develop
