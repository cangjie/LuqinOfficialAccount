using System;
using System.Threading.Tasks;
using LuqinOfficialAccount.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BigDealController:ControllerBase
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

