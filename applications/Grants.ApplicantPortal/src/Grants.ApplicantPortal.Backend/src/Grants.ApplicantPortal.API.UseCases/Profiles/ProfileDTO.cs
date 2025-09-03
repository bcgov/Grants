namespace Grants.ApplicantPortal.API.UseCases.Profiles;

public record ProfileDto(Guid ProfileId, string PluginId, string JsonData, DateTime PopulatedAt);

