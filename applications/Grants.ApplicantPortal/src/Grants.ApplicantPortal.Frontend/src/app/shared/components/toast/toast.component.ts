import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { Toast, ToastType, ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast.component.html',
  styleUrls: ['./toast.component.scss'],
})
export class ToastComponent implements OnInit, OnDestroy {
  toasts: Toast[] = [];
  private readonly destroy$ = new Subject<void>();
  private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();

  private readonly iconMap: Record<ToastType, string> = {
    warning: 'images/icons/warning.svg',
    error: 'images/icons/error.svg',
    success: 'images/icons/success.svg',
    info: 'images/icons/warning.svg',
  };

  private readonly titleMap: Record<ToastType, string> = {
    success: 'Success',
    error: 'Error',
    warning: 'Warning',
    info: 'Info',
  };

  constructor(private readonly toastService: ToastService) {}

  getIconSrc(type: ToastType): string {
    return this.iconMap[type];
  }

  getTitle(type: ToastType): string {
    return this.titleMap[type];
  }

  ngOnInit(): void {
    this.toastService.toast$
      .pipe(takeUntil(this.destroy$))
      .subscribe((toast) => {
        this.toasts.push(toast);
        if (toast.duration > 0) {
          const timer = setTimeout(() => this.remove(toast.id), toast.duration);
          this.timers.set(toast.id, timer);
        }
      });

    this.toastService.dismiss$
      .pipe(takeUntil(this.destroy$))
      .subscribe((id) => this.remove(id));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.timers.forEach((t) => clearTimeout(t));
  }

  remove(id: number): void {
    const timer = this.timers.get(id);
    if (timer) {
      clearTimeout(timer);
      this.timers.delete(id);
    }
    this.toasts = this.toasts.filter((t) => t.id !== id);
  }
}
