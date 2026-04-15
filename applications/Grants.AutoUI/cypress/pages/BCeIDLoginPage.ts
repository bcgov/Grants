export class BCeIDLoginPage {
  // ── Selectors ───────────────────────────────────────────────────────────────
  get panelContainer() {
    return cy.get(".panel");
  }

  get panelHeading() {
    return cy.get(".panel-heading");
  }

  get bceidLogo() {
    return cy.get("#bceidLogo");
  }

  get userIdInput() {
    return cy.get("#user");
  }

  get passwordInput() {
    return cy.get("#password");
  }

  get continueButton() {
    return cy.get('input[name="btnSubmit"]');
  }

  get errorContainer() {
    return cy.get(".bg-error");
  }

  get errorMessage() {
    return cy.get(".field-help-text");
  }

  get forgotLink() {
    return cy.get(".link-forgot");
  }

  // ── Actions ─────────────────────────────────────────────────────────────────
  verifyPageLoaded(): void {
    this.panelContainer.should("be.visible");
    this.panelHeading.should("contain.text", "Log in with");
    this.bceidLogo.should("be.visible");
  }

  enterUserId(username: string): void {
    this.userIdInput.clear().type(username);
  }

  enterPassword(password: string): void {
    this.passwordInput.clear().type(password);
  }

  clickContinue(): void {
    this.continueButton.click();
  }

  submitCredentials(username: string, password: string): void {
    this.enterUserId(username);
    this.enterPassword(password);
    this.clickContinue();
  }

  verifyErrorVisible(): void {
    this.errorContainer.should("not.have.class", "hidden");
  }

  getErrorText(): Cypress.Chainable<string> {
    return this.errorMessage.invoke("text");
  }

  verifyForgotLink(): void {
    this.forgotLink
      .should("be.visible")
      .and(
        "have.attr",
        "href",
        "https://www.development.bceid.ca/clp/account_recovery.aspx",
      )
      .and("have.text", "Forgot your user ID or password?");
  }
}

export const bceidLoginPage = new BCeIDLoginPage();
