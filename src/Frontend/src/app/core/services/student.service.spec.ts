import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { StudentService } from './student.service';

describe('StudentService', () => {
  let service: StudentService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(StudentService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists students with pagination params', () => {
    service.list(2, 10).subscribe(res => {
      expect(res.total).toBe(25);
      expect(res.items.length).toBe(2);
    });

    const req = http.expectOne(r =>
      r.url === 'http://localhost:5000/api/students' &&
      r.params.get('page') === '2' &&
      r.params.get('pageSize') === '10');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [{ id: 'a', fullName: 'A' }, { id: 'b', fullName: 'B' }], total: 25 });
  });

  it('creates with forceCreate flag', () => {
    service.create({ fullName: 'Ana Gil', email: 'a@uni.edu', documentId: '1', programId: 'p' }, true)
      .subscribe();

    const req = http.expectOne(r =>
      r.url === 'http://localhost:5000/api/students' &&
      r.params.get('forceCreate') === 'true');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'new-id' });
  });

  it('restores via the restore endpoint', () => {
    service.restore('abc').subscribe(res => expect(res.id).toBe('abc'));

    const req = http.expectOne('http://localhost:5000/api/students/abc/restore');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'abc' });
  });
});
