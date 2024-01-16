using System;
using System.Threading.Tasks;
using LuqinOfficialAccount.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BigDealController : ControllerBase
    {
        private readonly AppDBContext _db;
        private readonly IConfiguration _config;
        private readonly Settings _settings;
        public BigDealController(AppDBContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [HttpGet("{gid}")]
        public async Task<ActionResult<int>> UpdateFund(string gid, DateTime alertDate,
            double settle, double rate, double flow_amount, double flow_amount_5_avarage,
            double big_flow_amout, double big_percent, double mid_flow_amount,
            double mid_percent, double small_flow_amount, double small_percent)
        {
            Fund? f = await _db.fund.FindAsync(new object[] { gid, alertDate });
            if (f == null)
            {
                f = new Fund()
                {
                    gid = gid,
                    alert_date = alertDate,
                    settle = settle,
                    rate = rate,
                    flow_amount = flow_amount,
                    flow_amount_5_avarage = flow_amount_5_avarage,
                    big_flow_amount = big_flow_amout,
                    big_percent = big_percent,
                    mid_flow_amount = mid_flow_amount,
                    mid_percent = mid_percent,
                    small_flow_amount = small_flow_amount,
                    small_percent = small_percent
                };
                await _db.fund.AddAsync(f);
            }
            else
            {
                f.settle = settle;
                f.rate = rate;
                f.flow_amount = flow_amount;
                f.flow_amount_5_avarage = flow_amount_5_avarage;
                f.big_flow_amount = big_flow_amout;
                f.big_percent = big_percent;
                f.mid_flow_amount = mid_flow_amount;
                f.mid_percent = mid_percent;
                f.small_flow_amount = small_flow_amount;
                f.small_percent = small_percent;
                _db.fund.Entry(f).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            int i = await _db.SaveChangesAsync();
            return Ok(i);
        }


        public async Task<ActionResult<IEnumerable<BigDeal>>> GetBigDeal(string gid)
        {
            return await _db.bigDeal.Where(b => b.gid.Trim().Equals(gid.Trim()))
                .OrderByDescending(b => b.alert_date).AsNoTracking().ToListAsync();
        }
           

        [HttpGet]
        public async Task<ActionResult<int>> UpdateBigDeal(string gid, DateTime alertDate,
            double bigDealVol, double totalVol, double bigDealAmount, double totalAmount,
            double bigDealAvaragePrice, double uVol, double dVol, double eVol)
        {
            BigDeal bd = await _db.bigDeal.FindAsync(new object[] { gid, alertDate });
            int ret = 0;
            if (bd == null)
            {
                bd = new BigDeal()
                {
                    gid = gid,
                    alert_date = alertDate,
                    big_deal_vol = bigDealVol,
                    total_vol = totalVol,
                    big_deal_amount = bigDealAmount,
                    total_amount = totalAmount,
                    big_deal_ava_price = bigDealAvaragePrice,
                    u_vol = uVol,
                    d_vol = dVol,
                    e_vol = eVol,
                    update_date = DateTime.Now
                };
                await _db.bigDeal.AddAsync(bd);
                ret = 1;
            }
            else
            {
                bd.big_deal_vol = bigDealVol;
                bd.total_vol = totalVol;
                bd.big_deal_amount = bigDealAmount;
                bd.total_amount = totalAmount;
                bd.big_deal_ava_price = bigDealAvaragePrice;
                bd.u_vol = uVol;
                bd.d_vol = dVol;
                bd.e_vol = eVol;
                bd.update_date = DateTime.Now;
                _db.bigDeal.Entry(bd).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            await _db.SaveChangesAsync();
            return Ok(ret);
        }
	}
}

