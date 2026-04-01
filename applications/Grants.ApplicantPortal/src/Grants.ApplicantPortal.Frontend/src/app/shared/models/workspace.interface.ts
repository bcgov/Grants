export interface Provider {
  id: string;
  name: string;
}

export interface ProvidersResponse {
  pluginId: string;
  providers: Provider[];
}

export interface Plugin {
  pluginId: string;
  description: string;
  features: string[];
  providers: string[];
}

export interface WorkspaceSelection {
  workspace: Plugin;
  provider: string;
}

export interface PluginsResponse {
  plugins: Plugin[];
}

export interface WorkspaceState {
  selectedWorkspace: Plugin | null;
  selectedProvider: string | null;
  selectedProviderName: string | null;
  availableWorkspaces: Plugin[];
  isWorkspaceSelected: boolean;
  isProviderSelected: boolean;
  hasMultipleOrgs: boolean;
  applicantId: string | null;
  orgNumber: string;
  orgName: string;
}