using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuqinOfficialAccount.Models;
using Microsoft.Extensions.Configuration;


namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ReverseController : ControllerBase
	{
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly ChipController chipCtrl;

        private readonly ConceptController conceptCtrl;

        private readonly ResultCacheController resultHelper;

        private readonly LimitUpController limitUpHelper;

        public ReverseController(AppDBContext context, IConfiguration config)
		{
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            chipCtrl = new ChipController(_db, _config);
            Util._db = context;
            conceptCtrl = new ConceptController(context, config);
            resultHelper = new ResultCacheController(context, config);
            limitUpHelper = new LimitUpController(context, config);
            _db.Database.SetCommandTimeout(999);
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> OpenOverHigh(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            startDate = Util.GetLastTransactDate(startDate, 1, _db);
            endDate = Util.GetLastTransactDate(endDate, 1, _db);
            StockFilter reverseList = (StockFilter)((OkObjectResult)(await limitUpHelper.Reverse(days, startDate, endDate, sort)).Result).Value;

            for (int i = 0; i < reverseList.itemList.Count; i++)
            {
                Stock s = Stock.GetStock(reverseList.itemList[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(reverseList.itemList[i].alertDate.Date);
                if (alertIndex < 2 || alertIndex >= s.klineDay.Length - 1)
                {
                    continue;
                }
                double highest = 0;
                int prevLimitUpIndex = 0;
                for (int j = alertIndex - 1; j >= 0 && !KLine.IsLimitUp(s.klineDay, j); j--)
                {
                    highest = Math.Max(highest, s.klineDay[j].high);
                    prevLimitUpIndex = j;
                }
                prevLimitUpIndex--;
                if (prevLimitUpIndex < 0)
                {
                    continue;
                }
                highest = Math.Max(highest, s.klineDay[prevLimitUpIndex].high);
                if (s.klineDay[alertIndex + 1].open < highest)
                {
                    continue;
                }
                if (s.klineDay[alertIndex].settle >= highest)
                {
                    continue;
                }
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex+1].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                if (KLine.IsLimitUp(s.klineDay, alertIndex + 1))
                {
                    dr["信号"] = "📈";
                }
                else
                {
                    dr["信号"] = "";
                }
                dr["买入"] = s.klineDay[alertIndex+1].open;
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
        public async Task<ActionResult<StockFilter>> OpenOverHighBackToLastSettle(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            StockFilter reverseList = (StockFilter)((OkObjectResult)(await OpenOverHigh(days, startDate, endDate, sort)).Result).Value;
            for (int i = 0; reverseList != null && i < reverseList.itemList.Count; i++)
            {
                Stock s = Stock.GetStock(reverseList.itemList[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(reverseList.itemList[i].alertDate.Date);
                if (alertIndex < 2 || alertIndex >= s.klineDay.Length)
                {
                    continue;
                }
                if (s.klineDay[alertIndex].low > s.klineDay[alertIndex - 1].settle)
                {
                    continue;
                }
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["买入"] = s.klineDay[alertIndex - 1].settle;
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
        public async Task<ActionResult<StockFilter>> WithT(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));


            StockFilter reverseList = (StockFilter)((OkObjectResult)(await limitUpHelper.Reverse(days, startDate, endDate, sort)).Result).Value;
            for (int i = 0; i < reverseList.itemList.Count; i++)
            {
                Stock s = Stock.GetStock(reverseList.itemList[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(reverseList.itemList[i].alertDate.Date);
                if (alertIndex < 2 || alertIndex >= s.klineDay.Length)
                {
                    continue;
                }
                int prevLimitUpIndex = 0;
                for (int j = alertIndex - 1; j >= 0 && !KLine.IsLimitUp(s.klineDay, j); j--)
                {
                    //highest = Math.Max(highest, s.klineDay[j].high);
                    prevLimitUpIndex = j;
                }
                prevLimitUpIndex--;
                if (prevLimitUpIndex < 0)
                {
                    continue;
                }
                if (s.klineDay[prevLimitUpIndex].open != s.klineDay[prevLimitUpIndex].settle
                    && s.klineDay[alertIndex].open != s.klineDay[alertIndex].settle)
                {
                    continue;
                }
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["买入"] = s.klineDay[alertIndex].settle;
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
        public async Task<ActionResult<StockFilter>> HorseHead(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            startDate = Util.GetLastTransactDate(startDate, 1, _db);
            endDate = Util.GetLastTransactDate(endDate, 1, _db);

            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            StockFilter reverseList = (StockFilter)((OkObjectResult)(await limitUpHelper.Reverse(days, startDate, endDate, sort)).Result).Value;
            for (int i = 0; reverseList != null && i < reverseList.itemList.Count; i++)
            {
                Stock s = Stock.GetStock(reverseList.itemList[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(reverseList.itemList[i].alertDate.Date);
                if (alertIndex < 2 || alertIndex >= s.klineDay.Length - 1)
                {
                    continue;
                }

                if (!KLine.IsLimitUp(s.klineDay, alertIndex - 2))
                {
                    continue;
                }
                if (s.klineDay[alertIndex - 2].settle >= Math.Min(s.klineDay[alertIndex - 1].settle, s.klineDay[alertIndex - 1].open))
                {
                    continue;
                }

                if (s.klineDay[alertIndex + 1].open <= s.klineDay[alertIndex].settle)
                {
                    continue;
                }
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex + 1].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["买入"] = s.klineDay[alertIndex + 1].open;
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
        public async Task<ActionResult<StockFilter>> OpenHigh(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            startDate = Util.GetLastTransactDate(startDate, 1, _db);
            endDate = Util.GetLastTransactDate(endDate, 1, _db);
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("高开", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            StockFilter reverseList = (StockFilter)((OkObjectResult)(await limitUpHelper.Reverse(1, startDate, endDate, sort)).Result).Value;
            for (int i = 0; reverseList != null && i < reverseList.itemList.Count; i++)
            {
                Stock s = Stock.GetStock(reverseList.itemList[i].gid);

                

                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(reverseList.itemList[i].alertDate.Date);
                if (alertIndex < 2 || alertIndex >= s.klineDay.Length - 1)
                {
                    continue;
                }
                double buyPrice = 0;
                double open = s.klineDay[alertIndex + 1].open;
                double settle = s.klineDay[alertIndex].settle;
                double openHighRate = (open - settle) / settle;
                if (open < settle)
                {
                    continue;
                }
                if ((open - settle) / settle < 0.01)
                {
                    buyPrice = open;
                }
                else
                {
                    buyPrice = settle * 1.01;
                    if (s.klineDay[alertIndex + 1].low > buyPrice)
                    {
                        continue;
                    }
                        
                       
                }

                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex + 1].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["高开"] = Math.Round(openHighRate * 100, 2);
                if (KLine.IsLimitUp(s.klineDay, s.gid, alertIndex + 1))
                {
                    dr["信号"] = "📈";
                    if (openHighRate <= 0.03)
                    {
                        dr["信号"] = "3⃣️🥉";
                    }
                    else if (openHighRate <= 0.06)
                    {
                        dr["信号"] = "2⃣️🥈";
                    }
                    else if (openHighRate <= 0.09)
                    {
                        dr["信号"] = "1⃣️🥇";
                    }
                    else
                    {
                        dr["信号"] = "0⃣️🐮";
                    }
                }
                else
                {
                    if (openHighRate <= 0.03)
                    {
                        dr["信号"] = "3⃣️";
                    }
                    else if (openHighRate <= 0.06)
                    {
                        dr["信号"] = "2⃣️";
                    }
                    else if (openHighRate <= 0.09)
                    {
                        dr["信号"] = "1⃣️";
                    }
                    else
                    {
                        dr["信号"] = "0⃣️";
                    }
                    //dr["信号"] = "";

                }
                
                dr["买入"] = buyPrice;
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
        public async Task<ActionResult<StockFilter>> ResumeSettleLossRate(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            //startDate = Util.GetLastTransactDate(startDate, 5, _db);
            //endDate = Util.GetLastTransactDate(endDate, 5, _db);
            StockFilter reverseList = (StockFilter)((OkObjectResult)(await OpenHigh(1, startDate, endDate, sort)).Result).Value;
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            //dt.Columns.Add("高开", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            for (int i = 0; reverseList != null && i < reverseList.itemList.Count; i++)
            {
                Stock s = Stock.GetStock(reverseList.itemList[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                
                int alertIndex = s.GetItemIndex(reverseList.itemList[i].alertDate.Date);
                if (alertIndex < 3 || alertIndex >= s.klineDay.Length - 5)
                {
                    continue;
                }
                if (s.klineDay[alertIndex-1].settle <= s.klineDay[alertIndex].settle)
                {
                    continue;
                }
                int chances = 0;
                for (int j = alertIndex + 1; j < s.klineDay.Length && j <= alertIndex + 5; j++)
                {
                    if (s.klineDay[j].high > s.klineDay[alertIndex - 1].settle)
                    {
                        chances++;
                    }
                }
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                if (chances >= 2)
                {
                    dr["信号"] = "📈";
                }
                else
                {
                    dr["信号"] = "";
                }
                dr["买入"] = s.klineDay[alertIndex-1].settle;
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
    }
}

