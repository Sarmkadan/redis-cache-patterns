# Security Policy

## Reporting Security Vulnerabilities

**Do not** open public GitHub issues for security vulnerabilities. Instead, please report security issues responsibly using one of the following methods:

### GitHub Private Vulnerability Reporting (Recommended)

Submit vulnerability reports confidentially via GitHub's built-in vulnerability reporting feature:

**https://github.com/sarmkadan/redis-cache-patterns/security/advisories/new**

### Email

If you prefer to report via email, send details to:

**rutova2@gmail.com**

## Response Timeline

We take security seriously and commit to the following response timeline:

- **Acknowledgment**: Within 48 hours of receiving a report
- **Assessment & Fix**: Within 1 week of initial assessment
- **Disclosure**: Coordinated disclosure timeline after fix validation

## Supported Versions

Security updates are provided for:

| Version | Status       | Support Until |
|---------|--------------|---------------|
| 1.x     | Supported    | Latest patch  |
| < 1.0   | Unsupported  | N/A           |

We recommend always using the latest version to ensure you have the latest security patches.

## Security Best Practices

When using Redis Cache Patterns in your applications:

1. **Secure Redis Connection**
   - Use TLS/SSL when connecting to Redis in production
   - Set strong passwords for Redis authentication
   - Restrict Redis network access to trusted hosts

2. **Cache Sensitive Data**
   - Be mindful of what data you cache
   - Use appropriate TTLs for sensitive information
   - Consider encrypting sensitive data before caching

3. **Key Management**
   - Use namespaced cache keys to prevent key collisions
   - Avoid including sensitive information in cache keys
   - Regularly audit cache key patterns

4. **Dependency Updates**
   - Keep .NET SDK updated
   - Monitor and update NuGet dependencies regularly
   - Subscribe to security advisories

5. **Error Handling**
   - Avoid exposing sensitive information in error messages
   - Log security-related events appropriately
   - Use the provided error handling patterns

## Vulnerability Disclosure Process

Once a vulnerability is reported:

1. We will confirm receipt of your report
2. We will investigate and validate the vulnerability
3. We will develop and test a fix
4. We will coordinate a release timeline
5. We will credit the reporter (unless they prefer anonymity)

## Scope

This security policy covers:

- The core Redis Cache Patterns library
- Example implementations
- Documented configuration practices

This security policy does **not** cover:

- Third-party dependencies (report to their maintainers)
- Applications built using this library
- Infrastructure hosting the application

## Thank You

We appreciate security researchers and community members who help keep this project secure by responsibly reporting vulnerabilities.
