import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Subject } from 'rxjs';

import { ToastComponent } from './toast.component';
import { ToastService, Toast, ToastType } from '../../services/toast.service';

function makeToast(partial: Partial<Toast> = {}): Toast {
  return {
    id: partial.id ?? 1,
    message: partial.message ?? 'Test message',
    type: (partial.type as ToastType) ?? 'success',
    duration: partial.duration ?? 4000,
  };
}

describe('ToastComponent', () => {
  let component: ToastComponent;
  let fixture: ComponentFixture<ToastComponent>;
  let toastSubject: Subject<Toast>;
  let dismissSubject: Subject<number>;
  let toastServiceSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    toastSubject = new Subject<Toast>();
    dismissSubject = new Subject<number>();

    toastServiceSpy = jasmine.createSpyObj<ToastService>('ToastService', ['success'], {
      toast$: toastSubject.asObservable(),
      dismiss$: dismissSubject.asObservable(),
    });

    await TestBed.configureTestingModule({
      imports: [ToastComponent],
      providers: [{ provide: ToastService, useValue: toastServiceSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(ToastComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('starts with an empty toasts list', () => {
    expect(component.toasts).toEqual([]);
  });

  describe('ngOnInit — toast subscription', () => {
    it('adds a toast to the list when toast$ emits', () => {
      const toast = makeToast({ id: 10, message: 'Hello' });
      toastSubject.next(toast);
      expect(component.toasts.length).toBe(1);
      expect(component.toasts[0].id).toBe(10);
    });

    it('auto-removes the toast after its duration', fakeAsync(() => {
      const toast = makeToast({ id: 20, duration: 1000 });
      toastSubject.next(toast);
      expect(component.toasts.length).toBe(1);

      tick(1000);
      expect(component.toasts.length).toBe(0);
    }));
  });

  describe('dismiss subscription', () => {
    it('removes a toast when dismiss$ emits its id', () => {
      toastSubject.next(makeToast({ id: 5 }));
      toastSubject.next(makeToast({ id: 6 }));
      expect(component.toasts.length).toBe(2);

      dismissSubject.next(5);
      expect(component.toasts.length).toBe(1);
      expect(component.toasts[0].id).toBe(6);
    });
  });

  describe('remove', () => {
    it('removes the toast from the list by id', () => {
      toastSubject.next(makeToast({ id: 99, duration: 0 }));
      expect(component.toasts.length).toBe(1);

      component.remove(99);
      expect(component.toasts.length).toBe(0);
    });

    it('does not throw when removing a non-existent id', () => {
      expect(() => component.remove(999)).not.toThrow();
    });
  });

  describe('getIconSrc', () => {
    it('returns correct icon src for success', () => {
      expect(component.getIconSrc('success')).toContain('success');
    });

    it('returns correct icon src for error', () => {
      expect(component.getIconSrc('error')).toContain('error');
    });

    it('returns correct icon src for warning', () => {
      expect(component.getIconSrc('warning')).toContain('warning');
    });
  });

  describe('getTitle', () => {
    it('returns "Success" for success type', () => {
      expect(component.getTitle('success')).toBe('Success');
    });

    it('returns "Error" for error type', () => {
      expect(component.getTitle('error')).toBe('Error');
    });

    it('returns "Warning" for warning type', () => {
      expect(component.getTitle('warning')).toBe('Warning');
    });

    it('returns "Info" for info type', () => {
      expect(component.getTitle('info')).toBe('Info');
    });
  });

  it('cleans up on destroy', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
