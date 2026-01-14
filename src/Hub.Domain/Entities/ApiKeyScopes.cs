namespace Hub.Domain.Entities;

[Flags]
public enum ApiKeyScopes
{
    None = 0,
    Read = 1 << 0,
    Ingest = 1 << 1,
    Admin = 1 << 2

}


