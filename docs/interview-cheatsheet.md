# Cheat Sheet — Entrevista (1 página)

## Pitch (30 seg)
Sistema de registro de estudiantes: .NET 8 + Angular 17 + MySQL. CRUD, inscripción a 3 materias (9 créditos, profesores distintos), compañeros con privacidad. Clean Architecture, 64 pruebas verdes, `docker compose up`.

## Arquitectura
Angular (features) → API delgada → Application (casos de uso + puertos) → Domain (agregado `Enrollment`, reglas) ← Infrastructure (EF/Pomelo + MySQL). Sin MediatR/AutoMapper a propósito (YAGNI).

## Datos duros
- Reglas en dominio, no en controllers: 3 materias exactas, sin repetidas, profesores distintos, 9 créditos.
- Errores negocio `{code, message}`: 400/403/404/409/422, sin stack traces.
- Soft delete (`DeletedAt`) + reactivación; re-inscribir reactiva. Paginación SQL, batch sin N+1, health `/health`, CI en Actions.

## Demo (2 min)
1. Swagger `:5000/swagger` → crear estudiante (201).
2. Inscripción válida (201, 9 créditos) → misma-profesor (422 `COURSE_PROFESSOR_CONFLICT`).
3. Compañeros: miembro ve solo nombres; outsider → 403.
4. Eliminar → re-crear → modal **Reactivar vs Crear nuevo**.

## Trade-offs (decir en voz alta)
- Unicidad entre activos a nivel app (MySQL sin índices parciales) → documentado.
- Raza en doble inscripción → la cierra el índice único `(StudentId, Period)` + transacción (409).
- Sin JWT (el RQ no pide login) → autorización contextual por inscripción.

## Preguntas
- ¿Sin CQRS? → Lectura/escritura no escalan distinto aquí; YAGNI.
- ¿Escala? → API stateless, paginación SQL, sin N+1, health checks.
- ¿Siguiente iteración? → Roles + reportes por materia, sin tocar el dominio.

**Autor:** Abraham Enrique Cañon Vasquez · github.com/Knon20/sistema-registro-estudiantes
