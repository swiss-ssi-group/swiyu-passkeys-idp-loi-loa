// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Idp.Swiyu.Passkeys.Sts.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Idp.Swiyu.Passkeys.Sts.Data;

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
