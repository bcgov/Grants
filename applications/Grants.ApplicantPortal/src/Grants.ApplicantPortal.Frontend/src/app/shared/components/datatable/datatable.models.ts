export interface DatatableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  type?: 'text' | 'date' | 'currency' | 'badge' | 'email' | 'phone' | 'boolean';
  cssClass?: string;
  width?: string;
}

export interface DatatableBadgeConfig {
  field: string;
  badgeClassPrefix: string;
  badgeClasses: { [key: string]: string };
}

export interface DatatableActionItem {
  label: string;
  icon: string;
  action: string;
  cssClass?: string;
}

export interface DatatableConfig {
  columns: DatatableColumn[];
  actionsType?: 'chevron' | 'dropdown' | 'none';
  actionItems?: DatatableActionItem[];
  actionsVisibilityField?: string; // Field name to check for row-level action visibility
  badgeConfig?: DatatableBadgeConfig;
  rowClickable?: boolean;
  responsive?: boolean;
  striped?: boolean;
  hover?: boolean;
  loading?: boolean;
  noDataMessage?: string;
  loadingMessage?: string;
  tableClass?: string;
  containerClass?: string;
  // New sorting properties
  tableId?: string; // Unique identifier for the table (for localStorage)
  defaultSortField?: string; // Field representing original server order (e.g., 'lastUpdated')
  enableSortPersistence?: boolean; // Whether to persist sort state in localStorage
}

export interface DatatableRowClickEvent {
  row: any;
  index: number;
}

export interface DatatableActionEvent {
  action: string;
  row: any;
  index: number;
}

export interface DatatableSortEvent {
  column: string;
  direction: 'asc' | 'desc' | 'none';
}

export interface DatatableSortState {
  column: string;
  direction: 'asc' | 'desc' | 'none';
}