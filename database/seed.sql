-- Seed data mirror of Domain.ReferenceData.CatalogSeed (single source of truth).
-- Keep in sync with src/Domain/ReferenceData/CatalogSeed.cs.
-- 2 programs, 5 professors, 10 courses (2 per professor, all 3 credits).
-- Run after database/init.sql, or rely on API auto-seed on startup.

INSERT IGNORE INTO `Programs` (`Id`, `Name`, `Code`) VALUES
    ('11111111-1111-1111-1111-111111111111', 'Ingeniería de Sistemas', 'IS-01'),
    ('22222222-2222-2222-2222-222222222222', 'Administración de Empresas', 'AD-02');

INSERT IGNORE INTO `Professors` (`Id`, `FullName`, `Email`) VALUES
    ('20000000-0000-0000-0000-000000000001', 'Ana María Torres', 'ana.torres@uni.edu'),
    ('20000000-0000-0000-0000-000000000002', 'Carlos Ruiz', 'carlos.ruiz@uni.edu'),
    ('20000000-0000-0000-0000-000000000003', 'Jorge Ramírez', 'jorge.ramirez@uni.edu'),
    ('20000000-0000-0000-0000-000000000004', 'Lucía Fernández', 'lucia.fernandez@uni.edu'),
    ('20000000-0000-0000-0000-000000000005', 'Sofía Herrera', 'sofia.herrera@uni.edu');

INSERT IGNORE INTO `Courses` (`Id`, `Name`, `Code`, `Credits`, `ProfessorId`) VALUES
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
