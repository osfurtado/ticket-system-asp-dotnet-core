using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddedInviteTokenToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InviteToken",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InviteToken",
                table: "Projects");
        }
    }
}
