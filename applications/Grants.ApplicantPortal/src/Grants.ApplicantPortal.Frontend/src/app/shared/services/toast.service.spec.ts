import { TestBed } from '@angular/core/testing';

import { ToastService, Toast } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ToastService],
    });

    service = TestBed.inject(ToastService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('success', () => {
    it('emits a success toast with the given message', (done) => {
      service.toast$.subscribe((toast: Toast) => {
        expect(toast.type).toBe('success');
        expect(toast.message).toBe('Operation successful');
        done();
      });

      service.success('Operation successful');
    });

    it('uses the default duration of 4000ms', (done) => {
      service.toast$.subscribe((toast: Toast) => {
        expect(toast.duration).toBe(4000);
        done();
      });

      service.success('Done');
    });

    it('accepts a custom duration', (done) => {
      service.toast$.subscribe((toast: Toast) => {
        expect(toast.duration).toBe(2000);
        done();
      });

      service.success('Done', 2000);
    });
  });

  describe('error', () => {
    it('emits an error toast with the given message', (done) => {
      service.toast$.subscribe((toast: Toast) => {
        expect(toast.type).toBe('error');
        expect(toast.message).toBe('Something went wrong');
        done();
      });

      service.error('Something went wrong');
    });

    it('uses the default duration of 6000ms', (done) => {
      service.toast$.subscribe((toast: Toast) => {
        expect(toast.duration).toBe(6000);
        done();
      });

      service.error('Error');
    });
  });

  describe('warning', () => {
    it('emits a warning toast', (done) => {
      service.toast$.subscribe((toast: Toast) => {
        expect(toast.type).toBe('warning');
        expect(toast.message).toBe('Watch out');
        done();
      });

      service.warning('Watch out');
    });

    it('uses the default duration of 5000ms', (done) => {
      service.toast$.subscribe((toast: Toast) => {
        expect(toast.duration).toBe(5000);
        done();
      });

      service.warning('Careful');
    });
  });

  describe('info', () => {
    it('emits an info toast', (done) => {
      service.toast$.subscribe((toast: Toast) => {
        expect(toast.type).toBe('info');
        expect(toast.message).toBe('FYI');
        done();
      });

      service.info('FYI');
    });

    it('uses the default duration of 4000ms', (done) => {
      service.toast$.subscribe((toast: Toast) => {
        expect(toast.duration).toBe(4000);
        done();
      });

      service.info('Info');
    });
  });

  describe('dismiss', () => {
    it('emits the id on dismiss$', (done) => {
      service.dismiss$.subscribe((id: number) => {
        expect(id).toBe(42);
        done();
      });

      service.dismiss(42);
    });
  });

  describe('id assignment', () => {
    it('assigns incrementing ids to toasts', (done) => {
      const ids: number[] = [];

      service.toast$.subscribe((toast: Toast) => {
        ids.push(toast.id);
        if (ids.length === 2) {
          expect(ids[1]).toBe(ids[0] + 1);
          done();
        }
      });

      service.success('First');
      service.success('Second');
    });
  });
});
