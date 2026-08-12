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

  describe('getRowLink', () => {
    it('returns null when no linkConfig is set', () => {
      component.config.linkConfig = undefined;
      expect(component.getRowLink({ linkId: 'abc' })).toBeNull();
    });

    it('returns null when the row is missing the link field value', () => {
      component.config.linkConfig = { baseUrl: 'https://chefs.example.com/form/', linkField: 'linkId' };
      expect(component.getRowLink({ linkId: '' })).toBeNull();
      expect(component.getRowLink({})).toBeNull();
    });

    it('returns baseUrl concatenated with the row link field value', () => {
      component.config.linkConfig = { baseUrl: 'https://chefs.example.com/form/', linkField: 'linkId' };
      expect(component.getRowLink({ linkId: 'abc-123' })).toBe('https://chefs.example.com/form/abc-123');
    });
  });

  describe("column.type === 'link' rendering", () => {
    const LINK_CONFIG: DatatableConfig = {
      tableId: 'link-table',
      columns: [{ key: 'title', label: 'Submission', sortable: false, type: 'link' }],
      actionsType: 'none',
      linkConfig: { baseUrl: 'https://chefs.example.com/form/', linkField: 'linkId' },
      // getDisplayData() only applies its default pageSize during ngOnInit; since these
      // tests swap in a fresh config object post-init, pageSize must be set explicitly
      // or the pagination slice collapses to an empty array.
      pageSize: 10,
    };

    function render(config: DatatableConfig, data: any[]): void {
      component.config = { ...config };
      component.data = data;
      fixture.detectChanges();
    }

    // A row-clickable table is the realistic scenario where an unstopped keydown
    // on the link would otherwise bubble up to the <tr> and fire onRowClick.
    function expectRowClickNotEmittedFor(link: HTMLAnchorElement): void {
      let emitted: any;
      component.rowClick.subscribe((e) => (emitted = e));

      link.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
      link.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
      link.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true }));

      expect(emitted).toBeUndefined();
    }

    describe('desktop table', () => {
      it('renders an <a> tag with the correct href and link text when a link is available', () => {
        render(LINK_CONFIG, [{ title: 'Grant Application', linkId: 'abc-123' }]);

        const link = fixture.nativeElement.querySelector('a.datatable-cell-link');
        expect(link).withContext('anchor should render').not.toBeNull();
        expect(link.getAttribute('href')).toBe('https://chefs.example.com/form/abc-123');
        expect(link.textContent.trim()).toBe('Grant Application');
      });

      it('renders plain text (no anchor) when linkConfig cannot resolve a link', () => {
        render({ ...LINK_CONFIG, linkConfig: undefined }, [{ title: 'Grant Application', linkId: 'abc-123' }]);

        expect(fixture.nativeElement.querySelector('a.datatable-cell-link')).toBeNull();
        expect(fixture.nativeElement.textContent).toContain('Grant Application');
      });

      it('renders an em dash when the cell value is null', () => {
        render(LINK_CONFIG, [{ title: null, linkId: 'abc-123' }]);

        expect(fixture.nativeElement.querySelector('a.datatable-cell-link')).toBeNull();
        expect(fixture.nativeElement.textContent).toContain('—');
      });

      it('stops click and Enter/Space keydown from bubbling to the row', () => {
        render({ ...LINK_CONFIG, rowClickable: true }, [{ title: 'Grant Application', linkId: 'abc-123' }]);

        const link = fixture.nativeElement.querySelector('a.datatable-cell-link');
        expectRowClickNotEmittedFor(link);
      });
    });

    describe('mobile card view', () => {
      beforeEach(() => {
        component.isMobile = true;
      });

      it('renders an <a> tag with an external-link icon when a link is available', () => {
        render(LINK_CONFIG, [{ title: 'Grant Application', linkId: 'abc-123' }]);

        const link = fixture.nativeElement.querySelector('a.datatable-cell-link');
        expect(link).withContext('anchor should render').not.toBeNull();
        expect(link.getAttribute('href')).toBe('https://chefs.example.com/form/abc-123');
        expect(link.classList).toContain('datatable-cell-link-mobile');
        expect(link.querySelector('i.fa-external-link-alt')).not.toBeNull();
        expect(link.textContent.trim()).toBe('Grant Application');
      });

      it('renders plain text (no anchor) when linkConfig cannot resolve a link', () => {
        render({ ...LINK_CONFIG, linkConfig: undefined }, [{ title: 'Grant Application', linkId: 'abc-123' }]);

        expect(fixture.nativeElement.querySelector('a.datatable-cell-link')).toBeNull();
        expect(fixture.nativeElement.textContent).toContain('Grant Application');
      });

      it('stops click and Enter/Space keydown from bubbling to the card', () => {
        render({ ...LINK_CONFIG, rowClickable: true }, [{ title: 'Grant Application', linkId: 'abc-123' }]);

        const link = fixture.nativeElement.querySelector('a.datatable-cell-link');
        expectRowClickNotEmittedFor(link);
      });
    });
  });

  it('cleans up subscriptions on destroy', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
