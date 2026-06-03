import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadingOverlayComponent } from './loading-overlay.component';

describe('LoadingOverlayComponent', () => {
  let component: LoadingOverlayComponent;
  let fixture: ComponentFixture<LoadingOverlayComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoadingOverlayComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(LoadingOverlayComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('defaults isVisible to false', () => {
    expect(component.isVisible).toBeFalse();
  });

  it('defaults message to "Loading..."', () => {
    expect(component.message).toBe('Loading...');
  });

  it('defaults showMessage to true', () => {
    expect(component.showMessage).toBeTrue();
  });

  it('defaults transparent to false', () => {
    expect(component.transparent).toBeFalse();
  });

  it('does not render overlay when isVisible is false', () => {
    component.isVisible = false;
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.loading-overlay-card')).toBeNull();
  });

  it('renders overlay when isVisible is true', () => {
    component.isVisible = true;
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.loading-overlay-card')).not.toBeNull();
  });

  it('displays the message text when showMessage is true and isVisible is true', () => {
    component.isVisible = true;
    component.message = 'Please wait...';
    component.showMessage = true;
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.loading-text')?.textContent?.trim()).toBe('Please wait...');
  });

  it('does not display message text when showMessage is false', () => {
    component.isVisible = true;
    component.showMessage = false;
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.loading-text')).toBeNull();
  });

  it('applies transparent class when transparent is true', () => {
    component.isVisible = true;
    component.transparent = true;
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.loading-overlay-card.transparent')).not.toBeNull();
  });
});
