namespace Darwin.Application.Abstractions.Security;

public interface IProtectedStringService
{
    string? Protect(string? value);
    string? Unprotect(string? value);
}
