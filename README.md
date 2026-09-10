# Sistema de Registro de Estudiantes

Aplicación web para el registro de estudiantes y su inscripción a materias (3 materias, 9 créditos, profesores diferentes), con .NET 8 + Angular 17 + MySQL + Docker.

## Arquitectura

Clean Architecture / Hexagonal por capas: `Domain`, `Application`, `Infrastructure`, `API` + `Frontend` por features. Ver [docs/architecture.md](docs/architecture.md).

## Tecnologías

- Backend: .NET 8, ASP.NET Core Web API, EF Core (Pomelo), MySQL 8, Swagger
- Tests: xUnit, Moq, FluentAssertions
- Frontend: Angular 17, Standalone Components, Reactive Forms, HttpClient
- Infra: Docker, docker-compose

## Requisitos previos

- .NET 8 SDK
- Node 20+
- Docker Desktop (para `docker compose up`)
- MySQL local opcional, p. ej. MySQL Workbench (solo si corres sin Docker)

## Configuración de base de datos

La API aplica migraciones y seed automáticamente al arrancar. Manualmente:

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

Scripts: `database/init.sql` (schema idempotente), `database/seed.sql` (5 profesores, 10 materias, 2 por profesor).

Connection string (`src/API/appsettings.json` o env `ConnectionStrings__Default`):

```text
Server=mysql;Database=StudentDb;User=root;Password=Str0ng_Passw0rd!;
```

## Ejecución del backend

```bash
dotnet build
dotnet run --project src/API
# Swagger: http://localhost:5000/swagger (puerto según launchSettings; en Docker :5000)
```

## Ejecución del frontend

```bash
cd src/Frontend
npm install
npm start          # http://localhost:4200
ng build           # build producción -> dist/frontend/browser
```

El frontend apunta a `http://localhost:5000/api` (ver `src/Frontend/src/environments/`).

## Ejecución mediante Docker

```bash
docker compose up --build
# Frontend: http://localhost:4200
# API:      http://localhost:5000/swagger
# MySQL:    localhost:3306 (root / Str0ng_Passw0rd!)
```

## Ejecución de pruebas

```bash
dotnet test
```

69 pruebas verdes: 47 unitarias backend (13 dominio + 34 aplicación, xUnit + Moq + FluentAssertions), 11 de integración API (WebApplicationFactory + MySQL real vía Testcontainers) y 11 de frontend (Karma/Jasmine).

## Endpoints principales

```text
POST   /api/students[?forceCreate=true]
GET    /api/students?page=1&pageSize=20          -> {items, total}
GET    /api/students/{id}
PUT    /api/students/{id}
DELETE /api/students/{id}                        (soft delete)
POST   /api/students/{id}/restore                (reactiva eliminado)
GET    /api/students/{id}/courses                (solo sus materias)

GET    /api/programs|professors|courses?page=&pageSize=   -> {items, total}

POST   /api/enrollments                        {studentId, period, courseIds[3]}
GET    /api/enrollments/student/{id}?period=
PUT    /api/enrollments/student/{id}?period=   {courseIds[3]}
GET    /api/enrollments/courses/{courseId}/classmates?studentId=&page=&pageSize=   -> solo [{studentId, fullName}], 403 si no inscrito
```

## Salud y CI

```text
GET    /health   -> 200 si API + MySQL responden
```

CI en GitHub Actions (`.github/workflows/ci.yml`): `dotnet build/test` (incluye integración con MySQL vía Testcontainers) + `ng test` + `ng build`.

Errores de negocio: `{ "code": "COURSE_PROFESSOR_CONFLICT", "message": "..." }` con el HTTP apropiado (400/404/409/422).

## Decisiones arquitectónicas

- `Enrollment` como agregado rico: las invariantes viven en el dominio, no en controllers.
- Sin MediatR/AutoMapper: mapeo manual y servicios delgados (evita sobreingeniería).
- Índice único `(StudentId, Period)` + `SaveChanges` transaccional para concurrencia.
- Privacidad por diseño: compañeros solo expone nombres y exige estar inscrito (403 si no).

## Mejoras consideradas (fuera del alcance de la prueba)

Se evaluaron y se descartaron conscientemente por no estar en el RQ (YAGNI). El diseño las soporta sin reescribir el dominio:

- **Login JWT + refresh tokens y roles (estudiante/admin):** sin login porque el RQ no lo pide; la autorización contextual (`studentId` inscrito) cubre la privacidad exigida. Punto de entrada natural: middleware JWT + `[Authorize]` en controllers, sin tocar `Domain`.
- **Reportes de ocupación por materia:** queries de lectura nuevas sobre `EnrollmentCourses`; no requieren cambios de modelo.
- **Caché distribuida (Redis) para catálogo:** el catálogo cambia poco; hoy va directo a MySQL con paginación (suficiente a esta escala).
- **Rate limiting y throttling:** ASP.NET tiene `AddRateLimiter`; no se exigía protección contra abuso.
- **Observabilidad (Serilog/OpenTelemetry + dashboards):** hoy hay logs estándar + `/health`; tracing distribuido sería el siguiente paso en producción.
- **Notificaciones por email al inscribirse:** outbox + worker; fuera del RQ.
- **E2E con Playwright/Cypress:** hay 69 pruebas (unidad + integración + frontend); el flujo crítico ya está cubierto, E2E de navegador sería el complemento.
- **Manifiestos Kubernetes:** el `docker-compose` + `/health` cubre el despliegue pedido; K8s es el siguiente escalón.
- **i18n y auditoría visible (quién eliminó/restauró):** `DeletedAt` ya preserva el rastro en BD; UI de auditoría no pedida.

## Estructura

```text
/src
  /Domain /Application /Infrastructure /API /Frontend
/tests
  /Domain.Tests /Application.Tests
/docs
/database (init.sql, seed.sql)
docker-compose.yml
```
