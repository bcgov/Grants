import { AppSelectors } from '../selectors/registry';

class LandingPage {
  // ── Cards ─────────────────────────────────────────────────────────────────
  get orgInfoCard() {
    return cy.get(AppSelectors.Landing.orgInfoCard);
  }

  get submissionsCard() {
    return cy.get(AppSelectors.Landing.submissionsCard);
  }

  get contactsCard() {
    return cy.get(AppSelectors.Landing.contactsCard);
  }

  get addressesCard() {
    return cy.get(AppSelectors.Landing.addressesCard);
  }

  // ── Organization Information ───────────────────────────────────────────────
  get orgTable() {
    return cy.get(AppSelectors.Landing.orgTable);
  }

  // ── Submissions ───────────────────────────────────────────────────────────
  get submissionsTable() {
    return cy.get(AppSelectors.Landing.submissionsTable);
  }

  // ── Contacts ─────────────────────────────────────────────────────────────
  get addContactButton() {
    return cy.get(AppSelectors.Landing.addContactButton);
  }

  get primaryContactInfo() {
    return cy.get(AppSelectors.Landing.primaryContactInfo);
  }

  // ── Addresses ────────────────────────────────────────────────────────────
  get primaryAddressInfo() {
    return cy.get(AppSelectors.Landing.primaryAddressInfo);
  }

  verifyPageLoaded(): void {
    // Use path assertion to avoid failures caused by baseUrl formatting differences.
    cy.url().should("include", "/app/");
    this.orgInfoCard.should("be.visible");
    this.submissionsCard.should("be.visible");
    this.contactsCard.should("be.visible");
    this.addressesCard.should("be.visible");
  }
}

export const landingPage = new LandingPage();
