using System.IO;
using System.Security.Cryptography;
using ItungoSoft.Farmer.Data;
using ItungoSoft.Farmer.Models;

namespace ItungoSoft.Farmer.Services;

public static class AuthService
{
    public static FarmerUser? Current { get; private set; }

    static string Hash(string pw, byte[] salt) => Convert.ToBase64String(
        Rfc2898DeriveBytes.Pbkdf2(pw, salt, 100_000, HashAlgorithmName.SHA256, 32));

    /// Returns null on success, otherwise an error message. One farmer per installation.
    public static string? Register(string name, string user, string pw)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(user) || pw.Length < 6)
            return "Fill in all fields. Password needs at least 6 characters.";
        using var db = new AppDbContext();
        if (db.Farmers.Any()) return "This installation already has a farmer account. Please log in.";
        var salt = RandomNumberGenerator.GetBytes(16);
        db.Farmers.Add(new FarmerUser
        {
            FullName = name.Trim(), Username = user.Trim(),
            Salt = Convert.ToBase64String(salt), PasswordHash = Hash(pw, salt)
        });
        db.SaveChanges();
        return null;
    }

    public static bool Login(string user, string pw)
    {
        using var db = new AppDbContext();
        var f = db.Farmers.FirstOrDefault(x => x.Username == user.Trim());
        if (f == null) return false;
        var expected = Convert.FromBase64String(f.PasswordHash);
        var actual = Convert.FromBase64String(Hash(pw, Convert.FromBase64String(f.Salt)));
        if (!CryptographicOperations.FixedTimeEquals(expected, actual)) return false;
        Current = f;
        return true;
    }

    public static void Logout() => Current = null;

    public static string? UpdateProfile(string name, string user)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(user)) return "Name and username are required.";
        using var db = new AppDbContext();
        var f = db.Farmers.Find(Current!.Id)!;
        f.FullName = name.Trim(); f.Username = user.Trim();
        db.SaveChanges(); Current = f;
        return null;
    }

    public static string? ChangePassword(string oldPw, string newPw)
    {
        if (newPw.Length < 6) return "New password needs at least 6 characters.";
        using var db = new AppDbContext();
        var f = db.Farmers.Find(Current!.Id)!;
        var ok = CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(Hash(oldPw, Convert.FromBase64String(f.Salt))), Convert.FromBase64String(f.PasswordHash));
        if (!ok) return "Current password is incorrect.";
        var salt = RandomNumberGenerator.GetBytes(16);
        f.Salt = Convert.ToBase64String(salt); f.PasswordHash = Hash(newPw, salt);
        db.SaveChanges(); Current = f;
        return null;
    }
}
