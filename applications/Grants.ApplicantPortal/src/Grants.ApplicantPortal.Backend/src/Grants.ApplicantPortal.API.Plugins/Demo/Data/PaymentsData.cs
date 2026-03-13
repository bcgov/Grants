namespace Grants.ApplicantPortal.API.Plugins.Demo.Data;

/// <summary>
/// Static data provider for demo payment information.
/// Field names match the Unity API PAYMENTINFO contract (id, paymentNumber,
/// referenceNo, amount, paymentDate, paymentStatus).
/// </summary>
public static class PaymentsData
{
  public static object GenerateProgram1Payments(object baseData)
  {
    return new
    {
      payments = new[]
      {
        new
        {
          id = "36102811-9cc2-4161-bc7e-06a55a15597a",
          paymentNumber = "",
          referenceNo = "DEMO-2025-0001",
          amount = 333.33m,
          paymentDate = (string?)null,
          paymentStatus = "L1Pending"
        },
        new
        {
          id = "30c5e776-559c-4a0f-89ff-705cdfc7dc4d",
          paymentNumber = "CFA-123",
          referenceNo = "DEMO-2025-0003",
          amount = 152.34m,
          paymentDate = (string?)"2025-03-24 18:11:35.328",
          paymentStatus = "L1Pending"
        },
        new
        {
          id = "f2ceb478-21b5-4031-b812-c2dbaa023f4f",
          paymentNumber = "BBB-456",
          referenceNo = "DEMO-2025-0002",
          amount = 1111.11m,
          paymentDate = (string?)"2025-03-24 18:11:35.327",
          paymentStatus = "L2Pending"
        }
      }
    };
  }

  public static object GenerateProgram2Payments(object baseData)
  {
    return new
    {
      payments = new[]
      {
        new
        {
          id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
          paymentNumber = "EDU-001",
          referenceNo = "DEMO-2025-1001",
          amount = 5000.00m,
          paymentDate = (string?)"2025-04-10 09:30:00.000",
          paymentStatus = "L1Pending"
        },
        new
        {
          id = "b2c3d4e5-f6a7-8901-bcde-f12345678901",
          paymentNumber = "",
          referenceNo = "DEMO-2025-1002",
          amount = 2500.00m,
          paymentDate = (string?)null,
          paymentStatus = "L2Pending"
        }
      }
    };
  }
}
