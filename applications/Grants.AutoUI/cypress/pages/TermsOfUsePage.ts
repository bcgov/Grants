import { ExternalSelectors } from '../selectors/external-registry';

export class TermsOfUsePage {
  // ── Selectors ───────────────────────────────────────────────────────────────
  get termsHeading() {
    return cy.get(ExternalSelectors.BCServicesCardTerms.sectionHeading).first();
  }

  get acceptCheckbox() {
    return cy.get(ExternalSelectors.BCServicesCardTerms.acceptCheckbox);
  }

  get acceptLabel() {
    return cy.get(ExternalSelectors.BCServicesCardTerms.acceptLabel);
  }

  get continueButton() {
    return cy.get(ExternalSelectors.BCServicesCardTerms.continueButton);
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
