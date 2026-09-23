using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_opportunities_AspNetUsers_organizer_id",
                table: "opportunities");

            migrationBuilder.DropForeignKey(
                name: "FK_opportunities_departments_target_department_id",
                table: "opportunities");

            migrationBuilder.AddForeignKey(
                name: "FK_opportunities_AspNetUsers_organizer_id",
                table: "opportunities",
                column: "organizer_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_opportunities_departments_target_department_id",
                table: "opportunities",
                column: "target_department_id",
                principalTable: "departments",
                principalColumn: "department_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_opportunities_AspNetUsers_organizer_id",
                table: "opportunities");

            migrationBuilder.DropForeignKey(
                name: "FK_opportunities_departments_target_department_id",
                table: "opportunities");

            migrationBuilder.AddForeignKey(
                name: "FK_opportunities_AspNetUsers_organizer_id",
                table: "opportunities",
                column: "organizer_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_opportunities_departments_target_department_id",
                table: "opportunities",
                column: "target_department_id",
                principalTable: "departments",
                principalColumn: "department_id");
        }
    }
}
