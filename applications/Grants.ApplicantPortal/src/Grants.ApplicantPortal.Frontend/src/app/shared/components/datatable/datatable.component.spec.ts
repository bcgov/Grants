import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';

import { DatatableComponent } from './datatable.component';
import { TableSortService } from '../../services/table-sort.service';
import { DatatableConfig } from './datatable.models';

const BASIC_CONFIG: DatatableConfig = {
  tableId: 'test-table',
  defaultSortField: 'lastUpdated',
  columns: [
    { key: 'name', label: 'Name', sortable: true },
    { key: 'lastUpdated', label: 'Last Updated', sortable: true, type: 'date' },
  ],
};

describe('DatatableComponent', () => {
  let component: DatatableComponent;
  let fixture: ComponentFixture<DatatableComponent>;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [DatatableComponent],
      providers: [TableSortService],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(DatatableComponent);
    component = fixture.componentInstance;
    component.idSuffix = 'test';
    component.config = { ...BASIC_CONFIG };
    component.data = [];
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('throws when config is not provided', () => {
    const bare = TestBed.createComponent(DatatableComponent);
    bare.componentInstance.idSuffix = 'bare';
    bare.componentInstance.data = [];
    expect(() => bare.detectChanges()).toThrow();
  });

  describe('getDisplayData', () => {
    it('returns empty array when data is empty', () => {
      expect(component.getDisplayData()).toEqual([]);
    });

    it('returns all rows when count <= pageSize', () => {
      component.data = [
        { name: 'A', lastUpdated: '2024-01-01' },
        { name: 'B', lastUpdated: '2024-01-02' },
      ];
      component.config.pageSize = 10;
      component.ngOnChanges();
      expect(component.getDisplayData().length).toBe(2);
    });

    it('paginates when row count exceeds pageSize', () => {
      component.config.pageSize = 2;
      component.data = [
        { name: 'A', lastUpdated: '2024-01-01' },
        { name: 'B', lastUpdated: '2024-01-02' },
        { name: 'C', lastUpdated: '2024-01-03' },
      ];
      component.ngOnChanges();
      expect(component.getDisplayData().length).toBe(2);
    });
  });

  describe('totalRows', () => {
    it('returns 0 when data is empty', () => {
      expect(component.totalRows).toBe(0);
    });

    it('returns the number of non-null data items', () => {
      component.data = [
        { name: 'A', lastUpdated: '2024-01-01' },
        { name: 'B', lastUpdated: '2024-01-02' },
      ];
      component.ngOnChanges();
      expect(component.totalRows).toBe(2);
    });
  });

  describe('totalPages', () => {
    it('returns 1 when data fits on one page', () => {
      component.config.pageSize = 10;
      component.data = [{ name: 'A', lastUpdated: '2024-01-01' }];
      component.ngOnChanges();
      expect(component.totalPages).toBe(1);
    });
  });

  describe('goToPage', () => {
    it('updates currentPage when within bounds', () => {
      component.data = Array.from({ length: 10 }, (_, i) => ({
        name: `Item${i}`,
        lastUpdated: '2024-01-01',
      }));
      component.config.pageSize = 4;
      component.ngOnChanges();

      component.goToPage(2);
      expect(component.currentPage).toBe(2);
    });

    it('does not update currentPage when out of bounds', () => {
      component.data = [{ name: 'A', lastUpdated: '2024-01-01' }];
      component.config.pageSize = 10;
      component.ngOnChanges();

      component.goToPage(99);
      expect(component.currentPage).toBe(1);
    });
  });

  describe('onRowClick', () => {
    it('emits rowClick event when row is clickable', () => {
      component.config.rowClickable = true;
      let emitted: any;
      component.rowClick.subscribe((e) => (emitted = e));

      // Use a real DOM element so event.target is non-null and closest() works
      const td = document.createElement('td');
      const mockEvent = new MouseEvent('click', { bubbles: true });
      Object.defineProperty(mockEvent, 'target', { value: td, writable: false });
      component.onRowClick({ name: 'Row' }, 0, mockEvent);

      expect(emitted).toBeDefined();
      expect(emitted.index).toBe(0);
    });

    it('does not emit rowClick when rowClickable is false', () => {
      component.config.rowClickable = false;
      let emitted: any;
      component.rowClick.subscribe((e) => (emitted = e));

      const td = document.createElement('td');
      const mockEvent = new MouseEvent('click', { bubbles: true });
      Object.defineProperty(mockEvent, 'target', { value: td, writable: false });
      component.onRowClick({ name: 'Row' }, 0, mockEvent);

      expect(emitted).toBeUndefined();
    });
  });

  describe('getSortIcon', () => {
    it('delegates to TableSortService', () => {
      const icon = component.getSortIcon('name');
      expect(icon).toBe('fa-sort');
    });
  });

  describe('isColumnSorted', () => {
    it('returns false initially', () => {
      expect(component.isColumnSorted('name')).toBeFalse();
    });
  });

  describe('sortableColumns', () => {
    it('returns only columns marked sortable', () => {
      component.config.columns = [
        { key: 'name', label: 'Name', sortable: true },
        { key: 'status', label: 'Status', sortable: false },
      ];
      expect(component.sortableColumns.length).toBe(1);
      expect(component.sortableColumns[0].key).toBe('name');
    });
  });

  it('cleans up subscriptions on destroy', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
