using Hub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hub.Infrastructure.Security;

public sealed class ApiKeyService
{
    private readonly AppDbContext _db;
    private readonly ApiKeyCrypto _crypto;
    private readonly SecurityOptions _opt;

    public ApiKeyService(AppDbContext db,ApiKeyCrypto crypto, SecurityOptions opt)
    {
        _db = db;
        _crypto = crypto;
        _opt = opt;

    }

    public async Task<(ApiKey Row, string Plaintext)> CreateAsync(
        Guid workspaceId,
        string name,
        ApiKeyScopes scopes,
        DateTimeOffset? expiresAtUtc)
    {
        var (plaintext, prefix, hashB64, saltB64, iterations) = _crypto.Create();

        var row = new ApiKey
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            KeyPrefix = prefix,
            KeyHash = hashB64,
            KeySalt = saltB64,
            KeyIterations = iterations,
            Scopes = scopes,
            ExpiresAtUtc = expiresAtUtc
        };

        _db.ApiKeys.Add(row);
        await _db.SaveChangesAsync();

        return (row, plaintext);
    }

    public async Task<(ApiKey ApiKey, ApiKeyScopes Scopes)?> ValidateAsync(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            return null;
        };

        if (!plaintext.StartsWith("hub_", StringComparison.Ordinal))
        return null;

        var token = plaintext["hub_".Length..];
        if (token.Length < 8)
        return null;

        var prefixLen = Math.Min(_opt.PrefixLength,token.Length);
        var prefix = token[..prefixLen];

        var candidates = await _db.ApiKeys
        .Where(x => x.KeyPrefix == prefix && x.RevokedAtUtc == null)
        .ToListAsync();

        foreach (var c in candidates)
        {
            if(c.ExpiresAtUtc is not null && c.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            continue;

            if(_crypto.Verify(plaintext, c.KeyHash, c.KeySalt, c.KeyIterations))
            {
                c.LastUsedAtUtc = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();

                return (c, c.Scopes);
            }
        }

        return null;
    }
        

        public async Task <bool> RevokeAsync (Guid apiKeyId)
    {
        var row = await _db.ApiKeys.FirstOrDefaultAsync(x => x.Id == apiKeyId);
        if (row is null) return false;
        
        if(row.ExpiresAtUtc is null)
        {
            row.RevokedAtUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();

        }
        return true;
    }
    }
