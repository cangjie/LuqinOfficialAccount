using System;
using Microsoft.EntityFrameworkCore;
using LuqinOfficialAccount.Models;
namespace LuqinOfficialAccount
{
    public class AppDBContext:DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        public DbSet<LuqinOfficialAccount.Models.EfTest> EfTest { get; set; }

        public DbSet<OARecevie> oARecevie { get; set; }

        public DbSet<OAUser> oAUser { get; set; }

        public DbSet<User> user { get; set; }
    }
}
