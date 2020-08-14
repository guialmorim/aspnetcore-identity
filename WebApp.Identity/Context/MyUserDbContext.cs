using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Identity.Models
{
    public class UserDbContext : IdentityDbContext<User>
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Organization>(org => 
            {
                org.ToTable("Organizations");
                org.HasKey(x => x.Id);
                org.HasMany<User>()
                .WithOne()
                .HasForeignKey(x => x.OrganizationId)
                .IsRequired(false);
            });
        }
    }
}
