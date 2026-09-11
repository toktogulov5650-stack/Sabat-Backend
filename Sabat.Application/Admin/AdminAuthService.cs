using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Sabat.Application.Abstractions;
using Sabat.Application.Common;
using Sabat.Domain;

namespace Sabat.Application.Admin;

public sealed class AdminAuthService(
    ISabatDbContext dbContext,
    IAdminPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IAdminAuthService
{
    public async Task<AuthenticatedAdmin> BootstrapAsync(
        BootstrapAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.AdminUsers.AnyAsync(cancellationToken))
        {
            throw new ConflictException("The first administrator has already been created.");
        }

        var (email, normalizedEmail, displayName) = ValidateIdentity(
            request.Email,
            request.DisplayName);
        ValidatePassword(request.Password, nameof(request.Password));

        var now = timeProvider.GetUtcNow();
        var admin = new AdminUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            DisplayName = displayName,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = true,
            SessionVersion = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.AdminUsers.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAuthenticated(admin);
    }

    public async Task<AuthenticatedAdmin> AuthenticateAsync(
        AdminLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var admin = await dbContext.AdminUsers
            .SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

        if (admin is null || !admin.IsActive ||
            !passwordHasher.Verify(request.Password ?? string.Empty, admin.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var now = timeProvider.GetUtcNow();
        admin.LastLoginAt = now;
        admin.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAuthenticated(admin);
    }

    public async Task<AuthenticatedAdmin> GetSessionAsync(
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var admin = await GetActiveAdminAsync(adminId, cancellationToken);
        return MapAuthenticated(admin);
    }

    public async Task<AuthenticatedAdmin> ChangePasswordAsync(
        Guid adminId,
        ChangeAdminPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var admin = await GetActiveAdminAsync(adminId, cancellationToken);
        if (!passwordHasher.Verify(request.CurrentPassword ?? string.Empty, admin.PasswordHash))
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["currentPassword"] = ["The current password is incorrect."]
            });
        }

        ValidatePassword(request.NewPassword, nameof(request.NewPassword));
        if (passwordHasher.Verify(request.NewPassword, admin.PasswordHash))
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["newPassword"] = ["The new password must be different from the current password."]
            });
        }

        admin.PasswordHash = passwordHasher.Hash(request.NewPassword);
        admin.SessionVersion++;
        admin.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAuthenticated(admin);
    }

    public async Task RevokeSessionsAsync(
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var admin = await GetActiveAdminAsync(adminId, cancellationToken);
        admin.SessionVersion++;
        admin.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AdminUser> GetActiveAdminAsync(
        Guid adminId,
        CancellationToken cancellationToken)
    {
        var admin = await dbContext.AdminUsers
            .SingleOrDefaultAsync(user => user.Id == adminId, cancellationToken);
        if (admin is null || !admin.IsActive)
        {
            throw new UnauthorizedException("The administrator session is no longer valid.");
        }

        return admin;
    }

    private static (string Email, string NormalizedEmail, string DisplayName) ValidateIdentity(
        string? email,
        string? displayName)
    {
        var errors = new Dictionary<string, string[]>();
        var trimmedEmail = email?.Trim() ?? string.Empty;
        var trimmedName = displayName?.Trim() ?? string.Empty;

        if (trimmedEmail.Length is < 3 or > 254 || !IsEmail(trimmedEmail))
        {
            errors["email"] = ["A valid email address is required."];
        }

        if (trimmedName.Length is < 2 or > 120)
        {
            errors["displayName"] = ["Display name must contain between 2 and 120 characters."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }

        return (trimmedEmail, NormalizeEmail(trimmedEmail), trimmedName);
    }

    private static void ValidatePassword(string? password, string fieldName)
    {
        var value = password ?? string.Empty;
        var isStrong = value.Length is >= 12 and <= 256 &&
                       value.Any(char.IsUpper) &&
                       value.Any(char.IsLower) &&
                       value.Any(char.IsDigit) &&
                       value.Any(character => !char.IsLetterOrDigit(character));
        if (!isStrong)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                [char.ToLowerInvariant(fieldName[0]) + fieldName[1..]] =
                ["Password must be 12-256 characters and include upper-case, lower-case, numeric and special characters."]
            });
        }
    }

    private static string NormalizeEmail(string? email) =>
        (email ?? string.Empty).Trim().ToUpperInvariant();

    private static bool IsEmail(string value)
    {
        try
        {
            return new MailAddress(value).Address.Equals(value, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static AuthenticatedAdmin MapAuthenticated(AdminUser admin) =>
        new(
            new AdminUserDto(
                admin.Id,
                admin.Email,
                admin.DisplayName,
                admin.CreatedAt,
                admin.LastLoginAt),
            admin.SessionVersion);
}
