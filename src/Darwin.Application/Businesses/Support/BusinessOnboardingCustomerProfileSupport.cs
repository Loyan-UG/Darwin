using System;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.Support
{
    internal static class BusinessOnboardingCustomerProfileSupport
    {
        internal const string NotesPrefix = "[BusinessOnboardingProfile:";

        internal static string BuildNotes(Guid businessId) => $"{NotesPrefix}{businessId:D}]";

        internal static string? ExtractCompanyName(Business business) =>
            string.IsNullOrWhiteSpace(business.LegalName) ? business.Name?.Trim() : business.LegalName.Trim();

        internal static string? ExtractProvisioningEmail(Business business, User? ownerUser)
        {
            if (!string.IsNullOrWhiteSpace(business.ContactEmail))
            {
                return business.ContactEmail.Trim();
            }

            if (!string.IsNullOrWhiteSpace(business.SupportEmail))
            {
                return business.SupportEmail.Trim();
            }

            if (!string.IsNullOrWhiteSpace(ownerUser?.Email))
            {
                return ownerUser.Email.Trim();
            }

            return null;
        }

        internal static string ExtractFirstName(Business business, User? ownerUser)
        {
            if (!string.IsNullOrWhiteSpace(ownerUser?.FirstName))
            {
                return ownerUser.FirstName.Trim();
            }

            return ExtractCompanyName(business) ?? "Business";
        }

        internal static string ExtractLastName(User? ownerUser)
        {
            if (!string.IsNullOrWhiteSpace(ownerUser?.LastName))
            {
                return ownerUser.LastName.Trim();
            }

            return "Business";
        }

        internal static string ExtractPhone(Business business, User? ownerUser)
        {
            if (!string.IsNullOrWhiteSpace(business.ContactPhoneE164))
            {
                return business.ContactPhoneE164.Trim();
            }

            if (!string.IsNullOrWhiteSpace(ownerUser?.PhoneE164))
            {
                return ownerUser.PhoneE164.Trim();
            }

            return string.Empty;
        }

        internal static string? GetMissingReason(Business business, User? ownerUser)
        {
            if (string.IsNullOrWhiteSpace(ExtractCompanyName(business)))
            {
                return "Business name";
            }

            if (string.IsNullOrWhiteSpace(ExtractProvisioningEmail(business, ownerUser)))
            {
                return "Contact email";
            }

            return null;
        }

        internal static bool ApplyManagedValues(Customer customer, Business business, User? ownerUser)
        {
            var changed = false;
            changed |= SetRequiredIfDifferent(customer.FirstName, ExtractFirstName(business, ownerUser), value => customer.FirstName = value);
            changed |= SetRequiredIfDifferent(customer.LastName, ExtractLastName(ownerUser), value => customer.LastName = value);
            changed |= SetRequiredIfDifferent(customer.Email, ExtractProvisioningEmail(business, ownerUser) ?? string.Empty, value => customer.Email = value);
            changed |= SetRequiredIfDifferent(customer.Phone, ExtractPhone(business, ownerUser), value => customer.Phone = value);
            changed |= SetOptionalIfDifferent(customer.CompanyName, ExtractCompanyName(business), value => customer.CompanyName = value);

            if (customer.TaxProfileType != CustomerTaxProfileType.Business)
            {
                customer.TaxProfileType = CustomerTaxProfileType.Business;
                changed = true;
            }

            changed |= SetOptionalIfDifferent(customer.VatId, string.IsNullOrWhiteSpace(business.TaxId) ? null : business.TaxId.Trim(), value => customer.VatId = value);
            changed |= SetOptionalIfDifferent(customer.Notes, BuildNotes(business.Id), value => customer.Notes = value);
            return changed;
        }

        private static bool SetRequiredIfDifferent(string current, string value, Action<string> assign)
        {
            if (string.Equals(current, value, StringComparison.Ordinal))
            {
                return false;
            }

            assign(value);
            return true;
        }

        private static bool SetOptionalIfDifferent(string? current, string? value, Action<string?> assign)
        {
            if (string.Equals(current, value, StringComparison.Ordinal))
            {
                return false;
            }

            assign(value);
            return true;
        }
    }
}
