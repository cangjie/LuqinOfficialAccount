using System;
using System.Threading.Tasks;
using LuqinOfficialAccount.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Collections.Generic;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DealCountController : ControllerBase
	{
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        public DealCountController(AppDBContext context, IConfiguration config)
		{
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            Util._db = context;
        }




        [HttpGet("{gid}")]
        public ActionResult<List<DealCount>> GetDealCount(string gid)
        {
            List<DealCount> dArr = new List<DealCount>();
            Stock s = Stock.GetStock(gid);
            s.ForceRefreshKLineDay();
            s.LoadDealCount();
            for (int i = s.klineDay.Length - 1; i >= 0; i--)
            {
                if (s.klineDay[i].currentDealCount != null)
                {
                    DealCount d = s.klineDay[i].currentDealCount;
                    d.settleTime = d.settleTime.Date;
                    dArr.Add(d);
                    for (int j = s.klineDay[i].dealCount30Min.Count - 1; j >= 0; j--)
                    {
                        dArr.Add(s.klineDay[i].dealCount30Min[j]);
                    }
                }
            }
            return Ok(dArr);
        }

        private StockFilter AddDealCount(StockFilter sf, string sort, int days)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("流入", Type.GetType("System.Double"));
            dt.Columns.Add("大单流入", Type.GetType("System.Double"));

            for (int i = 0; sf != null && sf.itemList != null && i < sf.itemList.Count; i++)
            {
                StockFilter.Item item = sf.itemList[i];
                Stock s = Stock.GetStock(item.gid);
                try
                {
                    s.LoadDealCount();
                }
                catch
                {

                }
                int buyIndex = s.GetItemIndex(item.alertDate) + 1;

                if (KLine.IsLimitUp(s.klineDay, s.gid, buyIndex))
                {
                    continue;
                }

                if (buyIndex < 0 || buyIndex >= s.klineDay.Length)
                {
                    continue;
                }
                
                double bigBuying = 0;
                double buying = 0;

                if (s.klineDay[buyIndex].currentDealCount != null)
                {
                    bigBuying = s.klineDay[buyIndex].currentDealCount.net_huge_volume
                        + s.klineDay[buyIndex].currentDealCount.net_big_volume;
                    buying = bigBuying + s.klineDay[buyIndex].currentDealCount.net_mid_volume
                        + s.klineDay[buyIndex].currentDealCount.net_small_volume;


                }
                if (bigBuying == 0 && buying == 0)
                {
                    buying = s.klineDay[buyIndex].net_mf_vol / 100;
                }

                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = item.gid;
                dr["名称"] = item.name.Trim();
                dr["信号"] = item.signal;
                dr["买入"] = s.klineDay[buyIndex].settle;
                dr["大单流入"] = 10000 * bigBuying / s.klineDay[buyIndex].volume;
                double flowIn = 10000 * buying / s.klineDay[buyIndex].volume;
                dr["流入"] = flowIn;
                dt.Rows.Add(dr);


            }
            StockFilter newSf = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), days);
            return newSf;
        }


        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> Bread(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("调整天数", Type.GetType("System.Int32"));
            dt.Columns.Add("流入", Type.GetType("System.Double"));
            dt.Columns.Add("大单流入", Type.GetType("System.Double"));

            DateTime oriStartDate = startDate;
            DateTime oriEndDate = endDate;

            startDate = Util.GetLastTransactDate(startDate, 10, _db);
            endDate = Util.GetLastTransactDate(endDate, 1, _db);
            LimitUpController limitUpHelper = new LimitUpController(_db, _config);
            StockFilter sf = (StockFilter)((OkObjectResult)(await limitUpHelper.GetLimitUpTwice(0, startDate, endDate, "代码")).Result).Value;
            for (int i = 0; i < sf.itemList.Count; i++)
            {
                Stock s = Stock.GetStock(sf.itemList[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                    s.LoadDealCount();
                }
                catch
                {

                }
                int alertIndex = s.GetItemIndex(sf.itemList[i].alertDate.Date);
                if (alertIndex <= 1 || alertIndex > s.klineDay.Length)
                {
                    continue;
                }
                int highIndex = alertIndex;
                bool below3Line = false;
                for (int j = alertIndex; j < s.klineDay.Length - 1; j++)
                {
                    if (s.klineDay[j].high > s.klineDay[highIndex].high)
                    {
                        highIndex = j;
                        //break;
                    }
                    if (s.klineDay[j].settle < KLine.GetAverageSettlePrice(s.klineDay, j, 3, 3))
                    {
                        below3Line = true;
                        break;
                    }
                }
                if (!below3Line)
                {
                    continue;
                }
                for (int j = highIndex + 1; j < s.klineDay.Length; j++)
                {
                    if (s.klineDay[j].currentDealCount == null)
                    {
                        continue;
                    }
                    if (s.klineDay[j].settleTime.Date < oriStartDate || s.klineDay[j].settleTime.Date > oriEndDate)
                    {
                        continue;
                    }
                    if (dt.Select(" 代码 = '" + s.gid + "' and 日期 = '" + s.klineDay[j].settleTime.ToShortDateString() + "' ").Length > 0)
                    {
                        continue;
                    }
                    if (s.klineDay[j].currentDealCount.net_huge_volume > 0
                        || (s.klineDay[j].currentDealCount.net_big_volume + s.klineDay[j].currentDealCount.net_huge_volume) > 0
                        || (s.klineDay[j].currentDealCount.net_big_volume + s.klineDay[j].currentDealCount.net_huge_volume
                            + s.klineDay[j].currentDealCount.net_mid_volume + s.klineDay[j].currentDealCount.net_small_volume > 0))
                    {

                        if (j <= 0 || (s.klineDay[j].settle - s.klineDay[j - 1].settle) / s.klineDay[j - 1].settle <= -0.09)
                        {
                            continue;
                        }
                        DataRow dr = dt.NewRow();
                        dr["日期"] = s.klineDay[j].settleTime.Date;
                        dr["代码"] = s.gid;
                        dr["名称"] = s.name.Trim();
                        dr["信号"] = "";
                        dr["买入"] = s.klineDay[j].settle;
                        dr["调整天数"] = j - highIndex;
                        if (s.klineDay[j].volume == 0)
                        {
                            continue;
                        }
                        double flowIn = 10000 * (s.klineDay[j].currentDealCount.net_big_volume + s.klineDay[j].currentDealCount.net_huge_volume
                            + s.klineDay[j].currentDealCount.net_mid_volume + s.klineDay[j].currentDealCount.net_small_volume) / s.klineDay[j].volume;
                        double bigFlowIn = 10000 * (s.klineDay[j].currentDealCount.net_big_volume + s.klineDay[j].currentDealCount.net_huge_volume) / s.klineDay[j].volume;
                        flowIn = Math.Round(flowIn, 2);
                        bigFlowIn = Math.Round(bigFlowIn, 2);
                        if (flowIn < 10 && bigFlowIn < 10)
                        {
                            continue;
                        }
                        dr["流入"] = flowIn;
                        dr["大单流入"] = bigFlowIn;
                        dt.Rows.Add(dr);
                        break;
                    }
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



        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> Reverse(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            startDate = Util.GetLastTransactDate(startDate, 1, _db);
            endDate = Util.GetLastTransactDate(endDate, 1, _db);
            LimitUpController limitUpHelper = new LimitUpController(_db, _config);
            StockFilter sf = (StockFilter)((OkObjectResult)(await limitUpHelper.Reverse(days, startDate, endDate, "代码")).Result).Value;
            sf = AddDealCount(sf, sort, days);
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
        public async Task<ActionResult<StockFilter>> LimitUpTwice(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            startDate = Util.GetLastTransactDate(startDate, 1, _db);
            endDate = Util.GetLastTransactDate(endDate, 1, _db);
            LimitUpController limitUpHelper = new LimitUpController(_db, _config);
            StockFilter sf = (StockFilter)((OkObjectResult)(await limitUpHelper.GetLimitUpTwice(days, startDate, endDate, "代码")).Result).Value;
            sf = AddDealCount(sf, sort, days);
            try
            {
                return Ok(sf);
            }
            catch
            {
                return NotFound();
            }
        }

        
    }
}

