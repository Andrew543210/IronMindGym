using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>Registration and the password check at sign-in. The session cookie itself is the controller's work.</summary>
public sealed class AccountService(IronMindDbContext db, AthleteCalendars calendars, TimeProvider clock)
{
    /// <summary>The account with this email and password; any mismatch is refused with the same message.</summary>
    public User Authenticate(string email, string password)
    {
        var address = email.Trim();
        var user = db.Users.FirstOrDefault(u => u.Email == address);
        if (user is null)
        {
            // The same hashing work as for a wrong password, so the answer time does not tell which emails are registered.
            PasswordHashUtility.CheckWithoutAccount(password);
            throw new DomainValidationException("Invalid email or password.");
        }
        var check = PasswordHashUtility.Check(password, user.PasswordHash);
        if (check == PasswordCheck.Failed)
            throw new DomainValidationException("Invalid email or password.");
        if (check == PasswordCheck.ValidButOutdated)
        {
            // The hash was made with older settings; the password is known now, so it is stored with the current ones.
            user.PasswordHash = PasswordHashUtility.Hash(password);
            db.SaveChanges();
        }
        return user;
    }

    public User Register(RegisterRequest request)
    {
        var email = request.Email.Trim();
        var username = request.Username.Trim();
        if (db.Users.Any(u => u.Email == email))
            throw new DomainValidationException("A user with this email already exists.", nameof(request.Email));
        if (db.Users.Any(u => u.Username == username))
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
                Language = Localized.IsUkrainian ? "uk" : "en"
            }
        };
        db.Users.Add(user);
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // A parallel registration took the email or username between the checks above and the insert.
            throw new DomainValidationException("Email or username is already registered.");
        }
        return user;
    }

    public SessionResponse Describe(int userId)
    {
        var user = db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new { u.Username, u.Email, Language = u.Profile == null ? null : u.Profile.Language }).Single();
        var calendar = calendars.For(userId);
        return new SessionResponse(userId, user.Username, user.Email, calendar.IanaZoneId,
            calendar.Today, AthleteSetup.IsComplete(db, userId), user.Language);
    }
}
