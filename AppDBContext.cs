using System;
using Microsoft.EntityFrameworkCore;
using LuqinOfficialAccount.Models;
using LuqinOfficialAccount.Models.Simulator;
namespace LuqinOfficialAccount
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LimitUp>().HasKey(l => new { l.gid, l.alert_date });
            modelBuilder.Entity<LimitUpTwice>().HasKey(l => new { l.gid, l.alert_date });
            modelBuilder.Entity<ContinuousRise>().HasKey(c => new { c.alert_date, c.gid });
            modelBuilder.Entity<Holiday>().HasNoKey();
            modelBuilder.Entity<MACD>().HasKey(m => new { m.gid, m.alert_type, m.alert_time });
            modelBuilder.Entity<KDJ>().HasKey(m => new { m.gid, m.alert_type, m.alert_time });
            modelBuilder.Entity<Above3Line>().HasKey(m => new { m.gid, m.alert_date });
            modelBuilder.Entity<DoubleVolume>().HasKey(m => new { m.gid, m.alert_date });
            modelBuilder.Entity<DoubleVolumeWeek>().HasKey(m => new { m.gid, m.alert_date });
            modelBuilder.Entity<DoubleGreenLeg>().HasKey(m => new { m.gid, m.alert_date });
            modelBuilder.Entity<FirstLimitUpNewHigh>().HasKey(m => new { m.gid, m.alert_date });
            modelBuilder.Entity<Demark>().HasKey(m => new { m.gid, m.alert_time });
            modelBuilder.Entity<BuyingAlert>().HasNoKey();
            modelBuilder.Entity<FlowList>().HasNoKey();
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

        public DbSet<LuqinOfficialAccount.Models.MACD> MACD { get; set; }

        public DbSet<LuqinOfficialAccount.Models.KDJ> KDJ { get; set; }

        public DbSet<LuqinOfficialAccount.Models.Above3Line> Above3Line { get; set; }

        public DbSet<LuqinOfficialAccount.Models.Concept> Concept { get; set; }

        public DbSet<LuqinOfficialAccount.Models.ConceptMember> ConceptMember { get; set; }

        public DbSet<LuqinOfficialAccount.Models.DoubleVolume> DoubleVolume { get; set; }

        public DbSet<LuqinOfficialAccount.Models.DoubleVolumeWeek> DoubleVolumeWeek { get; set; }

        public DbSet<LuqinOfficialAccount.Models.DoubleGreenLeg> DoubleGreenLeg { get; set; }

        public DbSet<LuqinOfficialAccount.Models.FirstLimitUpNewHigh> FirstLimitUpNewHigh { get; set; }

        public DbSet<LuqinOfficialAccount.Models.Demark> demark { get; set; }

        public DbSet<LuqinOfficialAccount.Models.ResultCache> ResultCache { get; set; }

        public DbSet<Simulator> simulator { get; set; }

        public DbSet<SimulatorDaily> simulatorDaily { get; set; }

        public DbSet<SimulatorDailyTrans> simulatorDailyTrans { get; set; }

        public DbSet<SimulatorDailyHolding> simulatorDailyHolding { get; set; }

        public DbSet<bak_daily> bakDaily { get; set; }

        public DbSet<BakDailyDetail> bakDailyDetail { get; set; }

        public DbSet<BuyingAlert> buyingAlert { get; set; }

        public DbSet<Inflow> inflow { get; set; }

        public DbSet<FlowList> flowList { get; set; }
    }
}
