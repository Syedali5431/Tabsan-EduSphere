using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

// Same parameters as Argon2idPasswordHasher
const int SaltBytes = 32;
const int HashBytes = 32;
const int Iterations = 3;
const int MemoryKilobytes = 65536;
const int Parallelism = 4;
const string Prefix = "argon2id:";

string password = args.Length > 0 ? args[0] : "EduSphere147";

var salt = RandomNumberGenerator.GetBytes(SaltBytes);
var passwordBytes = Encoding.UTF8.GetBytes(password);

using var argon2 = new Argon2id(passwordBytes)
{
    Salt = salt,
    DegreeOfParallelism = Parallelism,
    MemorySize = MemoryKilobytes,
    Iterations = Iterations,
};

var hashBytes = argon2.GetBytes(HashBytes);
var result = $"{Prefix}{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hashBytes)}";
Console.WriteLine(result);

// Also verify the existing hash
if (args.Length > 1)
{
    var existingHash = args[1];
    if (existingHash.StartsWith(Prefix))
    {
        var withoutPrefix = existingHash[Prefix.Length..];
        var colonIndex = withoutPrefix.IndexOf(':');
        var existingSalt = Convert.FromBase64String(withoutPrefix[..colonIndex]);
        var existingHashBytes = Convert.FromBase64String(withoutPrefix[(colonIndex + 1)..]);

        using var argon2verify = new Argon2id(passwordBytes)
        {
            Salt = existingSalt,
            DegreeOfParallelism = Parallelism,
            MemorySize = MemoryKilobytes,
            Iterations = Iterations,
        };

        var computed = argon2verify.GetBytes(HashBytes);
        var match = computed.SequenceEqual(existingHashBytes);
        Console.WriteLine($"Verify existing hash: {match}");
    }
}
