using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuqinOfficialAccount.Models;
using Microsoft.Extensions.Configuration;

using System.Data;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class StockController: ControllerBase
	{
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        public StockController(AppDBContext context, IConfiguration config)
		{
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [HttpGet]
        public  ActionResult<Stock> GetStock(string gid)
        {
            bool find = false;
            Stock s = new Stock();
            Stock[] sArr = Util.stockList;


            for (int i = 0; i < sArr.Length; i++)
            {
                s = sArr[i];
                if (s.gid.Trim().Equals(gid) || (s.gid.EndsWith(gid.Trim()) && gid.Length == 6))
                {
                    s.RefreshKLine();
                    find = true;
                    break;
                }
            }

            if (find)
            {
                return new OkObjectResult(s);
            }
            else
            {
                return new NotFoundResult();
            }
            
        }

        
        [HttpGet("{days}")]
        public   ActionResult<StockFilter> BreakGate(int days, DateTime startDate, DateTime endDate, string sort = "")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            Stock[] stockArr = Util.stockList;
            for (var i = 0; i < stockArr.Length; i++)
            {
                Stock s = stockArr[i];
                s.RefreshKLine();
                int startIndex = s.GetItemIndex(startDate.Date);
                int endIndex = s.GetItemIndex(endDate.Date);
                if (startIndex < 0 || endIndex < startIndex)
                {
                    continue;
                }
                for (int j = startIndex; j <= endIndex && j < s.klineDay.Length; j++)
                {
                    KLine k = s.klineDay[j];
                    if ((k.high > 100 && k.low < 100) && k.open < k.settle)
                    {
                        bool isTop = true;

                        for (int m = j - 1; m >= 0; m--)
                        {
                            if (s.klineDay[m].high >= 100)
                            {
                                isTop = false;
                                break;
                            }
                        }
                        if (!isTop)
                        {
                            continue;
                        }
                        DataRow dr = dt.NewRow();
                        dr["日期"] = k.settleTime.Date;
                        dr["代码"] = s.gid;
                        dr["名称"] = s.name;
                        dr["买入"] = k.settle;
                        dr["信号"] = "";
                        dt.Rows.Add(dr);
                    }
                }
            }

            StockFilter sf = StockFilter.GetResult(dt.Select("", sort), 15);
            return Ok(sf);
        }
        



    }
}

