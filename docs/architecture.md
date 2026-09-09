# Arquitectura

## Estilo

Clean Architecture + Hexagonal (Ports & Adapters) + pragmático SOLID.

```text
Domain  ->  Application  ->  Infrastructure  ->  API
 (núcleo)    (casos uso)      (adaptadores)      (entrada)
```

- **Domain** (`src/Domain`): entidades (`Student`, `AcademicProgram`, `Professor`, `Course`, `Enrollment` + `EnrollmentCourse`), reglas (`EnrollmentRules`), excepciones (`DomainException` + códigos). Sin dependencias a EF, ASP.NET, Angular o BD.
- **Application** (`src/Application`): DTOs, puertos (`IStudentRepository`, `ICourseRepository`, `IEnrollmentRepository`, `IUnitOfWork`, ...), casos de uso (`StudentService`, `CatalogService`, `EnrollmentService`). Orquesta, no conoce SQL.
- **Infrastructure** (`src/Infrastructure`): `AppDbContext`, repositorios EF Core (Pomelo MySQL), `EfUnitOfWork`, seed. Único lugar que conoce MySQL.
- **API** (`src/API`): controllers delgados, middleware de errores, Swagger, DI, CORS.
- **Frontend** (`src/Frontend`): Angular 17 standalone, feature folders (`students`, `enrollment`, `consultas`), servicios (`StudentService`, `CatalogService`, `EnrollmentService`), Reactive Forms + HttpClient.

## Dependencias

```text
API -> Application -> Domain
API -> Infrastructure -> Application, Domain
Tests -> Domain / Application (con Moq, sin BD)
```

Inversión de dependencias: Application define puertos; Infrastructure los implementa; API los registra (`AddApplication`, `AddInfrastructure`).

## Agregado principal

`Enrollment` es agregado raíz. Protege invariantes en `Create`/`ReplaceCourses` vía `EnrollmentRules.ValidateSelection`. `StudentService` y `EnrollmentService` solo orquestan (cargar, validar existencia, delegar al agregado, guardar vía `IUnitOfWork`).

## Concurrencia

- Índice único `(StudentId, Period)` impide doble inscripción activa.
- `EfUnitOfWork.SaveChangesAsync` sincroniza `Enrollment.Items` <-> tabla `EnrollmentCourses` en una sola transacción (`SaveChanges` es atómico).
- Errores de concurrencia EF se traducen a `ConcurrencyException` -> HTTP 409 `CONCURRENCY_CONFLICT`.

## Decisiones

- MySQL 8 (requisito MySQL/SQL): imagen liviana, tooling simple (Workbench) y proveedor Pomelo maduro. Nada del test requiere algo exclusivo de SQL Server.
- Soft delete en `Student`/`Enrollment` (`IsDeleted` + filtro global): el borrado lógico conserva auditoría; la cascada a inscripciones se hace en código dentro de la misma transacción.
- Sin MediatR/AutoMapper: sobreingeniería para este tamaño; servicios + mapeo manual son más legibles.
- `EnrollmentCourse` como entidad con clave compuesta (no owned) para consultas eficientes por materia.
- Seed en `EnsureSeededAsync` + `database/seed.sql` con GUIDs fijos para pruebas manuales.
- Frontend previene combinaciones inválidas (UX), pero la regla real vive en el dominio (seguridad).

## Diagrama

```text
[Angular] --HTTP/JSON--> [API Controllers] --> [Application Services] --> [Domain Entities/Rules]
                              |                        |
                         Middleware/Swagger      Ports (interfaces)
                                                       |
                                              [EF Repositories + MySQL]
```

## Secuencia — crear inscripción

```text
UI -> POST /api/enrollments -> EnrollmentService.CreateAsync
  -> Students.GetById (404 si no existe)
  -> ValidateIds (3 ids, sin duplicados)
  -> Enrollments.GetByStudentAndPeriod (409 si ya existe)
  -> Courses.GetByIds (404 si falta alguna)
  -> Enrollment.Create (dominio valida count/duplicados/profesor/créditos)
  -> Enrollments.Add + UoW.Save (transacción)
  -> 201 EnrollmentDto
```
