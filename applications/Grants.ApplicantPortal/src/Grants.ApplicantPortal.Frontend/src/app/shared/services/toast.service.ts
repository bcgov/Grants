import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: number;
  message: string;
  type: ToastType;
  duration: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  private readonly toastSubject = new Subject<Toast>();
  private readonly dismissSubject = new Subject<number>();

  readonly toast$ = this.toastSubject.asObservable();
  readonly dismiss$ = this.dismissSubject.asObservable();

  success(message: string, duration = 4000): void {
    this.show(message, 'success', duration);
  }

  error(message: string, duration = 6000): void {
    this.show(message, 'error', duration);
  }

  warning(message: string, duration = 5000): void {
    this.show(message, 'warning', duration);
  }

  info(message: string, duration = 4000): void {
    this.show(message, 'info', duration);
  }

  dismiss(id: number): void {
    this.dismissSubject.next(id);
  }

  private show(message: string, type: ToastType, duration: number): void {
    this.toastSubject.next({ id: this.nextId++, message, type, duration });
  }
}
