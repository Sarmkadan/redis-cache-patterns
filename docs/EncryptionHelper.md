# EncryptionHelper

The `EncryptionHelper` type provides a collection of static utility methods for common cryptographic and data‑masking operations used throughout the redis‑cache‑patterns project. It offers hashing, verification, random data generation, and simple masking of sensitive information.

## API

### HashSha256
- **Purpose:** Computes the SHA‑256 hash of a plain‑text string and returns the result as a lowercase hexadecimal string.
- **Parameters:** `input` (string) – the text to be hashed.
- **Return value:** string – the hex‑encoded hash.
- **Throws:** `ArgumentNullException` if `input` is `null`.

### VerifyHash
- **Purpose:** Determines whether a given input string matches a previously computed hash value.
- **Parameters:** 
  - `input` (string) – the plain text to verify.
  - `hash` (string) – the expected hash value (hex string).
- **Return value:** `bool` – `true` if the hash of `input` equals `hash`; otherwise `false`.
- **Throws:** 
  - `ArgumentNullException` if either `input` or `hash` is `null`.
  - `FormatException` if `hash` is not a valid hexadecimal string.

### HashMd5
- **Purpose:** Computes the MD5 hash of a plain‑text string and returns the result as a lowercase hexadecimal string.
- **Parameters:** `input` (string) – the text to be hashed.
- **Return value:** string – the hex‑encoded MD5 hash.
- **Throws:** `ArgumentNullException` if `input` is `null`.

### GenerateRandomBytes
- **Purpose:** Generates a cryptographically strong random byte array of a specified length.
- **Parameters:** `length` (int) – number of random bytes to generate; must be zero or greater.
- **Return value:** byte[] – array filled with random values.
- **Throws:** `ArgumentOutOfRangeException` if `length` is less than zero.

### GenerateRandomString
- **Purpose:** Produces a random string of a given length using a supplied character set (or a default alphanumeric set if none is provided).
- **Parameters:** 
  - `length` (int) – desired length of the output string; must be zero or greater.
  - `charset` (string, optional) – characters permitted in the result; if `null` or empty, the default set `"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"` is used.
- **Return value:** string – random string of the requested length.
- **Throws:** 
  - `ArgumentOutOfRangeException` if `length` is less than zero.
  - `ArgumentException` if a non‑null `charset` is provided but is empty.

### MaskSensitiveData
- **Purpose:** Masks a sensitive string, replacing leading characters with asterisks while leaving a configurable number of trailing characters visible.
- **Parameters:** 
  - `input` (string) – the data to mask.
  - `visibleLength` (int, optional) – number of characters to keep unmasked at the end; defaults to 4.
- **Return value:** string – masked version of `input`.
- **Throws:** 
  - `ArgumentNullException` if `input` is `null`.
  - `ArgumentOutOfRangeException` if `visibleLength` is negative or greater than the length of `input`.

## Usage

Example 1: Hashing and verifying a password.

```csharp
string password = "P@ssw0rd!";
string hash = EncryptionHelper.HashSha256(password);
// Store hash in a database or cache
bool isValid = EncryptionHelper.VerifyHash(password, hash);
Console.WriteLine(isValid ? "Password matches" : "Invalid password");
```

Example 2: Generating a random token and masking a credit‑card number.

```csharp
byte[] salt = EncryptionHelper.GenerateRandomBytes(16);
string token = EncryptionHelper.GenerateRandomString(32, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
string cardNumber = "4111111111111111";
string masked = EncryptionHelper.MaskSensitiveData(cardNumber, 4);
// masked => "************1111"
```

## Notes

- All members are `static` and thread‑safe; they depend only on their parameters and the thread‑safe .NET cryptographic RNG (`RandomNumberGenerator`).
- `HashSha256` and `HashMd5` produce deterministic output for identical inputs. They are suitable for integrity checks but **not** for password storage without additional salting and a work‑factor.
- `VerifyHash` performs a case‑insensitive comparison of hex strings; it accepts both lower‑ and upper‑case hash values.
- `GenerateRandomBytes` uses `RandomNumberGenerator.Fill`, which is suitable for high‑frequency calls without exhausting entropy.
- `GenerateRandomString` will throw if an explicit `charset` is supplied but empty; a `null` charset falls back to the default alphanumeric set.
- `MaskSensitiveData` returns the original string when `visibleLength` is greater than or equal to the input length; if `visibleLength` is zero, the result consists solely of asterisks matching the input length.
- No static state is maintained by the type, so there are no additional thread‑safety considerations beyond those of the underlying .NET APIs.
