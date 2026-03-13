export interface DatatableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  type?: 'text' | 'date' | 'currency' | 'badge' | 'email' | 'phone' | 'boolean';
  cssClass?: string;
  width?: string;
  booleanFalseBlank?: boolean;
}

export interface DatatableBadgeConfig {
  field: string; // Field used for badge styling
  displayField?: string; // Optional field used for display text (defaults to field)
  badgeClassPrefix: string;
  badgeClasses: { [key: string]: string };
  fallbackClass?: string; // Optional fallback class for unknown badge values
}

export interface DatatableActionItem {
  label: string;
  icon: string;
  action: string;
  cssClass?: string;
}

export interface DatatableLinkConfig {
  baseUrl: string;     // Base URL prefix (e.g., linkSource from API)
  linkField: string;   // Field on the row containing the link id to append
}

export interface DatatableConfig {
  columns: DatatableColumn[];
  actionsType?: 'chevron' | 'dropdown' | 'none';
  actionItems?: DatatableActionItem[];
  actionsVisibilityField?: string; // Field name to check for row-level action visibility
  disabledActionsField?: string; // Field name to check if actions should render as disabled (shows X icon)
  disabledActionsTooltip?: string; // Tooltip text for disabled action rows
  linkConfig?: DatatableLinkConfig; // When set with chevron, renders an <a> tag instead of a button
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
  pageSize?: number; // Max rows per page before paging kicks in (default: 4)
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