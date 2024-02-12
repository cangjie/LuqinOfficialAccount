using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LuqinOfficialAccount.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MoneyFlowController:ControllerBase
	{
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        public MoneyFlowController(AppDBContext context, IConfiguration config)
		{
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            Util._db = context;
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<List<Inflow>>> GetMoneyFlow(DateTime date, int days)
        {
            return BadRequest();
        }


        [NonAction]
        public async Task<List<MoneyFlow>> LimitUp(DateTime startDate, DateTime endDate)
        {
            var list = await _db.moneyFlow.FromSqlRaw(" select alert_money_flow.* from alert_money_flow "
                + " left join limit_up on alert_money_flow.alert_date = limit_up.alert_date and alert_money_flow.gid = limit_up.gid "
                + " where limit_up.gid is not null and limit_up.alert_date >= '" + startDate.ToShortDateString() + "'  "
                + " and limit_up.alert_date <= '" + endDate.ToShortDateString() + "'  ")
                .AsNoTracking().ToListAsync();
            return list;
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> GetLimitUp(int days, DateTime startDate, DateTime endDate, string sort = "流入率 desc")
        {
            List<MoneyFlow> limitUpList = await LimitUp(startDate, endDate);
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("流入率", Type.GetType("System.Double"));
            dt.Columns.Add("换手率", Type.GetType("System.Double"));
            dt.Columns.Add("流换比", Type.GetType("System.Double"));
            for (int i = 0; i < limitUpList.Count; i++)
            {
                Stock s = Stock.GetStock(limitUpList[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(limitUpList[i].alert_date.Date);
                if (alertIndex < 1 || alertIndex >= s.klineDay.Length - 1)
                {
                    continue;
                }
                if (!KLine.IsLimitUp(s.klineDay, s.gid, alertIndex))
                {
                    continue;
                }
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["买入"] = s.klineDay[alertIndex].settle;
                dr["流入率"] = limitUpList[i].flow_percent;
                dr["换手率"] = s.klineDay[alertIndex].turnOver;
                dr["流换比"] = limitUpList[i].flow_percent * 100 / s.klineDay[alertIndex].turnOver;
                dt.Rows.Add(dr);
            }
            StockFilter sf = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), days);
            try
            {
                return Ok(sf);
            }
            catch
            {
                return NotFound();

            }
        }
        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> HorseHead(int days, DateTime startDate, DateTime endDate, string sort = "流入率 desc")
        {
            startDate = Util.GetLastTransactDate(startDate, 1, _db);
            endDate = Util.GetLastTransactDate(endDate, 1, _db);
            List<MoneyFlow> limitUpList = await LimitUp(startDate, endDate);
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("流入率", Type.GetType("System.Double"));
            dt.Columns.Add("换手率", Type.GetType("System.Double"));
            dt.Columns.Add("流换比", Type.GetType("System.Double"));
            for (int i = 0; i < limitUpList.Count; i++)
            {
                Stock s = Stock.GetStock(limitUpList[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(limitUpList[i].alert_date.Date);
                if (alertIndex < 1 || alertIndex >= s.klineDay.Length - 2)
                {
                    continue;
                }
                if (!KLine.IsLimitUp(s.klineDay, s.gid, alertIndex))
                {
                    continue;
                }
                if (KLine.IsLimitUp(s.klineDay, alertIndex + 1))
                {
                    continue;
                }
                if (s.klineDay[alertIndex + 1].net_mf_vol <= 0)
                {
                    continue;
                }

                if (s.klineDay[alertIndex + 1].open <= s.klineDay[alertIndex].settle
                    || s.klineDay[alertIndex + 1].settle <= s.klineDay[alertIndex].settle
                    || s.klineDay[alertIndex + 1].open > s.klineDay[alertIndex + 1].settle)
                {
                    continue;
                }

                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex + 1].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["买入"] = s.klineDay[alertIndex + 1].settle;
                double tShare = s.klineDay[alertIndex + 1].float_share;
                dr["流入率"] = (tShare == 0) ? 0 : s.klineDay[alertIndex + 1].net_mf_vol / tShare;
                dr["换手率"] = s.klineDay[alertIndex + 1].turnOver;
                dr["流换比"] = (double)dr["流入率"] * 100 / (double)dr["换手率"];
                dt.Rows.Add(dr);
            }
            StockFilter sf = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), days);
            try
            {
                return Ok(sf);
            }
            catch
            {
                return NotFound();

            }
        }
        [HttpGet]
        public async Task SearchMoneyFlow(DateTime startDate, DateTime endDate, string gid = "")
        {
            Stock[] sArr = Util.stockList;
            for (int i = 0; i < sArr.Length; i++)
            {
                if (sArr[i].gid.IndexOf(gid) <= 0)
                {
                    continue;
                }
                Stock s = sArr[i];
                s.ForceRefreshKLineDay();
                int startIndex = s.GetItemIndex(startDate.Date);
                int endIndex = s.GetItemIndex(endDate.Date);
                if (startIndex <= 0)
                {
                    continue;
                }
                if (endIndex < startIndex)
                {
                    endIndex = s.klineDay.Length - 1;
                }
                for (int j = startIndex; j <= endIndex; j++)
                {
                    if (j == 0)
                    {
                        continue;
                    }
                    if (s.klineDay[j].net_mf_vol <= 0 || s.klineDay[j].float_share <= 0)
                    {
                        continue;
                    }
                    if ((s.klineDay[j].settle - s.klineDay[j - 1].settle) / s.klineDay[j - 1].settle < -0.09)
                    {
                        continue;
                    }
                    MoneyFlow ml = new MoneyFlow()
                    {
                        alert_date = s.klineDay[j].settleTime.Date,
                        gid = s.gid,
                        flow_percent = s.klineDay[j].net_mf_vol / s.klineDay[j].float_share
                    };
                    var mList = await _db.moneyFlow
                        .Where(m => (m.alert_date.Date == s.klineDay[j].settleTime.Date && m.gid.Trim().Equals(s.gid.Trim())))
                        .ToListAsync();
                    if (mList == null || mList.Count <= 0)
                    {
                        try
                        {
                            await _db.moneyFlow.AddAsync(ml);
                            await _db.SaveChangesAsync();
                        }
                        catch
                        {

                        }
                    }
                    else
                    {
                        if (mList[0].flow_percent != ml.flow_percent)
                        {
                            try
                            {
                                mList[0].flow_percent = ml.flow_percent;
                                _db.moneyFlow.Entry(mList[0]).State = EntityState.Modified;
                                await _db.SaveChangesAsync();
                            }
                            catch
                            {

                            }
                        }
                    }
                }
            }
        }

    }
}

