using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Data;
using LuqinOfficialAccount.Models;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MonthPromoteStockController : ControllerBase
	{
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly string token = "4da2fbec9c2cee373d3aace9f9e200a315a2812dc11267c425010cec";

        public MonthPromoteStockController(AppDBContext context, IConfiguration config)
		{
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            Util._db = context;
        }

        [HttpGet]
        public async Task GetPromote(string month)
        {
            string postData = "{\n    \"api_name\": \"broker_recommend\",\n    \"token\": \""
                + token + "\",\n    \"params\": {\"month\": \"" + month
                + "\"},\n    \"fields\":\"\"\n\n}";
            string retJson = Util.GetWebContent("http://api.tushare.pro", postData);
            DragonTigerController.TopListStruct topList
                = JsonConvert.DeserializeObject<DragonTigerController.TopListStruct>(retJson);
            for (int i = 0; i < topList.data.items.Length; i++)
            {
                string gid = topList.data.items[i][2].ToString();
                gid = gid.Substring(7, 2).ToLower() + gid.Substring(0, 6);
                MonthPromoteStock mp = new MonthPromoteStock()
                {
                    month = topList.data.items[i][0].ToString().Trim(),
                    gid = gid,
                    broker = topList.data.items[i][1].ToString().Trim(),
                    name = topList.data.items[i][3].ToString().Trim()
                };
                try
                {
                    await _db.AddAsync(mp);
                    await _db.SaveChangesAsync();
                }
                catch
                {

                }
            }

        }

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> LimitUp(int days, DateTime startDate, DateTime endDate, string sort = "放量 desc")
        {
            var l = await _db.LimitUp.FromSqlRaw(" select * from limit_up where alert_date >= '"
                + startDate.ToShortDateString() + "' and alert_date <= '" + endDate.ToShortDateString() + "' "
                + " and exists ( select 'a' from month_promote_stock where month_promote_stock.gid =  limit_up.gid "
                + "and month = convert(varchar(4),year(limit_up.alert_date)) + SUBSTRING('00', 1, 2 - len(convert(varchar(2), month(limit_up.alert_date)))) + convert(varchar(2), month(limit_up.alert_date))  ) ")
                .AsNoTracking().ToListAsync();
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            //dt.Columns.Add("理由", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            //dt.Columns.Add("流入", Type.GetType("System.Double"));
            //dt.Columns.Add("大单流入", Type.GetType("System.Double"));
            for (int i = 0; i < l.Count; i++)
            {
                Stock s = Stock.GetStock(l[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                    s.LoadDealCount();
                }
                catch
                {

                }
                int alertIndex = -1;
                try
                {
                    alertIndex = s.GetItemIndex(l[i].alert_date.Date);
                }
                catch
                {
                    continue;
                }
                if (alertIndex <= 0 || alertIndex >= s.klineDay.Length - 1)
                {
                    continue;
                }
                DataRow dr = dt.NewRow();
                dr["代码"] = s.gid;
                dr["日期"] = s.klineDay[alertIndex].settleTime.Date;
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["买入"] = s.klineDay[alertIndex].settle;
                dt.Rows.Add(dr);
            }
            StockFilter sfNew = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), days);
            try
            {
                return Ok(sfNew);
            }
            catch
            {
                return NotFound();

            }
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> MACD(int days, DateTime startDate, DateTime endDate, string sort = "放量 desc")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            //dt.Columns.Add("理由", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("流入", Type.GetType("System.Double"));
            dt.Columns.Add("大单流入", Type.GetType("System.Double"));
            string startMonth = startDate.Year.ToString() + startDate.Month.ToString().PadLeft(2, '0');
            string endMonth = endDate.Year.ToString() + endDate.Month.ToString().PadLeft(2, '0');
            var l = await _db.monthStock.FromSqlRaw(" select *  from month_promote_stock "
                + " where month >= '" + startMonth + "' and month <= '" + endMonth + "' ")
                .AsNoTracking().ToListAsync();
            for (int i = 0; i < l.Count; i++)
            {
                Stock s = Stock.GetStock(l[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                    s.LoadDealCount();
                }
                catch
                {
                    continue;
                }
                DateTime alertDate = DateTime.Parse(l[i].month.Substring(0, 4) + "-" + l[i].month.Substring(4, 2) + "-01");
                int alertIndex = s.GetItemIndex(alertDate);
                for (int j = alertIndex; j < s.klineDay.Length; j++)
                {
                    if (s.macdDays(j) != 0)
                    {
                        continue;
                    }
                    DateTime currentDate = s.klineDay[j].settleTime.Date;
                    //string currentMonth = currentDate.Year.ToString() + currentDate.Month.ToString().PadLeft(2, '0');
                    if (currentDate < startDate || currentDate > endDate)
                    {
                        continue;
                    }
                    if (alertDate.Year !=  currentDate.Year || alertDate.Month != currentDate.Month)
                    {
                        continue;
                    }

                    DataRow[] drArr = dt.Select(" 日期 = '" + s.klineDay[j].settleTime.ToShortDateString() + "' and 代码 = '" + s.gid.Trim() + "' ");
                    if (drArr.Length > 0)
                    {
                        continue;
                    }

                    DataRow dr = dt.NewRow();

                    dr["代码"] = s.gid;
                    dr["日期"] = s.klineDay[j].settleTime.Date;
                    dr["名称"] = s.name.Trim();
                    dr["信号"] = "";
                    dr["买入"] = s.klineDay[j].settle;


                    double bigBuying = 0;
                    double buying = 0;

                    if (s.klineDay[alertIndex].currentDealCount != null)
                    {
                        bigBuying = s.klineDay[alertIndex].currentDealCount.net_huge_volume
                            + s.klineDay[alertIndex].currentDealCount.net_big_volume;
                        buying = bigBuying + s.klineDay[alertIndex].currentDealCount.net_mid_volume
                            + s.klineDay[alertIndex].currentDealCount.net_small_volume;


                    }
                    if (bigBuying == 0 && buying == 0)
                    {
                        buying = s.klineDay[alertIndex].net_mf_vol / 100;
                    }

                    double bigFlowIn = 10000 * bigBuying / s.klineDay[alertIndex].volume;
                    double flowIn = 10000 * buying / s.klineDay[alertIndex].volume;
                    if (bigFlowIn < 0 || flowIn < 0)
                    {
                        continue;
                    }
                    dr["大单流入"] = bigFlowIn;
                    dr["流入"] = flowIn;
                    dt.Rows.Add(dr);
                }

                

            }
            StockFilter sfNew = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), days);
            try
            {
                return Ok(sfNew);
            }
            catch
            {
                return NotFound();

            }

        }
    }
}

