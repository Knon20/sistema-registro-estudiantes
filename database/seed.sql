-- Seed data mirror of Domain.ReferenceData.CatalogSeed (single source of truth).
-- Keep in sync with src/Domain/ReferenceData/CatalogSeed.cs.
-- 2 programs, 5 professors, 10 courses (2 per professor, all 3 credits).

DECLARE @P_IS UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Programs WHERE Code = 'IS-01');

IF @P_IS IS NULL
BEGIN
    SET @P_IS = NEWID();
    INSERT INTO Programs (Id, Name, Code) VALUES (@P_IS, 'Ingeniería de Sistemas', 'IS-01');
END

IF NOT EXISTS (SELECT 1 FROM Professors)
BEGIN
    INSERT INTO Professors (Id, FullName, Email) VALUES
        ('20000000-0000-0000-0000-000000000001', 'Ana María Torres', 'ana.torres@uni.edu'),
        ('20000000-0000-0000-0000-000000000002', 'Carlos Ruiz', 'carlos.ruiz@uni.edu'),
        ('20000000-0000-0000-0000-000000000003', 'Lucía Fernández', 'lucia.fernandez@uni.edu'),
        ('20000000-0000-0000-0000-000000000004', 'Jorge Ramírez', 'jorge.ramirez@uni.edu'),
        ('20000000-0000-0000-0000-000000000005', 'Sofía Herrera', 'sofia.herrera@uni.edu');
END

IF NOT EXISTS (SELECT 1 FROM Courses)
BEGIN
    INSERT INTO Courses (Id, Name, Code, Credits, ProfessorId) VALUES
        ('30000000-0000-0000-0000-000000000001', 'Cálculo I', 'MAT-101', 3, '20000000-0000-0000-0000-000000000001'),
        ('30000000-0000-0000-0000-000000000002', 'Física I', 'FIS-102', 3, '20000000-0000-0000-0000-000000000001'),
        ('30000000-0000-0000-0000-000000000003', 'Programación I', 'SIS-103', 3, '20000000-0000-0000-0000-000000000002'),
        ('30000000-0000-0000-0000-000000000004', 'Bases de Datos', 'SIS-104', 3, '20000000-0000-0000-0000-000000000002'),
        ('30000000-0000-0000-0000-000000000005', 'Redes I', 'SIS-105', 3, '20000000-0000-0000-0000-000000000003'),
        ('30000000-0000-0000-0000-000000000006', 'Sistemas Operativos', 'SIS-106', 3, '20000000-0000-0000-0000-000000000003'),
        ('30000000-0000-0000-0000-000000000007', 'Inglés Técnico', 'HUM-107', 3, '20000000-0000-0000-0000-000000000004'),
        ('30000000-0000-0000-0000-000000000008', 'Ética Profesional', 'HUM-108', 3, '20000000-0000-0000-0000-000000000004'),
        ('30000000-0000-0000-0000-000000000009', 'Estadística', 'MAT-109', 3, '20000000-0000-0000-0000-000000000005'),
        ('30000000-0000-0000-0000-000000000010', 'Algoritmos', 'SIS-110', 3, '20000000-0000-0000-0000-000000000005');
END

-- Demo students (optional, for manual testing)
IF NOT EXISTS (SELECT 1 FROM Students WHERE Email = 'juan.perez@uni.edu')
BEGIN
    INSERT INTO Students (Id, FullName, Email, DocumentId, ProgramId, CreatedAt) VALUES
        (NEWID(), 'Juan Pérez', 'juan.perez@uni.edu', '1001001', @P_IS, SYSUTCDATETIME()),
        (NEWID(), 'María Gómez', 'maria.gomez@uni.edu', '1001002', @P_IS, SYSUTCDATETIME());
END
