import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, AfterViewInit, ViewEncapsulation, ViewChildren, QueryList, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { 
  DatatableConfig, 
  DatatableColumn,
  DatatableRowClickEvent,
  DatatableActionEvent,
  DatatableSortEvent
} from './datatable.models';
import { TableSortService, SortState, TableSortConfig } from '../../services/table-sort.service';

@Component({
  selector: 'app-datatable',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './datatable.component.html',
  styleUrls: ['./datatable.component.scss'],
  encapsulation: ViewEncapsulation.None
})
export class DatatableComponent implements OnInit, OnDestroy, OnChanges, AfterViewInit {
  @Input({ required: true }) idSuffix!: string;
  @Input() config!: DatatableConfig;
  @Input() data: any[] = [];
  @Input() loading = false;

  @Output() rowClick = new EventEmitter<DatatableRowClickEvent>();
  @Output() actionClick = new EventEmitter<DatatableActionEvent>();
  @Output() sort = new EventEmitter<DatatableSortEvent>();
  @Output() dataChange = new EventEmitter<any[]>(); // Emit sorted data

  // Sorting properties
  currentSortState: SortState | null = null;
  sortedData: any[] = [];
  private sortConfig: TableSortConfig | null = null;
  private readonly destroy$ = new Subject<void>();

  // Search properties
  searchTerm: string = '';
  private filteredData: any[] = [];
  private readonly searchSubject$ = new Subject<string>();

  // Pagination
  currentPage = 1;

  // Mobile responsive
  isMobile = false;
  mobileSortColumn = '';
  private mobileQuery!: MediaQueryList;
  private readonly mobileQueryHandler = (e: MediaQueryListEvent) => this.isMobile = e.matches;
  
  @ViewChildren('dropdownToggle') dropdownToggles!: QueryList<ElementRef>;

  private readonly dropdownHiddenHandler = (event: Event) => {
    this.setWrapperOverflow(event.target as HTMLElement | null, false);
  };

  constructor(
    private readonly tableSortService: TableSortService,
    private readonly elementRef: ElementRef
  ) {}

  ngOnInit(): void {
    // Set default configuration values
    if (!this.config) {
      throw new Error('Datatable config is required');
    }
    
    // Set defaults
    this.config.rowClickable = this.config.rowClickable ?? true;
    this.config.responsive = this.config.responsive ?? true;
    this.config.striped = this.config.striped ?? true;
    this.config.hover = this.config.hover ?? true;
    this.config.actionsType = this.config.actionsType ?? 'chevron';
    this.config.noDataMessage = this.config.noDataMessage ?? 'No data available.';
    this.config.loadingMessage = this.config.loadingMessage ?? 'Loading data...';
    this.config.enableSortPersistence = this.config.enableSortPersistence ?? true;
    this.config.defaultSortField = this.config.defaultSortField ?? 'lastUpdated';
    this.config.pageSize = this.config.pageSize ?? 4;
    
    // Setup sorting configuration
    if (this.config.tableId) {
      this.sortConfig = {
        tableId: this.config.tableId,
        defaultSortField: this.config.defaultSortField
      };
      
      // Restore sort state from localStorage if persistence is enabled
      if (this.config.enableSortPersistence) {
        const restoredState = this.tableSortService.getSortState(this.config.tableId);
        const columnExists = restoredState && this.config.columns.some(c => c.key === restoredState.column);
        this.currentSortState = columnExists ? restoredState : null;
      }
    }
    
    // Setup mobile detection
    if (typeof window !== 'undefined') {
      this.mobileQuery = globalThis.matchMedia('(max-width: 768px)');
      this.isMobile = this.mobileQuery.matches;
      this.mobileQuery.addEventListener('change', this.mobileQueryHandler);
    }

    // Initial sort of data
    this.applySorting();

    // Initialize mobile sort column from current sort state
    if (this.currentSortState) {
      this.mobileSortColumn = this.currentSortState.direction === 'none' ? '' 
        : `${this.currentSortState.column}:${this.currentSortState.direction}`;
    }

    // Setup search debounce
    if (this.config.enableSearch) {
      this.config.searchMinChars = this.config.searchMinChars ?? 1;
      this.config.searchPlaceholder = this.config.searchPlaceholder ?? 'Search...';
      this.searchSubject$
        .pipe(debounceTime(300), takeUntil(this.destroy$))
        .subscribe((term) => {
          this.applySearch(term);
        });
    }
  }

  ngAfterViewInit(): void {
    this.elementRef.nativeElement.addEventListener('hidden.bs.dropdown', this.dropdownHiddenHandler);
  }

  ngOnDestroy(): void {
    this.elementRef.nativeElement.removeEventListener('hidden.bs.dropdown', this.dropdownHiddenHandler);
    this.mobileQuery?.removeEventListener('change', this.mobileQueryHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  ngOnChanges(): void {
    // Re-apply filtering and sorting when data changes, preserving current page
    this.applySearch(this.searchTerm, false);
  }

  onSearchInput(term: string): void {
    this.searchSubject$.next(term);
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.applySearch('');
  }

  private applySearch(term: string, resetPage: boolean = true): void {
    const minChars = this.config?.searchMinChars ?? 1;
    if (this.config?.enableSearch && term.length >= minChars) {
      const lowerTerm = term.toLowerCase();
      this.filteredData = this.data.filter((row) =>
        this.config.columns.some((col) => {
          const value = this.getNestedProperty(row, col.key);
          return value != null && String(value).toLowerCase().includes(lowerTerm);
        })
      );
    } else {
      this.filteredData = [];
    }
    this.applySorting(resetPage);
  }

  onRowClick(row: any, index: number, event: Event): void {
    if (!this.config.rowClickable) return;
    
    // Check if click target is an action button or dropdown
    const target = event.target as HTMLElement;
    if (target.closest('.actions-column') || target.closest('.dropdown')) {
      return;
    }

    this.rowClick.emit({ row, index });
  }

  onActionClick(actionType: string, row: any, index: number, event: Event): void {
    event.stopPropagation();
    this.actionClick.emit({ action: actionType, row, index });
  }

  getRowLink(row: any): string | null {
    const linkConfig = this.config.linkConfig;
    if (!linkConfig?.baseUrl || !row[linkConfig.linkField]) {
      return null;
    }
    return `${linkConfig.baseUrl}${row[linkConfig.linkField]}`;
  }

  onSort(column: string): void {
    const columnConfig = this.config.columns.find(col => col.key === column);
    if (!columnConfig?.sortable || !this.sortConfig) return;

    // Use the 3-state sorting service
    this.currentSortState = this.tableSortService.cycleSort(
      this.sortConfig.tableId, 
      column, 
      this.sortConfig
    );

    // Apply sorting to data
    this.applySorting();

    // Emit sort event
    this.sort.emit({ 
      column, 
      direction: this.currentSortState.direction 
    });
  }

  /**
   * Applies sorting to the data and emits the sorted result
   */
  private applySorting(resetPage: boolean = true): void {
    const sourceData = this.isSearchActive ? this.filteredData : this.data;
    if (!this.sortConfig) {
      this.sortedData = [...sourceData];
    } else {
      this.sortedData = this.tableSortService.sortData(
        sourceData,
        this.currentSortState,
        this.sortConfig
      );
    }
    
    if (resetPage) {
      this.currentPage = 1;
    } else {
      // Clamp to last valid page (e.g., after deleting the only record on the last page)
      const maxPage = this.totalPages || 1;
      if (this.currentPage > maxPage) {
        this.currentPage = maxPage;
      }
    }

    // Emit the sorted data
    this.dataChange.emit(this.sortedData);
  }

  getCellValue(row: any, column: DatatableColumn): any {
    const value = this.getNestedProperty(row, column.key);
    
    switch (column.type) {
      case 'date':
        return value ? new Date(value) : null;
      case 'currency':
        return value ? Number(value) : 0;
      default:
        return value;
    }
  }

  private getNestedProperty(obj: any, path: string): any {
    return path.split('.').reduce((current, key) => current?.[key], obj);
  }

  getBadgeClass(row: any): string {
    if (!this.config.badgeConfig) return '';
    
    // Use field for styling determination
    const badgeValue = this.getNestedProperty(row, this.config.badgeConfig.field);
    const baseClass = this.config.badgeConfig.badgeClasses[badgeValue] || this.config.badgeConfig.fallbackClass || '';
    
    return `${this.config.badgeConfig.badgeClassPrefix} ${baseClass}`.trim();
  }

  getBadgeDisplayValue(row: any, column: any): string {
    if (!this.config.badgeConfig) return this.getCellValue(row, column);
    
    // Use displayField for text, or fall back to the column's field
    const displayField = this.config.badgeConfig.displayField || column.key;
    const value = this.getNestedProperty(row, displayField) || '';

    // Check for display label override
    if (this.config.badgeConfig.displayLabels?.[value]) {
      return this.config.badgeConfig.displayLabels[value];
    }

    return this.toDisplayLabel(value);
  }

  private toDisplayLabel(value: string): string {
    if (!value) return value;
    return value
      .replaceAll(/(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])/g, ' ')
      .split(/[\s_-]+/)
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  closeDropdown(dropdownToggle: any): void {
    if (dropdownToggle) {
      dropdownToggle.click();
    }
    this.setWrapperOverflow(dropdownToggle, false);
  }

  onDropdownToggle(toggleElement: HTMLElement, event: Event): void {
    event.stopPropagation();
    
    // Close all other open dropdowns before opening this one
    this.closeAllDropdownsExcept(toggleElement);

    // Allow the wrapper to grow so the dropdown menu isn't clipped
    requestAnimationFrame(() => {
      const menu = toggleElement.nextElementSibling;
      const isOpen = menu?.classList.contains('show');
      this.setWrapperOverflow(toggleElement, !!isOpen);
    });
  }

  private closeAllDropdownsExcept(exceptElement: HTMLElement): void {
    if (this.dropdownToggles) {
      this.dropdownToggles.forEach((toggleRef) => {
        if (toggleRef.nativeElement !== exceptElement) {
          const toggle = toggleRef.nativeElement;
          const dropdownMenu = toggle.nextElementSibling;
          
          // Remove Bootstrap's show class and reset aria-expanded
          if (dropdownMenu && dropdownMenu.classList.contains('show')) {
            dropdownMenu.classList.remove('show');
            toggle.setAttribute('aria-expanded', 'false');
            
            // Also remove show class from parent dropdown if it exists
            const parentDropdown = toggle.closest('.dropdown');
            if (parentDropdown) {
              parentDropdown.classList.remove('show');
            }
          }
        }
      });
    }
  }

  onDropdownClosed(): void {
    // This method can be used for cleanup if needed
  }

  private setWrapperOverflow(element: HTMLElement | null | undefined, open: boolean): void {
    const wrapper = element?.closest('.datatable-wrapper') as HTMLElement;
    if (wrapper) {
      wrapper.style.overflow = open ? 'visible' : '';
    }
  }

  getSortIcon(column: string): string {
    if (!this.sortConfig) return 'fa-sort';
    
    return this.tableSortService.getSortIcon(column, this.currentSortState);
  }

  getSortAriaLabel(column: string): string {
    if (!this.sortConfig) return `Sort ${column}`;
    
    return this.tableSortService.getSortAriaLabel(column, this.currentSortState);
  }

  isColumnSorted(column: string): boolean {
    return this.tableSortService.isColumnSorted(column, this.currentSortState);
  }

  trackByFn(index: number, item: any): any {
    if (!item) return index;
    return item.id || item.key || item.contactId || item.addressId || item.submissionId || index;
  }

  get isSearchActive(): boolean {
    const minChars = this.config?.searchMinChars ?? 1;
    return !!this.config?.enableSearch && this.searchTerm.length >= minChars;
  }

  /**
   * Gets the data to display (sorted + paginated)
   */
  getDisplayData(): any[] {
    const data = (this.sortedData.length > 0 || this.isSearchActive) ? this.sortedData : this.data;
    const filtered = data ? data.filter(item => item != null) : [];
    const pageSize = this.config.pageSize!;
    if (pageSize <= 0 || filtered.length <= pageSize) {
      return filtered;
    }
    const start = (this.currentPage - 1) * pageSize;
    return filtered.slice(start, start + pageSize);
  }

  get totalRows(): number {
    const data = (this.sortedData.length > 0 || this.isSearchActive) ? this.sortedData : this.data;
    return data ? data.filter(item => item != null).length : 0;
  }

  get totalPages(): number {
    const pageSize = this.config.pageSize!;
    return pageSize > 0 ? Math.ceil(this.totalRows / pageSize) : 1;
  }

  get showPager(): boolean {
    return this.totalRows > this.config.pageSize!;
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  get pagerPages(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  shouldShowActions(row: any): boolean {
    // If no visibility field is specified, always show actions
    if (!this.config.actionsVisibilityField) {
      return true;
    }
    
    // Check the specified field on the row data
    return !!row[this.config.actionsVisibilityField];
  }

  isActionsDisabled(row: any): boolean {
    if (!this.config.disabledActionsField) {
      return false;
    }
    return !row[this.config.disabledActionsField];
  }

  get sortableColumns(): DatatableColumn[] {
    return this.config.columns.filter(c => c.sortable);
  }

  onMobileSortChange(value: string): void {
    this.mobileSortColumn = value;
    if (!value) {
      if (this.sortConfig) {
        this.currentSortState = { column: '', direction: 'none' };
        this.tableSortService.setSortState(this.sortConfig.tableId, this.currentSortState);
        this.applySorting();
      }
      return;
    }
    const [column, direction] = value.split(':');
    if (!this.sortConfig) return;
    this.currentSortState = { column, direction: direction as 'asc' | 'desc' };
    this.tableSortService.setSortState(this.sortConfig.tableId, this.currentSortState);
    this.applySorting();
    this.sort.emit({ column, direction: direction as 'asc' | 'desc' });
  }

  onMobileCardClick(row: any, index: number, event: Event): void {
    if (!this.config.rowClickable) return;
    const target = event.target as HTMLElement;
    if (target.closest('.card-actions')) return;
    this.rowClick.emit({ row, index });
  }
}