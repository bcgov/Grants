import { TestBed } from '@angular/core/testing';

import { TableSortService, SortState, TableSortConfig } from './table-sort.service';

const TABLE_ID = 'test-table';
const CONFIG: TableSortConfig = { tableId: TABLE_ID, defaultSortField: 'lastUpdated' };

describe('TableSortService', () => {
  let service: TableSortService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TableSortService],
    });

    service = TestBed.inject(TableSortService);
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getSortState / setSortState', () => {
    it('returns null when no state is stored', () => {
      expect(service.getSortState(TABLE_ID)).toBeNull();
    });

    it('persists and retrieves sort state', () => {
      const state: SortState = { column: 'name', direction: 'asc' };
      service.setSortState(TABLE_ID, state);
      expect(service.getSortState(TABLE_ID)).toEqual(state);
    });

    it('removes state from localStorage when direction is none', () => {
      service.setSortState(TABLE_ID, { column: 'name', direction: 'asc' });
      service.setSortState(TABLE_ID, { column: 'name', direction: 'none' });
      expect(service.getSortState(TABLE_ID)).toBeNull();
    });
  });

  describe('cycleSort', () => {
    it('starts at asc when no prior state exists', () => {
      const result = service.cycleSort(TABLE_ID, 'name', CONFIG);
      expect(result.direction).toBe('asc');
      expect(result.column).toBe('name');
    });

    it('cycles asc -> desc on second click of same column', () => {
      service.cycleSort(TABLE_ID, 'name', CONFIG);
      const result = service.cycleSort(TABLE_ID, 'name', CONFIG);
      expect(result.direction).toBe('desc');
    });

    it('cycles desc -> none on third click of same column', () => {
      service.cycleSort(TABLE_ID, 'name', CONFIG);
      service.cycleSort(TABLE_ID, 'name', CONFIG);
      const result = service.cycleSort(TABLE_ID, 'name', CONFIG);
      expect(result.direction).toBe('none');
    });

    it('resets to asc when a different column is clicked', () => {
      service.cycleSort(TABLE_ID, 'name', CONFIG);
      const result = service.cycleSort(TABLE_ID, 'date', CONFIG);
      expect(result.direction).toBe('asc');
      expect(result.column).toBe('date');
    });
  });

  describe('sortData', () => {
    const data = [
      { name: 'Charlie', lastUpdated: '2024-01-03' },
      { name: 'Alice', lastUpdated: '2024-01-01' },
      { name: 'Bob', lastUpdated: '2024-01-02' },
    ];

    it('sorts by default field (desc by date) when sortState is null', () => {
      const result = service.sortData(data, null, CONFIG);
      expect(result[0].name).toBe('Charlie');
    });

    it('sorts ascending by column', () => {
      const state: SortState = { column: 'name', direction: 'asc' };
      const result = service.sortData(data, state, CONFIG);
      expect(result[0].name).toBe('Alice');
      expect(result[2].name).toBe('Charlie');
    });

    it('sorts descending by column', () => {
      const state: SortState = { column: 'name', direction: 'desc' };
      const result = service.sortData(data, state, CONFIG);
      expect(result[0].name).toBe('Charlie');
      expect(result[2].name).toBe('Alice');
    });

    it('does not mutate the original data array', () => {
      const original = [...data];
      service.sortData(data, { column: 'name', direction: 'asc' }, CONFIG);
      expect(data).toEqual(original);
    });
  });

  describe('getSortIcon', () => {
    it('returns fa-sort when column is not sorted', () => {
      expect(service.getSortIcon('name', null)).toBe('fa-sort');
    });

    it('returns fa-sort-up when column is sorted asc', () => {
      const state: SortState = { column: 'name', direction: 'asc' };
      expect(service.getSortIcon('name', state)).toBe('fa-sort-up');
    });

    it('returns fa-sort-down when column is sorted desc', () => {
      const state: SortState = { column: 'name', direction: 'desc' };
      expect(service.getSortIcon('name', state)).toBe('fa-sort-down');
    });

    it('returns fa-sort for a different column than the sorted one', () => {
      const state: SortState = { column: 'name', direction: 'asc' };
      expect(service.getSortIcon('date', state)).toBe('fa-sort');
    });
  });

  describe('isColumnSorted', () => {
    it('returns false when sortState is null', () => {
      expect(service.isColumnSorted('name', null)).toBeFalse();
    });

    it('returns true when column matches and direction is not none', () => {
      const state: SortState = { column: 'name', direction: 'asc' };
      expect(service.isColumnSorted('name', state)).toBeTrue();
    });

    it('returns false when direction is none', () => {
      const state: SortState = { column: 'name', direction: 'none' };
      expect(service.isColumnSorted('name', state)).toBeFalse();
    });
  });

  describe('getSortAriaLabel', () => {
    it('returns ascending label when no sort state', () => {
      expect(service.getSortAriaLabel('name', null)).toBe('Sort name ascending');
    });

    it('returns descending label when column is sorted asc', () => {
      const state: SortState = { column: 'name', direction: 'asc' };
      expect(service.getSortAriaLabel('name', state)).toBe('Sort name descending');
    });

    it('returns remove label when column is sorted desc', () => {
      const state: SortState = { column: 'name', direction: 'desc' };
      expect(service.getSortAriaLabel('name', state)).toBe('Remove name sorting');
    });
  });

  describe('clearSortState', () => {
    it('removes the stored state for a table', () => {
      service.setSortState(TABLE_ID, { column: 'name', direction: 'asc' });
      service.clearSortState(TABLE_ID);
      expect(service.getSortState(TABLE_ID)).toBeNull();
    });
  });
});
