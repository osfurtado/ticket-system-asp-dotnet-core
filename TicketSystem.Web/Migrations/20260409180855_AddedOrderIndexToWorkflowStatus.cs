using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddedOrderIndexToWorkflowStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "WorkflowStatus",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "WorkflowStatus");
        }
    }
}
