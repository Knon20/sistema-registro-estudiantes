import { Component, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfirmDialogService } from './confirm-dialog.service';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="dialog.state() as opts" class="overlay" (click)="onBackdrop($event)">
      <div class="modal" role="alertdialog" aria-modal="true" [attr.aria-label]="opts.title">
        <div class="glow-ring"></div>
        <div class="icon">
          <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>
        </div>
        <h3>{{ opts.title }}</h3>
        <p class="muted">{{ opts.message }}</p>
        <div class="actions" [class.three]="opts.altText">
          <button *ngIf="!opts.hideCancel" class="btn" (click)="dialog.resolve('cancel')">{{ opts.cancelText }}</button>
          <button *ngIf="opts.altText" class="btn" (click)="dialog.resolve('alt')">{{ opts.altText }}</button>
          <button class="btn" [class.danger-glow]="opts.tone !== 'brand'" [class.primary-glow]="opts.tone === 'brand'" (click)="dialog.resolve('confirm')">{{ opts.confirmText }}</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .overlay {
      position: fixed; inset: 0; z-index: 100;
      display: grid; place-items: center; padding: 1rem;
      background: rgba(4, 8, 20, .6);
      backdrop-filter: blur(8px);
      -webkit-backdrop-filter: blur(8px);
      animation: fade-in .18s ease;
    }
    .modal {
      position: relative;
      width: min(100%, 420px);
      background: var(--surface);
      backdrop-filter: blur(20px);
      -webkit-backdrop-filter: blur(20px);
      border: 1px solid var(--border);
      border-radius: 20px;
      box-shadow: var(--shadow);
      padding: 1.8rem 1.6rem 1.5rem;
      text-align: center;
      overflow: hidden;
      animation: pop-in .25s cubic-bezier(.2, 1.4, .4, 1);
    }
    .glow-ring {
      position: absolute; inset: -60px -60px auto -60px; height: 170px;
      background: radial-gradient(closest-side, rgba(178, 38, 30, .35), transparent);
      pointer-events: none;
    }
    .icon {
      display: inline-grid; place-items: center;
      width: 56px; height: 56px; border-radius: 18px; margin-bottom: .6rem;
      color: #ff8a80;
      background: linear-gradient(135deg, rgba(178, 38, 30, .35), rgba(79, 70, 229, .3));
      border: 1px solid var(--danger-border);
      box-shadow: 0 0 24px rgba(178, 38, 30, .45);
    }
    h3 { margin: .2rem 0 .5rem; font-size: 1.2rem; }
    p { margin: 0 0 1.3rem; font-size: .92rem; line-height: 1.5; }
    .actions { display: flex; gap: .6rem; }
    .actions .btn { flex: 1; justify-content: center; }
    .danger-glow {
      background: linear-gradient(92deg, #d32f2f, #7b1fa2);
      border: none; color: #fff;
      box-shadow: 0 4px 20px rgba(211, 47, 47, .5);
    }
    .primary-glow {
      background: linear-gradient(92deg, var(--primary), var(--accent));
      border: none; color: #fff;
      box-shadow: 0 4px 20px rgba(79, 70, 229, .5);
    }
    @keyframes fade-in { from { opacity: 0; } }
    @keyframes pop-in { from { opacity: 0; transform: scale(.92) translateY(10px); } }
    @media (max-width: 480px) {
      .modal { padding: 1.4rem 1.1rem 1.2rem; }
      .actions { flex-direction: column-reverse; }
    }
  `]
})
export class ConfirmDialogComponent {
  dialog = inject(ConfirmDialogService);

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.dialog.state()) this.dialog.resolve('cancel');
  }

  onBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.dialog.resolve('cancel');
  }
}
