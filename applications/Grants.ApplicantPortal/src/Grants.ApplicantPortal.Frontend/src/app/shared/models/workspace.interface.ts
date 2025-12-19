export interface Plugin {
  pluginId: string;
  description: string;
  features: string[];
}

export interface PluginsResponse {
  plugins: Plugin[];
}

export interface WorkspaceState {
  selectedWorkspace: Plugin | null;
  availableWorkspaces: Plugin[];
  isWorkspaceSelected: boolean;
}