namespace Grants.ApplicantPortal.API.Web.Profiles;

public class GetProfileByIdRequest
{
  public const string Route = "/Profiles/{ProfileId:Guid}";
  public static string BuildRoute(Guid profileId) => Route.Replace("{ProfileId:Guid}", profileId.ToString());

  public Guid ProfileId { get; set; }
}
