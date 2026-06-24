import { ExternalSelectors } from '../selectors/external-registry';

export class BCServicesCardPage {
  // ── Device Selection Selectors ───────────────────────────────────────────────
  get testWithUsernamePasswordTile() {
    return cy.get(ExternalSelectors.BCServicesCard.testWithPasswordTile);
  }

  get testWithUsernamePasswordTitle() {
    return this.testWithUsernamePasswordTile.find(ExternalSelectors.BCServicesCard.tileTitleHeading);
  }

  get testWithUsernamePasswordImage() {
    return cy.get(ExternalSelectors.BCServicesCard.testWithPasswordImage);
  }

  // ── Credential Form Selectors ────────────────────────────────────────────────
  get usernameInput() {
    return cy.get(ExternalSelectors.BCServicesCard.usernameInput);
  }

  get passwordInput() {
    return cy.get(ExternalSelectors.BCServicesCard.passwordInput);
  }

  get continueButton() {
    return cy.get(ExternalSelectors.BCServicesCard.continueButton);
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
