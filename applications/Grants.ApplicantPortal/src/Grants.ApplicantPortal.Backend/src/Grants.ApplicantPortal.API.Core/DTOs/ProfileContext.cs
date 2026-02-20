namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Context information for profile-related operations
/// </summary>
public record ProfileContext(
  Guid ProfileId,
  string PluginId,
  string Provider);
