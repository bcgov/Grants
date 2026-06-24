// ─────────────────────────────────────────────────────────────────────────────
// App Selector Registry
//
// Single source of truth for every selector this project OWNS (Angular app).
// Page objects import from here — never hardcode selector strings in a page
// object or spec.
//
// VALIDATION: data-cy selectors are automatically validated against Angular
// HTML templates by:  npm run validate:selectors
//
// SYNC: After changing any data-cy attribute in the Angular frontend, run the
// /sync-selectors Claude skill to detect drift and update this file.
// ─────────────────────────────────────────────────────────────────────────────

export const AppSelectors = {

  // ── Login page (/auth/login) ──────────────────────────────────────────────
  Login: {
    card:   '[data-cy="login-card"]',
    button: '[data-cy="login-btn"]',
  },

  // ── Navigation shell (sidebar + header) ──────────────────────────────────
  Nav: {
    menu:               'nav.nav-menu',
    applicantInfoLink:  '[data-cy="nav-applicant-info"]',
    paymentsLink:       '[data-cy="nav-payments"]',

    // Workspace dropdown (header)
    workspaceDropdown:      '[data-cy="workspace-dropdown"]',
    workspaceDropdownMenu:  "ul[aria-labelledby='workspace-dropdown']",
    dropdownHeader:         '.dropdown-header',
    providerItemActive:     '.provider-item.active',
    // Factory — dynamic per provider ID
    providerItem: (id: string) => `[data-cy="provider-item-${id}"]`,
    changeWorkspaceButton:  '[data-cy="change-workspace-btn"]',

    // User dropdown (header)
    userDropdownButton: '[data-cy="header-user-dropdown"]',
    userDropdownMenu:   "ul[aria-labelledby='header-user-dropdown']",
    logoutButton:       '[data-cy="header-user-dropdown-logout"]',
  },

  // ── Workspace + provider selection screen ────────────────────────────────
  Workspace: {
    workspaceSelect:      '[data-cy="workspace-select"]',
    workspaceContinueBtn: '[data-cy="workspace-continue-btn"]',
    workspaceBackToLogin: '[data-cy="workspace-back-to-login-btn"]',
    providerLabel:        'label[for="provider-select"]',
    providerSelect:       '[data-cy="provider-select"]',
    providerContinueBtn:  '[data-cy="provider-continue-btn"]',
    providerBackBtn:      '[data-cy="provider-back-btn"]',
  },

  // ── Landing page (/app) ───────────────────────────────────────────────────
  Landing: {
    orgInfoCard:        '[data-cy="card-organization"]',
    submissionsCard:    '[data-cy="card-submissions"]',
    contactsCard:       '[data-cy="card-contacts"]',
    addressesCard:      '[data-cy="card-addresses"]',
    // Compound — selects the orgbook table nested inside the org card
    orgTable:           '[data-cy="card-organization"] .orgbook-table',
    submissionsTable:   '[data-cy="datatable-submissions"]',
    addContactButton:   '[data-cy="contact-add-btn"]',
    primaryContactInfo: '[data-cy="primary-contact-info"]',
    primaryAddressInfo: '[data-cy="primary-address-info"]',
  },

  // ── Payments page (/app/payments) ─────────────────────────────────────────
  Payments: {
    pageInner:   '[data-cy="payments-page-inner"]',
    card:        '[data-cy="payments-card"]',
    header:      '[data-cy="payments-header"]',
    table:       '[data-cy="datatable-payments"]',
    searchInput: '[data-cy="datatable-search-payments"]',
    tableBody:   '[data-cy="datatable-body-payments"]',
  },

} as const;
