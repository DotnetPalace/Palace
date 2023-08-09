using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Palace.Server.Migrations
{
    /// <inheritdoc />
    public partial class Creation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MicroServiceSetting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceName = table.Column<string>(type: "TEXT", nullable: false),
                    MainAssembly = table.Column<string>(type: "TEXT", nullable: false),
                    Arguments = table.Column<string>(type: "TEXT", nullable: true),
                    AlwaysStarted = table.Column<bool>(type: "INTEGER", nullable: false),
                    PackageFileName = table.Column<string>(type: "TEXT", nullable: false),
                    GroupName = table.Column<string>(type: "TEXT", nullable: true),
                    ThreadLimitBeforeRestart = table.Column<int>(type: "INTEGER", nullable: true),
                    ThreadLimitBeforeAlert = table.Column<int>(type: "INTEGER", nullable: true),
                    NoHealthCheckCountBeforeRestart = table.Column<int>(type: "INTEGER", nullable: true),
                    NoHealthCheckCountCountBeforeAlert = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxWorkingSetLimitBeforeRestart = table.Column<long>(type: "INTEGER", nullable: true),
                    MaxWorkingSetLimitBeforeAlert = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MicroServiceSetting", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MicroServiceSetting");
        }
    }
}
