using FluentAssertions;

namespace Darwin.WebAdmin.Tests.Smoke;

public sealed class HumanResourcesWebAdminSourceTests
{
    [Fact]
    public void HrCoreViews_Should_Render_Internal_Admin_Surface_Without_Finance_Payroll_Or_Public_Mutations()
    {
        var root = RepositoryRoot();
        var layout = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Shared", "_Layout.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "HumanResources", "HumanResourcesController.cs"));
        var viewsRoot = Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "HumanResources");
        var combined = string.Join(Environment.NewLine, Directory.GetFiles(viewsRoot, "*.cshtml").Select(File.ReadAllText));

        layout.Should().Contain("asp-controller=\"HumanResources\"");
        layout.Should().Contain("asp-action=\"Employees\"");
        layout.Should().Contain("asp-action=\"WorkSchedules\"");
        layout.Should().Contain("asp-action=\"AttendanceEvents\"");
        layout.Should().Contain("asp-action=\"TimeEntries\"");
        layout.Should().Contain("asp-action=\"Timesheets\"");
        layout.Should().Contain("asp-action=\"LeaveRequests\"");
        layout.Should().Contain("asp-action=\"Absences\"");
        layout.Should().Contain("asp-action=\"PayrollPeriods\"");
        layout.Should().Contain("asp-action=\"PayrollRules\"");
        layout.Should().Contain("asp-action=\"PayrollRuns\"");
        controller.Should().Contain("CreateEmployee");
        controller.Should().Contain("CreateTimesheet");
        controller.Should().Contain("CreateLeaveRequest");
        controller.Should().Contain("CreateAbsence");
        controller.Should().Contain("CreatePayrollPeriod");
        controller.Should().Contain("PreparePayrollPeriod");
        controller.Should().Contain("CreatePayrollRuleSet");
        controller.Should().Contain("AddPayrollRuleComponent");
        controller.Should().Contain("CreatePayrollRun");
        controller.Should().Contain("CalculatePayrollRun");
        controller.Should().Contain("UploadEmployeeDocument");
        controller.Should().Contain("DownloadEmployeeDocument");
        controller.Should().Contain("ArchiveEmployeeDocument");
        combined.Should().Contain("@Html.AntiForgeryToken()");
        combined.Should().Contain("RowVersion");
        combined.Should().Contain("PersonnelDocuments");

        combined.Should().NotContain("CreatePayment");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("Refund");
        combined.Should().NotContain("SupplierPayment");
        combined.Should().NotContain("FinanceExport");
        combined.Should().NotContain("PayrollCalculation");
        combined.Should().NotContain("PayrollProvider");
        combined.Should().NotContain("MobileApi");
        combined.Should().NotContain("MemberCommerce");
        controller.Should().NotContain("[Route(\"api");
    }

    [Fact]
    public void HrPayrollRuleViews_Should_Render_RuleFoundation_Without_Run_Payslip_Posting_Provider_Or_Public_Mutations()
    {
        var root = RepositoryRoot();
        var viewsRoot = Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "HumanResources");
        var list = File.ReadAllText(Path.Combine(viewsRoot, "PayrollRules.cshtml"));
        var editor = File.ReadAllText(Path.Combine(viewsRoot, "PayrollRuleSetEditor.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "HumanResources", "HumanResourcesController.cs"));
        var handlers = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "HumanResources", "Commands", "HrPayrollRuleHandlers.cs"));

        list.Should().Contain("PayrollRules");
        editor.Should().Contain("AddPayrollRuleComponent");
        editor.Should().Contain("ArchivePayrollRuleComponent");
        editor.Should().Contain("@Html.AntiForgeryToken()");
        editor.Should().Contain("rowVersion");
        controller.Should().Contain("_createPayrollRuleSet.HandleAsync");
        controller.Should().Contain("_upsertPayrollRuleComponent.HandleAsync");
        handlers.Should().Contain("PayrollRuleSetEffectiveDateOverlap");

        var combined = list + editor + handlers;
        combined.Should().NotContain("PayrollCalculation");
        combined.Should().NotContain("Payslip");
        combined.Should().NotContain("PayrollProvider");
        combined.Should().NotContain("StatutoryFiling");
        combined.Should().NotContain("JournalEntry");
        combined.Should().NotContain("FinanceExport");
        combined.Should().NotContain("CreatePayment");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("Refund");
        combined.Should().NotContain("MobileApi");
        combined.Should().NotContain("MemberCommerce");
        controller.Should().NotContain("[Route(\"api");
    }

    [Fact]
    public void HrPayrollRunViews_Should_Render_Internal_RunWorkflow_With_PayslipArtifacts_And_Posting_Without_Provider_Or_Public_Mutations()
    {
        var root = RepositoryRoot();
        var viewsRoot = Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "HumanResources");
        var list = File.ReadAllText(Path.Combine(viewsRoot, "PayrollRuns.cshtml"));
        var create = File.ReadAllText(Path.Combine(viewsRoot, "PayrollRunCreate.cshtml"));
        var editor = File.ReadAllText(Path.Combine(viewsRoot, "PayrollRunEditor.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "HumanResources", "HumanResourcesController.cs"));
        var handlers = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "HumanResources", "Commands", "HrPayrollRunHandlers.cs"));
        var payslipService = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "HumanResources", "Services", "PayrollPayslipArtifactService.cs"));

        list.Should().Contain("PayrollRuns");
        create.Should().Contain("CreatePayrollRun");
        editor.Should().Contain("CalculatePayrollRun");
        editor.Should().Contain("UpdatePayrollRunLifecycle");
        editor.Should().Contain("PostPayrollRun");
        editor.Should().Contain("PostingJournalEntry");
        editor.Should().Contain("GeneratePayrollPayslips");
        editor.Should().Contain("DownloadPayrollPayslip");
        editor.Should().Contain("@Html.AntiForgeryToken()");
        editor.Should().Contain("rowVersion");
        controller.Should().Contain("_createPayrollRun.HandleAsync");
        controller.Should().Contain("_calculatePayrollRun.HandleAsync");
        controller.Should().Contain("_postPayrollRun.HandleAsync");
        controller.Should().Contain("_payrollPayslips.GenerateForRunAsync");
        handlers.Should().Contain("approved-payroll-period");
        handlers.Should().Contain("JournalEntryPostingKind.PayrollRunPosted");
        handlers.Should().Contain("FinancePostingAccountRole.PayrollExpense");
        payslipService.Should().Contain("PdfContentType = \"application/pdf\"");
        payslipService.Should().Contain("HtmlContentType = \"text/html\"");
        payslipService.Should().Contain("TemplateCode = \"darwin-payroll-payslip\"");
        payslipService.Should().Contain("TemplateVersion = \"v1\"");
        payslipService.Should().Contain("RenderPdf");
        payslipService.Should().Contain("html-source");

        var combined = list + create + editor + controller + handlers + payslipService;
        combined.Should().NotContain("PayrollProvider");
        combined.Should().NotContain("StatutoryFiling");
        combined.Should().NotContain("SalaryPayment");
        combined.Should().NotContain("CreateJournalEntry");
        combined.Should().NotContain("UpdateJournalEntry");
        combined.Should().NotContain("FinanceExport");
        combined.Should().NotContain("CreatePayment");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("Refund");
        combined.Should().NotContain("MobileApi");
        combined.Should().NotContain("MemberCommerce");
        controller.Should().NotContain("[Route(\"api");
    }

    [Fact]
    public void HrPayrollPaymentViews_Should_Render_Internal_JournalBacked_PaymentWorkflow_Without_BankApi_Public_Or_CustomerPayment_Mutations()
    {
        var root = RepositoryRoot();
        var viewsRoot = Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "HumanResources");
        var list = File.ReadAllText(Path.Combine(viewsRoot, "PayrollPayments.cshtml"));
        var editor = File.ReadAllText(Path.Combine(viewsRoot, "PayrollPaymentEditor.cshtml"));
        var runEditor = File.ReadAllText(Path.Combine(viewsRoot, "PayrollRunEditor.cshtml"));
        var layout = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Shared", "_Layout.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "HumanResources", "HumanResourcesController.cs"));
        var handlers = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "HumanResources", "Commands", "HrPayrollPaymentHandlers.cs"));

        layout.Should().Contain("asp-action=\"PayrollPayments\"");
        list.Should().Contain("PayrollPayments");
        editor.Should().Contain("PostPayrollPayment");
        editor.Should().Contain("CancelPayrollPayment");
        editor.Should().Contain("ReversePayrollPayment");
        editor.Should().Contain("SettlePayrollPaymentFromBankReconciliation");
        editor.Should().Contain("CreatePayrollPaymentBankCorrection");
        editor.Should().Contain("PostPayrollPaymentBankCorrection");
        editor.Should().Contain("CancelPayrollPaymentBankCorrection");
        editor.Should().Contain("PostingJournalEntry");
        editor.Should().Contain("ReversalJournalEntry");
        editor.Should().Contain("BankSettlementJournalEntry");
        editor.Should().Contain("BankReconciliation");
        editor.Should().Contain("@Html.AntiForgeryToken()");
        editor.Should().Contain("rowVersion");
        runEditor.Should().Contain("CreatePayrollPayment");
        controller.Should().Contain("_createPayrollPayment.HandleAsync");
        controller.Should().Contain("_postPayrollPayment.HandleAsync");
        controller.Should().Contain("_cancelPayrollPayment.HandleAsync");
        controller.Should().Contain("_reversePayrollPayment.HandleAsync");
        controller.Should().Contain("_settlePayrollPaymentFromBankReconciliation.HandleAsync");
        controller.Should().Contain("_createPayrollPaymentBankCorrection.HandleAsync");
        controller.Should().Contain("_postPayrollPaymentBankCorrection.HandleAsync");
        controller.Should().Contain("_cancelPayrollPaymentBankCorrection.HandleAsync");
        handlers.Should().Contain("JournalEntryPostingKind.PayrollPaymentPosted");
        handlers.Should().Contain("JournalEntryPostingKind.Reversal");
        handlers.Should().Contain("JournalEntryPostingKind.PayrollPaymentBankSettled");
        handlers.Should().Contain("JournalEntryPostingKind.PayrollPaymentBankCorrection");
        handlers.Should().Contain("FinancePostingAccountRole.PayrollPayable");
        handlers.Should().Contain("FinancePostingAccountRole.CashClearing");
        handlers.Should().Contain("payroll-payment-reversed");
        handlers.Should().Contain("payroll-payment-bank-settled");
        handlers.Should().Contain("payroll-payment-bank-correction");
        handlers.Should().Contain("BankReconciliationMatchStatus.Matched");
        handlers.Should().Contain("AccountType.Asset");

        var combined = list + editor + runEditor + controller + handlers;
        combined.Should().NotContain("PayrollProvider");
        combined.Should().NotContain("StatutoryFiling");
        combined.Should().NotContain("BankCredential");
        combined.Should().NotContain("BankApi");
        combined.Should().NotContain("DirectBankSettlement");
        combined.Should().NotContain("CustomerPayment");
        combined.Should().NotContain("SupplierPayment");
        combined.Should().NotContain("CreatePayment");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("Refund");
        combined.Should().NotContain("FinanceExport");
        combined.Should().NotContain("MobileApi");
        combined.Should().NotContain("MemberCommerce");
        controller.Should().NotContain("[Route(\"api");
    }

    [Fact]
    public void EmployeePayslipSelfService_Should_Use_Dedicated_MemberApi_With_PdfOnly_DownloadAudit_And_No_AdminOrTreasuryLeakage()
    {
        var root = RepositoryRoot();
        var webApiController = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebApi", "Controllers", "Member", "MemberPayrollPayslipsController.cs"));
        var queryHandlers = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "HumanResources", "Queries", "MemberPayrollPayslipQueries.cs"));
        var contracts = File.ReadAllText(Path.Combine(root, "src", "Darwin.Contracts", "HumanResources", "MemberPayslipContracts.cs"));
        var mobileRoutes = File.ReadAllText(Path.Combine(root, "src", "Darwin.Mobile.Shared", "Api", "ApiRoutes.cs"));
        var mobileService = File.ReadAllText(Path.Combine(root, "src", "Darwin.Mobile.Shared", "Services", "Payroll", "MemberPayrollService.cs"));
        var webAdminController = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "HumanResources", "HumanResourcesController.cs"));

        webApiController.Should().Contain("[Authorize]");
        webApiController.Should().Contain("[Route(\"api/v1/member/payroll/payslips\")]");
        webApiController.Should().Contain("[HttpGet]");
        webApiController.Should().Contain("[HttpGet(\"{id:guid}/document\")]");
        webApiController.Should().Contain("DownloadMyPayslipDocumentHandler");
        webApiController.Should().NotContain("[HttpPost]");
        webApiController.Should().NotContain("HumanResourcesController");

        queryHandlers.Should().Contain("member.UserId == userId");
        queryHandlers.Should().Contain("member.BusinessId == employee.BusinessId");
        queryHandlers.Should().Contain("PayrollPayslipArtifactService.PdfContentType");
        queryHandlers.Should().Contain("AuditTrailAction.Exported");
        queryHandlers.Should().Contain("hr.employee_payslip.downloaded");
        queryHandlers.Should().NotContain("HtmlContentType");
        queryHandlers.Should().NotContain("PayrollProvider");
        queryHandlers.Should().NotContain("BankReconciliation");
        queryHandlers.Should().NotContain("JournalEntry");
        queryHandlers.Should().NotContain("PostingJournalEntryId");

        contracts.Should().Contain("public sealed class MemberPayslipSummary");
        contracts.Should().Contain("PaymentStatus");
        contracts.Should().Contain("DocumentPath");
        contracts.Should().NotContain("Journal");
        contracts.Should().NotContain("Bank");
        contracts.Should().NotContain("Provider");
        contracts.Should().NotContain("Html");

        mobileRoutes.Should().Contain("api/v1/member/payroll/payslips");
        mobileService.Should().Contain("GetFileResultAsync");
        mobileService.Should().Contain("Payslip data is not locally cached");
        mobileService.Should().NotContain("SecureStorage");
        mobileService.Should().NotContain("Preferences");
        mobileService.Should().NotContain("File.Write");

        webAdminController.Should().Contain("DownloadPayrollPayslip");
        webAdminController.Should().NotContain("api/v1/member/payroll/payslips");
    }

    [Fact]
    public void HrPersonnelDocumentViews_Should_Use_Internal_DocumentRecord_Workflow_Without_Public_Or_Finance_Mutations()
    {
        var root = RepositoryRoot();
        var view = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "HumanResources", "EmployeeEditor.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "HumanResources", "HumanResourcesController.cs"));
        var service = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "HumanResources", "Services", "PersonnelDocumentService.cs"));

        view.Should().Contain("UploadEmployeeDocument");
        view.Should().Contain("DownloadEmployeeDocument");
        view.Should().Contain("ArchiveEmployeeDocument");
        view.Should().Contain("enctype=\"multipart/form-data\"");
        view.Should().Contain("@Html.AntiForgeryToken()");
        view.Should().Contain("rowVersion");
        service.Should().Contain("DocumentRecordService");
        service.Should().Contain("IObjectStorageService");
        service.Should().Contain("PersonnelDocuments");

        var combined = view + service;
        combined.Should().NotContain("[Route(\"api");
        combined.Should().NotContain("MobileApi");
        combined.Should().NotContain("MemberCommerce");
        combined.Should().NotContain("FinanceExport");
        combined.Should().NotContain("CreatePayment");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("Refund");
        combined.Should().NotContain("PayrollProvider");
    }

    [Fact]
    public void HrTimeViews_Should_Expose_WebAdmin_First_Time_Workflow_Without_Payroll_Or_Mobile_Clock()
    {
        var root = RepositoryRoot();
        var viewsRoot = Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "HumanResources");
        var schedules = File.ReadAllText(Path.Combine(viewsRoot, "WorkScheduleEditor.cshtml"));
        var attendance = File.ReadAllText(Path.Combine(viewsRoot, "AttendanceEventEditor.cshtml"));
        var timeEntry = File.ReadAllText(Path.Combine(viewsRoot, "TimeEntryEditor.cshtml"));
        var timesheet = File.ReadAllText(Path.Combine(viewsRoot, "TimesheetEditor.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "HumanResources", "HumanResourcesController.cs"));

        schedules.Should().Contain("MondayMinutes");
        schedules.Should().Contain("CreateWorkScheduleException");
        attendance.Should().Contain("CreateAttendanceEvent");
        timeEntry.Should().Contain("DurationMinutes");
        timesheet.Should().Contain("UpdateTimesheetLifecycle");
        timesheet.Should().Contain("TimesheetStatus.Approved");
        timesheet.Should().Contain("@Html.AntiForgeryToken()");
        timesheet.Should().Contain("rowVersion");
        controller.Should().Contain("UpdateTimesheetLifecycle");
        controller.Should().Contain("_updateTimesheetLifecycle.HandleAsync");

        var combined = schedules + attendance + timeEntry + timesheet;
        combined.Should().NotContain("PayrollProvider");
        combined.Should().NotContain("serviceWorker");
        combined.Should().NotContain("navigator.serviceWorker");
        combined.Should().NotContain("IndexedDB");
        combined.Should().NotContain("localStorage");
        combined.Should().NotContain("PublicApi");
    }

    [Fact]
    public void HrLeaveViews_Should_Render_Dedicated_LeaveAndAbsence_Workflow_Without_Payroll_Or_Finance_Mutations()
    {
        var root = RepositoryRoot();
        var viewsRoot = Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "HumanResources");
        var leave = File.ReadAllText(Path.Combine(viewsRoot, "LeaveRequestEditor.cshtml"));
        var absences = File.ReadAllText(Path.Combine(viewsRoot, "AbsenceEditor.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "HumanResources", "HumanResourcesController.cs"));

        leave.Should().Contain("UpdateLeaveRequestLifecycle");
        leave.Should().Contain("LeaveRequestStatus.Approved");
        absences.Should().Contain("CreateAbsence");
        leave.Should().Contain("@Html.AntiForgeryToken()");
        absences.Should().Contain("@Html.AntiForgeryToken()");
        leave.Should().Contain("rowVersion");
        controller.Should().Contain("_updateLeaveRequestLifecycle.HandleAsync");

        var combined = leave + absences + controller;
        combined.Should().NotContain("PayrollCalculation");
        combined.Should().NotContain("SupplierPayment");
        combined.Should().NotContain("FinanceExport");
        combined.Should().NotContain("CreatePayment");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("Refund");
        combined.Should().NotContain("MobileApi");
        combined.Should().NotContain("MemberCommerce");
    }

    [Fact]
    public void HrPayrollPeriodViews_Should_Render_Summary_Workflow_Without_LegalPayroll_Provider_Or_Finance_Mutations()
    {
        var root = RepositoryRoot();
        var viewsRoot = Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "HumanResources");
        var list = File.ReadAllText(Path.Combine(viewsRoot, "PayrollPeriods.cshtml"));
        var editor = File.ReadAllText(Path.Combine(viewsRoot, "PayrollPeriodEditor.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "HumanResources", "HumanResourcesController.cs"));

        list.Should().Contain("PayrollPeriods");
        editor.Should().Contain("PreparePayrollPeriod");
        editor.Should().Contain("UpdatePayrollPeriodLifecycle");
        editor.Should().Contain("PayrollPeriodStatus.Approved");
        editor.Should().Contain("@Html.AntiForgeryToken()");
        editor.Should().Contain("rowVersion");
        controller.Should().Contain("_preparePayrollPeriod.HandleAsync");
        controller.Should().Contain("_updatePayrollPeriodLifecycle.HandleAsync");

        var combined = list + editor + controller;
        combined.Should().NotContain("PayrollCalculation");
        combined.Should().NotContain("PayrollProvider");
        combined.Should().NotContain("SocialInsurance");
        combined.Should().NotContain("FinanceExport");
        combined.Should().NotContain("SupplierPayment");
        combined.Should().NotContain("CreatePayment");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("Refund");
        combined.Should().NotContain("MobileApi");
        combined.Should().NotContain("MemberCommerce");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Darwin.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
