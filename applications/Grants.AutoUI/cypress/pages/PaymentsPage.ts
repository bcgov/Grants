import { AppSelectors } from '../selectors/registry';

class PaymentsPage {
  get pageInner() {
    return cy.get(AppSelectors.Payments.pageInner);
  }

  get paymentsCard() {
    return cy.get(AppSelectors.Payments.card);
  }

  get pageHeader() {
    return cy.get(AppSelectors.Payments.header);
  }

  get paymentsTable() {
    return cy.get(AppSelectors.Payments.table).find('table');
  }

  get searchInput() {
    return cy.get(AppSelectors.Payments.searchInput);
  }

  get tableRows() {
    return cy.get(AppSelectors.Payments.tableBody).find('tr');
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
