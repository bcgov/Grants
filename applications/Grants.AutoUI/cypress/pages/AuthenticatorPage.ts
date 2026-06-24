import { ExternalSelectors } from '../selectors/external-registry';

export class AuthenticatorPage {
  // ── Selectors ───────────────────────────────────────────────────────────────
  get cardContainer() {
    return cy.get(ExternalSelectors.Keycloak.cardContainer);
  }

  get pageTitle() {
    return cy.get(ExternalSelectors.Keycloak.pageTitle);
  }

  get socialProvidersSection() {
    return cy.get(ExternalSelectors.Keycloak.socialProviders);
  }

  get idirMfaLink() {
    return cy.get(ExternalSelectors.Keycloak.idirLink);
  }

  get bceidLink() {
    return cy.get(ExternalSelectors.Keycloak.bceidLink);
  }

  get bcServicesCardLink() {
    return cy.get(ExternalSelectors.Keycloak.bcServicesCard);
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
