class LandingPage {
  // Cards
  get orgInfoCard() {
    return cy.get("#card-organization");
  }

  get submissionsCard() {
    return cy.get("#card-submissions");
  }

  get contactsCard() {
    return cy.get("#card-contacts");
  }

  get addressesCard() {
    return cy.get("#card-addresses");
  }

  // Organization Information
  get orgTable() {
    return cy.get("#card-organization .orgbook-table");
  }

  // Submissions
  get submissionsTable() {
    return cy.get("#card-submissions .datatable");
  }

  // Contacts
  get addContactButton() {
    return cy.get("#contact-add-btn");
  }

  get primaryContactInfo() {
    return cy.get("#primary-contact-info");
  }

  // Addresses
  get primaryAddressInfo() {
    return cy.get("#primary-address-info");
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
