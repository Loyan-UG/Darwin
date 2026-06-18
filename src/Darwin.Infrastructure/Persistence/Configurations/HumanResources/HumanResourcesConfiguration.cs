using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.HumanResources
{
    /// <summary>
    /// Configures HR core master data and employment metadata.
    /// </summary>
    public sealed class HumanResourcesConfiguration :
        IEntityTypeConfiguration<Employee>,
        IEntityTypeConfiguration<Department>,
        IEntityTypeConfiguration<Position>,
        IEntityTypeConfiguration<EmploymentContract>,
        IEntityTypeConfiguration<WorkSchedule>,
        IEntityTypeConfiguration<WorkScheduleException>,
        IEntityTypeConfiguration<AttendanceEvent>,
        IEntityTypeConfiguration<TimeEntry>,
        IEntityTypeConfiguration<Timesheet>,
        IEntityTypeConfiguration<TimesheetLine>,
        IEntityTypeConfiguration<LeaveRequest>,
        IEntityTypeConfiguration<AbsenceRecord>,
        IEntityTypeConfiguration<PayrollPeriod>,
        IEntityTypeConfiguration<PayrollPeriodLine>,
        IEntityTypeConfiguration<PayrollRuleSet>,
        IEntityTypeConfiguration<PayrollRuleComponent>,
        IEntityTypeConfiguration<PayrollRun>,
        IEntityTypeConfiguration<PayrollRunLine>,
        IEntityTypeConfiguration<PayrollRunLineComponent>,
        IEntityTypeConfiguration<PayrollPayslip>,
        IEntityTypeConfiguration<PayrollPayment>,
        IEntityTypeConfiguration<PayrollPaymentAllocation>,
        IEntityTypeConfiguration<PayrollPaymentBankCorrection>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("Employees", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EmployeeNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            builder.Property(x => x.PreferredName).HasMaxLength(100);
            builder.Property(x => x.WorkEmail).HasMaxLength(254);
            builder.Property(x => x.WorkPhone).HasMaxLength(64);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.PrivacyClassification).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.BusinessMemberId);
            builder.HasIndex(x => x.DepartmentId);
            builder.HasIndex(x => x.PositionId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.HireDateUtc);
            builder.HasIndex(x => x.TerminationDateUtc);
            builder.HasIndex(x => new { x.BusinessId, x.EmployeeNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasMany(x => x.EmploymentContracts)
                .WithOne()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<BusinessMember>()
                .WithMany()
                .HasForeignKey(x => x.BusinessMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Department>()
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Position>()
                .WithMany()
                .HasForeignKey(x => x.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.ToTable("Departments", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
            builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.SortOrder).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.ParentDepartmentId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.SortOrder);
            builder.HasIndex(x => new { x.BusinessId, x.Code })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasMany(x => x.Children)
                .WithOne()
                .HasForeignKey(x => x.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public void Configure(EntityTypeBuilder<Position> builder)
        {
            builder.ToTable("Positions", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
            builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.SortOrder).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.DepartmentId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.SortOrder);
            builder.HasIndex(x => new { x.BusinessId, x.Code })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<Department>()
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public void Configure(EntityTypeBuilder<EmploymentContract> builder)
        {
            builder.ToTable("EmploymentContracts", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ContractNumber).IsRequired().HasMaxLength(100);
            builder.Property(x => x.EmploymentType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.PrivacyClassification).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.StartDateUtc);
            builder.HasIndex(x => x.EndDateUtc);
            builder.HasIndex(x => new { x.BusinessId, x.ContractNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }

        public void Configure(EntityTypeBuilder<WorkSchedule> builder)
        {
            builder.ToTable("WorkSchedules", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ScheduleCode).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.EffectiveFromUtc);
            builder.HasIndex(x => x.EffectiveToUtc);
            builder.HasIndex(x => new { x.BusinessId, x.EmployeeId, x.ScheduleCode })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Exceptions)
                .WithOne()
                .HasForeignKey(x => x.WorkScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<WorkScheduleException> builder)
        {
            builder.ToTable("WorkScheduleExceptions", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Reason).IsRequired().HasMaxLength(200);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.WorkScheduleId);
            builder.HasIndex(x => x.WorkDateUtc);
            builder.HasIndex(x => new { x.WorkScheduleId, x.WorkDateUtc })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }

        public void Configure(EntityTypeBuilder<AttendanceEvent> builder)
        {
            builder.ToTable("AttendanceEvents", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.SourceReference).HasMaxLength(200);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.EventType);
            builder.HasIndex(x => x.OccurredAtUtc);

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public void Configure(EntityTypeBuilder<TimeEntry> builder)
        {
            builder.ToTable("TimeEntries", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.WorkType).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.RejectionReason).HasMaxLength(1000);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.WorkScheduleId);
            builder.HasIndex(x => x.WorkDateUtc);
            builder.HasIndex(x => x.Status);

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<WorkSchedule>()
                .WithMany()
                .HasForeignKey(x => x.WorkScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public void Configure(EntityTypeBuilder<Timesheet> builder)
        {
            builder.ToTable("Timesheets", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TimesheetNumber).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.ReviewNotes).HasMaxLength(1000);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PeriodStartUtc);
            builder.HasIndex(x => x.PeriodEndUtc);
            builder.HasIndex(x => new { x.BusinessId, x.EmployeeId, x.TimesheetNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            builder.HasIndex(x => new { x.BusinessId, x.EmployeeId, x.PeriodStartUtc, x.PeriodEndUtc })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.TimesheetId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<TimesheetLine> builder)
        {
            builder.ToTable("TimesheetLines", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.TimesheetId);
            builder.HasIndex(x => x.TimeEntryId);
            builder.HasIndex(x => new { x.TimesheetId, x.TimeEntryId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<TimeEntry>()
                .WithMany()
                .HasForeignKey(x => x.TimeEntryId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public void Configure(EntityTypeBuilder<LeaveRequest> builder)
        {
            builder.ToTable("LeaveRequests", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RequestNumber).IsRequired().HasMaxLength(100);
            builder.Property(x => x.LeaveType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.ReviewNotes).HasMaxLength(1000);
            builder.Property(x => x.PrivacyClassification).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.LeaveType);
            builder.HasIndex(x => x.StartDateUtc);
            builder.HasIndex(x => x.EndDateUtc);
            builder.HasIndex(x => new { x.BusinessId, x.EmployeeId, x.RequestNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public void Configure(EntityTypeBuilder<AbsenceRecord> builder)
        {
            builder.ToTable("AbsenceRecords", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AbsenceType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.PrivacyClassification).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.LeaveRequestId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.AbsenceType);
            builder.HasIndex(x => x.StartDateUtc);
            builder.HasIndex(x => x.EndDateUtc);

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<LeaveRequest>()
                .WithMany()
                .HasForeignKey(x => x.LeaveRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public void Configure(EntityTypeBuilder<PayrollPeriod> builder)
        {
            builder.ToTable("PayrollPeriods", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PeriodCode).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.ReviewNotes).HasMaxLength(1000);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PeriodStartUtc);
            builder.HasIndex(x => x.PeriodEndUtc);
            builder.HasIndex(x => new { x.BusinessId, x.PeriodCode })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            builder.HasIndex(x => new { x.BusinessId, x.PeriodStartUtc, x.PeriodEndUtc })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<PayrollPeriodLine> builder)
        {
            builder.ToTable("PayrollPeriodLines", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SummaryJson).HasMaxLength(8000);
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.PayrollPeriodId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => new { x.PayrollPeriodId, x.EmployeeId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public void Configure(EntityTypeBuilder<PayrollRuleSet> builder)
        {
            builder.ToTable("PayrollRuleSets", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.JurisdictionCode).IsRequired().HasMaxLength(16);
            builder.Property(x => x.RuleSetCode).IsRequired().HasMaxLength(64);
            builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.RuleVersion).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.JurisdictionCode);
            builder.HasIndex(x => x.RuleSetCode);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.EffectiveFromUtc);
            builder.HasIndex(x => x.EffectiveToUtc);
            builder.HasIndex(x => new { x.BusinessId, x.JurisdictionCode, x.RuleSetCode, x.RuleVersion })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasMany(x => x.Components)
                .WithOne()
                .HasForeignKey(x => x.PayrollRuleSetId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<PayrollRuleComponent> builder)
        {
            builder.ToTable("PayrollRuleComponents", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ComponentCode).IsRequired().HasMaxLength(64);
            builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.ComponentType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.CalculationMethod).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Basis).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.ThresholdJson).HasMaxLength(8000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.PayrollRuleSetId);
            builder.HasIndex(x => x.ComponentType);
            builder.HasIndex(x => x.SortOrder);
            builder.HasIndex(x => new { x.PayrollRuleSetId, x.ComponentCode })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }

        public void Configure(EntityTypeBuilder<PayrollRun> builder)
        {
            builder.ToTable("PayrollRuns", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RunNumber).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.JurisdictionCode).IsRequired().HasMaxLength(16);
            builder.Property(x => x.RuleSetCode).IsRequired().HasMaxLength(64);
            builder.Property(x => x.RuleVersion).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.ReviewNotes).HasMaxLength(1000);
            builder.Property(x => x.SourceSnapshotJson).HasMaxLength(16000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.PayrollPeriodId);
            builder.HasIndex(x => x.PayrollRuleSetId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PeriodStartUtc);
            builder.HasIndex(x => x.PeriodEndUtc);
            builder.HasIndex(x => x.PostedAtUtc);
            builder.HasIndex(x => x.PostingJournalEntryId);
            builder.HasIndex(x => new { x.BusinessId, x.RunNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            builder.HasIndex(x => new { x.BusinessId, x.PayrollPeriodId, x.PayrollRuleSetId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<PayrollPeriod>()
                .WithMany()
                .HasForeignKey(x => x.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<PayrollRuleSet>()
                .WithMany()
                .HasForeignKey(x => x.PayrollRuleSetId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<PayrollRunLine> builder)
        {
            builder.ToTable("PayrollRunLines", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EmployeeNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.EmployeeName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.EmployeeSnapshotJson).HasMaxLength(16000);
            builder.Property(x => x.ContractSnapshotJson).HasMaxLength(16000);
            builder.Property(x => x.PeriodLineSnapshotJson).HasMaxLength(16000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.PayrollRunId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.EmploymentContractId);
            builder.HasIndex(x => new { x.PayrollRunId, x.EmployeeId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<EmploymentContract>()
                .WithMany()
                .HasForeignKey(x => x.EmploymentContractId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Components)
                .WithOne()
                .HasForeignKey(x => x.PayrollRunLineId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<PayrollRunLineComponent> builder)
        {
            builder.ToTable("PayrollRunLineComponents", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ComponentCode).IsRequired().HasMaxLength(64);
            builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.ComponentType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.CalculationMethod).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Basis).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.RuleSnapshotJson).HasMaxLength(16000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.PayrollRunId);
            builder.HasIndex(x => x.PayrollRunLineId);
            builder.HasIndex(x => x.PayrollRuleComponentId);
            builder.HasIndex(x => x.ComponentType);
            builder.HasIndex(x => x.SortOrder);
        }

        public void Configure(EntityTypeBuilder<PayrollPayslip> builder)
        {
            builder.ToTable("PayrollPayslips", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PayslipNumber).IsRequired().HasMaxLength(120);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.SnapshotJson).HasMaxLength(32000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.PayrollRunId);
            builder.HasIndex(x => x.PayrollRunLineId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.DocumentRecordId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.GeneratedAtUtc);
            builder.HasIndex(x => new { x.BusinessId, x.PayslipNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            builder.HasIndex(x => new { x.PayrollRunLineId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<PayrollRun>()
                .WithMany()
                .HasForeignKey(x => x.PayrollRunId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<PayrollRunLine>()
                .WithMany()
                .HasForeignKey(x => x.PayrollRunLineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public void Configure(EntityTypeBuilder<PayrollPayment> builder)
        {
            builder.ToTable("PayrollPayments", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PaymentNumber).HasMaxLength(120);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.Reference).HasMaxLength(256);
            builder.Property(x => x.ReversalReason).HasMaxLength(1000);
            builder.Property(x => x.BankSettlementNotes).HasMaxLength(1000);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.PayrollRunId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PaymentDateUtc);
            builder.HasIndex(x => x.PostingJournalEntryId);
            builder.HasIndex(x => x.PostedAtUtc);
            builder.HasIndex(x => x.ReversalJournalEntryId);
            builder.HasIndex(x => x.ReversedAtUtc);
            builder.HasIndex(x => x.BankSettledAtUtc);
            builder.HasIndex(x => x.BankSettlementJournalEntryId);
            builder.HasIndex(x => x.BankSettlementReconciliationMatchId);
            builder.HasIndex(x => new { x.BusinessId, x.PaymentNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [PaymentNumber] IS NOT NULL");

            builder.HasOne<PayrollRun>()
                .WithMany()
                .HasForeignKey(x => x.PayrollRunId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Allocations)
                .WithOne()
                .HasForeignKey(x => x.PayrollPaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<PayrollPaymentAllocation> builder)
        {
            builder.ToTable("PayrollPaymentAllocations", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Memo).HasMaxLength(1000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.PayrollPaymentId);
            builder.HasIndex(x => x.PayrollRunId);
            builder.HasIndex(x => x.PayrollRunLineId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => new { x.PayrollPaymentId, x.PayrollRunLineId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<PayrollRun>()
                .WithMany()
                .HasForeignKey(x => x.PayrollRunId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<PayrollRunLine>()
                .WithMany()
                .HasForeignKey(x => x.PayrollRunLineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public void Configure(EntityTypeBuilder<PayrollPaymentBankCorrection> builder)
        {
            builder.ToTable("PayrollPaymentBankCorrections", schema: "HumanResources");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CorrectionType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.PayrollPaymentId);
            builder.HasIndex(x => x.BankReconciliationMatchId);
            builder.HasIndex(x => x.BankStatementLineId);
            builder.HasIndex(x => x.OriginalBankSettlementJournalEntryId);
            builder.HasIndex(x => x.CorrectionJournalEntryId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CorrectionDateUtc);
            builder.HasIndex(x => x.PostedAtUtc);
            builder.HasIndex(x => new { x.PayrollPaymentId, x.CorrectionType, x.BankReconciliationMatchId })
                .IsUnique()
                .HasDatabaseName("UX_PayrollPaymentBankCorrections_Payment_Type_Reconciliation_Active")
                .HasFilter("[IsDeleted] = 0 AND [Status] <> 'Cancelled'");

            builder.HasOne<PayrollPayment>()
                .WithMany()
                .HasForeignKey(x => x.PayrollPaymentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
