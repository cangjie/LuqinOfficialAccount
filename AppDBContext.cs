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
            modelBuilder.Entity<LimitUp>().HasKey(l => new { l.gid, l.alert_date});
            modelBuilder.Entity<LimitUpTwice>().HasKey(l => new { l.gid, l.alert_date });
            modelBuilder.Entity<ContinuousRise>().HasKey(c => new { c.alert_date, c.gid });
            modelBuilder.Entity<Holiday>().HasNoKey();
        }

        

        public DbSet<LuqinOfficialAccount.Models.EfTest> EfTest { get; set; }

        public DbSet<OARecevie> oARecevie { get; set; }

        public DbSet<OAUser> oAUser { get; set; }

        public DbSet<User> user { get; set; }

        public DbSet<OASent> oASent { get; set; }

        public DbSet<PosterScanLog> posterScanLog { get; set; }

        public DbSet<Promote> promote { get; set; }

        public DbSet<Token> token { get; set; }

        public DbSet<UserMediaAsset> userMediaAsset { get; set; }

        public DbSet<Holiday> holiday { get; set; }

        public DbSet<LuqinOfficialAccount.Models.LimitUp> LimitUp { get; set; }

        public DbSet<LuqinOfficialAccount.Models.ContinuousRise> ContinuousRise { get; set; }

        public DbSet<LuqinOfficialAccount.Models.LimitUpTwice> LimitUpTwice { get; set; }

        public DbSet<LuqinOfficialAccount.Models.Chip> Chip { get; set; }

        public DbSet<LuqinOfficialAccount.Models.BigRise> BigRise { get; set; }


    }
}
