export class AuthenticatorPage {
  // ── Selectors ───────────────────────────────────────────────────────────────
  get cardContainer() {
    return cy.get(".card-pf");
  }

  get pageTitle() {
    return cy.get("#kc-page-title");
  }

  get socialProvidersSection() {
    return cy.get("#kc-social-providers");
  }

  get idirMfaLink() {
    return cy.get("#social-azureidir");
  }

  get bceidLink() {
    return cy.get("#social-bceidboth");
  }

  get bcServicesCardLink() {
    return cy.get("#social-grants-portal-5361");
  }

  // ── Actions ─────────────────────────────────────────────────────────────────
  clickBCServicesCard(): void {
    this.bcServicesCardLink.click();
  }

  verifyPageLoaded(): void {
    this.cardContainer.should("be.visible");
    this.pageTitle.should("contain.text", "Authenticate with:");
    this.socialProvidersSection.should("be.visible");
  }
}

export const authenticatorPage = new AuthenticatorPage();
