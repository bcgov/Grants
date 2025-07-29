namespace Grants.ApplicantPortal.API.Web.Configurations;

public class KeycloakConfiguration
{
    public const string SectionName = "Keycloak";
    
    public string AuthServerUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public bool SslRequired { get; set; } = true;
    public int ConfidentialPort { get; set; } = 0;
    public KeycloakCredentials Credentials { get; set; } = new();
}

public class KeycloakCredentials
{
    public string Secret { get; set; } = string.Empty;
}
