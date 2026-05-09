using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Compliance;
using Darwin.Application;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Net.Http;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Unit tests for <see cref="CreateCustomerHandler"/>, <see cref="UpdateCustomerHandler"/>,
/// <see cref="CreateLeadHandler"/>, <see cref="UpdateLeadHandler"/>, and
/// <see cref="ConvertLeadToCustomerHandler"/>.
/// </summary>
public sealed class CustomerLeadHandlerTests
{
    // ─── CreateCustomerHandler ────────────────────────────────────────────────

    [Fact]
    public async Task CreateCustomer_Should_PersistCustomer_WhenValidDto()
    {
        await using var db = CustomerLeadDbContext.Create();

        var handler = new CreateCustomerHandler(
            db,
            new CustomerCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var id = await handler.HandleAsync(new CustomerCreateDto
        {
            FirstName = "  Ada  ",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Phone = "+441234567890",
            TaxProfileType = CustomerTaxProfileType.Consumer
        }, TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();

        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        customer.FirstName.Should().Be("Ada");
        customer.Email.Should().Be("ada@example.com");
        customer.TaxProfileType.Should().Be(CustomerTaxProfileType.Consumer);
    }

    [Fact]
    public async Task CreateCustomer_Should_PersistBusinessCustomer_WithCompanyName()
    {
        await using var db = CustomerLeadDbContext.Create();

        var handler = new CreateCustomerHandler(
            db,
            new CustomerCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var id = await handler.HandleAsync(new CustomerCreateDto
        {
            FirstName = "Johann",
            LastName = "Schmidt",
            Email = "j.schmidt@acme.de",
            TaxProfileType = CustomerTaxProfileType.Business,
            CompanyName = "  ACME GmbH  ",
            VatId = "DE123456789"
        }, TestContext.Current.CancellationToken);

        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        customer.CompanyName.Should().Be("ACME GmbH");
        customer.VatId.Should().Be("DE123456789");
        customer.TaxProfileType.Should().Be(CustomerTaxProfileType.Business);
    }

    [Fact]
    public async Task CreateCustomer_Should_PersistAddresses_WhenProvided()
    {
        await using var db = CustomerLeadDbContext.Create();

        var handler = new CreateCustomerHandler(
            db,
            new CustomerCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var id = await handler.HandleAsync(new CustomerCreateDto
        {
            FirstName = "Maria",
            LastName = "Meier",
            Email = "maria@example.de",
            TaxProfileType = CustomerTaxProfileType.Consumer,
            Addresses = new List<CustomerAddressDto>
            {
                new()
                {
                    Line1 = "Musterstr. 1",
                    City = "Berlin",
                    PostalCode = "10115",
                    Country = "DE",
                    IsDefaultBilling = true,
                    IsDefaultShipping = true
                }
            }
        }, TestContext.Current.CancellationToken);

        var addresses = await db.Set<CustomerAddress>()
            .Where(x => x.CustomerId == id)
            .ToListAsync(TestContext.Current.CancellationToken);

        addresses.Should().ContainSingle();
        addresses[0].Line1.Should().Be("Musterstr. 1");
        addresses[0].IsDefaultBilling.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCustomer_Should_Throw_WhenLinkedUserNotFound()
    {
        await using var db = CustomerLeadDbContext.Create();

        var handler = new CreateCustomerHandler(
            db,
            new CustomerCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new CustomerCreateDto
        {
            UserId = Guid.NewGuid(),
            TaxProfileType = CustomerTaxProfileType.Consumer
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("LinkedUserNotFound");
    }

    [Fact]
    public async Task CreateCustomer_Should_LinkToUser_WhenUserExists()
    {
        await using var db = CustomerLeadDbContext.Create();
        var userId = Guid.NewGuid();

        db.Set<User>().Add(MakeUser(userId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateCustomerHandler(
            db,
            new CustomerCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var id = await handler.HandleAsync(new CustomerCreateDto
        {
            UserId = userId,
            TaxProfileType = CustomerTaxProfileType.Consumer
        }, TestContext.Current.CancellationToken);

        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        customer.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task CreateCustomer_Should_NormalizeNullableFields_WhenWhitespaceProvided()
    {
        await using var db = CustomerLeadDbContext.Create();

        var handler = new CreateCustomerHandler(
            db,
            new CustomerCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var id = await handler.HandleAsync(new CustomerCreateDto
        {
            FirstName = "Max",
            LastName = "Muster",
            Email = "max@test.de",
            TaxProfileType = CustomerTaxProfileType.Consumer,
            Notes = "   ",
            VatId = "  "
        }, TestContext.Current.CancellationToken);

        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        customer.Notes.Should().BeNull();
        customer.VatId.Should().BeNull();
    }

    // ─── UpdateCustomerHandler ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCustomer_Should_PersistChanges_WhenCustomerExists()
    {
        await using var db = CustomerLeadDbContext.Create();
        var customerId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Old",
            LastName = "Name",
            Email = "old@example.com",
            Phone = "+49100000000",
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerHandler(
            db,
            new CustomerEditValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new CustomerEditDto
        {
            Id = customerId,
            RowVersion = rowVersion,
            FirstName = "New",
            LastName = "Name",
            Email = "new@example.com",
            TaxProfileType = CustomerTaxProfileType.Consumer
        }, TestContext.Current.CancellationToken);

        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == customerId, TestContext.Current.CancellationToken);
        customer.FirstName.Should().Be("New");
        customer.Email.Should().Be("new@example.com");
    }

    [Fact]
    public async Task UpdateCustomer_Should_AddNewAddress_WhenNotExistingInDto()
    {
        await using var db = CustomerLeadDbContext.Create();
        var customerId = Guid.NewGuid();
        var rowVersion = new byte[] { 5, 6, 7, 8 };

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Max",
            LastName = "Muster",
            Email = "max@example.de",
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerHandler(
            db,
            new CustomerEditValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new CustomerEditDto
        {
            Id = customerId,
            RowVersion = rowVersion,
            FirstName = "Max",
            LastName = "Muster",
            Email = "max@example.de",
            TaxProfileType = CustomerTaxProfileType.Consumer,
            Addresses = new List<CustomerAddressDto>
            {
                new()
                {
                    Line1 = "Neue Str. 5",
                    City = "Hamburg",
                    PostalCode = "20095",
                    Country = "DE"
                }
            }
        }, TestContext.Current.CancellationToken);

        var addresses = await db.Set<CustomerAddress>()
            .Where(x => x.CustomerId == customerId)
            .ToListAsync(TestContext.Current.CancellationToken);

        addresses.Should().ContainSingle();
        addresses[0].Line1.Should().Be("Neue Str. 5");
    }

    [Fact]
    public async Task UpdateCustomer_Should_RemoveAddress_WhenNotPresentInDto()
    {
        await using var db = CustomerLeadDbContext.Create();
        var customerId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        var customer = new Customer
        {
            Id = customerId,
            FirstName = "Petra",
            LastName = "Muster",
            Email = "petra@example.de",
            RowVersion = rowVersion.ToArray()
        };
        customer.Addresses.Add(new CustomerAddress
        {
            Id = addressId,
            CustomerId = customerId,
            Line1 = "Old Str. 1",
            City = "Munich",
            PostalCode = "80331",
            Country = "DE"
        });
        db.Set<Customer>().Add(customer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerHandler(
            db,
            new CustomerEditValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new CustomerEditDto
        {
            Id = customerId,
            RowVersion = rowVersion,
            FirstName = "Petra",
            LastName = "Muster",
            Email = "petra@example.de",
            TaxProfileType = CustomerTaxProfileType.Consumer,
            Addresses = new List<CustomerAddressDto>()
        }, TestContext.Current.CancellationToken);

        var addressCount = await db.Set<CustomerAddress>()
            .CountAsync(x => x.CustomerId == customerId, TestContext.Current.CancellationToken);
        addressCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateCustomer_Should_Throw_WhenCustomerNotFound()
    {
        await using var db = CustomerLeadDbContext.Create();

        var handler = new UpdateCustomerHandler(
            db,
            new CustomerEditValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new CustomerEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            FirstName = "X",
            LastName = "Y",
            Email = "x@y.de"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CustomerNotFound");
    }

    [Fact]
    public async Task UpdateCustomer_Should_Throw_WhenRowVersionMismatches()
    {
        await using var db = CustomerLeadDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Test",
            LastName = "Test",
            Email = "test@test.de",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerHandler(
            db,
            new CustomerEditValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new CustomerEditDto
        {
            Id = customerId,
            RowVersion = new byte[] { 9, 9, 9, 9 },
            FirstName = "Test",
            LastName = "Test",
            Email = "test@test.de"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // ─── CreateLeadHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateLead_Should_PersistLead_WhenValidDto()
    {
        await using var db = CustomerLeadDbContext.Create();

        var handler = new CreateLeadHandler(
            db,
            new LeadCreateValidator());

        var id = await handler.HandleAsync(new LeadCreateDto
        {
            FirstName = "  Hans  ",
            LastName = "Müller",
            Email = "hans@example.de",
            Phone = "+4917012345678",
            Source = "  Referral  ",
            Status = LeadStatus.New
        }, TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();

        var lead = await db.Set<Lead>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        lead.FirstName.Should().Be("Hans");
        lead.LastName.Should().Be("Müller");
        lead.Email.Should().Be("hans@example.de");
        lead.Source.Should().Be("Referral");
        lead.Status.Should().Be(LeadStatus.New);
    }

    [Fact]
    public async Task CreateLead_Should_NormalizeOptionalFields_WhenWhitespaceProvided()
    {
        await using var db = CustomerLeadDbContext.Create();

        var handler = new CreateLeadHandler(
            db,
            new LeadCreateValidator());

        var id = await handler.HandleAsync(new LeadCreateDto
        {
            FirstName = "Test",
            LastName = "Lead",
            Email = "test@lead.de",
            Phone = "+4917099999999",
            Notes = "  ",
            Source = "   ",
            Status = LeadStatus.New
        }, TestContext.Current.CancellationToken);

        var lead = await db.Set<Lead>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        lead.Notes.Should().BeNull();
        lead.Source.Should().BeNull();
    }

    // ─── UpdateLeadHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLead_Should_PersistChanges_WhenLeadExists()
    {
        await using var db = CustomerLeadDbContext.Create();
        var leadId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        db.Set<Lead>().Add(new Lead
        {
            Id = leadId,
            FirstName = "Old",
            LastName = "Lead",
            Email = "old@lead.de",
            Phone = "+4917000000000",
            Status = LeadStatus.New,
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLeadHandler(
            db,
            new LeadEditValidator(),
            new TestStringLocalizer());

        await handler.HandleAsync(new LeadEditDto
        {
            Id = leadId,
            RowVersion = rowVersion,
            FirstName = "Updated",
            LastName = "Lead",
            Email = "updated@lead.de",
            Phone = "+4917011111111",
            Status = LeadStatus.Qualified
        }, TestContext.Current.CancellationToken);

        var lead = await db.Set<Lead>().SingleAsync(x => x.Id == leadId, TestContext.Current.CancellationToken);
        lead.FirstName.Should().Be("Updated");
        lead.Email.Should().Be("updated@lead.de");
        lead.Status.Should().Be(LeadStatus.Qualified);
    }

    [Fact]
    public async Task UpdateLead_Should_Throw_WhenLeadNotFound()
    {
        await using var db = CustomerLeadDbContext.Create();

        var handler = new UpdateLeadHandler(
            db,
            new LeadEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new LeadEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            FirstName = "X",
            LastName = "Y",
            Email = "x@y.de",
            Phone = "+4917000000000",
            Status = LeadStatus.New
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("LeadNotFound");
    }

    [Fact]
    public async Task UpdateLead_Should_Throw_WhenRowVersionMismatches()
    {
        await using var db = CustomerLeadDbContext.Create();
        var leadId = Guid.NewGuid();

        db.Set<Lead>().Add(new Lead
        {
            Id = leadId,
            FirstName = "Test",
            LastName = "Lead",
            Email = "test@lead.de",
            Phone = "+4917000000000",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLeadHandler(
            db,
            new LeadEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new LeadEditDto
        {
            Id = leadId,
            RowVersion = new byte[] { 9, 9, 9, 9 },
            FirstName = "Test",
            LastName = "Lead",
            Email = "test@lead.de",
            Phone = "+4917000000000",
            Status = LeadStatus.New
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // ─── ConvertLeadToCustomerHandler ─────────────────────────────────────────

    [Fact]
    public async Task ConvertLeadToCustomer_Should_CreateNewCustomer_WhenNoMatchByEmail()
    {
        await using var db = CustomerLeadDbContext.Create();
        var leadId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        db.Set<Lead>().Add(new Lead
        {
            Id = leadId,
            FirstName = "Karl",
            LastName = "Bauer",
            Email = "karl@bauer.de",
            Phone = "+4917012345678",
            Notes = "Interested in enterprise plan",
            Status = LeadStatus.Qualified,
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConvertLeadToCustomerHandler(
            db,
            new ConvertLeadToCustomerValidator(),
            new TestStringLocalizer());

        var customerId = await handler.HandleAsync(new ConvertLeadToCustomerDto
        {
            LeadId = leadId,
            RowVersion = rowVersion,
            CopyNotesToCustomer = true
        }, TestContext.Current.CancellationToken);

        customerId.Should().NotBeEmpty();

        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == customerId, TestContext.Current.CancellationToken);
        customer.FirstName.Should().Be("Karl");
        customer.LastName.Should().Be("Bauer");
        customer.Email.Should().Be("karl@bauer.de");
        customer.Notes.Should().Be("Interested in enterprise plan");

        var lead = await db.Set<Lead>().SingleAsync(x => x.Id == leadId, TestContext.Current.CancellationToken);
        lead.Status.Should().Be(LeadStatus.Converted);
        lead.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task ConvertLeadToCustomer_Should_ReuseExistingCustomer_WhenEmailMatches()
    {
        await using var db = CustomerLeadDbContext.Create();
        var leadId = Guid.NewGuid();
        var existingCustomerId = Guid.NewGuid();
        var rowVersion = new byte[] { 3, 3, 3, 3 };

        db.Set<Customer>().Add(new Customer
        {
            Id = existingCustomerId,
            FirstName = "Existing",
            LastName = "Customer",
            Email = "shared@example.com",
            Phone = "+4917099999",
        });

        db.Set<Lead>().Add(new Lead
        {
            Id = leadId,
            FirstName = "Karl",
            LastName = "Bauer",
            Email = "shared@example.com",
            Phone = "+4917012345678",
            Status = LeadStatus.Qualified,
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConvertLeadToCustomerHandler(
            db,
            new ConvertLeadToCustomerValidator(),
            new TestStringLocalizer());

        var customerId = await handler.HandleAsync(new ConvertLeadToCustomerDto
        {
            LeadId = leadId,
            RowVersion = rowVersion
        }, TestContext.Current.CancellationToken);

        customerId.Should().Be(existingCustomerId);

        var customerCount = await db.Set<Customer>().CountAsync(TestContext.Current.CancellationToken);
        customerCount.Should().Be(1, "existing customer should be reused, not duplicated");
    }

    [Fact]
    public async Task ConvertLeadToCustomer_Should_ReturnExistingCustomerId_WhenAlreadyConverted()
    {
        await using var db = CustomerLeadDbContext.Create();
        var leadId = Guid.NewGuid();
        var existingCustomerId = Guid.NewGuid();
        var rowVersion = new byte[] { 7, 7, 7, 7 };

        db.Set<Customer>().Add(new Customer
        {
            Id = existingCustomerId,
            FirstName = "Already",
            LastName = "Converted",
            Email = "already@converted.de",
            Phone = "+4917099999",
        });

        db.Set<Lead>().Add(new Lead
        {
            Id = leadId,
            FirstName = "Already",
            LastName = "Converted",
            Email = "already@converted.de",
            Phone = "+4917012345678",
            Status = LeadStatus.Converted,
            CustomerId = existingCustomerId,
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConvertLeadToCustomerHandler(
            db,
            new ConvertLeadToCustomerValidator(),
            new TestStringLocalizer());

        var customerId = await handler.HandleAsync(new ConvertLeadToCustomerDto
        {
            LeadId = leadId,
            RowVersion = rowVersion
        }, TestContext.Current.CancellationToken);

        customerId.Should().Be(existingCustomerId);
    }

    [Fact]
    public async Task ConvertLeadToCustomer_Should_SetBusinessTaxProfile_WhenLeadHasCompanyName()
    {
        await using var db = CustomerLeadDbContext.Create();
        var leadId = Guid.NewGuid();
        var rowVersion = new byte[] { 2, 2, 2, 2 };

        db.Set<Lead>().Add(new Lead
        {
            Id = leadId,
            FirstName = "CEO",
            LastName = "Corporate",
            Email = "ceo@corp.de",
            Phone = "+4917000000001",
            CompanyName = "Corp GmbH",
            Status = LeadStatus.Qualified,
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConvertLeadToCustomerHandler(
            db,
            new ConvertLeadToCustomerValidator(),
            new TestStringLocalizer());

        var customerId = await handler.HandleAsync(new ConvertLeadToCustomerDto
        {
            LeadId = leadId,
            RowVersion = rowVersion
        }, TestContext.Current.CancellationToken);

        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == customerId, TestContext.Current.CancellationToken);
        customer.TaxProfileType.Should().Be(CustomerTaxProfileType.Business);
        customer.CompanyName.Should().Be("Corp GmbH");
    }

    [Fact]
    public async Task ConvertLeadToCustomer_Should_NotCopyNotes_WhenCopyNotesToCustomerIsFalse()
    {
        await using var db = CustomerLeadDbContext.Create();
        var leadId = Guid.NewGuid();
        var rowVersion = new byte[] { 4, 4, 4, 4 };

        db.Set<Lead>().Add(new Lead
        {
            Id = leadId,
            FirstName = "Quiet",
            LastName = "Lead",
            Email = "quiet@lead.de",
            Phone = "+4917088888888",
            Notes = "Confidential notes",
            Status = LeadStatus.New,
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConvertLeadToCustomerHandler(
            db,
            new ConvertLeadToCustomerValidator(),
            new TestStringLocalizer());

        var customerId = await handler.HandleAsync(new ConvertLeadToCustomerDto
        {
            LeadId = leadId,
            RowVersion = rowVersion,
            CopyNotesToCustomer = false
        }, TestContext.Current.CancellationToken);

        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == customerId, TestContext.Current.CancellationToken);
        customer.Notes.Should().BeNull();
    }

    [Fact]
    public async Task ConvertLeadToCustomer_Should_Throw_WhenLeadNotFound()
    {
        await using var db = CustomerLeadDbContext.Create();

        var handler = new ConvertLeadToCustomerHandler(
            db,
            new ConvertLeadToCustomerValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new ConvertLeadToCustomerDto
        {
            LeadId = Guid.NewGuid(),
            RowVersion = new byte[] { 1 }
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("LeadNotFound");
    }

    [Fact]
    public async Task ConvertLeadToCustomer_Should_Throw_WhenRowVersionMismatches()
    {
        await using var db = CustomerLeadDbContext.Create();
        var leadId = Guid.NewGuid();

        db.Set<Lead>().Add(new Lead
        {
            Id = leadId,
            FirstName = "Test",
            LastName = "Lead",
            Email = "test@lead.de",
            Phone = "+4917000000000",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConvertLeadToCustomerHandler(
            db,
            new ConvertLeadToCustomerValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new ConvertLeadToCustomerDto
        {
            LeadId = leadId,
            RowVersion = new byte[] { 9, 9, 9, 9 }
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // ─── Shared helpers ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCustomerTaxProfile_Should_UpdateBusinessVatId_WhenRowVersionMatches()
    {
        await using var db = CustomerLeadDbContext.Create();
        var customerId = Guid.NewGuid();
        var rowVersion = new byte[] { 2, 4, 6, 8 };

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Tax",
            LastName = "Customer",
            Email = "tax.customer@example.de",
            TaxProfileType = CustomerTaxProfileType.Business,
            CompanyName = "Old GmbH",
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerTaxProfileHandler(
            db,
            new CustomerTaxProfileUpdateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new CustomerTaxProfileUpdateDto
        {
            Id = customerId,
            RowVersion = rowVersion,
            TaxProfileType = CustomerTaxProfileType.Business,
            CompanyName = "  New GmbH  ",
            VatId = "  DE123456789  "
        }, TestContext.Current.CancellationToken);

        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == customerId, TestContext.Current.CancellationToken);
        customer.TaxProfileType.Should().Be(CustomerTaxProfileType.Business);
        customer.CompanyName.Should().Be("New GmbH");
        customer.VatId.Should().Be("DE123456789");
    }

    [Fact]
    public async Task UpdateCustomerTaxProfile_Should_Throw_WhenBusinessVatIdMissing()
    {
        await using var db = CustomerLeadDbContext.Create();
        var customerId = Guid.NewGuid();
        var rowVersion = new byte[] { 2, 4, 6, 8 };

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Tax",
            LastName = "Customer",
            Email = "tax.customer@example.de",
            TaxProfileType = CustomerTaxProfileType.Business,
            CompanyName = "Existing GmbH",
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerTaxProfileHandler(
            db,
            new CustomerTaxProfileUpdateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new CustomerTaxProfileUpdateDto
        {
            Id = customerId,
            RowVersion = rowVersion,
            TaxProfileType = CustomerTaxProfileType.Business,
            CompanyName = "Existing GmbH"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*CustomerBusinessRequiresVatIdForTaxCompliance*");
    }

    [Fact]
    public async Task UpdateCustomerTaxProfile_Should_Throw_WhenRowVersionMismatches()
    {
        await using var db = CustomerLeadDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Tax",
            LastName = "Customer",
            Email = "tax.customer@example.de",
            TaxProfileType = CustomerTaxProfileType.Business,
            CompanyName = "Existing GmbH",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerTaxProfileHandler(
            db,
            new CustomerTaxProfileUpdateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new CustomerTaxProfileUpdateDto
        {
            Id = customerId,
            RowVersion = new byte[] { 9, 9, 9, 9 },
            TaxProfileType = CustomerTaxProfileType.Business,
            CompanyName = "Existing GmbH",
            VatId = "DE123456789"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task UpdateCustomerVatValidationDecision_Should_PersistStatus_WhenRowVersionMatches()
    {
        await using var db = CustomerLeadDbContext.Create();
        var customerId = Guid.NewGuid();
        var rowVersion = new byte[] { 6, 6, 6, 6 };
        var checkedAtUtc = new DateTime(2026, 5, 9, 11, 0, 0, DateTimeKind.Utc);

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Tax",
            LastName = "Customer",
            Email = "tax.customer@example.de",
            TaxProfileType = CustomerTaxProfileType.Business,
            CompanyName = "Existing GmbH",
            VatId = "DE123456789",
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerVatValidationDecisionHandler(
            db,
            new FixedClock(checkedAtUtc),
            new TestStringLocalizer());

        await handler.HandleAsync(new CustomerVatValidationDecisionDto
        {
            Id = customerId,
            RowVersion = rowVersion,
            Status = CustomerVatValidationStatus.Valid,
            Source = "  operator  ",
            Message = "  VIES check confirmed.  "
        }, TestContext.Current.CancellationToken);

        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == customerId, TestContext.Current.CancellationToken);
        customer.VatValidationStatus.Should().Be(CustomerVatValidationStatus.Valid);
        customer.VatValidationCheckedAtUtc.Should().Be(checkedAtUtc);
        customer.VatValidationSource.Should().Be("operator");
        customer.VatValidationMessage.Should().Be("VIES check confirmed.");
    }

    [Fact]
    public async Task UpdateCustomerTaxProfile_Should_ResetVatValidation_WhenVatIdChanges()
    {
        await using var db = CustomerLeadDbContext.Create();
        var customerId = Guid.NewGuid();
        var rowVersion = new byte[] { 7, 7, 7, 7 };

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Tax",
            LastName = "Customer",
            Email = "tax.customer@example.de",
            TaxProfileType = CustomerTaxProfileType.Business,
            CompanyName = "Existing GmbH",
            VatId = "DE123456789",
            VatValidationStatus = CustomerVatValidationStatus.Valid,
            VatValidationCheckedAtUtc = new DateTime(2026, 5, 8, 11, 0, 0, DateTimeKind.Utc),
            VatValidationSource = "operator",
            VatValidationMessage = "Confirmed",
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerTaxProfileHandler(
            db,
            new CustomerTaxProfileUpdateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new CustomerTaxProfileUpdateDto
        {
            Id = customerId,
            RowVersion = rowVersion,
            TaxProfileType = CustomerTaxProfileType.Business,
            CompanyName = "Existing GmbH",
            VatId = "DE987654321"
        }, TestContext.Current.CancellationToken);

        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == customerId, TestContext.Current.CancellationToken);
        customer.VatValidationStatus.Should().Be(CustomerVatValidationStatus.Unknown);
        customer.VatValidationCheckedAtUtc.Should().BeNull();
        customer.VatValidationSource.Should().BeNull();
        customer.VatValidationMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateCustomerVatIdHandler_Should_PersistProviderResult()
    {
        await using var db = CustomerLeadDbContext.Create();
        var customerId = Guid.NewGuid();
        var rowVersion = new byte[] { 7, 7, 7, 7 };
        var checkedAtUtc = new DateTime(2026, 5, 9, 13, 15, 0, DateTimeKind.Utc);

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.test",
            Phone = "+491234567",
            TaxProfileType = CustomerTaxProfileType.Business,
            VatId = "DE123456789",
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCustomerVatIdHandler(
            db,
            new FixedClock(checkedAtUtc),
            new FixedVatValidationProvider(CustomerVatValidationStatus.Valid, "vies", "VIES confirmed the VAT ID."),
            new TestStringLocalizer());

        var status = await handler.HandleAsync(new CustomerVatValidationLookupDto
        {
            Id = customerId,
            RowVersion = rowVersion
        }, TestContext.Current.CancellationToken);

        status.Should().Be(CustomerVatValidationStatus.Valid);
        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == customerId, TestContext.Current.CancellationToken);
        customer.VatValidationStatus.Should().Be(CustomerVatValidationStatus.Valid);
        customer.VatValidationCheckedAtUtc.Should().Be(checkedAtUtc);
        customer.VatValidationSource.Should().Be("vies");
        customer.VatValidationMessage.Should().Be("VIES confirmed the VAT ID.");
    }

    [Fact]
    public async Task ValidateCustomerVatIdHandler_Should_PersistUnknown_WhenProviderFails()
    {
        await using var db = CustomerLeadDbContext.Create();
        var customerId = Guid.NewGuid();
        var rowVersion = new byte[] { 8, 8, 8, 8 };
        var checkedAtUtc = new DateTime(2026, 5, 9, 14, 0, 0, DateTimeKind.Utc);

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.test",
            Phone = "+491234567",
            TaxProfileType = CustomerTaxProfileType.Business,
            VatId = "DE123456789",
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCustomerVatIdHandler(
            db,
            new FixedClock(checkedAtUtc),
            new ThrowingVatValidationProvider(),
            new TestStringLocalizer());

        var status = await handler.HandleAsync(new CustomerVatValidationLookupDto
        {
            Id = customerId,
            RowVersion = rowVersion
        }, TestContext.Current.CancellationToken);

        status.Should().Be(CustomerVatValidationStatus.Unknown);
        var customer = await db.Set<Customer>().SingleAsync(x => x.Id == customerId, TestContext.Current.CancellationToken);
        customer.VatValidationStatus.Should().Be(CustomerVatValidationStatus.Unknown);
        customer.VatValidationCheckedAtUtc.Should().Be(checkedAtUtc);
        customer.VatValidationSource.Should().Be("provider.unavailable");
        customer.VatValidationMessage.Should().Be("VAT validation provider failed; manual review is required.");
    }

    private static User MakeUser(Guid userId) =>
        new("user@example.com", "hashed-password", "security-stamp")
        {
            Id = userId,
            FirstName = "User",
            LastName = "Test",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

    private sealed class CustomerLeadDbContext : DbContext, IAppDbContext
    {
        private CustomerLeadDbContext(DbContextOptions<CustomerLeadDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static CustomerLeadDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CustomerLeadDbContext>()
                .UseInMemoryDatabase($"darwin_customer_lead_tests_{Guid.NewGuid()}")
                .Options;
            return new CustomerLeadDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.NormalizedEmail).IsRequired();
                b.Property(x => x.UserName).IsRequired();
                b.Property(x => x.NormalizedUserName).IsRequired();
                b.Property(x => x.PasswordHash).IsRequired();
                b.Property(x => x.SecurityStamp).IsRequired();
                b.Property(x => x.Locale).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.Timezone).IsRequired();
                b.Property(x => x.ChannelsOptInJson).IsRequired();
                b.Property(x => x.FirstTouchUtmJson).IsRequired();
                b.Property(x => x.LastTouchUtmJson).IsRequired();
                b.Property(x => x.ExternalIdsJson).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Customer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).IsRequired();
                b.Property(x => x.LastName).IsRequired();
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.Phone).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.CustomerSegments);
                b.Ignore(x => x.Interactions);
                b.Ignore(x => x.Consents);
                b.Ignore(x => x.Opportunities);
                b.Ignore(x => x.Invoices);
            });

            modelBuilder.Entity<CustomerAddress>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Line1).IsRequired();
                b.Property(x => x.City).IsRequired();
                b.Property(x => x.PostalCode).IsRequired();
                b.Property(x => x.Country).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Lead>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).IsRequired();
                b.Property(x => x.LastName).IsRequired();
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.Phone).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Interactions);
            });
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; }
    }

    private sealed class FixedVatValidationProvider : IVatValidationProvider
    {
        private readonly CustomerVatValidationStatus _status;
        private readonly string _source;
        private readonly string _message;

        public FixedVatValidationProvider(CustomerVatValidationStatus status, string source, string message)
        {
            _status = status;
            _source = source;
            _message = message;
        }

        public Task<VatValidationProviderResult> ValidateAsync(string vatId, CancellationToken ct = default)
        {
            vatId.Should().Be("DE123456789");
            return Task.FromResult(new VatValidationProviderResult
            {
                Status = _status,
                Source = _source,
                Message = _message
            });
        }
    }

    private sealed class ThrowingVatValidationProvider : IVatValidationProvider
    {
        public Task<VatValidationProviderResult> ValidateAsync(string vatId, CancellationToken ct = default) =>
            throw new HttpRequestException("Simulated VIES outage");
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
