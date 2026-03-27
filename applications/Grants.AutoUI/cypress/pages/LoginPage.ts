export class LoginPage {
  // ── Selectors ───────────────────────────────────────────────────────────────
  get loginCard() {
    return cy.get("#login-card");
  }
  get loginButton() {
    return cy.get("#login-btn");
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
