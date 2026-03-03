import { Injectable } from '@angular/core';

export type SortDirection = 'asc' | 'desc' | 'none';

export interface SortState {
  column: string;
  direction: SortDirection;
}

export interface TableSortConfig {
  tableId: string;
  defaultSortField: string; // The field that represents the original server order (e.g., 'lastUpdated')
}

@Injectable({
  providedIn: 'root'
})
export class TableSortService {
  private readonly STORAGE_KEY_PREFIX = 'table_sort_';

  /**
   * Gets the current sort state for a table
   */
  getSortState(tableId: string): SortState | null {
    try {
      const stored = localStorage.getItem(this.getStorageKey(tableId));
      return stored ? JSON.parse(stored) : null;
    } catch (error) {
      console.warn('Error reading sort state from localStorage:', error);
      return null;
    }
  }

  /**
   * Sets the sort state for a table
   */
  setSortState(tableId: string, sortState: SortState): void {
    try {
      if (sortState.direction === 'none') {
        // Remove from localStorage when no sorting is applied
        localStorage.removeItem(this.getStorageKey(tableId));
      } else {
        localStorage.setItem(this.getStorageKey(tableId), JSON.stringify(sortState));
      }
    } catch (error) {
      console.warn('Error saving sort state to localStorage:', error);
    }
  }

  /**
   * Handles the 3-state sorting cycle: none -> asc -> desc -> none
   * Returns the new sort state
   */
  cycleSort(tableId: string, column: string, config: TableSortConfig): SortState {
    const currentState = this.getSortState(tableId);
    let newDirection: SortDirection;

    if (!currentState || currentState.column !== column) {
      // First click on this column or different column
      newDirection = 'asc';
    } else {
      // Cycle through states for the same column
      switch (currentState.direction) {
        case 'asc':
          newDirection = 'desc';
          break;
        case 'desc':
          newDirection = 'none';
          break;
        case 'none':
        default:
          newDirection = 'asc';
          break;
      }
    }

    const newState: SortState = { column, direction: newDirection };
    this.setSortState(tableId, newState);
    return newState;
  }

  /**
   * Sorts an array of data based on the sort state
   */
  sortData<T>(data: T[], sortState: SortState | null, config: TableSortConfig): T[] {
    if (!sortState || sortState.direction === 'none') {
      // Return data in original order (by lastUpdated field)
      return [...data].sort((a, b) => {
        const aValue = this.getNestedProperty(a, config.defaultSortField);
        const bValue = this.getNestedProperty(b, config.defaultSortField);
        
        // Sort by lastUpdated in descending order (newest first) as default
        return new Date(bValue).getTime() - new Date(aValue).getTime();
      });
    }

    return [...data].sort((a, b) => {
      const aValue = this.getNestedProperty(a, sortState.column);
      const bValue = this.getNestedProperty(b, sortState.column);

      let comparison = this.compareValues(aValue, bValue);
      
      return sortState.direction === 'desc' ? -comparison : comparison;
    });
  }

  /**
   * Gets the appropriate sort icon class for a column
   */
  getSortIcon(column: string, sortState: SortState | null): string {
    if (!sortState || sortState.column !== column) {
      return 'fa-sort'; // Default unsorted icon
    }

    switch (sortState.direction) {
      case 'asc':
        return 'fa-sort-up';
      case 'desc':
        return 'fa-sort-down';
      case 'none':
      default:
        return 'fa-sort';
    }
  }

  /**
   * Checks if a column is currently being sorted
   */
  isColumnSorted(column: string, sortState: SortState | null): boolean {
    return sortState?.column === column && sortState?.direction !== 'none';
  }

  /**
   * Gets the sort direction text for accessibility
   */
  getSortAriaLabel(column: string, sortState: SortState | null): string {
    if (!sortState || sortState.column !== column) {
      return `Sort ${column} ascending`;
    }

    switch (sortState.direction) {
      case 'asc':
        return `Sort ${column} descending`;
      case 'desc':
        return `Remove ${column} sorting`;
      case 'none':
      default:
        return `Sort ${column} ascending`;
    }
  }

  /**
   * Clears all sort state for a table
   */
  clearSortState(tableId: string): void {
    localStorage.removeItem(this.getStorageKey(tableId));
  }

  private getStorageKey(tableId: string): string {
    return `${this.STORAGE_KEY_PREFIX}${tableId}`;
  }

  private getNestedProperty(obj: any, path: string): any {
    return path.split('.').reduce((current, key) => current?.[key], obj);
  }

  private compareValues(a: any, b: any): number {
    // Handle null/undefined values
    if (a == null && b == null) return 0;
    if (a == null) return -1;
    if (b == null) return 1;

    // Handle different data types
    if (typeof a === 'string' && typeof b === 'string') {
      return a.toLowerCase().localeCompare(b.toLowerCase());
    }

    if (typeof a === 'number' && typeof b === 'number') {
      return a - b;
    }

    if (typeof a === 'boolean' && typeof b === 'boolean') {
      return a === b ? 0 : a ? 1 : -1;
    }

    // Handle dates
    const aDate = new Date(a);
    const bDate = new Date(b);
    if (!isNaN(aDate.getTime()) && !isNaN(bDate.getTime())) {
      return aDate.getTime() - bDate.getTime();
    }

    // Default string comparison
    return String(a).toLowerCase().localeCompare(String(b).toLowerCase());
  }
}