using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "announcements",
                columns: table => new
                {
                    announcement_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    author_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    department_id = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcements", x => x.announcement_id);
                    table.ForeignKey(
                        name: "FK_announcements_AspNetUsers_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_announcements_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "department_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "connections",
                columns: table => new
                {
                    connection_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    receiver_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connections", x => x.connection_id);
                    table.ForeignKey(
                        name: "FK_connections_AspNetUsers_receiver_user_id",
                        column: x => x.receiver_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_connections_AspNetUsers_sender_user_id",
                        column: x => x.sender_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    receiver_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    is_read = table.Column<bool>(type: "INTEGER", nullable: false),
                    sent_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.message_id);
                    table.ForeignKey(
                        name: "FK_messages_AspNetUsers_receiver_user_id",
                        column: x => x.receiver_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_messages_AspNetUsers_sender_user_id",
                        column: x => x.sender_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_announcements_author_user_id",
                table: "announcements",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_announcements_department_id",
                table: "announcements",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_connections_receiver_user_id",
                table: "connections",
                column: "receiver_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_connections_sender_user_id",
                table: "connections",
                column: "sender_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_messages_receiver_user_id",
                table: "messages",
                column: "receiver_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_messages_sender_user_id",
                table: "messages",
                column: "sender_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "announcements");

            migrationBuilder.DropTable(
                name: "connections");

            migrationBuilder.DropTable(
                name: "messages");
        }
    }
}
