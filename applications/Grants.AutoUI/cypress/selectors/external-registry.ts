// ─────────────────────────────────────────────────────────────────────────────
// External Selector Registry
//
// Selectors for third-party pages this project does NOT own:
//   Keycloak, BC Services Card, BCeID
//
// These are NEVER validated against the Angular app source — they live on
// external identity provider pages and can only be updated manually when those
// providers change their UI.
// ─────────────────────────────────────────────────────────────────────────────

export const ExternalSelectors = {

  // ── Keycloak authenticator chooser ───────────────────────────────────────
  Keycloak: {
    cardContainer:    '.card-pf',
    pageTitle:        '#kc-page-title',
    socialProviders:  '#kc-social-providers',
    idirLink:         '#social-azureidir',
    bceidLink:        '#social-bceidboth',
    bcServicesCard:   '#social-grants-portal-5361',
  },

  // ── BC Services Card — device selection + credential form ────────────────
  BCServicesCard: {
    testWithPasswordTile:  '#tile_btn_test_with_username_password_device_div_id',
    testWithPasswordImage: '#image_test_with_username_password_device_div_id',
    tileTitleHeading:      'h2',
    usernameInput:         '#username',
    passwordInput:         '#password',
    continueButton:        '#submit-btn',
  },

  // ── BC Services Card — Terms of Use page ─────────────────────────────────
  BCServicesCardTerms: {
    sectionHeading: '.section h2',
    acceptCheckbox: '#accept',
    // Checkbox is display:none — click the label as the visible interaction target
    acceptLabel:    'label[for="accept"]',
    continueButton: '#btnSubmit',
  },

  // ── BCeID login page ──────────────────────────────────────────────────────
  BCeID: {
    panelContainer: '.panel',
    panelHeading:   '.panel-heading',
    logo:           '#bceidLogo',
    userIdInput:    '#user',
    passwordInput:  '#password',
    continueButton: 'input[name="btnSubmit"]',
    errorContainer: '.bg-error',
    errorMessage:   '.field-help-text',
    forgotLink:     '.link-forgot',
  },

} as const;
