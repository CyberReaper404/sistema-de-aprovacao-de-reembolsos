using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reembolso.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReimbursementDecisionPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReasonCode",
                table: "workflow_actions",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ComplementationRequestedAt",
                table: "reimbursement_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionComment",
                table: "reimbursement_requests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionReasonCode",
                table: "reimbursement_requests",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasPendingComplementation",
                table: "reimbursement_requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptRequiredAlways",
                table: "reimbursement_categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionDeadlineDays",
                table: "reimbursement_categories",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReasonCode",
                table: "workflow_actions");

            migrationBuilder.DropColumn(
                name: "ComplementationRequestedAt",
                table: "reimbursement_requests");

            migrationBuilder.DropColumn(
                name: "DecisionComment",
                table: "reimbursement_requests");

            migrationBuilder.DropColumn(
                name: "DecisionReasonCode",
                table: "reimbursement_requests");

            migrationBuilder.DropColumn(
                name: "HasPendingComplementation",
                table: "reimbursement_requests");

            migrationBuilder.DropColumn(
                name: "ReceiptRequiredAlways",
                table: "reimbursement_categories");

            migrationBuilder.DropColumn(
                name: "SubmissionDeadlineDays",
                table: "reimbursement_categories");
        }
    }
}
