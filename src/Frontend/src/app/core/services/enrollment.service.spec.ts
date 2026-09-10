import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { EnrollmentService } from './enrollment.service';

describe('EnrollmentService', () => {
  let service: EnrollmentService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(EnrollmentService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the requester studentId when loading classmates', () => {
    service.classmates('course-1', 'student-9', 1, 20).subscribe(res => {
      expect(res.total).toBe(2);
    });

    const req = http.expectOne(r =>
      r.url === 'http://localhost:5000/api/enrollments/courses/course-1/classmates' &&
      r.params.get('studentId') === 'student-9' &&
      r.params.get('page') === '1');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [{ studentId: 'x', fullName: 'X' }], total: 2 });
  });

  it('loads only the requester courses', () => {
    service.myCourses('student-9').subscribe(res => {
      expect(res.length).toBe(3);
    });

    const req = http.expectOne('http://localhost:5000/api/students/student-9/courses');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'a' }, { id: 'b' }, { id: 'c' }]);
  });
});
