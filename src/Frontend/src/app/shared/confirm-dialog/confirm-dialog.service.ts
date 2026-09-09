import { Injectable, signal } from '@angular/core';

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
}

/**
 * Futuristic confirm dialog state. Usage:
 *   const ok = await dialog.open({ title, message });
 * Renders through <app-confirm-dialog /> (mounted once in AppComponent).
 */
@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  state = signal<ConfirmOptions | null>(null);
  private resolver: ((value: boolean) => void) | null = null;

  open(opts: ConfirmOptions): Promise<boolean> {
    this.state.set({ confirmText: 'Confirmar', cancelText: 'Cancelar', ...opts });
    return new Promise<boolean>((resolve) => { this.resolver = resolve; });
  }

  resolve(value: boolean): void {
    this.state.set(null);
    this.resolver?.(value);
    this.resolver = null;
  }
}
