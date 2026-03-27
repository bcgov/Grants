export class TermsOfUsePage {
  // ── Selectors ───────────────────────────────────────────────────────────────
  get termsHeading() {
    return cy.get(".section h2").first();
  }

  get acceptCheckbox() {
    return cy.get("#accept");
  }

  get acceptLabel() {
    return cy.get('label[for="accept"]');
  }

  get continueButton() {
    return cy.get("#btnSubmit");
  }

  // ── Actions ─────────────────────────────────────────────────────────────────
  verifyPageLoaded(): void {
    this.termsHeading.should("contain.text", "Terms of Use");
    this.acceptCheckbox.should("exist");
    this.continueButton.should("be.visible");
  }

  acceptTerms(): void {
    // The checkbox has display:none — click the label which is the visible interaction target
    this.acceptLabel.click();
  }

  clickContinue(): void {
    this.continueButton.click();
  }

  acceptAndContinue(): void {
    this.acceptTerms();
    this.clickContinue();
  }
}

export const termsOfUsePage = new TermsOfUsePage();
