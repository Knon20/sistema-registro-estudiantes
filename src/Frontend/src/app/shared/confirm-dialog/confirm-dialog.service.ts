import { Injectable, signal } from '@angular/core';

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  /** Optional third action (e.g. "Crear nuevo"). Shows an extra button. */
  altText?: string;
  /** Red (danger) or indigo (brand) accent for the confirm button. */
  tone?: 'danger' | 'brand';
  /** Info-only modal: hides the cancel button. */
  hideCancel?: boolean;
}

export type DialogResult = 'confirm' | 'alt' | 'cancel';

/**
 * Modal state. Usage:
 *   const ok = await dialog.open({ title, message });                 // confirm/cancel
 *   const choice = await dialog.openThree({ ..., altText: '...' });   // + third action
 * Renders through <app-confirm-dialog /> (mounted once in AppComponent).
 */
@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  state = signal<ConfirmOptions | null>(null);
  private resolver: ((value: DialogResult) => void) | null = null;

  open(opts: ConfirmOptions): Promise<boolean> {
    return this.openThree(opts).then(r => r === 'confirm');
  }

  openThree(opts: ConfirmOptions): Promise<DialogResult> {
    this.state.set({ confirmText: 'Confirmar', cancelText: 'Cancelar', tone: 'danger', ...opts });
    return new Promise<DialogResult>((resolve) => { this.resolver = resolve; });
  }

  resolve(value: DialogResult): void {
    this.state.set(null);
    this.resolver?.(value);
    this.resolver = null;
  }
}
