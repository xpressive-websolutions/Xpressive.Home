using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Xpressive.Home.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Device",
                columns: table => new
                {
                    Gateway = table.Column<string>(maxLength: 64, nullable: false),
                    Id = table.Column<string>(maxLength: 64, nullable: false),
                    Name = table.Column<string>(maxLength: 64, nullable: false),
                    Properties = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Device", x => new { x.Gateway, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "Radio",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 16, nullable: false),
                    Name = table.Column<string>(maxLength: 128, nullable: false),
                    ImageUrl = table.Column<string>(maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Radio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Room",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(nullable: false),
                    Icon = table.Column<string>(maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Room", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomScript",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    GroupId = table.Column<Guid>(nullable: false),
                    ScriptId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomScript", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomScriptGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    RoomId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(nullable: false),
                    Icon = table.Column<string>(maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomScriptGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledScript",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ScriptId = table.Column<Guid>(nullable: false),
                    CronTab = table.Column<string>(maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledScript", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Script",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 64, nullable: false),
                    Name = table.Column<string>(maxLength: 64, nullable: false),
                    JavaScript = table.Column<string>(nullable: false),
                    IsEnabled = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Script", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TriggeredScript",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ScriptId = table.Column<Guid>(nullable: false),
                    Variable = table.Column<string>(maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriggeredScript", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Variable",
                columns: table => new
                {
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    DataType = table.Column<string>(maxLength: 15, nullable: false),
                    Value = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variable", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "WebHook",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 32, nullable: false),
                    GatewayName = table.Column<string>(maxLength: 16, nullable: false),
                    DeviceId = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebHook", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomDevice",
                columns: table => new
                {
                    Gateway = table.Column<string>(maxLength: 16, nullable: false),
                    Id = table.Column<string>(maxLength: 64, nullable: false),
                    RoomId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomDevice", x => new { x.Gateway, x.Id });
                    table.ForeignKey(
                        name: "FK_RoomDevice_Room",
                        column: x => x.RoomId,
                        principalTable: "Room",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomDevice_RoomId",
                table: "RoomDevice",
                column: "RoomId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Device");

            migrationBuilder.DropTable(
                name: "Radio");

            migrationBuilder.DropTable(
                name: "RoomDevice");

            migrationBuilder.DropTable(
                name: "RoomScript");

            migrationBuilder.DropTable(
                name: "RoomScriptGroup");

            migrationBuilder.DropTable(
                name: "ScheduledScript");

            migrationBuilder.DropTable(
                name: "Script");

            migrationBuilder.DropTable(
                name: "TriggeredScript");

            migrationBuilder.DropTable(
                name: "Variable");

            migrationBuilder.DropTable(
                name: "WebHook");

            migrationBuilder.DropTable(
                name: "Room");
        }
    }
}
