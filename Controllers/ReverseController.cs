﻿using System;
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

    }
}

