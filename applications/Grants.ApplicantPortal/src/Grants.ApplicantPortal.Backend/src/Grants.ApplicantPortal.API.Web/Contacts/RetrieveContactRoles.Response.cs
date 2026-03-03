namespace Grants.ApplicantPortal.API.Web.Contacts;

public record RetrieveContactRolesResponse(
    string PluginId,
    IReadOnlyList<ContactRoleDto> Roles
);

public record ContactRoleDto(
    string Key,
    string Label
);
