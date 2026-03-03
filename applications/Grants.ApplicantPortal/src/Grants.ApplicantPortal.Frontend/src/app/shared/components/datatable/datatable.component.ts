import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, ViewEncapsulation, ViewChildren, QueryList, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { 
  DatatableConfig, 
  DatatableColumn,
  DatatableRowClickEvent,
  DatatableActionEvent,
  DatatableSortEvent,
  DatatableActionItem,
  DatatableBadgeConfig,
  DatatableLinkConfig,
  DatatableSortState
} from './datatable.models';
import { TableSortService, SortState, TableSortConfig } from '../../services/table-sort.service';

@Component({
  selector: 'app-datatable',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './datatable.component.html',
  styleUrls: ['./datatable.component.scss'],
  encapsulation: ViewEncapsulation.None
})
export class DatatableComponent implements OnInit, OnDestroy, OnChanges {
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
  
  @ViewChildren('dropdownToggle') dropdownToggles!: QueryList<ElementRef>;

  constructor(private tableSortService: TableSortService) {}

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
    
    // Setup sorting configuration
    if (this.config.tableId) {
      this.sortConfig = {
        tableId: this.config.tableId,
        defaultSortField: this.config.defaultSortField
      };
      
      // Restore sort state from localStorage if persistence is enabled
      if (this.config.enableSortPersistence) {
        this.currentSortState = this.tableSortService.getSortState(this.config.tableId);
      }
    }
    
    // Initial sort of data
    this.applySorting();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  ngOnChanges(): void {
    // Re-apply sorting when data changes
    this.applySorting();
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
  private applySorting(): void {
    if (!this.sortConfig) {
      this.sortedData = [...this.data];
    } else {
      this.sortedData = this.tableSortService.sortData(
        this.data,
        this.currentSortState,
        this.sortConfig
      );
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
    return this.getNestedProperty(row, displayField) || '';
  }

  closeDropdown(dropdownToggle: any): void {
    if (dropdownToggle) {
      dropdownToggle.click();
    }
  }

  onDropdownToggle(index: number, event: Event): void {
    event.stopPropagation();
    
    // Close all other open dropdowns before opening this one
    this.closeAllDropdownsExcept(index);
  }

  private closeAllDropdownsExcept(exceptIndex: number): void {
    if (this.dropdownToggles) {
      this.dropdownToggles.forEach((toggleRef, index) => {
        if (index !== exceptIndex) {
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

  /**
   * Gets the data to display (sorted data)
   */
  getDisplayData(): any[] {
    const data = this.sortedData.length > 0 ? this.sortedData : this.data;
    // Filter out any undefined or null items to prevent trackByFn errors
    return data ? data.filter(item => item != null) : [];
  }

  shouldShowActions(row: any): boolean {
    // If no visibility field is specified, always show actions
    if (!this.config.actionsVisibilityField) {
      return true;
    }
    
    // Check the specified field on the row data
    return !!row[this.config.actionsVisibilityField];
  }
}