using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApiKeysSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "KeyHash",
                table: "ApiKeys",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAtUtc",
                table: "ApiKeys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KeyIterations",
                table: "ApiKeys",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "KeyPrefix",
                table: "ApiKeys",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KeySalt",
                table: "ApiKeys",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUsedAtUtc",
                table: "ApiKeys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Scopes",
                table: "ApiKeys",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyPrefix",
                table: "ApiKeys",
                column: "KeyPrefix");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_KeyPrefix",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "KeyIterations",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "KeyPrefix",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "KeySalt",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "LastUsedAtUtc",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "Scopes",
                table: "ApiKeys");

            migrationBuilder.AlterColumn<string>(
                name: "KeyHash",
                table: "ApiKeys",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);
        }
    }
}
