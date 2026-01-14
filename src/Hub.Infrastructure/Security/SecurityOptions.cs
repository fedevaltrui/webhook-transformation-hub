namespace Hub.Infrastructure.Security;

public sealed class SecurityOptions
{
    public string ApiKeyPepper { get; set;} = null!;
    public string BootstrapToken { get; set; }= null!;

    public int DefaultIterations{ get; set; }= 210_000;
    public int SaltBytes{ get; set; }  = 16;
    public int HashBytes{ get; set; } = 32;

    public int PrefixLength{ get; set; } = 10;

}