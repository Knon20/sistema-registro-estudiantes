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

32 pruebas: dominio (11) + aplicación (21) con xUnit + Moq + FluentAssertions.

## Endpoints principales

```text
POST   /api/students
GET    /api/students?page=1&pageSize=20          -> {items, total}
GET    /api/students/{id}
PUT    /api/students/{id}
DELETE /api/students/{id}

GET    /api/programs|professors|courses?page=&pageSize=   -> {items, total}

POST   /api/enrollments                        {studentId, period, courseIds[3]}
GET    /api/enrollments/student/{id}?period=
PUT    /api/enrollments/student/{id}?period=   {courseIds[3]}
GET    /api/enrollments/courses/{courseId}/classmates?page=&pageSize=   -> solo [{studentId, fullName}]
```

Errores de negocio: `{ "code": "COURSE_PROFESSOR_CONFLICT", "message": "..." }` con el HTTP apropiado (400/404/409/422).

## Decisiones arquitectónicas

- `Enrollment` como agregado rico: las invariantes viven en el dominio, no en controllers.
- Sin MediatR/AutoMapper: mapeo manual y servicios delgados (evita sobreingeniería).
- Índice único `(StudentId, Period)` + `SaveChanges` transaccional para concurrencia.
- Privacidad por diseño: compañeros solo expone nombres.

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
