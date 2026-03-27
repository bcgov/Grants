export class BCServicesCardPage {
  // ── Device Selection Selectors ───────────────────────────────────────────────
  get testWithUsernamePasswordTile() {
    return cy.get("#tile_btn_test_with_username_password_device_div_id");
  }

  get testWithUsernamePasswordTitle() {
    return this.testWithUsernamePasswordTile.find("h2");
  }

  get testWithUsernamePasswordImage() {
    return cy.get("#image_test_with_username_password_device_div_id");
  }

  // ── Credential Form Selectors ────────────────────────────────────────────────
  get usernameInput() {
    return cy.get("#username");
  }

  get passwordInput() {
    return cy.get("#password");
  }

  get continueButton() {
    return cy.get("#submit-btn");
  }

  // ── Actions ─────────────────────────────────────────────────────────────────
  verifyPageLoaded(): void {
    this.testWithUsernamePasswordTile.should("be.visible");
    this.testWithUsernamePasswordTitle.should(
      "contain.text",
      "Test with username and password",
    );
  }

  clickTestWithUsernamePassword(): void {
    this.testWithUsernamePasswordTile.click();
  }

  enterUsername(username: string): void {
    this.usernameInput.should("be.visible").clear().type(username);
  }

  enterPassword(password: string): void {
    this.passwordInput.should("be.visible").clear().type(password);
  }

  clickContinue(): void {
    this.continueButton.should("be.visible").click();
  }

  submitCredentials(username: string, password: string): void {
    this.enterUsername(username);
    this.enterPassword(password);
    this.clickContinue();
  }
}

export const bcServicesCardPage = new BCServicesCardPage();
