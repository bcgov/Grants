import { loginPage } from "../../pages/LoginPage";
import { authenticatorPage } from "../../pages/AuthenticatorPage";
import { bcServicesCardPage } from "../../pages/BCServicesCardPage";
import { termsOfUsePage } from "../../pages/TermsOfUsePage";
import { workspaceProviderSelectionPage } from "../../pages/WorkspaceProviderSelectionPage";

export class LoginFlows {
  /**
   * Performs the full BC Services Card login flow:
   *   1. Visit the app landing page
   *   2. Click Login
   *   3. Click BC Services Card on the Keycloak authenticator
   *   4. Select "Test with username and password" device
   *   5. Enter credentials and continue
   *   6. Accept Terms of Use and continue
   *   7. Select workspace and continue
   *   8. Select provider and continue
   */
  loginByBCSC(
    username: string,
    password: string,
    workspaceName: string,
    providerName: string,
  ): void {
    // Ensure we always start from a clean auth state when this helper is called.
    cy.clearCookies();
    cy.clearLocalStorage();

    loginPage.visit();
    loginPage.clickLogin();
    authenticatorPage.clickBCServicesCard();
    bcServicesCardPage.clickTestWithUsernamePassword();
    bcServicesCardPage.submitCredentials(username, password);
    termsOfUsePage.acceptAndContinue();
    workspaceProviderSelectionPage.completeSelection(
      workspaceName,
      providerName,
    );
  }
}

export const loginFlows = new LoginFlows();
