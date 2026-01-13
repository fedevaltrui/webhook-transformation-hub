using Microsoft.EntityFrameworkCore;
using Hub.Domain.Entities;
using System.Dynamic;


namespace Hub.Infrastructure;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Endpoint> Endpoints => Set<Endpoint>();
    public DbSet<IngestRequest> IngestRequests => Set<IngestRequest>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        {
            
            //Workspace
            modelBuilder.Entity<Workspace>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(120).IsRequired();
                e.HasIndex(x => x.Name);

            });

            // ApiKey

            modelBuilder.Entity<ApiKey>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(120).IsRequired();
                e.Property(x =>x.KeyHash).HasMaxLength(256).IsRequired();
                e.HasIndex(x =>x.KeyHash).IsUnique();
                e.HasOne(x => x.Workspace)
                    .WithMany(x=>x.ApiKeys)
                    .HasForeignKey(x=>x.WorkspaceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Endpoint>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(120).IsRequired();
                e.Property(x => x.EndpointKey).HasMaxLength(80).IsRequired();
                e.Property(x => x.DestinationUrl).HasMaxLength(2048).IsRequired();
                e.Property(x => x.SigningSecret).HasMaxLength(256);
            
                e.HasIndex(x => x.EndpointKey).IsUnique();

                e.HasOne(x => x.Workspace)
                    .WithMany(x => x.Endpoints)
                    .HasForeignKey(x => x.WorkspaceId)
                    .OnDelete(DeleteBehavior.Cascade);

            });

// IngestRequest
        modelBuilder.Entity<IngestRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Method).HasMaxLength(16).IsRequired();

            // jsonb en Postgres
            e.Property(x => x.HeadersJson).HasColumnType("jsonb").IsRequired();
            e.Property(x => x.BodyJson).HasColumnType("jsonb").IsRequired();

            e.HasIndex(x => new { x.EndpointId, x.IdempotencyKey });

            e.HasOne(x => x.Endpoint)
             .WithMany(x => x.IngestRequests)
             .HasForeignKey(x => x.EndpointId)
             .OnDelete(DeleteBehavior.Cascade);
        });

         // Delivery
        modelBuilder.Entity<Delivery>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Error).HasMaxLength(2000);

            e.HasIndex(x => new { x.IngestRequestId, x.Attempt });

            e.HasOne(x => x.IngestRequest)
             .WithMany(x => x.Deliveries)
             .HasForeignKey(x => x.IngestRequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        }    }

}