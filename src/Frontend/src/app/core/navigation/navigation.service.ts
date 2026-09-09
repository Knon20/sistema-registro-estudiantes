import { Injectable, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

/**
 * True "back" navigation: returns to the previous in-app route.
 * Falls back to /students on deep links (no history).
 */
@Injectable({ providedIn: 'root' })
export class NavigationService {
  private router = inject(Router);
  private previousUrl: string | null = null;
  private currentUrl: string | null = null;

  constructor() {
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(e => {
        this.previousUrl = this.currentUrl;
        this.currentUrl = e.urlAfterRedirects;
      });
  }

  back(fallback = '/students'): void {
    if (this.previousUrl && this.previousUrl !== this.currentUrl)
      this.router.navigateByUrl(this.previousUrl);
    else
      this.router.navigateByUrl(fallback);
  }
}
