export interface ProgramDto { id: string; name: string; code: string; }
export interface ProfessorDto { id: string; fullName: string; email: string; }
export interface CourseDto { id: string; name: string; code: string; credits: number; professorId: string; professorName?: string | null; }
export interface StudentDto { id: string; fullName: string; email: string; documentId: string; programId: string; programName?: string | null; createdAt: string; }
export interface EnrollmentDto { id: string; studentId: string; studentName: string; period: string; totalCredits: number; courses: CourseDto[]; createdAt: string; updatedAt?: string | null; }
export interface ClassmateDto { studentId: string; fullName: string; }
export interface ApiError { code: string; message: string; }
