import { AppSelectors } from '../selectors/registry';

export class LoginPage {
  // ── Selectors ───────────────────────────────────────────────────────────────
  get loginCard() {
    return cy.get(AppSelectors.Login.card);
  }
  get loginButton() {
    return cy.get(AppSelectors.Login.button);
  }

  // ── Actions ─────────────────────────────────────────────────────────────────
  visit(): void {
    cy.visit("/");
  }

  clickLogin(): void {
    this.loginButton.click();
  }
}

export const loginPage = new LoginPage();
