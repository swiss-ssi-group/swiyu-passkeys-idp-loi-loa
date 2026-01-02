using Idp.Swiyu.Passkeys.Sts.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Idp.Swiyu.Passkeys.Sts.Domain;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SwiyuIdentity> SwiyuIdentity => Set<SwiyuIdentity>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<SwiyuIdentity>().HasKey(m => m.Id);
        builder.Entity<SwiyuIdentity>().Property(b => b.Id).ValueGeneratedOnAdd();

        base.OnModelCreating(builder);
    }
}
