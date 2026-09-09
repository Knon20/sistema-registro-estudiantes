import { HttpErrorResponse } from '@angular/common/http';
import { ApiError } from '../models';

/** Single place that extracts a user-friendly message from HTTP errors. */
export function apiMessage(error: unknown, fallback: string): string {
  const api = (error as HttpErrorResponse)?.error as ApiError | undefined;
  return api?.message ?? fallback;
}
