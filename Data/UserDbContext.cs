using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserIdentityApi.Data.Entities;

namespace UserIdentityApi.Data
{
    public class UserDbContext : IdentityDbContext<User, Role, int, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    {
        public UserDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<UserRole>().ToTable("UserRole");
            modelBuilder.Entity<RoleClaim>().ToTable("RoleClaim");
            modelBuilder.Entity<UserClaim>().ToTable("UserClaim");
            modelBuilder.Entity<UserToken>().ToTable("UserToken");
            modelBuilder.Entity<UserLogin>().ToTable("UserLogin");

            // Configure relationships
            modelBuilder.Entity<UserRole>(b =>
            {
                b.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();

                b.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();
            });

            modelBuilder.Entity<UserClaim>(b =>
            {
                b.HasOne(uc => uc.User)
                    .WithMany(u => u.Claims)
                    .HasForeignKey(uc => uc.UserId)
                    .IsRequired();
            });

            modelBuilder.Entity<RoleClaim>(b =>
            {
                b.HasOne(rc => rc.Role)
                    .WithMany(r => r.RoleClaims)
                    .HasForeignKey(rc => rc.RoleId)
                    .IsRequired();
            });

            modelBuilder.Entity<UserLogin>(b =>
            {
                b.HasOne(ul => ul.User)
                    .WithMany(u => u.Logins)
                    .HasForeignKey(ul => ul.UserId)
                    .IsRequired();
            });

            modelBuilder.Entity<UserToken>(b =>
            {
                b.HasOne(ut => ut.User)
                    .WithMany(u => u.Tokens)
                    .HasForeignKey(ut => ut.UserId)
                    .IsRequired();
            });
        }
    }
}