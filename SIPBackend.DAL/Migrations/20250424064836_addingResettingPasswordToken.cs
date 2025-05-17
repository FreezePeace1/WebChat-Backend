using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIPBackend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class addingResettingPasswordToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TokenExpires",
                table: "AspNetUsers",
                newName: "RefreshTokenExpires");

            migrationBuilder.RenameColumn(
                name: "TokenCreated",
                table: "AspNetUsers",
                newName: "RefreshTokenCreated");

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetPasswordTokenExpires",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetPasswordTokenExpires",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "RefreshTokenExpires",
                table: "AspNetUsers",
                newName: "TokenExpires");

            migrationBuilder.RenameColumn(
                name: "RefreshTokenCreated",
                table: "AspNetUsers",
                newName: "TokenCreated");
        }
    }
}
