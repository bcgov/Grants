using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grants.ApplicantPortal.API.Infrastructure.Data.Migrations;

  /// <inheritdoc />
  public partial class AddProfileSubject : Migration
  {
      /// <inheritdoc />
      protected override void Up(MigrationBuilder migrationBuilder)
      {
          migrationBuilder.AddColumn<string>(
              name: "Subject",
              table: "Profiles",
              type: "TEXT",
              nullable: false,
              defaultValue: "");
      }

      /// <inheritdoc />
      protected override void Down(MigrationBuilder migrationBuilder)
      {
          migrationBuilder.DropColumn(
              name: "Subject",
              table: "Profiles");
      }
  }
