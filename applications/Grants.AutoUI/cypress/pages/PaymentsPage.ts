class PaymentsPage {
  get pageInner() {
    return cy.get('[data-cy="payments-page-inner"]');
  }

  get paymentsCard() {
    return cy.get('[data-cy="payments-card"]');
  }

  get pageHeader() {
    return cy.get('[data-cy="payments-header"]');
  }

  get paymentsTable() {
    return cy.get('[data-cy="datatable-payments"]').find('table');
  }

  get searchInput() {
    return cy.get('[data-cy="datatable-search-payments"]');
  }

  get tableRows() {
    return cy.get('[data-cy="datatable-body-payments"]').find('tr');
  }

  verifyPageLoaded(): void {
    cy.url().should("include", "/app/payments");
    this.pageHeader.should("contain.text", "Payments");
    this.paymentsTable.should("be.visible");
  }

  verifyCoreColumns(): void {
    this.paymentsTable.within(() => {
      cy.contains("th", "Payment ID").should("be.visible");
      cy.contains("th", "Submission #").should("be.visible");
      cy.contains("th", "Payment Status").should("be.visible");
      cy.contains("th", "Paid Amount").should("be.visible");
      cy.contains("th", "Paid Date").should("be.visible");
    });
  }
}

export const paymentsPage = new PaymentsPage();
