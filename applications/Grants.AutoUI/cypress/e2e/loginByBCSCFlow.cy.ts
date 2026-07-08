import { loginPage } from "../pages/LoginPage";
import { authenticatorPage } from "../pages/AuthenticatorPage";
import { bcServicesCardPage } from "../pages/BCServicesCardPage";
import { termsOfUsePage } from "../pages/TermsOfUsePage";
import { workspaceProviderSelectionPage } from "../pages/WorkspaceProviderSelectionPage";
import { landingPage } from "../pages/LandingPage";
import { navMenuPage } from "../pages/NavMenuPage";
import { paymentsPage } from "../pages/PaymentsPage";
import { AppSelectors } from "../selectors/registry";

// testIsolation: false — browser state is preserved between tests so the flow
// navigates forward once without resetting between each step.
describe(
  "Login by BC Services Card - Full Flow",
  { testIsolation: false },
  () => {
    const getRequiredEnv = (key: string): string => {
      const value = Cypress.env(key);
      if (!value) {
        throw new Error(
          `Missing required Cypress env variable "${key}". ` +
            "Provide it via --env, CI config, or a local cypress.env.json.",
        );
      }
      return String(value);
    };

    const username = () => getRequiredEnv("bcscUsername");
    const password = () => getRequiredEnv("bcscPassword");
    const currentEnv = String(
      Cypress.env("ENV") || Cypress.env("environment") || "",
    ).toLowerCase();
    const shouldValidatePayments = currentEnv !== "prod";
    const workspaceName = () => getRequiredEnv("workspaceName");
    const providerName = () => getRequiredEnv("providerName");

    const waitForPostLoginDestination = () => {
      cy.location("href", { timeout: 60000 }).should((href) => {
        expect(
          href.includes("acceptTerms") ||
            href.includes("/workspace-selector") ||
            href.includes("/app/"),
          `expected post-login destination, got ${href}`,
        ).to.be.true;
      });
    };

    const waitForWorkspaceSelectionOrApp = () => {
      cy.get("body", { timeout: 60000 }).should(($body) => {
        const pathname = $body[0].ownerDocument.location.pathname;
        const hasWorkspaceSelect =
          $body.find(AppSelectors.Workspace.workspaceSelect).length > 0;
        const hasProviderSelect =
          $body.find(AppSelectors.Workspace.providerSelect).length > 0;

        expect(
          pathname.startsWith("/app/") ||
            (pathname === "/workspace-selector" &&
              (hasWorkspaceSelect || hasProviderSelect)),
          `expected app route or workspace selection controls, got ${pathname}`,
        ).to.be.true;
      });
    };

    before(() => {
      // Ensure a clean logged-out browser state before linear, non-isolated flow steps.
      cy.clearCookies();
      cy.clearLocalStorage();
      loginPage.visit();
      cy.window().then((win) => {
        win.sessionStorage.clear();
      });
    });

    context("Step 1: Landing Page", () => {
      it("displays the login card and button", () => {
        loginPage.loginCard.should("be.visible");
        loginPage.loginButton.should("be.visible").and("contain.text", "Login");
      });

      it("clicks the login button", () => {
        loginPage.clickLogin();
      });
    });

    context("Step 2: Keycloak Authenticator", () => {
      it("displays the authenticator page with BC Services Card option", () => {
        authenticatorPage.verifyPageLoaded();
        authenticatorPage.bcServicesCardLink.should("be.visible").within(() => {
          cy.get(".kc-social-provider-name").should(
            "contain.text",
            "BC Services Card",
          );
        });
      });

      it("clicks BC Services Card", () => {
        authenticatorPage.clickBCServicesCard();
      });
    });

    context("Step 3: BC Services Card Device Selection", () => {
      it("displays the device selection page", () => {
        bcServicesCardPage.verifyPageLoaded();
      });

      it("shows the 'Test with username and password' tile and clicks it", () => {
        bcServicesCardPage.testWithUsernamePasswordTitle.should(
          "contain.text",
          "Test with username and password",
        );
        bcServicesCardPage.clickTestWithUsernamePassword();
      });
    });

    context("Step 4: BC Services Card Credential Form", () => {
      it("displays the username and password inputs and Continue button", () => {
        bcServicesCardPage.usernameInput.should("be.visible");
        bcServicesCardPage.passwordInput.should("be.visible");
        bcServicesCardPage.continueButton.should("be.visible");
      });

      it("enters username and password then clicks Continue", () => {
        bcServicesCardPage.enterUsername(username());
        bcServicesCardPage.usernameInput.should("have.value", username());
        bcServicesCardPage.enterPassword(password());
        bcServicesCardPage.clickContinue();
      });
    });

    context("Step 5: Terms of Use", () => {
      // The app skips this page when the user has already accepted terms server-side.
      // Conditionally verify and accept only when the page is actually presented.
      it("accepts terms if presented, then proceeds", () => {
        waitForPostLoginDestination();

        cy.location("href").then((href) => {
          if (href.includes("acceptTerms")) {
            termsOfUsePage.verifyPageLoaded();
            termsOfUsePage.acceptCheckbox.should("not.be.checked");
            termsOfUsePage.acceptAndContinue();
            waitForPostLoginDestination();
          }
        });
      });
    });

    context("Step 6: Workspace and Provider Selection", () => {
      it("selects workspace and continues", () => {
        waitForWorkspaceSelectionOrApp();

        cy.get("body").then(($body) => {
          const pathname = $body[0].ownerDocument.location.pathname;

          if (pathname.startsWith("/app/")) {
            return;
          }

          if ($body.find(AppSelectors.Workspace.workspaceSelect).length > 0) {
            workspaceProviderSelectionPage.verifyWorkspaceScreenLoaded();
            workspaceProviderSelectionPage.selectWorkspace(workspaceName());
            workspaceProviderSelectionPage.continueFromWorkspace();
          }
        });
      });

      it("waits for provider screen, selects provider, and continues", () => {
        waitForWorkspaceSelectionOrApp();

        cy.get("body").then(($body) => {
          const pathname = $body[0].ownerDocument.location.pathname;

          if (pathname.startsWith("/app/")) {
            return;
          }

          if ($body.find(AppSelectors.Workspace.providerSelect).length > 0) {
            workspaceProviderSelectionPage.verifyProviderScreenLoaded();
            workspaceProviderSelectionPage.selectProvider(providerName());
            workspaceProviderSelectionPage.continueFromProvider();
            cy.location("pathname", { timeout: 60000 }).should("include", "/app/");
          }
        });
      });
    });

    context("Step 7: Portal Landing Page", () => {
      it("displays all four dashboard cards", () => {
        landingPage.verifyPageLoaded();
      });

      it("shows the Organization Information card with org table", () => {
        landingPage.orgInfoCard
          .find("h3")
          .should("contain.text", "Organization Information");
        landingPage.verifyOrganizationContentLoaded();
      });

      it("shows the Submissions card with submissions table", () => {
        landingPage.submissionsCard
          .find("h3")
          .should("contain.text", "Submissions");
        landingPage.submissionsTable.should("be.visible");
      });

      it("shows the Contact Information card with Add button and primary contact", () => {
        landingPage.contactsCard
          .find("h3")
          .should("contain.text", "Contact Information");
        landingPage.contactsCard.then(($card) => {
          const addButton = $card.find(AppSelectors.Landing.addContactButton);

          if (addButton.length > 0 && addButton.is(":visible")) {
            landingPage.addContactButton
              .should("be.visible")
              .and("contain.text", "Add");
          }
        });
        landingPage.verifyContactsContentLoaded();
      });

      it("shows the Address Information card with primary address", () => {
        landingPage.addressesCard
          .find("h3")
          .should("contain.text", "Address Information");
        landingPage.verifyAddressesContentLoaded();
      });
    });

    context("Step 8: Navigation Menu and Workspace Indicator", () => {
      it("shows the Applicant Info and Payments nav links", () => {
        navMenuPage.verifyPrimaryNavItems();
      });

      it("shows the workspace dropdown button with selected workspace and provider", () => {
        navMenuPage.verifyWorkspaceSelection();
      });

      it("opens the workspace dropdown and validates menu contents", () => {
        navMenuPage.openWorkspaceDropdown();
        navMenuPage.verifyWorkspaceDropdownMenu();
        // Close dropdown explicitly so next click targets are not obscured.
        navMenuPage.closeWorkspaceDropdown();
      });

      it("clicks the Payments nav link", () => {
        if (!shouldValidatePayments) {
          cy.log("Skipping Payments navigation in prod.");
          return;
        }

        navMenuPage.clickPayments();
      });
    });

    context("Step 9: Payments Page", () => {
      it("navigates to the Payments page", () => {
        if (!shouldValidatePayments) {
          cy.log("Skipping Payments page assertions in prod.");
          return;
        }

        paymentsPage.verifyPageLoaded();
      });

      it("displays the payments search input", () => {
        if (!shouldValidatePayments) {
          cy.log("Skipping Payments page assertions in prod.");
          return;
        }

        paymentsPage.searchInput.should("be.visible");
      });

      it("displays payment rows in the table", () => {
        if (!shouldValidatePayments) {
          cy.log("Skipping Payments data assertions in prod.");
          return;
        }

        paymentsPage.tableRows.should("have.length.greaterThan", 0);
      });

      it("shows the core payments table columns", () => {
        if (!shouldValidatePayments) {
          cy.log("Skipping Payments page assertions in prod.");
          return;
        }

        paymentsPage.verifyCoreColumns();
      });
    });

    context("Step 10: User Header Dropdown and Logout", () => {
      it("displays the user avatar dropdown button in the header", () => {
        navMenuPage.userDropdownButton.should("be.visible");
      });

      it("opens the user dropdown and validates the logout option", () => {
        navMenuPage.openUserDropdown();
        navMenuPage.userDropdownMenu.should("be.visible");
        navMenuPage.logoutButton
          .should("be.visible")
          .and("have.attr", "href", "#");
      });
    });
  },
);
