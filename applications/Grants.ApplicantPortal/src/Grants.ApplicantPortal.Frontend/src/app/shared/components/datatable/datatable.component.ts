import { Component, Input, Output, EventEmitter, OnInit, ViewEncapsulation, ViewChildren, QueryList, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { 
  DatatableConfig, 
  DatatableColumn,
  DatatableRowClickEvent,
  DatatableActionEvent,
  DatatableSortEvent,
  DatatableActionItem,
  DatatableBadgeConfig
} from './datatable.models';

@Component({
  selector: 'app-datatable',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './datatable.component.html',
  styleUrls: ['./datatable.component.scss'],
  encapsulation: ViewEncapsulation.None
})
export class DatatableComponent implements OnInit {
  @Input() config!: DatatableConfig;
  @Input() data: any[] = [];
  @Input() loading = false;

  @Output() rowClick = new EventEmitter<DatatableRowClickEvent>();
  @Output() actionClick = new EventEmitter<DatatableActionEvent>();
  @Output() sort = new EventEmitter<DatatableSortEvent>();

  currentSort: { column: string; direction: 'asc' | 'desc' } | null = null;
  
  @ViewChildren('dropdownToggle') dropdownToggles!: QueryList<ElementRef>;

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

  onSort(column: string): void {
    const columnConfig = this.config.columns.find(col => col.key === column);
    if (!columnConfig?.sortable) return;

    let direction: 'asc' | 'desc' = 'asc';
    
    if (this.currentSort?.column === column) {
      direction = this.currentSort.direction === 'asc' ? 'desc' : 'asc';
    }

    this.currentSort = { column, direction };
    this.sort.emit({ column, direction });
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
    
    const badgeValue = this.getNestedProperty(row, this.config.badgeConfig.field);
    const baseClass = this.config.badgeConfig.badgeClasses[badgeValue] || '';
    
    return `${this.config.badgeConfig.badgeClassPrefix} ${baseClass}`.trim();
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
    if (!this.currentSort || this.currentSort.column !== column) {
      return '';
    }
    return this.currentSort.direction === 'asc' ? 'fa-sort-up' : 'fa-sort-down';
  }

  trackByFn(index: number, item: any): any {
    return item.id || item.key || index;
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