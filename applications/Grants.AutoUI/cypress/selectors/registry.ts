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
// NOTE — Dynamic bindings: selectors generated via Angular [attr.data-cy]="expr"
// bindings are not detectable by the static validator. They will appear in the
// "onlyInRegistry" report but are NOT orphans — they exist at runtime. Each
// such entry is marked with a comment below.
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

  // ── Auth callback (/auth/callback) ────────────────────────────────────────
  Auth: {
    callbackProcessing: '[data-cy="callback-processing"]',
    callbackError:      '[data-cy="callback-error"]',
    callbackErrorMsg:   '[data-cy="callback-error-message"]',
  },

  // ── Navigation shell (sidebar + header) ──────────────────────────────────
  Nav: {
    menu:               'nav.nav-menu',
    applicantInfoLink:  '[data-cy="nav-applicant-info"]',
    paymentsLink:       '[data-cy="nav-payments"]',

    // Header — page title and org header info
    pageTitle:      '[data-cy="header-page-title"]',
    applicantId:    '[data-cy="applicant-id"]',
    applicantName:  '[data-cy="applicant-name"]',

    // Workspace dropdown (header)
    workspaceDropdown:      '[data-cy="workspace-dropdown"]',
    workspaceDropdownMenu:  "ul[aria-labelledby='workspace-dropdown']",
    dropdownHeader:         '.dropdown-header',
    providerItemActive:     '.provider-item.active',
    // Factory — dynamic per provider ID
    providerItem: (id: string) => `[data-cy="provider-item-${id}"]`,
    changeWorkspaceButton:  '[data-cy="change-workspace-btn"]',

    // User dropdown (header) — data-cy values are generated at runtime by
    // user-dropdown.component.html via [attr.data-cy]="dropdownId" and
    // [attr.data-cy]="dropdownId + '-logout'"; invisible to static validator.
    userDropdownButton: '[data-cy="header-user-dropdown"]',
    userDropdownMenu:   "ul[aria-labelledby='header-user-dropdown']",
    logoutButton:       '[data-cy="header-user-dropdown-logout"]',
  },

  // ── Layout shell (mobile navigation controls) ─────────────────────────────
  Layout: {
    mobileHamburgerBtn: '[data-cy="mobile-hamburger-btn"]',
    mobileCloseBtn:     '[data-cy="mobile-close-btn"]',
  },

  // ── Workspace + provider selection screen ────────────────────────────────
  Workspace: {
    workspaceSelect:      '[data-cy="workspace-select"]',
    workspaceContinueBtn: '[data-cy="workspace-continue-btn"]',
    workspaceBackToLogin: '[data-cy="workspace-back-to-login-btn"]',
    providerLabel:        'label[for="provider-select"]',
    providerSelect:       '[data-cy="provider-select"]',
    providerContinueBtn:  '[data-cy="provider-continue-btn"]',
    // Conditionally rendered — absent in single-workspace mode (@if !isSingleWorkspace)
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
    orgTableInner:      '.orgbook-table',
    orgNameField:       '[data-cy="org-name"]',
    regNumberField:     '[data-cy="reg-number"]',
    submissionsTable:   '[data-cy="datatable-submissions"]',
    addContactButton:   '[data-cy="contact-add-btn"]',
    primaryContactInfo: '[data-cy="primary-contact-info"]',
    noPrimaryContact:   '[data-cy="no-primary-contact"]',
    primaryAddressInfo: '[data-cy="primary-address-info"]',
    // Factory — one block per address type. Mirrors the component's idKey derivation
    // (trim, lowercase, non-alphanumeric runs to dashes) so multi-word configured types
    // such as "Home Office" resolve to primary-address-home-office.
    primaryAddressBlock: (typeKey: string) =>
      `[data-cy="primary-address-${typeKey.toLowerCase().split(/[^a-z0-9]+/).filter(Boolean).join('-')}"]`,
    noAddressesMessage: '[data-cy="no-addresses-message"]',
  },

  // ── Submissions — Related Links modal (/app/applicant-info) ──────────────
  SubmissionsModal: {
    modal:                    '[data-cy="related-links-modal"]',
    modalLabel:               '[data-cy="related-links-modal-label"]',
    modalCloseBtn:            '[data-cy="related-links-modal-close-btn"]',
    applicantMessage:         '[data-cy="related-links-modal-applicant-message"]',
    eligibleForRenewal:       '[data-cy="related-links-modal-eligible-for-renewal"]',
    eligibleForRenewalToggle: '[data-cy="related-links-modal-eligible-for-renewal-toggle"]',
    closeFooterBtn:           '[data-cy="related-links-modal-close-footer-btn"]',
  },

  // ── Addresses (/app/applicant-info) ──────────────────────────────────────
  Addresses: {
    addBtn:           '[data-cy="address-add-btn"]',
    noAddressesMsg:   '[data-cy="no-addresses-message"]',
    modal:            '[data-cy="address-modal"]',
    modalLabel:       '[data-cy="add-address-modal-label"]',
    modalCloseBtn:    '[data-cy="address-modal-close-btn"]',
    typeSelect:       '[data-cy="address-type"]',
    isPrimaryToggle:  '[data-cy="address-is-primary"]',
    streetInput:      '[data-cy="address-street"]',
    unitInput:        '[data-cy="address-unit"]',
    street2Input:     '[data-cy="address-street-2"]',
    cityInput:        '[data-cy="address-city"]',
    provinceSelect:   '[data-cy="address-province"]',
    postalCodeInput:  '[data-cy="address-postal-code"]',
    modalCancelBtn:   '[data-cy="address-modal-cancel-btn"]',
    modalSaveBtn:     '[data-cy="address-modal-save-btn"]',
    deleteModal:      '[data-cy="address-delete-modal"]',
    deleteModalLabel: '[data-cy="delete-address-modal-label"]',
    deleteCancelBtn:  '[data-cy="address-delete-cancel-btn"]',
    deleteConfirmBtn: '[data-cy="address-delete-confirm-btn"]',
  },

  // ── Contacts (/app/applicant-info) ───────────────────────────────────────
  Contacts: {
    noPrimaryContact: '[data-cy="no-primary-contact"]',
    modal:            '[data-cy="contact-modal"]',
    modalLabel:       '[data-cy="add-contact-modal-label"]',
    modalCloseBtn:    '[data-cy="contact-modal-close-btn"]',
    roleSelect:       '[data-cy="contact-role"]',
    isPrimaryToggle:  '[data-cy="contact-is-primary"]',
    fullNameInput:    '[data-cy="contact-full-name"]',
    titleInput:       '[data-cy="contact-title"]',
    emailInput:       '[data-cy="contact-email"]',
    phoneInput:       '[data-cy="contact-phone"]',
    modalCancelBtn:   '[data-cy="contact-modal-cancel-btn"]',
    modalSaveBtn:     '[data-cy="contact-modal-save-btn"]',
    deleteModal:      '[data-cy="contact-delete-modal"]',
    deleteModalLabel: '[data-cy="delete-contact-modal-label"]',
    deleteCancelBtn:  '[data-cy="contact-delete-cancel-btn"]',
    deleteConfirmBtn: '[data-cy="contact-delete-confirm-btn"]',
  },

  // ── Organization (/app/applicant-info) ───────────────────────────────────
  Organization: {
    saveBtn:       '[data-cy="org-save-btn"]',
    cancelBtn:     '[data-cy="org-cancel-btn"]',
    editBtn:       '[data-cy="org-edit-btn"]',
    searchInput:   '[data-cy="org-search"]',
    searchResults: '[data-cy="org-search-results"]',
    nameField:     '[data-cy="org-name"]',
    regNumber:     '[data-cy="reg-number"]',
    statusField:   '[data-cy="org-status"]',
    typeField:     '[data-cy="org-type"]',
    nonRegOrgName: '[data-cy="non-reg-org-name"]',
    sizeField:     '[data-cy="org-size"]',
    fiscalMonth:   '[data-cy="fiscal-month"]',
    fiscalDay:     '[data-cy="fiscal-day"]',
  },

  // ── Payments page (/app/payments) ─────────────────────────────────────────
  Payments: {
    pageInner:   '[data-cy="payments-page-inner"]',
    card:        '[data-cy="payments-card"]',
    header:      '[data-cy="payments-header"]',
    // The three selectors below are generated at runtime by datatable.component via
    // [attr.data-cy]="'datatable-' + idSuffix" etc. (idSuffix="payments");
    // invisible to static validator.
    table:       '[data-cy="datatable-payments"]',
    searchInput: '[data-cy="datatable-search-payments"]',
    tableBody:   '[data-cy="datatable-body-payments"]',
  },

} as const;
