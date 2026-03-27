class PaymentsPage {
  get pageInner() {
    return cy.get(".page-inner");
  }

  get paymentsCard() {
    return this.pageInner.find(".card");
  }

  get pageHeader() {
    return this.paymentsCard.find(".card-header h3");
  }

  get paymentsTable() {
    return this.paymentsCard.find(".datatable");
  }

  get searchInput() {
    return this.paymentsCard.find("input.search-input");
  }

  get tableRows() {
    return this.paymentsTable.find("tbody .datatable-row");
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
