using Darwin.Application.HumanResources.Commands;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Application.HumanResources.Queries;
using Darwin.Application.HumanResources.Services;
using Darwin.Domain.Enums;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.ViewModels.HumanResources;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.Controllers.Admin.HumanResources;

public sealed class HumanResourcesController : AdminBaseController
{
    private readonly AdminReferenceDataService _referenceData;
    private readonly GetEmployeesPageHandler _getEmployees;
    private readonly GetEmployeeDetailHandler _getEmployee;
    private readonly CreateEmployeeHandler _createEmployee;
    private readonly UpdateEmployeeHandler _updateEmployee;
    private readonly ArchiveEmployeeHandler _archiveEmployee;
    private readonly GetDepartmentsPageHandler _getDepartments;
    private readonly GetDepartmentDetailHandler _getDepartment;
    private readonly CreateDepartmentHandler _createDepartment;
    private readonly UpdateDepartmentHandler _updateDepartment;
    private readonly ArchiveDepartmentHandler _archiveDepartment;
    private readonly GetPositionsPageHandler _getPositions;
    private readonly GetPositionDetailHandler _getPosition;
    private readonly CreatePositionHandler _createPosition;
    private readonly UpdatePositionHandler _updatePosition;
    private readonly ArchivePositionHandler _archivePosition;
    private readonly GetEmploymentContractsPageHandler _getContracts;
    private readonly GetEmploymentContractDetailHandler _getContract;
    private readonly CreateEmploymentContractHandler _createContract;
    private readonly UpdateEmploymentContractHandler _updateContract;
    private readonly ArchiveEmploymentContractHandler _archiveContract;
    private readonly GetWorkSchedulesPageHandler _getSchedules;
    private readonly GetWorkScheduleDetailHandler _getSchedule;
    private readonly CreateWorkScheduleHandler _createSchedule;
    private readonly UpdateWorkScheduleHandler _updateSchedule;
    private readonly ArchiveWorkScheduleHandler _archiveSchedule;
    private readonly CreateWorkScheduleExceptionHandler _createScheduleException;
    private readonly GetAttendanceEventsPageHandler _getAttendanceEvents;
    private readonly CreateAttendanceEventHandler _createAttendanceEvent;
    private readonly GetTimeEntriesPageHandler _getTimeEntries;
    private readonly GetTimeEntryDetailHandler _getTimeEntry;
    private readonly CreateTimeEntryHandler _createTimeEntry;
    private readonly UpdateTimeEntryHandler _updateTimeEntry;
    private readonly GetTimesheetsPageHandler _getTimesheets;
    private readonly GetTimesheetDetailHandler _getTimesheet;
    private readonly CreateTimesheetHandler _createTimesheet;
    private readonly UpdateTimesheetHandler _updateTimesheet;
    private readonly UpdateTimesheetLifecycleHandler _updateTimesheetLifecycle;
    private readonly GetLeaveRequestsPageHandler _getLeaveRequests;
    private readonly GetLeaveRequestDetailHandler _getLeaveRequest;
    private readonly CreateLeaveRequestHandler _createLeaveRequest;
    private readonly UpdateLeaveRequestHandler _updateLeaveRequest;
    private readonly UpdateLeaveRequestLifecycleHandler _updateLeaveRequestLifecycle;
    private readonly GetAbsenceRecordsPageHandler _getAbsenceRecords;
    private readonly GetAbsenceRecordDetailHandler _getAbsenceRecord;
    private readonly CreateAbsenceRecordHandler _createAbsenceRecord;
    private readonly UpdateAbsenceRecordHandler _updateAbsenceRecord;
    private readonly GetPayrollPeriodsPageHandler _getPayrollPeriods;
    private readonly GetPayrollPeriodDetailHandler _getPayrollPeriod;
    private readonly CreatePayrollPeriodHandler _createPayrollPeriod;
    private readonly UpdatePayrollPeriodHandler _updatePayrollPeriod;
    private readonly PreparePayrollPeriodSummaryHandler _preparePayrollPeriod;
    private readonly UpdatePayrollPeriodLifecycleHandler _updatePayrollPeriodLifecycle;
    private readonly GetPayrollRuleSetsPageHandler _getPayrollRuleSets;
    private readonly GetPayrollRuleSetDetailHandler _getPayrollRuleSet;
    private readonly CreatePayrollRuleSetHandler _createPayrollRuleSet;
    private readonly UpdatePayrollRuleSetHandler _updatePayrollRuleSet;
    private readonly ArchivePayrollRuleSetHandler _archivePayrollRuleSet;
    private readonly UpsertPayrollRuleComponentHandler _upsertPayrollRuleComponent;
    private readonly ArchivePayrollRuleComponentHandler _archivePayrollRuleComponent;
    private readonly GetPayrollRunsPageHandler _getPayrollRuns;
    private readonly GetPayrollRunDetailHandler _getPayrollRun;
    private readonly CreatePayrollRunHandler _createPayrollRun;
    private readonly CalculatePayrollRunHandler _calculatePayrollRun;
    private readonly UpdatePayrollRunLifecycleHandler _updatePayrollRunLifecycle;
    private readonly PostPayrollRunHandler _postPayrollRun;
    private readonly PayrollPayslipArtifactService _payrollPayslips;
    private readonly GetPayrollPaymentsPageHandler _getPayrollPayments;
    private readonly GetPayrollPaymentDetailHandler _getPayrollPayment;
    private readonly GetPayrollPaymentDraftHandler _getPayrollPaymentDraft;
    private readonly CreatePayrollPaymentHandler _createPayrollPayment;
    private readonly UpdatePayrollPaymentHandler _updatePayrollPayment;
    private readonly PostPayrollPaymentHandler _postPayrollPayment;
    private readonly CancelPayrollPaymentHandler _cancelPayrollPayment;
    private readonly ReversePayrollPaymentHandler _reversePayrollPayment;
    private readonly SettlePayrollPaymentFromBankReconciliationHandler _settlePayrollPaymentFromBankReconciliation;
    private readonly CreatePayrollPaymentBankCorrectionHandler _createPayrollPaymentBankCorrection;
    private readonly PostPayrollPaymentBankCorrectionHandler _postPayrollPaymentBankCorrection;
    private readonly CancelPayrollPaymentBankCorrectionHandler _cancelPayrollPaymentBankCorrection;
    private readonly PersonnelDocumentService _personnelDocuments;

    public HumanResourcesController(
        AdminReferenceDataService referenceData,
        GetEmployeesPageHandler getEmployees,
        GetEmployeeDetailHandler getEmployee,
        CreateEmployeeHandler createEmployee,
        UpdateEmployeeHandler updateEmployee,
        ArchiveEmployeeHandler archiveEmployee,
        GetDepartmentsPageHandler getDepartments,
        GetDepartmentDetailHandler getDepartment,
        CreateDepartmentHandler createDepartment,
        UpdateDepartmentHandler updateDepartment,
        ArchiveDepartmentHandler archiveDepartment,
        GetPositionsPageHandler getPositions,
        GetPositionDetailHandler getPosition,
        CreatePositionHandler createPosition,
        UpdatePositionHandler updatePosition,
        ArchivePositionHandler archivePosition,
        GetEmploymentContractsPageHandler getContracts,
        GetEmploymentContractDetailHandler getContract,
        CreateEmploymentContractHandler createContract,
        UpdateEmploymentContractHandler updateContract,
        ArchiveEmploymentContractHandler archiveContract,
        GetWorkSchedulesPageHandler getSchedules,
        GetWorkScheduleDetailHandler getSchedule,
        CreateWorkScheduleHandler createSchedule,
        UpdateWorkScheduleHandler updateSchedule,
        ArchiveWorkScheduleHandler archiveSchedule,
        CreateWorkScheduleExceptionHandler createScheduleException,
        GetAttendanceEventsPageHandler getAttendanceEvents,
        CreateAttendanceEventHandler createAttendanceEvent,
        GetTimeEntriesPageHandler getTimeEntries,
        GetTimeEntryDetailHandler getTimeEntry,
        CreateTimeEntryHandler createTimeEntry,
        UpdateTimeEntryHandler updateTimeEntry,
        GetTimesheetsPageHandler getTimesheets,
        GetTimesheetDetailHandler getTimesheet,
        CreateTimesheetHandler createTimesheet,
        UpdateTimesheetHandler updateTimesheet,
        UpdateTimesheetLifecycleHandler updateTimesheetLifecycle,
        GetLeaveRequestsPageHandler getLeaveRequests,
        GetLeaveRequestDetailHandler getLeaveRequest,
        CreateLeaveRequestHandler createLeaveRequest,
        UpdateLeaveRequestHandler updateLeaveRequest,
        UpdateLeaveRequestLifecycleHandler updateLeaveRequestLifecycle,
        GetAbsenceRecordsPageHandler getAbsenceRecords,
        GetAbsenceRecordDetailHandler getAbsenceRecord,
        CreateAbsenceRecordHandler createAbsenceRecord,
        UpdateAbsenceRecordHandler updateAbsenceRecord,
        GetPayrollPeriodsPageHandler getPayrollPeriods,
        GetPayrollPeriodDetailHandler getPayrollPeriod,
        CreatePayrollPeriodHandler createPayrollPeriod,
        UpdatePayrollPeriodHandler updatePayrollPeriod,
        PreparePayrollPeriodSummaryHandler preparePayrollPeriod,
        UpdatePayrollPeriodLifecycleHandler updatePayrollPeriodLifecycle,
        GetPayrollRuleSetsPageHandler getPayrollRuleSets,
        GetPayrollRuleSetDetailHandler getPayrollRuleSet,
        CreatePayrollRuleSetHandler createPayrollRuleSet,
        UpdatePayrollRuleSetHandler updatePayrollRuleSet,
        ArchivePayrollRuleSetHandler archivePayrollRuleSet,
        UpsertPayrollRuleComponentHandler upsertPayrollRuleComponent,
        ArchivePayrollRuleComponentHandler archivePayrollRuleComponent,
        GetPayrollRunsPageHandler getPayrollRuns,
        GetPayrollRunDetailHandler getPayrollRun,
        CreatePayrollRunHandler createPayrollRun,
        CalculatePayrollRunHandler calculatePayrollRun,
        UpdatePayrollRunLifecycleHandler updatePayrollRunLifecycle,
        PostPayrollRunHandler postPayrollRun,
        PayrollPayslipArtifactService payrollPayslips,
        GetPayrollPaymentsPageHandler getPayrollPayments,
        GetPayrollPaymentDetailHandler getPayrollPayment,
        GetPayrollPaymentDraftHandler getPayrollPaymentDraft,
        CreatePayrollPaymentHandler createPayrollPayment,
        UpdatePayrollPaymentHandler updatePayrollPayment,
        PostPayrollPaymentHandler postPayrollPayment,
        CancelPayrollPaymentHandler cancelPayrollPayment,
        ReversePayrollPaymentHandler reversePayrollPayment,
        SettlePayrollPaymentFromBankReconciliationHandler settlePayrollPaymentFromBankReconciliation,
        CreatePayrollPaymentBankCorrectionHandler createPayrollPaymentBankCorrection,
        PostPayrollPaymentBankCorrectionHandler postPayrollPaymentBankCorrection,
        CancelPayrollPaymentBankCorrectionHandler cancelPayrollPaymentBankCorrection,
        PersonnelDocumentService personnelDocuments)
    {
        _referenceData = referenceData;
        _getEmployees = getEmployees;
        _getEmployee = getEmployee;
        _createEmployee = createEmployee;
        _updateEmployee = updateEmployee;
        _archiveEmployee = archiveEmployee;
        _getDepartments = getDepartments;
        _getDepartment = getDepartment;
        _createDepartment = createDepartment;
        _updateDepartment = updateDepartment;
        _archiveDepartment = archiveDepartment;
        _getPositions = getPositions;
        _getPosition = getPosition;
        _createPosition = createPosition;
        _updatePosition = updatePosition;
        _archivePosition = archivePosition;
        _getContracts = getContracts;
        _getContract = getContract;
        _createContract = createContract;
        _updateContract = updateContract;
        _archiveContract = archiveContract;
        _getSchedules = getSchedules;
        _getSchedule = getSchedule;
        _createSchedule = createSchedule;
        _updateSchedule = updateSchedule;
        _archiveSchedule = archiveSchedule;
        _createScheduleException = createScheduleException;
        _getAttendanceEvents = getAttendanceEvents;
        _createAttendanceEvent = createAttendanceEvent;
        _getTimeEntries = getTimeEntries;
        _getTimeEntry = getTimeEntry;
        _createTimeEntry = createTimeEntry;
        _updateTimeEntry = updateTimeEntry;
        _getTimesheets = getTimesheets;
        _getTimesheet = getTimesheet;
        _createTimesheet = createTimesheet;
        _updateTimesheet = updateTimesheet;
        _updateTimesheetLifecycle = updateTimesheetLifecycle;
        _getLeaveRequests = getLeaveRequests;
        _getLeaveRequest = getLeaveRequest;
        _createLeaveRequest = createLeaveRequest;
        _updateLeaveRequest = updateLeaveRequest;
        _updateLeaveRequestLifecycle = updateLeaveRequestLifecycle;
        _getAbsenceRecords = getAbsenceRecords;
        _getAbsenceRecord = getAbsenceRecord;
        _createAbsenceRecord = createAbsenceRecord;
        _updateAbsenceRecord = updateAbsenceRecord;
        _getPayrollPeriods = getPayrollPeriods;
        _getPayrollPeriod = getPayrollPeriod;
        _createPayrollPeriod = createPayrollPeriod;
        _updatePayrollPeriod = updatePayrollPeriod;
        _preparePayrollPeriod = preparePayrollPeriod;
        _updatePayrollPeriodLifecycle = updatePayrollPeriodLifecycle;
        _getPayrollRuleSets = getPayrollRuleSets;
        _getPayrollRuleSet = getPayrollRuleSet;
        _createPayrollRuleSet = createPayrollRuleSet;
        _updatePayrollRuleSet = updatePayrollRuleSet;
        _archivePayrollRuleSet = archivePayrollRuleSet;
        _upsertPayrollRuleComponent = upsertPayrollRuleComponent;
        _archivePayrollRuleComponent = archivePayrollRuleComponent;
        _getPayrollRuns = getPayrollRuns;
        _getPayrollRun = getPayrollRun;
        _createPayrollRun = createPayrollRun;
        _calculatePayrollRun = calculatePayrollRun;
        _updatePayrollRunLifecycle = updatePayrollRunLifecycle;
        _postPayrollRun = postPayrollRun;
        _payrollPayslips = payrollPayslips;
        _getPayrollPayments = getPayrollPayments;
        _getPayrollPayment = getPayrollPayment;
        _getPayrollPaymentDraft = getPayrollPaymentDraft;
        _createPayrollPayment = createPayrollPayment;
        _updatePayrollPayment = updatePayrollPayment;
        _postPayrollPayment = postPayrollPayment;
        _cancelPayrollPayment = cancelPayrollPayment;
        _reversePayrollPayment = reversePayrollPayment;
        _settlePayrollPaymentFromBankReconciliation = settlePayrollPaymentFromBankReconciliation;
        _createPayrollPaymentBankCorrection = createPayrollPaymentBankCorrection;
        _postPayrollPaymentBankCorrection = postPayrollPaymentBankCorrection;
        _cancelPayrollPaymentBankCorrection = cancelPayrollPaymentBankCorrection;
        _personnelDocuments = personnelDocuments;
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(Employees));

    [HttpGet]
    public async Task<IActionResult> Employees(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, EmployeeQueueFilter filter = EmployeeQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
        var vm = new HrListVm<EmployeeListItemDto, EmployeeQueueFilter>
        {
            BusinessId = businessId,
            Query = q ?? string.Empty,
            Filter = filter,
            FilterItems = BuildEnumOptions(filter),
            BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
            Page = page,
            PageSize = pageSize
        };
        if (businessId.HasValue)
        {
            var result = await _getEmployees.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);
            vm.Items = result.Items;
            vm.Total = result.Total;
        }
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> CreateEmployee(Guid? businessId = null, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
        var vm = new EmployeeEditVm { BusinessId = businessId ?? Guid.Empty, MetadataJson = "{}" };
        await PopulateEmployeeOptionsAsync(vm, ct).ConfigureAwait(false);
        ViewData["IsCreate"] = true;
        return View("EmployeeEditor", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee(EmployeeEditVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateEmployeeOptionsAsync(vm, ct).ConfigureAwait(false);
            ViewData["IsCreate"] = true;
            return View("EmployeeEditor", vm);
        }
        try
        {
            await _createEmployee.HandleAsync(MapEmployee(vm), ct).ConfigureAwait(false);
            SetSuccessMessage("EmployeeCreated");
            return RedirectToAction(nameof(Employees), new { businessId = vm.BusinessId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException)
        {
            AddModelErrorMessage("EmployeeSaveFailed");
            await PopulateEmployeeOptionsAsync(vm, ct).ConfigureAwait(false);
            ViewData["IsCreate"] = true;
            return View("EmployeeEditor", vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditEmployee(Guid id, CancellationToken ct = default)
    {
        var dto = await _getEmployee.HandleAsync(id, ct).ConfigureAwait(false);
        if (dto is null)
        {
            SetErrorMessage("EmployeeNotFound");
            return RedirectToAction(nameof(Employees));
        }
        var vm = MapEmployee(dto);
        await PopulateEmployeeOptionsAsync(vm, ct).ConfigureAwait(false);
        return View("EmployeeEditor", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEmployee(EmployeeEditVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateEmployeeOptionsAsync(vm, ct).ConfigureAwait(false);
            return View("EmployeeEditor", vm);
        }
        try
        {
            await _updateEmployee.HandleAsync(MapEmployee(vm), ct).ConfigureAwait(false);
            SetSuccessMessage("EmployeeUpdated");
            return RedirectToAction(nameof(Employees), new { businessId = vm.BusinessId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException)
        {
            AddModelErrorMessage("EmployeeSaveFailed");
            await PopulateEmployeeOptionsAsync(vm, ct).ConfigureAwait(false);
            return View("EmployeeEditor", vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveEmployee(Guid id, string rowVersion, Guid businessId, CancellationToken ct = default)
    {
        var result = await _archiveEmployee.HandleAsync(new HrArchiveDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct).ConfigureAwait(false);
        if (result.Succeeded) SetSuccessMessage("EmployeeArchived"); else SetErrorMessage("EmployeeArchiveFailed");
        return RedirectToAction(nameof(Employees), new { businessId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadEmployeeDocument(PersonnelDocumentUploadVm vm, CancellationToken ct = default)
    {
        if (vm.File is null || vm.File.Length == 0)
        {
            SetErrorMessage("PersonnelDocumentFileRequired");
            return RedirectToAction(nameof(EditEmployee), new { id = vm.EmployeeId });
        }

        await using var stream = vm.File.OpenReadStream();
        var result = await _personnelDocuments.UploadAsync(new PersonnelDocumentUploadDto
        {
            BusinessId = vm.BusinessId,
            EmployeeId = vm.EmployeeId,
            DocumentKind = vm.DocumentKind,
            Title = vm.Title,
            FileName = vm.File.FileName,
            ContentType = string.IsNullOrWhiteSpace(vm.File.ContentType) ? "application/octet-stream" : vm.File.ContentType,
            SizeBytes = vm.File.Length,
            Content = stream,
            RetentionUntilUtc = vm.RetentionUntilUtc,
            LegalHold = vm.LegalHold,
            PrivacyClassification = vm.PrivacyClassification,
            MetadataJson = vm.MetadataJson
        }, ct).ConfigureAwait(false);
        if (result.Succeeded) SetSuccessMessage("PersonnelDocumentUploaded"); else SetErrorMessage(result.Error ?? "PersonnelDocumentUploadFailed");
        return RedirectToAction(nameof(EditEmployee), new { id = vm.EmployeeId });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadEmployeeDocument(Guid businessId, Guid documentId, CancellationToken ct = default)
    {
        var result = await _personnelDocuments.DownloadAsync(businessId, documentId, ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            SetErrorMessage(result.Error ?? "PersonnelDocumentDownloadFailed");
            return RedirectToAction(nameof(Employees), new { businessId });
        }

        var download = result.Value ?? throw new InvalidOperationException("PersonnelDocumentDownloadFailed");
        return File(download.Content, download.ContentType, download.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveEmployeeDocument(Guid businessId, Guid employeeId, Guid documentId, string rowVersion, CancellationToken ct = default)
    {
        var result = await _personnelDocuments.ArchiveAsync(businessId, documentId, DecodeBase64RowVersion(rowVersion), ct).ConfigureAwait(false);
        if (result.Succeeded) SetSuccessMessage("PersonnelDocumentArchived"); else SetErrorMessage(result.Error ?? "PersonnelDocumentArchiveFailed");
        return RedirectToAction(nameof(EditEmployee), new { id = employeeId });
    }

    [HttpGet]
    public Task<IActionResult> Departments(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, DepartmentQueueFilter filter = DepartmentQueueFilter.All, CancellationToken ct = default)
        => ListDepartmentsAsync(businessId, page, pageSize, q, filter, ct);

    [HttpGet]
    public Task<IActionResult> Positions(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, PositionQueueFilter filter = PositionQueueFilter.All, CancellationToken ct = default)
        => ListPositionsAsync(businessId, page, pageSize, q, filter, ct);

    [HttpGet]
    public async Task<IActionResult> EmploymentContracts(Guid? businessId = null, Guid? employeeId = null, int page = 1, int pageSize = 20, string? q = null, EmploymentContractQueueFilter filter = EmploymentContractQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
        var vm = new HrListVm<EmploymentContractListItemDto, EmploymentContractQueueFilter>
        {
            BusinessId = businessId,
            Query = q ?? string.Empty,
            Filter = filter,
            FilterItems = BuildEnumOptions(filter),
            BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
            Page = page,
            PageSize = pageSize
        };
        if (businessId.HasValue)
        {
            var result = await _getContracts.HandleAsync(businessId.Value, employeeId, page, pageSize, q, filter, ct).ConfigureAwait(false);
            vm.Items = result.Items;
            vm.Total = result.Total;
        }
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> CreateDepartment(Guid? businessId = null, CancellationToken ct = default) { var vm = new DepartmentEditVm { BusinessId = await ResolveBusinessRequiredAsync(businessId, ct), MetadataJson = "{}" }; await PopulateDepartmentOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("DepartmentEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateDepartment(DepartmentEditVm vm, CancellationToken ct = default) => await SaveDepartmentAsync(vm, true, ct);
    [HttpGet] public async Task<IActionResult> EditDepartment(Guid id, CancellationToken ct = default) { var dto = await _getDepartment.HandleAsync(id, ct); if (dto is null) { SetErrorMessage("DepartmentNotFound"); return RedirectToAction(nameof(Departments)); } var vm = MapDepartment(dto); await PopulateDepartmentOptionsAsync(vm, ct); return View("DepartmentEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditDepartment(DepartmentEditVm vm, CancellationToken ct = default) => await SaveDepartmentAsync(vm, false, ct);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ArchiveDepartment(Guid id, string rowVersion, Guid businessId, CancellationToken ct = default) { var result = await _archiveDepartment.HandleAsync(new HrArchiveDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct); if (result.Succeeded) SetSuccessMessage("DepartmentArchived"); else SetErrorMessage("DepartmentArchiveFailed"); return RedirectToAction(nameof(Departments), new { businessId }); }

    [HttpGet] public async Task<IActionResult> CreatePosition(Guid? businessId = null, CancellationToken ct = default) { var vm = new PositionEditVm { BusinessId = await ResolveBusinessRequiredAsync(businessId, ct), MetadataJson = "{}" }; await PopulatePositionOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("PositionEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreatePosition(PositionEditVm vm, CancellationToken ct = default) => await SavePositionAsync(vm, true, ct);
    [HttpGet] public async Task<IActionResult> EditPosition(Guid id, CancellationToken ct = default) { var dto = await _getPosition.HandleAsync(id, ct); if (dto is null) { SetErrorMessage("PositionNotFound"); return RedirectToAction(nameof(Positions)); } var vm = MapPosition(dto); await PopulatePositionOptionsAsync(vm, ct); return View("PositionEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditPosition(PositionEditVm vm, CancellationToken ct = default) => await SavePositionAsync(vm, false, ct);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ArchivePosition(Guid id, string rowVersion, Guid businessId, CancellationToken ct = default) { var result = await _archivePosition.HandleAsync(new HrArchiveDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct); if (result.Succeeded) SetSuccessMessage("PositionArchived"); else SetErrorMessage("PositionArchiveFailed"); return RedirectToAction(nameof(Positions), new { businessId }); }

    [HttpGet] public async Task<IActionResult> CreateEmploymentContract(Guid? businessId = null, Guid? employeeId = null, CancellationToken ct = default) { var vm = new EmploymentContractEditVm { BusinessId = await ResolveBusinessRequiredAsync(businessId, ct), EmployeeId = employeeId ?? Guid.Empty, MetadataJson = "{}" }; await PopulateContractOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("EmploymentContractEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateEmploymentContract(EmploymentContractEditVm vm, CancellationToken ct = default) => await SaveContractAsync(vm, true, ct);
    [HttpGet] public async Task<IActionResult> EditEmploymentContract(Guid id, CancellationToken ct = default) { var dto = await _getContract.HandleAsync(id, ct); if (dto is null) { SetErrorMessage("EmploymentContractNotFound"); return RedirectToAction(nameof(EmploymentContracts)); } var vm = MapContract(dto); await PopulateContractOptionsAsync(vm, ct); return View("EmploymentContractEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditEmploymentContract(EmploymentContractEditVm vm, CancellationToken ct = default) => await SaveContractAsync(vm, false, ct);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ArchiveEmploymentContract(Guid id, string rowVersion, Guid businessId, CancellationToken ct = default) { var result = await _archiveContract.HandleAsync(new HrArchiveDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct); if (result.Succeeded) SetSuccessMessage("EmploymentContractArchived"); else SetErrorMessage("EmploymentContractArchiveFailed"); return RedirectToAction(nameof(EmploymentContracts), new { businessId }); }

    [HttpGet]
    public async Task<IActionResult> WorkSchedules(Guid? businessId = null, Guid? employeeId = null, int page = 1, int pageSize = 20, string? q = null, WorkScheduleQueueFilter filter = WorkScheduleQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct);
        var vm = new HrListVm<WorkScheduleListItemDto, WorkScheduleQueueFilter> { BusinessId = businessId, Query = q ?? string.Empty, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct), Page = page, PageSize = pageSize };
        if (businessId.HasValue) { var result = await _getSchedules.HandleAsync(businessId.Value, employeeId, page, pageSize, q, filter, ct); vm.Items = result.Items; vm.Total = result.Total; }
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> CreateWorkSchedule(Guid? businessId = null, Guid? employeeId = null, CancellationToken ct = default) { var vm = new WorkScheduleEditVm { BusinessId = await ResolveBusinessRequiredAsync(businessId, ct), EmployeeId = employeeId ?? Guid.Empty, MetadataJson = "{}" }; await PopulateScheduleOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("WorkScheduleEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateWorkSchedule(WorkScheduleEditVm vm, CancellationToken ct = default) => await SaveScheduleAsync(vm, true, ct);
    [HttpGet] public async Task<IActionResult> EditWorkSchedule(Guid id, CancellationToken ct = default) { var dto = await _getSchedule.HandleAsync(id, ct); if (dto is null) { SetErrorMessage("WorkScheduleNotFound"); return RedirectToAction(nameof(WorkSchedules)); } var vm = MapSchedule(dto); await PopulateScheduleOptionsAsync(vm, ct); return View("WorkScheduleEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditWorkSchedule(WorkScheduleEditVm vm, CancellationToken ct = default) => await SaveScheduleAsync(vm, false, ct);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ArchiveWorkSchedule(Guid id, string rowVersion, Guid businessId, CancellationToken ct = default) { var result = await _archiveSchedule.HandleAsync(new HrArchiveDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct); if (result.Succeeded) SetSuccessMessage("WorkScheduleArchived"); else SetErrorMessage("WorkScheduleArchiveFailed"); return RedirectToAction(nameof(WorkSchedules), new { businessId }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateWorkScheduleException(WorkScheduleExceptionVm vm, CancellationToken ct = default) { try { await _createScheduleException.HandleAsync(new WorkScheduleExceptionDto { BusinessId = vm.BusinessId, WorkScheduleId = vm.WorkScheduleId, WorkDateUtc = vm.WorkDateUtc, ScheduledMinutes = vm.ScheduledMinutes, Reason = vm.Reason, InternalNotes = vm.InternalNotes, MetadataJson = vm.MetadataJson }, ct); SetSuccessMessage("WorkScheduleExceptionCreated"); } catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException) { SetErrorMessage("WorkScheduleExceptionSaveFailed"); } return RedirectToAction(nameof(EditWorkSchedule), new { id = vm.WorkScheduleId }); }

    [HttpGet]
    public async Task<IActionResult> AttendanceEvents(Guid? businessId = null, Guid? employeeId = null, int page = 1, int pageSize = 20, AttendanceEventQueueFilter filter = AttendanceEventQueueFilter.All, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct);
        var vm = new HrListVm<AttendanceEventListItemDto, AttendanceEventQueueFilter> { BusinessId = businessId, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct), Page = page, PageSize = pageSize };
        if (businessId.HasValue) { var result = await _getAttendanceEvents.HandleAsync(businessId.Value, employeeId, page, pageSize, filter, fromUtc, toUtc, ct); vm.Items = result.Items; vm.Total = result.Total; }
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> CreateAttendanceEvent(Guid? businessId = null, Guid? employeeId = null, CancellationToken ct = default) { var vm = new AttendanceEventEditVm { BusinessId = await ResolveBusinessRequiredAsync(businessId, ct), EmployeeId = employeeId ?? Guid.Empty, MetadataJson = "{}" }; await PopulateAttendanceOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("AttendanceEventEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateAttendanceEvent(AttendanceEventEditVm vm, CancellationToken ct = default) { if (!ModelState.IsValid) { await PopulateAttendanceOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("AttendanceEventEditor", vm); } try { await _createAttendanceEvent.HandleAsync(MapAttendance(vm), ct); SetSuccessMessage("AttendanceEventCreated"); return RedirectToAction(nameof(AttendanceEvents), new { businessId = vm.BusinessId, employeeId = vm.EmployeeId }); } catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException) { AddModelErrorMessage("AttendanceEventSaveFailed"); await PopulateAttendanceOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("AttendanceEventEditor", vm); } }

    [HttpGet]
    public async Task<IActionResult> TimeEntries(Guid? businessId = null, Guid? employeeId = null, int page = 1, int pageSize = 20, string? q = null, TimeEntryQueueFilter filter = TimeEntryQueueFilter.All, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct);
        var vm = new HrListVm<TimeEntryListItemDto, TimeEntryQueueFilter> { BusinessId = businessId, Query = q ?? string.Empty, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct), Page = page, PageSize = pageSize };
        if (businessId.HasValue) { var result = await _getTimeEntries.HandleAsync(businessId.Value, employeeId, page, pageSize, q, filter, fromUtc, toUtc, ct); vm.Items = result.Items; vm.Total = result.Total; }
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> CreateTimeEntry(Guid? businessId = null, Guid? employeeId = null, CancellationToken ct = default) { var vm = new TimeEntryEditVm { BusinessId = await ResolveBusinessRequiredAsync(businessId, ct), EmployeeId = employeeId ?? Guid.Empty, MetadataJson = "{}" }; await PopulateTimeEntryOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("TimeEntryEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateTimeEntry(TimeEntryEditVm vm, CancellationToken ct = default) => await SaveTimeEntryAsync(vm, true, ct);
    [HttpGet] public async Task<IActionResult> EditTimeEntry(Guid id, CancellationToken ct = default) { var dto = await _getTimeEntry.HandleAsync(id, ct); if (dto is null) { SetErrorMessage("TimeEntryNotFound"); return RedirectToAction(nameof(TimeEntries)); } var vm = MapTimeEntry(dto); await PopulateTimeEntryOptionsAsync(vm, ct); return View("TimeEntryEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditTimeEntry(TimeEntryEditVm vm, CancellationToken ct = default) => await SaveTimeEntryAsync(vm, false, ct);

    [HttpGet]
    public async Task<IActionResult> Timesheets(Guid? businessId = null, Guid? employeeId = null, int page = 1, int pageSize = 20, string? q = null, TimesheetQueueFilter filter = TimesheetQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct);
        var vm = new HrListVm<TimesheetListItemDto, TimesheetQueueFilter> { BusinessId = businessId, Query = q ?? string.Empty, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct), Page = page, PageSize = pageSize };
        if (businessId.HasValue) { var result = await _getTimesheets.HandleAsync(businessId.Value, employeeId, page, pageSize, q, filter, ct); vm.Items = result.Items; vm.Total = result.Total; }
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> CreateTimesheet(Guid? businessId = null, Guid? employeeId = null, CancellationToken ct = default) { var vm = new TimesheetEditVm { BusinessId = await ResolveBusinessRequiredAsync(businessId, ct), EmployeeId = employeeId ?? Guid.Empty, PeriodStartUtc = DateTime.UtcNow.Date.AddDays(-6), PeriodEndUtc = DateTime.UtcNow.Date, MetadataJson = "{}" }; await PopulateTimesheetOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("TimesheetEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateTimesheet(TimesheetEditVm vm, CancellationToken ct = default) => await SaveTimesheetAsync(vm, true, ct);
    [HttpGet] public async Task<IActionResult> EditTimesheet(Guid id, CancellationToken ct = default) { var dto = await _getTimesheet.HandleAsync(id, ct); if (dto is null) { SetErrorMessage("TimesheetNotFound"); return RedirectToAction(nameof(Timesheets)); } var vm = MapTimesheet(dto); await PopulateTimesheetOptionsAsync(vm, ct); return View("TimesheetEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditTimesheet(TimesheetEditVm vm, CancellationToken ct = default) => await SaveTimesheetAsync(vm, false, ct);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> UpdateTimesheetLifecycle(Guid id, string rowVersion, TimesheetStatus target, string? notes, Guid businessId, CancellationToken ct = default) { var result = await _updateTimesheetLifecycle.HandleAsync(new HrTimeLifecycleDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion), Notes = notes }, target, ct); if (result.Succeeded) SetSuccessMessage("TimesheetUpdated"); else SetErrorMessage(result.Error ?? "TimesheetUpdateFailed"); return RedirectToAction(nameof(EditTimesheet), new { id }); }

    [HttpGet]
    public async Task<IActionResult> LeaveRequests(Guid? businessId = null, Guid? employeeId = null, int page = 1, int pageSize = 20, string? q = null, LeaveRequestQueueFilter filter = LeaveRequestQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct);
        var vm = new HrListVm<LeaveRequestListItemDto, LeaveRequestQueueFilter> { BusinessId = businessId, Query = q ?? string.Empty, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct), Page = page, PageSize = pageSize };
        if (businessId.HasValue) { var result = await _getLeaveRequests.HandleAsync(businessId.Value, employeeId, page, pageSize, q, filter, ct); vm.Items = result.Items; vm.Total = result.Total; }
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> CreateLeaveRequest(Guid? businessId = null, Guid? employeeId = null, CancellationToken ct = default) { var vm = new LeaveRequestEditVm { BusinessId = await ResolveBusinessRequiredAsync(businessId, ct), EmployeeId = employeeId ?? Guid.Empty, StartDateUtc = DateTime.UtcNow.Date, EndDateUtc = DateTime.UtcNow.Date, MetadataJson = "{}" }; await PopulateLeaveRequestOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("LeaveRequestEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateLeaveRequest(LeaveRequestEditVm vm, CancellationToken ct = default) => await SaveLeaveRequestAsync(vm, true, ct);
    [HttpGet] public async Task<IActionResult> EditLeaveRequest(Guid id, CancellationToken ct = default) { var dto = await _getLeaveRequest.HandleAsync(id, ct); if (dto is null) { SetErrorMessage("LeaveRequestNotFound"); return RedirectToAction(nameof(LeaveRequests)); } var vm = MapLeaveRequest(dto); await PopulateLeaveRequestOptionsAsync(vm, ct); return View("LeaveRequestEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditLeaveRequest(LeaveRequestEditVm vm, CancellationToken ct = default) => await SaveLeaveRequestAsync(vm, false, ct);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> UpdateLeaveRequestLifecycle(Guid id, string rowVersion, LeaveRequestStatus target, string? notes, Guid businessId, CancellationToken ct = default) { var result = await _updateLeaveRequestLifecycle.HandleAsync(new HrTimeLifecycleDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion), Notes = notes }, target, ct); if (result.Succeeded) SetSuccessMessage("LeaveRequestUpdated"); else SetErrorMessage(result.Error ?? "LeaveRequestUpdateFailed"); return RedirectToAction(nameof(EditLeaveRequest), new { id }); }

    [HttpGet]
    public async Task<IActionResult> Absences(Guid? businessId = null, Guid? employeeId = null, int page = 1, int pageSize = 20, AbsenceRecordQueueFilter filter = AbsenceRecordQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct);
        var vm = new HrListVm<AbsenceRecordListItemDto, AbsenceRecordQueueFilter> { BusinessId = businessId, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct), Page = page, PageSize = pageSize };
        if (businessId.HasValue) { var result = await _getAbsenceRecords.HandleAsync(businessId.Value, employeeId, page, pageSize, filter, ct); vm.Items = result.Items; vm.Total = result.Total; }
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> CreateAbsence(Guid? businessId = null, Guid? employeeId = null, Guid? leaveRequestId = null, CancellationToken ct = default) { var vm = new AbsenceRecordEditVm { BusinessId = await ResolveBusinessRequiredAsync(businessId, ct), EmployeeId = employeeId ?? Guid.Empty, LeaveRequestId = leaveRequestId, StartDateUtc = DateTime.UtcNow.Date, EndDateUtc = DateTime.UtcNow.Date, MetadataJson = "{}" }; await PopulateAbsenceOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("AbsenceEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateAbsence(AbsenceRecordEditVm vm, CancellationToken ct = default) => await SaveAbsenceAsync(vm, true, ct);
    [HttpGet] public async Task<IActionResult> EditAbsence(Guid id, CancellationToken ct = default) { var dto = await _getAbsenceRecord.HandleAsync(id, ct); if (dto is null) { SetErrorMessage("AbsenceRecordNotFound"); return RedirectToAction(nameof(Absences)); } var vm = MapAbsence(dto); await PopulateAbsenceOptionsAsync(vm, ct); return View("AbsenceEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditAbsence(AbsenceRecordEditVm vm, CancellationToken ct = default) => await SaveAbsenceAsync(vm, false, ct);

    [HttpGet]
    public async Task<IActionResult> PayrollPeriods(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, PayrollPeriodQueueFilter filter = PayrollPeriodQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct);
        var vm = new HrListVm<PayrollPeriodListItemDto, PayrollPeriodQueueFilter> { BusinessId = businessId, Query = q ?? string.Empty, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct), Page = page, PageSize = pageSize };
        if (businessId.HasValue) { var result = await _getPayrollPeriods.HandleAsync(businessId.Value, page, pageSize, q, filter, ct); vm.Items = result.Items; vm.Total = result.Total; }
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> CreatePayrollPeriod(Guid? businessId = null, CancellationToken ct = default) { var today = DateTime.UtcNow.Date; var vm = new PayrollPeriodEditVm { BusinessId = await ResolveBusinessRequiredAsync(businessId, ct), PeriodStartUtc = new DateTime(today.Year, today.Month, 1), PeriodEndUtc = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)), MetadataJson = "{}" }; await PopulatePayrollPeriodOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("PayrollPeriodEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreatePayrollPeriod(PayrollPeriodEditVm vm, CancellationToken ct = default) => await SavePayrollPeriodAsync(vm, true, ct);
    [HttpGet] public async Task<IActionResult> EditPayrollPeriod(Guid id, CancellationToken ct = default) { var dto = await _getPayrollPeriod.HandleAsync(id, ct); if (dto is null) { SetErrorMessage("PayrollPeriodNotFound"); return RedirectToAction(nameof(PayrollPeriods)); } var vm = MapPayrollPeriod(dto); await PopulatePayrollPeriodOptionsAsync(vm, ct); return View("PayrollPeriodEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditPayrollPeriod(PayrollPeriodEditVm vm, CancellationToken ct = default) => await SavePayrollPeriodAsync(vm, false, ct);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> PreparePayrollPeriod(Guid id, string rowVersion, Guid businessId, CancellationToken ct = default) { var result = await _preparePayrollPeriod.HandleAsync(new HrTimeLifecycleDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct); if (result.Succeeded) SetSuccessMessage("PayrollPeriodPrepared"); else SetErrorMessage(result.Error ?? "PayrollPeriodPrepareFailed"); return RedirectToAction(nameof(EditPayrollPeriod), new { id }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> UpdatePayrollPeriodLifecycle(Guid id, string rowVersion, PayrollPeriodStatus target, string? notes, Guid businessId, CancellationToken ct = default) { var result = await _updatePayrollPeriodLifecycle.HandleAsync(new HrTimeLifecycleDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion), Notes = notes }, target, ct); if (result.Succeeded) SetSuccessMessage("PayrollPeriodUpdated"); else SetErrorMessage(result.Error ?? "PayrollPeriodUpdateFailed"); return RedirectToAction(nameof(EditPayrollPeriod), new { id }); }

    [HttpGet]
    public async Task<IActionResult> PayrollRules(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, PayrollRuleSetQueueFilter filter = PayrollRuleSetQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct);
        var vm = new HrListVm<PayrollRuleSetListItemDto, PayrollRuleSetQueueFilter> { BusinessId = businessId, Query = q ?? string.Empty, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct), Page = page, PageSize = pageSize };
        if (businessId.HasValue) { var result = await _getPayrollRuleSets.HandleAsync(businessId.Value, page, pageSize, q, filter, ct); vm.Items = result.Items; vm.Total = result.Total; }
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> CreatePayrollRuleSet(Guid? businessId = null, CancellationToken ct = default) { var vm = new PayrollRuleSetEditVm { BusinessId = await ResolveBusinessRequiredAsync(businessId, ct), JurisdictionCode = "DE", Currency = "EUR", EffectiveFromUtc = DateTime.UtcNow.Date, MetadataJson = "{}" }; await PopulatePayrollRuleSetOptionsAsync(vm, ct); ViewData["IsCreate"] = true; return View("PayrollRuleSetEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreatePayrollRuleSet(PayrollRuleSetEditVm vm, CancellationToken ct = default) => await SavePayrollRuleSetAsync(vm, true, ct);
    [HttpGet] public async Task<IActionResult> EditPayrollRuleSet(Guid id, CancellationToken ct = default) { var dto = await _getPayrollRuleSet.HandleAsync(id, ct); if (dto is null) { SetErrorMessage("PayrollRuleSetNotFound"); return RedirectToAction(nameof(PayrollRules)); } var vm = MapPayrollRuleSet(dto); await PopulatePayrollRuleSetOptionsAsync(vm, ct); return View("PayrollRuleSetEditor", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditPayrollRuleSet(PayrollRuleSetEditVm vm, CancellationToken ct = default) => await SavePayrollRuleSetAsync(vm, false, ct);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ArchivePayrollRuleSet(Guid id, string rowVersion, Guid businessId, CancellationToken ct = default) { var result = await _archivePayrollRuleSet.HandleAsync(new HrArchiveDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct); if (result.Succeeded) SetSuccessMessage("PayrollRuleSetArchived"); else SetErrorMessage(result.Error ?? "PayrollRuleSetArchiveFailed"); return RedirectToAction(nameof(PayrollRules), new { businessId }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> AddPayrollRuleComponent(PayrollRuleComponentVm vm, CancellationToken ct = default) { var result = await _upsertPayrollRuleComponent.HandleAsync(MapPayrollRuleComponent(vm), ct); if (result.Succeeded) SetSuccessMessage("PayrollRuleComponentSaved"); else SetErrorMessage(result.Error ?? "PayrollRuleComponentSaveFailed"); return RedirectToAction(nameof(EditPayrollRuleSet), new { id = vm.PayrollRuleSetId }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ArchivePayrollRuleComponent(Guid id, string rowVersion, Guid payrollRuleSetId, CancellationToken ct = default) { var result = await _archivePayrollRuleComponent.HandleAsync(new HrArchiveDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct); if (result.Succeeded) SetSuccessMessage("PayrollRuleComponentArchived"); else SetErrorMessage(result.Error ?? "PayrollRuleComponentArchiveFailed"); return RedirectToAction(nameof(EditPayrollRuleSet), new { id = payrollRuleSetId }); }

    [HttpGet]
    public async Task<IActionResult> PayrollRuns(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, PayrollRunQueueFilter filter = PayrollRunQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct);
        var vm = new HrListVm<PayrollRunListItemDto, PayrollRunQueueFilter> { BusinessId = businessId, Query = q ?? string.Empty, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct), Page = page, PageSize = pageSize };
        if (businessId.HasValue) { var result = await _getPayrollRuns.HandleAsync(businessId.Value, page, pageSize, q, filter, ct); vm.Items = result.Items; vm.Total = result.Total; }
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> CreatePayrollRun(Guid? businessId = null, CancellationToken ct = default) { var vm = new PayrollRunCreateVm { BusinessId = await ResolveBusinessRequiredAsync(businessId, ct), MetadataJson = "{}" }; await PopulatePayrollRunCreateOptionsAsync(vm, ct); return View("PayrollRunCreate", vm); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreatePayrollRun(PayrollRunCreateVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) { await PopulatePayrollRunCreateOptionsAsync(vm, ct); return View("PayrollRunCreate", vm); }
        try { var id = await _createPayrollRun.HandleAsync(MapPayrollRunCreate(vm), ct); SetSuccessMessage("PayrollRunCreated"); return RedirectToAction(nameof(EditPayrollRun), new { id }); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) { AddModelErrorMessage("PayrollRunCreateFailed"); await PopulatePayrollRunCreateOptionsAsync(vm, ct); return View("PayrollRunCreate", vm); }
    }

    [HttpGet] public async Task<IActionResult> EditPayrollRun(Guid id, CancellationToken ct = default) { var dto = await _getPayrollRun.HandleAsync(id, ct); if (dto is null) { SetErrorMessage("PayrollRunNotFound"); return RedirectToAction(nameof(PayrollRuns)); } return View("PayrollRunEditor", new PayrollRunDetailVm { Run = dto, Payslips = (await _payrollPayslips.GetForRunAsync(id, ct).ConfigureAwait(false)).ToList(), PayslipStorageReady = _payrollPayslips.IsStorageReady() }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CalculatePayrollRun(Guid id, string rowVersion, CancellationToken ct = default) { var result = await _calculatePayrollRun.HandleAsync(new PayrollRunLifecycleDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct); if (result.Succeeded) SetSuccessMessage("PayrollRunCalculated"); else SetErrorMessage(result.Error ?? "PayrollRunCalculateFailed"); return RedirectToAction(nameof(EditPayrollRun), new { id }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> UpdatePayrollRunLifecycle(Guid id, string rowVersion, PayrollRunStatus target, string? notes, CancellationToken ct = default) { var result = await _updatePayrollRunLifecycle.HandleAsync(new PayrollRunLifecycleDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion), Notes = notes }, target, ct); if (result.Succeeded) SetSuccessMessage("PayrollRunUpdated"); else SetErrorMessage(result.Error ?? "PayrollRunUpdateFailed"); return RedirectToAction(target == PayrollRunStatus.Cancelled ? nameof(PayrollRuns) : nameof(EditPayrollRun), new { id }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> PostPayrollRun(Guid id, string rowVersion, CancellationToken ct = default) { var result = await _postPayrollRun.HandleAsync(new PayrollRunLifecycleDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct); if (result.Succeeded) SetSuccessMessage("PayrollRunPosted"); else SetErrorMessage(result.Error ?? "PayrollRunPostFailed"); return RedirectToAction(nameof(EditPayrollRun), new { id }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> GeneratePayrollPayslips(Guid id, string rowVersion, CancellationToken ct = default) { var result = await _payrollPayslips.GenerateForRunAsync(new GeneratePayrollPayslipsDto { PayrollRunId = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct); if (result.Succeeded) SetSuccessMessage("PayrollPayslipsGenerated"); else SetErrorMessage(result.Error ?? "PayrollPayslipGenerationFailed"); return RedirectToAction(nameof(EditPayrollRun), new { id }); }
    [HttpGet] public async Task<IActionResult> DownloadPayrollPayslip(Guid businessId, Guid payslipId, CancellationToken ct = default) { var result = await _payrollPayslips.DownloadAsync(businessId, payslipId, ct); if (!result.Succeeded || result.Value is null) { SetErrorMessage(result.Error ?? "PayrollPayslipDownloadFailed"); return RedirectToAction(nameof(PayrollRuns), new { businessId }); } return File(result.Value.Content, result.Value.ContentType, result.Value.FileName); }

    [HttpGet]
    public async Task<IActionResult> PayrollPayments(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, PayrollPaymentQueueFilter filter = PayrollPaymentQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
        var dto = await _getPayrollPayments.HandleAsync(businessId, q, filter, page, pageSize, ct).ConfigureAwait(false);
        var vm = new HrListVm<PayrollPaymentListItemDto, PayrollPaymentQueueFilter>
        {
            BusinessId = dto.BusinessId,
            Query = dto.Query,
            Filter = filter,
            FilterItems = BuildEnumOptions(filter),
            BusinessOptions = await _referenceData.GetBusinessOptionsAsync(dto.BusinessId, ct).ConfigureAwait(false),
            Page = dto.Page,
            PageSize = dto.PageSize,
            Total = dto.Total,
            Items = dto.Items
        };
        return View("PayrollPayments", vm);
    }

    [HttpGet]
    public async Task<IActionResult> CreatePayrollPayment(Guid? businessId = null, Guid? payrollRunId = null, CancellationToken ct = default)
    {
        var dto = await _getPayrollPaymentDraft.HandleAsync(businessId, payrollRunId, ct).ConfigureAwait(false);
        var vm = MapPayrollPaymentVm(dto);
        await PopulatePayrollPaymentOptionsAsync(vm, ct).ConfigureAwait(false);
        return View("PayrollPaymentEditor", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayrollPayment(PayrollPaymentEditVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) { await PopulatePayrollPaymentOptionsAsync(vm, ct).ConfigureAwait(false); return View("PayrollPaymentEditor", vm); }
        try { var id = await _createPayrollPayment.HandleAsync(MapPayrollPaymentCreate(vm), ct).ConfigureAwait(false); SetSuccessMessage("PayrollPaymentCreated"); return RedirectToAction(nameof(EditPayrollPayment), new { id }); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) { AddModelErrorMessage("PayrollPaymentCreateFailed"); await PopulatePayrollPaymentOptionsAsync(vm, ct).ConfigureAwait(false); return View("PayrollPaymentEditor", vm); }
    }

    [HttpGet]
    public async Task<IActionResult> EditPayrollPayment(Guid id, CancellationToken ct = default)
    {
        var dto = await _getPayrollPayment.HandleAsync(id, ct).ConfigureAwait(false);
        if (dto is null) { SetErrorMessage("PayrollPaymentNotFound"); return RedirectToAction(nameof(PayrollPayments)); }
        var vm = MapPayrollPaymentVm(dto);
        await PopulatePayrollPaymentOptionsAsync(vm, ct).ConfigureAwait(false);
        return View("PayrollPaymentEditor", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPayrollPayment(PayrollPaymentEditVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) { await PopulatePayrollPaymentOptionsAsync(vm, ct).ConfigureAwait(false); return View("PayrollPaymentEditor", vm); }
        try { await _updatePayrollPayment.HandleAsync(MapPayrollPaymentEdit(vm), ct).ConfigureAwait(false); SetSuccessMessage("PayrollPaymentUpdated"); return RedirectToAction(nameof(EditPayrollPayment), new { id = vm.Id }); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) { AddModelErrorMessage("PayrollPaymentUpdateFailed"); await PopulatePayrollPaymentOptionsAsync(vm, ct).ConfigureAwait(false); return View("PayrollPaymentEditor", vm); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PostPayrollPayment(Guid id, string rowVersion, CancellationToken ct = default)
    {
        try { await _postPayrollPayment.HandleAsync(new PayrollPaymentLifecycleActionDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct).ConfigureAwait(false); SetSuccessMessage("PayrollPaymentPosted"); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) { SetErrorMessage("PayrollPaymentPostFailed"); }
        return RedirectToAction(nameof(EditPayrollPayment), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelPayrollPayment(Guid id, string rowVersion, CancellationToken ct = default)
    {
        try { await _cancelPayrollPayment.HandleAsync(new PayrollPaymentLifecycleActionDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) }, ct).ConfigureAwait(false); SetSuccessMessage("PayrollPaymentCancelled"); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) { SetErrorMessage("PayrollPaymentCancelFailed"); return RedirectToAction(nameof(EditPayrollPayment), new { id }); }
        return RedirectToAction(nameof(PayrollPayments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReversePayrollPayment(Guid id, string rowVersion, string? reason, CancellationToken ct = default)
    {
        try { await _reversePayrollPayment.HandleAsync(new PayrollPaymentLifecycleActionDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion), Reason = reason }, ct).ConfigureAwait(false); SetSuccessMessage("PayrollPaymentReversed"); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) { SetErrorMessage("PayrollPaymentReverseFailed"); }
        return RedirectToAction(nameof(EditPayrollPayment), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SettlePayrollPaymentFromBankReconciliation(Guid id, string rowVersion, Guid bankReconciliationMatchId, string? notes, CancellationToken ct = default)
    {
        try
        {
            await _settlePayrollPaymentFromBankReconciliation.HandleAsync(new PayrollPaymentBankSettlementActionDto
            {
                Id = id,
                RowVersion = DecodeBase64RowVersion(rowVersion),
                BankReconciliationMatchId = bankReconciliationMatchId,
                Notes = notes
            }, ct).ConfigureAwait(false);
            SetSuccessMessage("PayrollPaymentBankSettled");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("PayrollPaymentBankSettlementFailed");
        }
        return RedirectToAction(nameof(EditPayrollPayment), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayrollPaymentBankCorrection(Guid id, string rowVersion, PayrollPaymentBankCorrectionType correctionType, Guid bankReconciliationMatchId, Guid? bankStatementLineId, string reason, string? internalNotes, CancellationToken ct = default)
    {
        try
        {
            await _createPayrollPaymentBankCorrection.HandleAsync(new PayrollPaymentBankCorrectionCreateDto
            {
                PayrollPaymentId = id,
                PayrollPaymentRowVersion = DecodeBase64RowVersion(rowVersion),
                CorrectionType = correctionType,
                BankReconciliationMatchId = bankReconciliationMatchId,
                BankStatementLineId = bankStatementLineId,
                Reason = reason,
                InternalNotes = internalNotes
            }, ct).ConfigureAwait(false);
            SetSuccessMessage("PayrollPaymentBankCorrectionCreated");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("PayrollPaymentBankCorrectionCreateFailed");
        }
        return RedirectToAction(nameof(EditPayrollPayment), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostPayrollPaymentBankCorrection(Guid id, Guid correctionId, string correctionRowVersion, CancellationToken ct = default)
    {
        try
        {
            await _postPayrollPaymentBankCorrection.HandleAsync(new PayrollPaymentBankCorrectionActionDto
            {
                Id = correctionId,
                RowVersion = DecodeBase64RowVersion(correctionRowVersion)
            }, ct).ConfigureAwait(false);
            SetSuccessMessage("PayrollPaymentBankCorrectionPosted");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("PayrollPaymentBankCorrectionPostFailed");
        }
        return RedirectToAction(nameof(EditPayrollPayment), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelPayrollPaymentBankCorrection(Guid id, Guid correctionId, string correctionRowVersion, CancellationToken ct = default)
    {
        try
        {
            await _cancelPayrollPaymentBankCorrection.HandleAsync(new PayrollPaymentBankCorrectionActionDto
            {
                Id = correctionId,
                RowVersion = DecodeBase64RowVersion(correctionRowVersion)
            }, ct).ConfigureAwait(false);
            SetSuccessMessage("PayrollPaymentBankCorrectionCancelled");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("PayrollPaymentBankCorrectionCancelFailed");
        }
        return RedirectToAction(nameof(EditPayrollPayment), new { id });
    }

    private async Task<IActionResult> ListDepartmentsAsync(Guid? businessId, int page, int pageSize, string? q, DepartmentQueueFilter filter, CancellationToken ct)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct);
        var vm = new HrListVm<DepartmentListItemDto, DepartmentQueueFilter> { BusinessId = businessId, Query = q ?? string.Empty, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct), Page = page, PageSize = pageSize };
        if (businessId.HasValue) { var result = await _getDepartments.HandleAsync(businessId.Value, page, pageSize, q, filter, ct); vm.Items = result.Items; vm.Total = result.Total; }
        return View("Departments", vm);
    }

    private async Task<IActionResult> ListPositionsAsync(Guid? businessId, int page, int pageSize, string? q, PositionQueueFilter filter, CancellationToken ct)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct);
        var vm = new HrListVm<PositionListItemDto, PositionQueueFilter> { BusinessId = businessId, Query = q ?? string.Empty, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct), Page = page, PageSize = pageSize };
        if (businessId.HasValue) { var result = await _getPositions.HandleAsync(businessId.Value, page, pageSize, q, filter, ct); vm.Items = result.Items; vm.Total = result.Total; }
        return View("Positions", vm);
    }

    private async Task<Guid> ResolveBusinessRequiredAsync(Guid? businessId, CancellationToken ct) => (await _referenceData.ResolveBusinessIdAsync(businessId, ct)) ?? Guid.Empty;

    private async Task<IActionResult> SaveDepartmentAsync(DepartmentEditVm vm, bool isCreate, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await PopulateDepartmentOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("DepartmentEditor", vm); }
        try { if (isCreate) await _createDepartment.HandleAsync(MapDepartment(vm), ct); else await _updateDepartment.HandleAsync(MapDepartment(vm), ct); SetSuccessMessage(isCreate ? "DepartmentCreated" : "DepartmentUpdated"); return RedirectToAction(nameof(Departments), new { businessId = vm.BusinessId }); }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException) { AddModelErrorMessage("DepartmentSaveFailed"); await PopulateDepartmentOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("DepartmentEditor", vm); }
    }

    private async Task<IActionResult> SavePositionAsync(PositionEditVm vm, bool isCreate, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await PopulatePositionOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("PositionEditor", vm); }
        try { if (isCreate) await _createPosition.HandleAsync(MapPosition(vm), ct); else await _updatePosition.HandleAsync(MapPosition(vm), ct); SetSuccessMessage(isCreate ? "PositionCreated" : "PositionUpdated"); return RedirectToAction(nameof(Positions), new { businessId = vm.BusinessId }); }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException) { AddModelErrorMessage("PositionSaveFailed"); await PopulatePositionOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("PositionEditor", vm); }
    }

    private async Task<IActionResult> SaveContractAsync(EmploymentContractEditVm vm, bool isCreate, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await PopulateContractOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("EmploymentContractEditor", vm); }
        try { if (isCreate) await _createContract.HandleAsync(MapContract(vm), ct); else await _updateContract.HandleAsync(MapContract(vm), ct); SetSuccessMessage(isCreate ? "EmploymentContractCreated" : "EmploymentContractUpdated"); return RedirectToAction(nameof(EmploymentContracts), new { businessId = vm.BusinessId, employeeId = vm.EmployeeId }); }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException) { AddModelErrorMessage("EmploymentContractSaveFailed"); await PopulateContractOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("EmploymentContractEditor", vm); }
    }

    private async Task PopulateEmployeeOptionsAsync(EmployeeEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        var departments = vm.BusinessId == Guid.Empty ? new List<DepartmentListItemDto>() : (await _getDepartments.HandleAsync(vm.BusinessId, 1, 200, null, DepartmentQueueFilter.Active, ct)).Items;
        var positions = vm.BusinessId == Guid.Empty ? new List<PositionListItemDto>() : (await _getPositions.HandleAsync(vm.BusinessId, 1, 200, null, PositionQueueFilter.Active, ct)).Items;
        vm.DepartmentOptions = OptionalOptions(departments.Select(x => new SelectListItem(x.DisplayName, x.Id.ToString(), x.Id == vm.DepartmentId)));
        vm.PositionOptions = OptionalOptions(positions.Select(x => new SelectListItem(x.DisplayName, x.Id.ToString(), x.Id == vm.PositionId)));
        vm.StatusOptions = BuildEnumOptions(vm.Status);
        vm.PrivacyOptions = BuildEnumOptions(vm.PrivacyClassification);
        vm.DocumentKindOptions = BuildEnumOptions(DocumentRecordKind.StaffDocument);
        vm.DocumentPrivacyOptions = BuildEnumOptions(HrPrivacyClassification.Restricted);
        if (vm.Id != Guid.Empty)
        {
            vm.Documents = (await _personnelDocuments.GetDocumentsAsync(vm.Id, ct).ConfigureAwait(false)).ToList();
        }
    }

    private async Task PopulateDepartmentOptionsAsync(DepartmentEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        var departments = vm.BusinessId == Guid.Empty ? new List<DepartmentListItemDto>() : (await _getDepartments.HandleAsync(vm.BusinessId, 1, 200, null, DepartmentQueueFilter.Active, ct)).Items.Where(x => x.Id != vm.Id).ToList();
        vm.ParentDepartmentOptions = OptionalOptions(departments.Select(x => new SelectListItem(x.DisplayName, x.Id.ToString(), x.Id == vm.ParentDepartmentId)));
        vm.StatusOptions = BuildEnumOptions(vm.Status);
    }

    private async Task PopulatePositionOptionsAsync(PositionEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        var departments = vm.BusinessId == Guid.Empty ? new List<DepartmentListItemDto>() : (await _getDepartments.HandleAsync(vm.BusinessId, 1, 200, null, DepartmentQueueFilter.Active, ct)).Items;
        vm.DepartmentOptions = OptionalOptions(departments.Select(x => new SelectListItem(x.DisplayName, x.Id.ToString(), x.Id == vm.DepartmentId)));
        vm.StatusOptions = BuildEnumOptions(vm.Status);
    }

    private async Task PopulateContractOptionsAsync(EmploymentContractEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        var employees = vm.BusinessId == Guid.Empty ? new List<EmployeeListItemDto>() : (await _getEmployees.HandleAsync(vm.BusinessId, 1, 200, null, EmployeeQueueFilter.Active, ct)).Items;
        vm.EmployeeOptions = employees.Select(x => new SelectListItem($"{x.EmployeeNumber} - {x.FullName}", x.Id.ToString(), x.Id == vm.EmployeeId)).ToList();
        vm.EmploymentTypeOptions = BuildEnumOptions(vm.EmploymentType);
        vm.StatusOptions = BuildEnumOptions(vm.Status);
        vm.PrivacyOptions = BuildEnumOptions(vm.PrivacyClassification);
    }

    private async Task<IActionResult> SaveScheduleAsync(WorkScheduleEditVm vm, bool isCreate, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await PopulateScheduleOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("WorkScheduleEditor", vm); }
        try { if (isCreate) await _createSchedule.HandleAsync(MapSchedule(vm), ct); else await _updateSchedule.HandleAsync(MapSchedule(vm), ct); SetSuccessMessage(isCreate ? "WorkScheduleCreated" : "WorkScheduleUpdated"); return RedirectToAction(nameof(WorkSchedules), new { businessId = vm.BusinessId, employeeId = vm.EmployeeId }); }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException) { AddModelErrorMessage("WorkScheduleSaveFailed"); await PopulateScheduleOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("WorkScheduleEditor", vm); }
    }

    private async Task<IActionResult> SaveTimeEntryAsync(TimeEntryEditVm vm, bool isCreate, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await PopulateTimeEntryOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("TimeEntryEditor", vm); }
        try { if (isCreate) await _createTimeEntry.HandleAsync(MapTimeEntry(vm), ct); else await _updateTimeEntry.HandleAsync(MapTimeEntry(vm), ct); SetSuccessMessage(isCreate ? "TimeEntryCreated" : "TimeEntryUpdated"); return RedirectToAction(nameof(TimeEntries), new { businessId = vm.BusinessId, employeeId = vm.EmployeeId }); }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException) { AddModelErrorMessage("TimeEntrySaveFailed"); await PopulateTimeEntryOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("TimeEntryEditor", vm); }
    }

    private async Task<IActionResult> SaveTimesheetAsync(TimesheetEditVm vm, bool isCreate, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await PopulateTimesheetOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("TimesheetEditor", vm); }
        try { if (isCreate) await _createTimesheet.HandleAsync(MapTimesheet(vm), ct); else await _updateTimesheet.HandleAsync(MapTimesheet(vm), ct); SetSuccessMessage(isCreate ? "TimesheetCreated" : "TimesheetUpdated"); return RedirectToAction(nameof(Timesheets), new { businessId = vm.BusinessId, employeeId = vm.EmployeeId }); }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException) { AddModelErrorMessage("TimesheetSaveFailed"); await PopulateTimesheetOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("TimesheetEditor", vm); }
    }

    private async Task PopulateScheduleOptionsAsync(WorkScheduleEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        var employees = vm.BusinessId == Guid.Empty ? new List<EmployeeListItemDto>() : (await _getEmployees.HandleAsync(vm.BusinessId, 1, 200, null, EmployeeQueueFilter.Active, ct)).Items;
        vm.EmployeeOptions = employees.Select(x => new SelectListItem($"{x.EmployeeNumber} - {x.FullName}", x.Id.ToString(), x.Id == vm.EmployeeId)).ToList();
        vm.StatusOptions = BuildEnumOptions(vm.Status);
    }

    private async Task PopulateAttendanceOptionsAsync(AttendanceEventEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        var employees = vm.BusinessId == Guid.Empty ? new List<EmployeeListItemDto>() : (await _getEmployees.HandleAsync(vm.BusinessId, 1, 200, null, EmployeeQueueFilter.Active, ct)).Items;
        vm.EmployeeOptions = employees.Select(x => new SelectListItem($"{x.EmployeeNumber} - {x.FullName}", x.Id.ToString(), x.Id == vm.EmployeeId)).ToList();
        vm.EventTypeOptions = BuildEnumOptions(vm.EventType);
    }

    private async Task PopulateTimeEntryOptionsAsync(TimeEntryEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        var employees = vm.BusinessId == Guid.Empty ? new List<EmployeeListItemDto>() : (await _getEmployees.HandleAsync(vm.BusinessId, 1, 200, null, EmployeeQueueFilter.Active, ct)).Items;
        var schedules = vm.BusinessId == Guid.Empty ? new List<WorkScheduleListItemDto>() : (await _getSchedules.HandleAsync(vm.BusinessId, vm.EmployeeId == Guid.Empty ? null : vm.EmployeeId, 1, 200, null, WorkScheduleQueueFilter.Active, ct)).Items;
        vm.EmployeeOptions = employees.Select(x => new SelectListItem($"{x.EmployeeNumber} - {x.FullName}", x.Id.ToString(), x.Id == vm.EmployeeId)).ToList();
        vm.ScheduleOptions = OptionalOptions(schedules.Select(x => new SelectListItem(x.ScheduleCode, x.Id.ToString(), x.Id == vm.WorkScheduleId)));
        vm.SourceOptions = BuildEnumOptions(vm.Source);
        vm.StatusOptions = BuildEnumOptions(vm.Status);
    }

    private async Task PopulateTimesheetOptionsAsync(TimesheetEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        var employees = vm.BusinessId == Guid.Empty ? new List<EmployeeListItemDto>() : (await _getEmployees.HandleAsync(vm.BusinessId, 1, 200, null, EmployeeQueueFilter.Active, ct)).Items;
        var entries = vm.BusinessId == Guid.Empty ? new List<TimeEntryListItemDto>() : (await _getTimeEntries.HandleAsync(vm.BusinessId, vm.EmployeeId == Guid.Empty ? null : vm.EmployeeId, 1, 200, null, TimeEntryQueueFilter.All, vm.PeriodStartUtc, vm.PeriodEndUtc, ct)).Items.Where(x => x.Status is TimeEntryStatus.Draft or TimeEntryStatus.Submitted or TimeEntryStatus.Rejected || vm.TimeEntryIds.Contains(x.Id)).ToList();
        vm.EmployeeOptions = employees.Select(x => new SelectListItem($"{x.EmployeeNumber} - {x.FullName}", x.Id.ToString(), x.Id == vm.EmployeeId)).ToList();
        vm.TimeEntryOptions = entries.Select(x => new SelectListItem($"{x.WorkDateUtc:yyyy-MM-dd} {x.WorkType} {x.DurationMinutes}m", x.Id.ToString(), vm.TimeEntryIds.Contains(x.Id))).ToList();
        vm.StatusOptions = BuildEnumOptions(vm.Status);
    }

    private async Task<IActionResult> SaveLeaveRequestAsync(LeaveRequestEditVm vm, bool isCreate, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await PopulateLeaveRequestOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("LeaveRequestEditor", vm); }
        try { if (isCreate) await _createLeaveRequest.HandleAsync(MapLeaveRequest(vm), ct); else await _updateLeaveRequest.HandleAsync(MapLeaveRequest(vm), ct); SetSuccessMessage(isCreate ? "LeaveRequestCreated" : "LeaveRequestUpdated"); return RedirectToAction(nameof(LeaveRequests), new { businessId = vm.BusinessId, employeeId = vm.EmployeeId }); }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException) { AddModelErrorMessage("LeaveRequestSaveFailed"); await PopulateLeaveRequestOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("LeaveRequestEditor", vm); }
    }

    private async Task<IActionResult> SaveAbsenceAsync(AbsenceRecordEditVm vm, bool isCreate, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await PopulateAbsenceOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("AbsenceEditor", vm); }
        try { if (isCreate) await _createAbsenceRecord.HandleAsync(MapAbsence(vm), ct); else await _updateAbsenceRecord.HandleAsync(MapAbsence(vm), ct); SetSuccessMessage(isCreate ? "AbsenceCreated" : "AbsenceUpdated"); return RedirectToAction(nameof(Absences), new { businessId = vm.BusinessId, employeeId = vm.EmployeeId }); }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException) { AddModelErrorMessage("AbsenceSaveFailed"); await PopulateAbsenceOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("AbsenceEditor", vm); }
    }

    private async Task PopulateLeaveRequestOptionsAsync(LeaveRequestEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        var employees = vm.BusinessId == Guid.Empty ? new List<EmployeeListItemDto>() : (await _getEmployees.HandleAsync(vm.BusinessId, 1, 200, null, EmployeeQueueFilter.Active, ct)).Items;
        vm.EmployeeOptions = employees.Select(x => new SelectListItem($"{x.EmployeeNumber} - {x.FullName}", x.Id.ToString(), x.Id == vm.EmployeeId)).ToList();
        vm.LeaveTypeOptions = BuildEnumOptions(vm.LeaveType);
        vm.StatusOptions = BuildEnumOptions(vm.Status);
        vm.PrivacyOptions = BuildEnumOptions(vm.PrivacyClassification);
    }

    private async Task PopulateAbsenceOptionsAsync(AbsenceRecordEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        var employees = vm.BusinessId == Guid.Empty ? new List<EmployeeListItemDto>() : (await _getEmployees.HandleAsync(vm.BusinessId, 1, 200, null, EmployeeQueueFilter.Active, ct)).Items;
        var leaveRequests = vm.BusinessId == Guid.Empty ? new List<LeaveRequestListItemDto>() : (await _getLeaveRequests.HandleAsync(vm.BusinessId, vm.EmployeeId == Guid.Empty ? null : vm.EmployeeId, 1, 200, null, LeaveRequestQueueFilter.Approved, ct)).Items;
        vm.EmployeeOptions = employees.Select(x => new SelectListItem($"{x.EmployeeNumber} - {x.FullName}", x.Id.ToString(), x.Id == vm.EmployeeId)).ToList();
        vm.LeaveRequestOptions = OptionalOptions(leaveRequests.Select(x => new SelectListItem(x.RequestNumber, x.Id.ToString(), x.Id == vm.LeaveRequestId)));
        vm.AbsenceTypeOptions = BuildEnumOptions(vm.AbsenceType);
        vm.StatusOptions = BuildEnumOptions(vm.Status);
        vm.PrivacyOptions = BuildEnumOptions(vm.PrivacyClassification);
    }

    private async Task<IActionResult> SavePayrollPeriodAsync(PayrollPeriodEditVm vm, bool isCreate, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await PopulatePayrollPeriodOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("PayrollPeriodEditor", vm); }
        try { if (isCreate) await _createPayrollPeriod.HandleAsync(MapPayrollPeriod(vm), ct); else await _updatePayrollPeriod.HandleAsync(MapPayrollPeriod(vm), ct); SetSuccessMessage(isCreate ? "PayrollPeriodCreated" : "PayrollPeriodUpdated"); return RedirectToAction(nameof(PayrollPeriods), new { businessId = vm.BusinessId }); }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException) { AddModelErrorMessage("PayrollPeriodSaveFailed"); await PopulatePayrollPeriodOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("PayrollPeriodEditor", vm); }
    }

    private async Task PopulatePayrollPeriodOptionsAsync(PayrollPeriodEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        vm.StatusOptions = BuildEnumOptions(vm.Status);
    }

    private async Task<IActionResult> SavePayrollRuleSetAsync(PayrollRuleSetEditVm vm, bool isCreate, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await PopulatePayrollRuleSetOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("PayrollRuleSetEditor", vm); }
        try { if (isCreate) await _createPayrollRuleSet.HandleAsync(MapPayrollRuleSet(vm), ct); else await _updatePayrollRuleSet.HandleAsync(MapPayrollRuleSet(vm), ct); SetSuccessMessage(isCreate ? "PayrollRuleSetCreated" : "PayrollRuleSetUpdated"); return RedirectToAction(nameof(PayrollRules), new { businessId = vm.BusinessId }); }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException or ArgumentException) { AddModelErrorMessage("PayrollRuleSetSaveFailed"); await PopulatePayrollRuleSetOptionsAsync(vm, ct); ViewData["IsCreate"] = isCreate; return View("PayrollRuleSetEditor", vm); }
    }

    private async Task PopulatePayrollRuleSetOptionsAsync(PayrollRuleSetEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        vm.StatusOptions = BuildEnumOptions(vm.Status);
        vm.ComponentTypeOptions = BuildEnumOptions(PayrollRuleComponentType.GrossPay);
        vm.CalculationMethodOptions = BuildEnumOptions(PayrollRuleCalculationMethod.Percentage);
        vm.BasisOptions = BuildEnumOptions(PayrollRuleBasis.GrossPay);
        vm.NewComponent.BusinessId = vm.BusinessId;
        vm.NewComponent.PayrollRuleSetId = vm.Id;
        vm.NewComponent.ThresholdJson ??= "{}";
        vm.NewComponent.MetadataJson ??= "{}";
    }

    private async Task PopulatePayrollRunCreateOptionsAsync(PayrollRunCreateVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        var periods = vm.BusinessId == Guid.Empty ? new List<PayrollPeriodListItemDto>() : (await _getPayrollPeriods.HandleAsync(vm.BusinessId, 1, 200, null, PayrollPeriodQueueFilter.Approved, ct)).Items;
        var rules = vm.BusinessId == Guid.Empty ? new List<PayrollRuleSetListItemDto>() : (await _getPayrollRuleSets.HandleAsync(vm.BusinessId, 1, 200, null, PayrollRuleSetQueueFilter.Active, ct)).Items;
        vm.PayrollPeriodOptions = periods.Select(x => new SelectListItem($"{x.PeriodCode} ({x.PeriodStartUtc:yyyy-MM-dd} - {x.PeriodEndUtc:yyyy-MM-dd})", x.Id.ToString(), x.Id == vm.PayrollPeriodId)).ToList();
        vm.PayrollRuleSetOptions = rules.Select(x => new SelectListItem($"{x.RuleSetCode} {x.RuleVersion} ({x.JurisdictionCode})", x.Id.ToString(), x.Id == vm.PayrollRuleSetId)).ToList();
    }

    private async Task PopulatePayrollPaymentOptionsAsync(PayrollPaymentEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct);
        vm.PaymentMethodOptions = BuildEnumOptions(vm.PaymentMethod);
        vm.Allocations ??= new List<PayrollPaymentAllocationDto>();
        vm.MetadataJson ??= "{}";
    }

    private static List<SelectListItem> OptionalOptions(IEnumerable<SelectListItem> items)
    {
        var list = new List<SelectListItem> { new(string.Empty, string.Empty) };
        list.AddRange(items);
        return list;
    }

    private List<SelectListItem> BuildEnumOptions<TEnum>(TEnum selected) where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Select(x => new SelectListItem(T(x.ToString()), x.ToString(), EqualityComparer<TEnum>.Default.Equals(x, selected))).ToList();

    private static EmployeeEditDto MapEmployee(EmployeeEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, BusinessMemberId = vm.BusinessMemberId, DepartmentId = vm.DepartmentId, PositionId = vm.PositionId, EmployeeNumber = vm.EmployeeNumber, FirstName = vm.FirstName, LastName = vm.LastName, PreferredName = vm.PreferredName, WorkEmail = vm.WorkEmail, WorkPhone = vm.WorkPhone, Status = vm.Status, HireDateUtc = vm.HireDateUtc, TerminationDateUtc = vm.TerminationDateUtc, PrivacyClassification = vm.PrivacyClassification, InternalNotes = vm.InternalNotes, MetadataJson = vm.MetadataJson };
    private static EmployeeEditVm MapEmployee(EmployeeDetailDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, BusinessMemberId = dto.BusinessMemberId, DepartmentId = dto.DepartmentId, PositionId = dto.PositionId, EmployeeNumber = dto.EmployeeNumber, FirstName = dto.FirstName, LastName = dto.LastName, PreferredName = dto.PreferredName, WorkEmail = dto.WorkEmail, WorkPhone = dto.WorkPhone, Status = dto.Status, HireDateUtc = dto.HireDateUtc, TerminationDateUtc = dto.TerminationDateUtc, PrivacyClassification = dto.PrivacyClassification, InternalNotes = dto.InternalNotes, MetadataJson = dto.MetadataJson, DepartmentName = dto.DepartmentName, PositionName = dto.PositionName, Contracts = dto.Contracts };
    private static DepartmentEditDto MapDepartment(DepartmentEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, ParentDepartmentId = vm.ParentDepartmentId, Code = vm.Code, DisplayName = vm.DisplayName, Status = vm.Status, SortOrder = vm.SortOrder, Description = vm.Description, MetadataJson = vm.MetadataJson };
    private static DepartmentEditVm MapDepartment(DepartmentListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, ParentDepartmentId = dto.ParentDepartmentId, Code = dto.Code, DisplayName = dto.DisplayName, Status = dto.Status, SortOrder = dto.SortOrder, Description = dto.Description, MetadataJson = dto.MetadataJson, ParentDepartmentName = dto.ParentDepartmentName };
    private static PositionEditDto MapPosition(PositionEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, DepartmentId = vm.DepartmentId, Code = vm.Code, DisplayName = vm.DisplayName, Status = vm.Status, SortOrder = vm.SortOrder, Description = vm.Description, MetadataJson = vm.MetadataJson };
    private static PositionEditVm MapPosition(PositionListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, DepartmentId = dto.DepartmentId, Code = dto.Code, DisplayName = dto.DisplayName, Status = dto.Status, SortOrder = dto.SortOrder, Description = dto.Description, MetadataJson = dto.MetadataJson, DepartmentName = dto.DepartmentName };
    private static EmploymentContractEditDto MapContract(EmploymentContractEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, EmployeeId = vm.EmployeeId, ContractNumber = vm.ContractNumber, EmploymentType = vm.EmploymentType, Status = vm.Status, StartDateUtc = vm.StartDateUtc, EndDateUtc = vm.EndDateUtc, WeeklyHoursMinor = vm.WeeklyHoursMinor, PrivacyClassification = vm.PrivacyClassification, InternalNotes = vm.InternalNotes, MetadataJson = vm.MetadataJson };
    private static EmploymentContractEditVm MapContract(EmploymentContractListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, EmployeeId = dto.EmployeeId, EmployeeName = dto.EmployeeName, ContractNumber = dto.ContractNumber, EmploymentType = dto.EmploymentType, Status = dto.Status, StartDateUtc = dto.StartDateUtc, EndDateUtc = dto.EndDateUtc, WeeklyHoursMinor = dto.WeeklyHoursMinor, PrivacyClassification = dto.PrivacyClassification, InternalNotes = dto.InternalNotes, MetadataJson = dto.MetadataJson };
    private static WorkScheduleEditDto MapSchedule(WorkScheduleEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, EmployeeId = vm.EmployeeId, ScheduleCode = vm.ScheduleCode, Status = vm.Status, EffectiveFromUtc = vm.EffectiveFromUtc, EffectiveToUtc = vm.EffectiveToUtc, MondayMinutes = vm.MondayMinutes, TuesdayMinutes = vm.TuesdayMinutes, WednesdayMinutes = vm.WednesdayMinutes, ThursdayMinutes = vm.ThursdayMinutes, FridayMinutes = vm.FridayMinutes, SaturdayMinutes = vm.SaturdayMinutes, SundayMinutes = vm.SundayMinutes, InternalNotes = vm.InternalNotes, MetadataJson = vm.MetadataJson };
    private static WorkScheduleEditVm MapSchedule(WorkScheduleListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, EmployeeId = dto.EmployeeId, EmployeeName = dto.EmployeeName, ScheduleCode = dto.ScheduleCode, Status = dto.Status, EffectiveFromUtc = dto.EffectiveFromUtc, EffectiveToUtc = dto.EffectiveToUtc, MondayMinutes = dto.MondayMinutes, TuesdayMinutes = dto.TuesdayMinutes, WednesdayMinutes = dto.WednesdayMinutes, ThursdayMinutes = dto.ThursdayMinutes, FridayMinutes = dto.FridayMinutes, SaturdayMinutes = dto.SaturdayMinutes, SundayMinutes = dto.SundayMinutes, InternalNotes = dto.InternalNotes, MetadataJson = dto.MetadataJson, Exceptions = dto.Exceptions };
    private static AttendanceEventEditDto MapAttendance(AttendanceEventEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, EmployeeId = vm.EmployeeId, EventType = vm.EventType, OccurredAtUtc = vm.OccurredAtUtc, SourceReference = vm.SourceReference, InternalNotes = vm.InternalNotes, MetadataJson = vm.MetadataJson };
    private static TimeEntryEditDto MapTimeEntry(TimeEntryEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, EmployeeId = vm.EmployeeId, WorkScheduleId = vm.WorkScheduleId, WorkDateUtc = vm.WorkDateUtc, DurationMinutes = vm.DurationMinutes, BreakMinutes = vm.BreakMinutes, Source = vm.Source, Status = vm.Status, WorkType = vm.WorkType, Description = vm.Description, RejectionReason = vm.RejectionReason, InternalNotes = vm.InternalNotes, MetadataJson = vm.MetadataJson };
    private static TimeEntryEditVm MapTimeEntry(TimeEntryListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, EmployeeId = dto.EmployeeId, EmployeeName = dto.EmployeeName, WorkScheduleId = dto.WorkScheduleId, ScheduleCode = dto.ScheduleCode, WorkDateUtc = dto.WorkDateUtc, DurationMinutes = dto.DurationMinutes, BreakMinutes = dto.BreakMinutes, Source = dto.Source, Status = dto.Status, WorkType = dto.WorkType, Description = dto.Description, RejectionReason = dto.RejectionReason, InternalNotes = dto.InternalNotes, MetadataJson = dto.MetadataJson };
    private static TimesheetEditDto MapTimesheet(TimesheetEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, EmployeeId = vm.EmployeeId, TimesheetNumber = vm.TimesheetNumber, Status = vm.Status, PeriodStartUtc = vm.PeriodStartUtc, PeriodEndUtc = vm.PeriodEndUtc, ReviewNotes = vm.ReviewNotes, InternalNotes = vm.InternalNotes, MetadataJson = vm.MetadataJson, TimeEntryIds = vm.TimeEntryIds ?? new List<Guid>() };
    private static TimesheetEditVm MapTimesheet(TimesheetListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, EmployeeId = dto.EmployeeId, EmployeeName = dto.EmployeeName, TimesheetNumber = dto.TimesheetNumber, Status = dto.Status, PeriodStartUtc = dto.PeriodStartUtc, PeriodEndUtc = dto.PeriodEndUtc, ReviewNotes = dto.ReviewNotes, InternalNotes = dto.InternalNotes, MetadataJson = dto.MetadataJson, TotalWorkMinutes = dto.TotalWorkMinutes, TotalBreakMinutes = dto.TotalBreakMinutes, TimeEntryIds = dto.TimeEntryIds, Lines = dto.Lines };
    private static LeaveRequestEditDto MapLeaveRequest(LeaveRequestEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, EmployeeId = vm.EmployeeId, RequestNumber = vm.RequestNumber, LeaveType = vm.LeaveType, Status = vm.Status, StartDateUtc = vm.StartDateUtc, EndDateUtc = vm.EndDateUtc, RequestedMinutes = vm.RequestedMinutes, ReviewNotes = vm.ReviewNotes, PrivacyClassification = vm.PrivacyClassification, InternalNotes = vm.InternalNotes, MetadataJson = vm.MetadataJson };
    private static LeaveRequestEditVm MapLeaveRequest(LeaveRequestListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, EmployeeId = dto.EmployeeId, EmployeeName = dto.EmployeeName, RequestNumber = dto.RequestNumber, LeaveType = dto.LeaveType, Status = dto.Status, StartDateUtc = dto.StartDateUtc, EndDateUtc = dto.EndDateUtc, RequestedMinutes = dto.RequestedMinutes, ReviewNotes = dto.ReviewNotes, PrivacyClassification = dto.PrivacyClassification, InternalNotes = dto.InternalNotes, MetadataJson = dto.MetadataJson };
    private static AbsenceRecordEditDto MapAbsence(AbsenceRecordEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, EmployeeId = vm.EmployeeId, LeaveRequestId = vm.LeaveRequestId, AbsenceType = vm.AbsenceType, Status = vm.Status, StartDateUtc = vm.StartDateUtc, EndDateUtc = vm.EndDateUtc, AbsenceMinutes = vm.AbsenceMinutes, PrivacyClassification = vm.PrivacyClassification, InternalNotes = vm.InternalNotes, MetadataJson = vm.MetadataJson };
    private static AbsenceRecordEditVm MapAbsence(AbsenceRecordListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, EmployeeId = dto.EmployeeId, EmployeeName = dto.EmployeeName, LeaveRequestId = dto.LeaveRequestId, LeaveRequestNumber = dto.LeaveRequestNumber, AbsenceType = dto.AbsenceType, Status = dto.Status, StartDateUtc = dto.StartDateUtc, EndDateUtc = dto.EndDateUtc, AbsenceMinutes = dto.AbsenceMinutes, PrivacyClassification = dto.PrivacyClassification, InternalNotes = dto.InternalNotes, MetadataJson = dto.MetadataJson };
    private static PayrollPeriodEditDto MapPayrollPeriod(PayrollPeriodEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, PeriodCode = vm.PeriodCode, Status = vm.Status, PeriodStartUtc = vm.PeriodStartUtc, PeriodEndUtc = vm.PeriodEndUtc, ReviewNotes = vm.ReviewNotes, InternalNotes = vm.InternalNotes, MetadataJson = vm.MetadataJson };
    private static PayrollPeriodEditVm MapPayrollPeriod(PayrollPeriodListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, PeriodCode = dto.PeriodCode, Status = dto.Status, PeriodStartUtc = dto.PeriodStartUtc, PeriodEndUtc = dto.PeriodEndUtc, EmployeeCount = dto.EmployeeCount, TotalWorkMinutes = dto.TotalWorkMinutes, TotalBreakMinutes = dto.TotalBreakMinutes, TotalAbsenceMinutes = dto.TotalAbsenceMinutes, ReviewNotes = dto.ReviewNotes, InternalNotes = dto.InternalNotes, MetadataJson = dto.MetadataJson, Lines = dto.Lines };
    private static PayrollRuleSetEditDto MapPayrollRuleSet(PayrollRuleSetEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, JurisdictionCode = vm.JurisdictionCode, RuleSetCode = vm.RuleSetCode, DisplayName = vm.DisplayName, RuleVersion = vm.RuleVersion, Status = vm.Status, EffectiveFromUtc = vm.EffectiveFromUtc, EffectiveToUtc = vm.EffectiveToUtc, Currency = vm.Currency, Description = vm.Description, MetadataJson = vm.MetadataJson };
    private static PayrollRuleSetEditVm MapPayrollRuleSet(PayrollRuleSetListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, JurisdictionCode = dto.JurisdictionCode, RuleSetCode = dto.RuleSetCode, DisplayName = dto.DisplayName, RuleVersion = dto.RuleVersion, Status = dto.Status, EffectiveFromUtc = dto.EffectiveFromUtc, EffectiveToUtc = dto.EffectiveToUtc, Currency = dto.Currency, Description = dto.Description, MetadataJson = dto.MetadataJson, ComponentCount = dto.ComponentCount, Components = dto.Components, NewComponent = new PayrollRuleComponentVm { BusinessId = dto.BusinessId, PayrollRuleSetId = dto.Id, ThresholdJson = "{}", MetadataJson = "{}" } };
    private static PayrollRuleComponentDto MapPayrollRuleComponent(PayrollRuleComponentVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, PayrollRuleSetId = vm.PayrollRuleSetId, ComponentCode = vm.ComponentCode, DisplayName = vm.DisplayName, ComponentType = vm.ComponentType, CalculationMethod = vm.CalculationMethod, Basis = vm.Basis, RateBasisPoints = vm.RateBasisPoints, AmountMinor = vm.AmountMinor, ThresholdJson = vm.ThresholdJson, IsEmployerCost = vm.IsEmployerCost, SortOrder = vm.SortOrder, MetadataJson = vm.MetadataJson };
    private static PayrollRunCreateDto MapPayrollRunCreate(PayrollRunCreateVm vm) => new() { BusinessId = vm.BusinessId, PayrollPeriodId = vm.PayrollPeriodId, PayrollRuleSetId = vm.PayrollRuleSetId, ReviewNotes = vm.ReviewNotes, MetadataJson = vm.MetadataJson };
    private static PayrollPaymentCreateDto MapPayrollPaymentCreate(PayrollPaymentEditVm vm) => new() { BusinessId = vm.BusinessId, PayrollRunId = vm.PayrollRunId, PaymentMethod = vm.PaymentMethod, PaymentDateUtc = vm.PaymentDateUtc, Currency = vm.Currency, Reference = vm.Reference, InternalNotes = vm.InternalNotes, MetadataJson = vm.MetadataJson, Allocations = vm.Allocations ?? new List<PayrollPaymentAllocationDto>() };
    private static PayrollPaymentEditDto MapPayrollPaymentEdit(PayrollPaymentEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion ?? Array.Empty<byte>(), BusinessId = vm.BusinessId, PayrollRunId = vm.PayrollRunId, PaymentMethod = vm.PaymentMethod, PaymentDateUtc = vm.PaymentDateUtc, Currency = vm.Currency, Reference = vm.Reference, InternalNotes = vm.InternalNotes, MetadataJson = vm.MetadataJson, Allocations = vm.Allocations ?? new List<PayrollPaymentAllocationDto>() };
    private static PayrollPaymentEditVm MapPayrollPaymentVm(PayrollPaymentEditDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, PayrollRunId = dto.PayrollRunId, PaymentNumber = dto.PaymentNumber, Status = dto.Status, PaymentMethod = dto.PaymentMethod, PaymentDateUtc = dto.PaymentDateUtc, Currency = dto.Currency, TotalAmountMinor = dto.TotalAmountMinor, Reference = dto.Reference, PostingJournalEntryId = dto.PostingJournalEntryId, PostedAtUtc = dto.PostedAtUtc, CancelledAtUtc = dto.CancelledAtUtc, ReversalJournalEntryId = dto.ReversalJournalEntryId, ReversedAtUtc = dto.ReversedAtUtc, ReversalReason = dto.ReversalReason, BankSettledAtUtc = dto.BankSettledAtUtc, BankSettlementJournalEntryId = dto.BankSettlementJournalEntryId, BankSettlementReconciliationMatchId = dto.BankSettlementReconciliationMatchId, BankSettlementNotes = dto.BankSettlementNotes, BankSettlementCandidates = dto.BankSettlementCandidates ?? new List<PayrollPaymentBankSettlementCandidateDto>(), BankCorrectionCandidates = dto.BankCorrectionCandidates ?? new List<PayrollPaymentBankSettlementCandidateDto>(), BankCorrections = dto.BankCorrections ?? new List<PayrollPaymentBankCorrectionListItemDto>(), InternalNotes = dto.InternalNotes, MetadataJson = dto.MetadataJson, Allocations = dto.Allocations ?? new List<PayrollPaymentAllocationDto>() };
}
