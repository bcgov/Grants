import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';

import { SidebarComponent } from './sidebar.component';

describe('SidebarComponent', () => {
  let component: SidebarComponent;
  let fixture: ComponentFixture<SidebarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SidebarComponent, RouterTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(SidebarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('defaults collapsed to false', () => {
    expect(component.collapsed).toBeFalse();
  });

  describe('toggleCollapse', () => {
    it('emits collapsedChange with the opposite value', () => {
      let emitted: boolean | undefined;
      component.collapsedChange.subscribe((val: boolean) => {
        emitted = val;
      });

      component.collapsed = false;
      component.toggleCollapse();

      expect(emitted).toBeTrue();
    });

    it('emits false when currently collapsed', () => {
      let emitted: boolean | undefined;
      component.collapsedChange.subscribe((val: boolean) => {
        emitted = val;
      });

      component.collapsed = true;
      component.toggleCollapse();

      expect(emitted).toBeFalse();
    });
  });

  describe('isCollapsed host binding', () => {
    it('returns the value of the collapsed input', () => {
      component.collapsed = true;
      expect(component.isCollapsed).toBeTrue();

      component.collapsed = false;
      expect(component.isCollapsed).toBeFalse();
    });
  });
});
