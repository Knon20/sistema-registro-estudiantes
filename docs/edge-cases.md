# Casos borde

| Escenario | Comportamiento esperado |
|-----------|------------------------|
| Registrar estudiante duplicado (email/documento activo) | 409 `STUDENT_DUPLICATED` |
| Re-registrar email/documento de un eliminado | 409 `STUDENT_DELETED_EXISTS` + datos del candidato → UI ofrece Reactivar (`POST /api/students/{id}/restore`) o Crear nuevo (`POST /api/students?forceCreate=true`, nuevo ID) |
| Inscribir más de 3 materias | 422 `ENROLLMENT_COURSE_COUNT_INVALID` |
| Inscribir menos de 3 materias | 422 `ENROLLMENT_COURSE_COUNT_INVALID` |
| Dos materias del mismo profesor | 422 `COURSE_PROFESSOR_CONFLICT` |
| Materia inexistente | 404 `COURSE_NOT_FOUND` |
| Misma materia dos veces | 422 `COURSE_DUPLICATED` |
| Modificar inscripción a combinación inválida | 422 según regla violada; la inscripción previa queda intacta |
| Eliminar estudiante con inscripción | 204; estudiante e inscripción se marcan eliminados (soft delete, auditoría preservada); desaparecen de listados y compañeros |
| Re-inscribir periodo con inscripción eliminada | 201; la inscripción eliminada se reactiva con la nueva selección (el índice único incluye borrados) |
| Solicitudes concurrentes (doble POST mismo estudiante+periodo) | Una gana (201), la otra 409 `ENROLLMENT_DUPLICATED` o `CONCURRENCY_CONFLICT` |
| Consultar estudiante inexistente | 404 `STUDENT_NOT_FOUND` |
| Compañeros de materia sin inscritos | 200 `[]` |
| Compañeros de materia inexistente | 404 `COURSE_NOT_FOUND` |
| Consultar compañeros sin estar inscrito | 403 `CLASSMATES_FORBIDDEN` |
| Consultar compañeros sin identificarse | 400 `VALIDATION_FAILED` |
| Inscripción inexistente (GET) | 404 `ENROLLMENT_NOT_FOUND` |

Los errores de negocio usan el formato `{ "code": "...", "message": "..." }` sin stack traces (middleware centralizado).
