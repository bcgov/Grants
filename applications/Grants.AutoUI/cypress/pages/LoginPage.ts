export class LoginPage {
  // ── Selectors ───────────────────────────────────────────────────────────────
  get loginCard() {
    return cy.get('[data-cy="login-card"]');
  }
  get loginButton() {
    return cy.get('[data-cy="login-btn"]');
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
