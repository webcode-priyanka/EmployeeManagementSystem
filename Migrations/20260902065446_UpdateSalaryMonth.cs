using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSalaryMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Salaries");

            migrationBuilder.RenameColumn(
                name: "SalaryMonth",
                table: "Salaries",
                newName: "Month");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Month",
                table: "Salaries",
                newName: "SalaryMonth");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Salaries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
