class LandingPage {
  // Cards
  get orgInfoCard() {
    return cy.get('[data-cy="card-organization"]');
  }

  get submissionsCard() {
    return cy.get('[data-cy="card-submissions"]');
  }

  get contactsCard() {
    return cy.get('[data-cy="card-contacts"]');
  }

  get addressesCard() {
    return cy.get('[data-cy="card-addresses"]');
  }

  // Organization Information
  get orgTable() {
    return cy.get('[data-cy="card-organization"] .orgbook-table');
  }

  // Submissions
  get submissionsTable() {
    return cy.get('[data-cy="datatable-submissions"]');
  }

  // Contacts
  get addContactButton() {
    return cy.get('[data-cy="contact-add-btn"]');
  }

  get primaryContactInfo() {
    return cy.get('[data-cy="primary-contact-info"]');
  }

  // Addresses
  get primaryAddressInfo() {
    return cy.get('[data-cy="primary-address-info"]');
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
