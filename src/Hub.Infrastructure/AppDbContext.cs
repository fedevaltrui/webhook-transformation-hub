using Microsoft.EntityFrameworkCore;

namespace Hub.Infrastructure;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}
}