using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public sealed class AccountService(IronMindDbContext db, AthleteCalendars calendars, TimeProvider clock)
{
    public async Task<User> AuthenticateAsync(string email, string password, CancellationToken cancellationToken)
    {
        var address = email.Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == address, cancellationToken);
        if (user is null)
        {
            // The same hashing work as for a wrong password, so the answer time does not reveal registered emails.
            PasswordHashUtility.CheckWithoutAccount(password);
            throw new DomainValidationException("Invalid email or password.");
        }
        var check = PasswordHashUtility.Check(password, user.PasswordHash);
        if (check == PasswordCheck.Failed)
            throw new DomainValidationException("Invalid email or password.");
        if (check == PasswordCheck.ValidButOutdated)
        {
            // The hash was made with older settings; the password is known now, so it is stored again.
            user.PasswordHash = PasswordHashUtility.Hash(password);
            await db.SaveChangesAsync(cancellationToken);
        }
        return user;
    }

    public async Task<User> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var username = request.Username.Trim();
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new DomainValidationException("A user with this email already exists.", nameof(request.Email));
        if (await db.Users.AnyAsync(u => u.Username == username, cancellationToken))
            throw new DomainValidationException("This username is already taken.", nameof(request.Username));

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = PasswordHashUtility.Hash(request.Password),
            CreatedAt = clock.UtcNow(),
            Profile = new Profile
            {
                Age = request.Age,
                TimeZone = AthleteCalendar.TryFindZone(request.TimeZone, out _) ? request.TimeZone : null,
                Language = Localized.IsUkrainian ? Language.Ukrainian : Language.English
            }
        };
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // A parallel registration took the email or username between the checks above and the insert.
            throw new DomainValidationException("Email or username is already registered.");
        }
        return user;
    }

    public async Task<SessionResponse> DescribeAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new { u.Username, u.Email, Language = u.Profile == null ? null : u.Profile.Language }).SingleAsync(cancellationToken);
        var calendar = await calendars.ForAsync(userId, cancellationToken);
        return new SessionResponse(userId, user.Username, user.Email, calendar.IanaZoneId,
            calendar.Today, await AthleteSetup.IsCompleteAsync(db, userId, cancellationToken), user.Language);
    }
}
