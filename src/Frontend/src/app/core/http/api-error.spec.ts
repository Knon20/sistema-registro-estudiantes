import { HttpErrorResponse } from '@angular/common/http';
import { apiMessage } from './api-error';

describe('apiMessage', () => {
  it('extracts the backend message', () => {
    const err = new HttpErrorResponse({
      error: { code: 'STUDENT_DUPLICATED', message: 'A student with the same email already exists.' },
      status: 409,
    });
    expect(apiMessage(err, 'fallback')).toBe('A student with the same email already exists.');
  });

  it('falls back when there is no backend payload', () => {
    expect(apiMessage(new HttpErrorResponse({ status: 500 }), 'No se pudo guardar.'))
      .toBe('No se pudo guardar.');
    expect(apiMessage(null, 'fallback')).toBe('fallback');
    expect(apiMessage(undefined, 'fallback')).toBe('fallback');
  });

  it('falls back when the payload has no message', () => {
    const err = new HttpErrorResponse({ error: { code: 'X' }, status: 400 });
    expect(apiMessage(err, 'fallback')).toBe('fallback');
  });
});
