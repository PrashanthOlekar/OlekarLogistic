using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Olekar.Api.Data;

namespace Olekar.Api.Services;

/// <summary>Thrown for expected problems; turned into a JSON { error } response with the given status.</summary>
public class ApiException : Exception
{
    public int Status { get; }
    public ApiException(int status, string message) : base(message) => Status = status;
    public static ApiException BadRequest(string m) => new(400, m);
    public static ApiException NotFound(string m) => new(404, m);
    public static ApiException Forbidden(string m) => new(403, m);
    public static ApiException Conflict(string m) => new(409, m);
}

public static class Guard
{
    public static void Require(bool condition, string message)
    {
        if (!condition) throw ApiException.BadRequest(message);
    }
}

public static class ClaimsExtensions
{
    public static long UserId(this ClaimsPrincipal user) =>
        long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new ApiException(401, "Please sign in again."));
}

// ---------------------------------------------------------------- tokens
public class JwtOptions
{
    public string Issuer { get; set; } = "Olekar";
    public string Audience { get; set; } = "Olekar";
    public string Key { get; set; } = "";
    public int AccessTokenHours { get; set; } = 12;
}

public class TokenService
{
    private readonly JwtOptions _opt;
    public TokenService(IConfiguration config) => _opt = config.GetSection("Jwt").Get<JwtOptions>()!;

    public string Create(User u)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, u.UserId.ToString()),
            new Claim(ClaimTypes.Role, u.Role),
            new Claim(ClaimTypes.Name, u.FullName),
            new Claim(ClaimTypes.MobilePhone, u.Mobile)
        };
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_opt.Issuer, _opt.Audience, claims,
            expires: DateTime.UtcNow.AddHours(_opt.AccessTokenHours), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ---------------------------------------------------------------- OTP
public class OtpService
{
    private readonly OlekarDbContext _db;
    private readonly SettingsService _settings;
    public OtpService(OlekarDbContext db, SettingsService settings) { _db = db; _settings = settings; }

    private static string Hash(string mobile, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{mobile}:{code}:olekar")));

    public static string NewCode(int digits) =>
        RandomNumberGenerator.GetInt32((int)Math.Pow(10, digits - 1), (int)Math.Pow(10, digits)).ToString();

    /// <summary>Creates a login/signup OTP. Returns the plain code so the caller can send it (or show it in Development).</summary>
    public async Task<string> IssueAsync(string mobile, string purpose)
    {
        var recent = await _db.OtpCodes.CountAsync(o => o.Mobile == mobile && o.CreatedAt > DateTime.UtcNow.AddMinutes(-10));
        if (recent >= 5) throw new ApiException(429, "Too many codes requested. Try again in 10 minutes.");
        var code = NewCode(6);
        var minutes = await _settings.GetIntAsync("OtpExpiryMinutes", 10);
        _db.OtpCodes.Add(new OtpCode { Mobile = mobile, Purpose = purpose, CodeHash = Hash(mobile, code), ExpiresAt = DateTime.UtcNow.AddMinutes(minutes) });
        await _db.SaveChangesAsync();
        return code;
    }

    public async Task VerifyAsync(string mobile, string code, string purpose)
    {
        var otp = await _db.OtpCodes
            .Where(o => o.Mobile == mobile && o.Purpose == purpose && o.UsedAt == null && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync()
            ?? throw ApiException.BadRequest("This code has expired. Request a new one.");
        var max = await _settings.GetIntAsync("OtpMaxAttempts", 5);
        if (otp.Attempts >= max) throw ApiException.BadRequest("Too many wrong tries. Request a new code.");
        if (otp.CodeHash != Hash(mobile, code.Trim()))
        {
            otp.Attempts++;
            await _db.SaveChangesAsync();
            throw ApiException.BadRequest("That code is not correct.");
        }
        otp.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

// ---------------------------------------------------------------- personal data encryption
/// <summary>Encrypts PAN, bank account numbers and trip OTPs with ASP.NET Core Data Protection.
/// In production persist the key ring to Azure Blob + Key Vault so keys survive restarts.</summary>
public class PiiProtector
{
    private readonly IDataProtector _p;
    public PiiProtector(IDataProtectionProvider provider) => _p = provider.CreateProtector("Olekar.Pii.v1");
    public byte[] ProtectBytes(string plain) => _p.Protect(Encoding.UTF8.GetBytes(plain));
    public string Protect(string plain) => _p.Protect(plain);
    public string Unprotect(string cipher) => _p.Unprotect(cipher);
}

// ---------------------------------------------------------------- settings
public class SettingsService
{
    private readonly OlekarDbContext _db;
    public SettingsService(OlekarDbContext db) => _db = db;

    public async Task<decimal> GetDecimalAsync(string key, decimal fallback)
    {
        var v = await _db.Settings.Where(s => s.SettingKey == key).Select(s => s.SettingValue).FirstOrDefaultAsync();
        return decimal.TryParse(v, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : fallback;
    }
    public async Task<int> GetIntAsync(string key, int fallback) => (int)await GetDecimalAsync(key, fallback);
}

// ---------------------------------------------------------------- pricing
public record PriceBreakdown(decimal DistanceKm, int Days, decimal VehicleCost, decimal DriverCost, decimal Freight,
    decimal TaxAmount, decimal TotalAmount, decimal Commission, decimal OwnerPayout, decimal CommissionPercent, decimal GstPercent);

public class PricingService
{
    private readonly SettingsService _settings;
    public PricingService(SettingsService settings) => _settings = settings;

    /// <summary>Road distance estimate: straight-line distance × 1.24, or 18 km inside one city.
    /// Replace with Google / Azure Maps distance in production.</summary>
    public static decimal RoadKm(City from, City to)
    {
        if (from.CityId == to.CityId) return 18;
        const double R = 6371, rad = Math.PI / 180;
        double la1 = (double)from.Latitude * rad, la2 = (double)to.Latitude * rad;
        double dLa = la2 - la1, dLo = ((double)to.Longitude - (double)from.Longitude) * rad;
        var a = Math.Pow(Math.Sin(dLa / 2), 2) + Math.Cos(la1) * Math.Cos(la2) * Math.Pow(Math.Sin(dLo / 2), 2);
        return Math.Round((decimal)(2 * R * Math.Asin(Math.Sqrt(a)) * 1.24), 1);
    }

    /// <summary>Customer pays freight (vehicle + driver) plus GST on freight.
    /// Olekar's commission is taken from the owner's side, so the owner receives freight − commission.</summary>
    public async Task<PriceBreakdown> PriceAsync(VehicleType vt, decimal km)
    {
        var commissionPct = await _settings.GetDecimalAsync("CommissionPercent", 7);
        var gstPct = await _settings.GetDecimalAsync("GstOnFreightPercent", 5);
        var vehicle = Math.Round(Math.Max(vt.MinFare, vt.RatePerKm * km), 0);
        var days = Math.Max(1, (int)Math.Ceiling(km / 400m));
        var driver = days * vt.DriverBattaPerDay;
        var freight = vehicle + driver;
        var tax = Math.Round(freight * gstPct / 100m, 2);
        var commission = Math.Round(freight * commissionPct / 100m, 2);
        return new PriceBreakdown(km, days, vehicle, driver, freight, tax, freight + tax, commission, freight - commission, commissionPct, gstPct);
    }
}

// ---------------------------------------------------------------- files
public record StoredFile(string Path, string FileName, string ContentType, long Size, string Sha256);

/// <summary>Saves uploads to a private folder in development. Swap for Azure Blob Storage in production;
/// only this class changes.</summary>
public class FileStorage
{
    private static readonly string[] Allowed = { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".heic" };
    private readonly string _root;
    public FileStorage(IConfiguration config, IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, config["Storage:LocalFolder"] ?? "App_Data/uploads");
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> SaveAsync(IFormFile file)
    {
        Guard.Require(file.Length > 0, "The file is empty.");
        Guard.Require(file.Length <= 10 * 1024 * 1024, "Files must be 10 MB or smaller.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        Guard.Require(Allowed.Contains(ext), "Upload a PDF or a photo (JPG, PNG, WEBP, HEIC).");
        var name = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext}";
        var full = Path.Combine(_root, name);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using (var fs = File.Create(full)) await file.CopyToAsync(fs);
        await using var read = File.OpenRead(full);
        var sha = Convert.ToHexString(await SHA256.HashDataAsync(read));
        return new StoredFile(name, Path.GetFileName(file.FileName), file.ContentType, file.Length, sha);
    }

    public string FullPath(string relative) => Path.Combine(_root, relative);
}

// ---------------------------------------------------------------- audit
public static class Audit
{
    public static void Log(OlekarDbContext db, HttpContext http, string action, string entityType, long entityId, object? newValues = null)
    {
        long? actor = long.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
            IpAddress = http.Connection.RemoteIpAddress?.ToString()
        });
    }
}
