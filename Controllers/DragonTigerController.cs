using System;
using System.Linq;
using Microsoft.AspNetCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuqinOfficialAccount.Models;
using Microsoft.Extensions.Configuration;
using System.Data;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DragonTigerController:ControllerBase
	{
        private readonly AppDBContext _db;
        private readonly IConfiguration _config;
        private readonly Settings _settings;
        private readonly ChipController _chipCtrl;
        private readonly ConceptController _conceptCtrl;

        private readonly string token = "4da2fbec9c2cee373d3aace9f9e200a315a2812dc11267c425010cec";
        private class DataStruct
        {
            public string[] fields { get; set; }
            public object[][] items { get; set; }
            public bool has_more { get; set; }
        }

        private class TopListStruct
        {
            
            public string request_id { get; set; }
            public string code { get; set; }
            public string msg { get; set; }
            public DataStruct data { get; set; }

        }

       


        public DragonTigerController(AppDBContext context, IConfiguration config)
		{
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _chipCtrl = new ChipController(_db, _config);
            _conceptCtrl = new ConceptController(context, config);
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> GetList(int days, DateTime startDate, DateTime endDate, string sort = "放量 desc")
        {
            startDate = Util.GetLastTransactDate(startDate, 1, _db);
            endDate = Util.GetLastTransactDate(endDate, 1, _db);
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("理由", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("流入", Type.GetType("System.Double"));
            dt.Columns.Add("大单流入", Type.GetType("System.Double"));

            var l = await _db.dragonTiger.Where(d => (d.alert_date >= startDate.Date
                && d.alert_date <= endDate.Date && d.net_amount > 0 && d.pct_change > -9.3))
                .OrderByDescending(d => d.alert_date).AsNoTracking().ToListAsync();
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
                if (alertIndex <= 0 || alertIndex >= s.klineDay.Length-1)
                {
                    continue;
                }
                if (s.klineDay[alertIndex].high == s.klineDay[alertIndex].open
                    && s.klineDay[alertIndex].high == s.klineDay[alertIndex].settle)
                {
                    continue;
                }

                string reason = l[i].reason.Trim();
                if (reason.IndexOf("ST") >= 0 || reason.IndexOf("退") >= 0)
                {
                    continue;
                }
                DataRow[] drArr = dt.Select(" 代码 = '" + s.gid + "' and 日期 = '" + s.klineDay[alertIndex+1].settleTime.ToShortDateString() + "' ");
                if (drArr.Length > 0)
                {
                    drArr[0]["理由"] = drArr[0]["理由"].ToString() + "," + l[i].reason.Trim();
                    continue;
                }
                if (s.klineDay[alertIndex + 1].low > s.klineDay[alertIndex].settle)
                {
                    continue;
                }
                if (s.name.ToUpper().IndexOf("ST") >= 0)
                {
                    continue;
                }

                DataRow dr = dt.NewRow();
                dr["代码"] = s.gid;
                dr["日期"] = s.klineDay[alertIndex+1].settleTime.Date;
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["理由"] = l[i].reason.Trim();
                dr["买入"] = s.klineDay[alertIndex].settle;
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

        [HttpGet]
        public async Task GetTopListForDays(DateTime startDate, DateTime endDate)
        {
            for (DateTime i = startDate; i < endDate; i = i.AddDays(1))
            {
                if (!Util.IsTransacDay(i, _db))
                {
                    continue;
                }
                await GetTopList(i);
            }
            
        }


        [HttpGet]
        public async Task GetTopList(DateTime date)
        {
            string postData = "{\n    \"api_name\": \"top_list\",\n    \"token\": \""
                + token + "\",\n    \"params\": {\"trade_date\": \"" + date.ToString("yyyyMMdd")
                + "\"},\n    \"fields\":\"\"\n\n}";
            string retJson = Util.GetWebContent("http://api.tushare.pro", postData);
            TopListStruct topList = JsonConvert.DeserializeObject<TopListStruct>(retJson);
            for (int i = 0; i < topList.data.items.Length; i++)
            {
                string[] fields = topList.data.fields;
                object[] items = topList.data.items[i];
                DragonTiger dt = GetDragonTigerFromData(items, fields);
                string gid = dt.gid.Substring(2, 6) + "." + dt.gid.Substring(0, 2).ToUpper();
                dt.details = GetTopInst(date, gid);

                
                try
                {
                    await _db.dragonTiger.AddAsync(dt);
                    await _db.SaveChangesAsync();
                    for (int j = 0; j < dt.details.Count; j++)
                    {
                        dt.details[j].dragon_tiger_list_id = dt.id;
                        await _db.dragonTigerDetail.AddAsync(dt.details[j]);
                    }
                    await _db.SaveChangesAsync();
                }
                catch(Exception err)
                {
                    Console.WriteLine(err.ToString());
                }

                //Console.WriteLine(topList.data.items[i][0].ToString());
            }
        }

        [NonAction]
        public List<DragonTigerDetail> GetTopInst(DateTime date, string gid)
        {
            List<DragonTigerDetail> list = new List<DragonTigerDetail>();

            string postData = "{\n    \"api_name\": \"top_inst\",\n    \"token\": \""
                + token + "\",\n    \"params\": {\"trade_date\": \"" + date.ToString("yyyyMMdd")
                + "\", \"ts_code\": \"" + gid + "\"},\n    \"fields\":\"\"\n\n}";
            string retJson = Util.GetWebContent("http://api.tushare.pro", postData);
            TopListStruct topList = JsonConvert.DeserializeObject<TopListStruct>(retJson);
            for (int i = 0; i < topList.data.items.Length; i++)
            {
                DragonTigerDetail dtl = GetDraonTigerDetailFromData(topList.data.items[i], topList.data.fields);
                list.Add(dtl);
                Console.WriteLine(topList.data.items[i][0].ToString());
            }
            return list;
        }

        [NonAction]
        public DragonTigerDetail GetDraonTigerDetailFromData(object[] item, string[] fields)
        {
            if (item.Length != fields.Length)
            {
                return null;
            }
            DragonTigerDetail dt = new DragonTigerDetail();
            for (int i = 0; i < item.Length; i++)
            {
                string value = "";
                try
                {
                    value = item[i].ToString();
                }
                catch
                {
                    value = "";
                }
                switch (fields[i].Trim())
                {
                    case "trade_date":
                        //string dateStr = item[i].ToString();
                        dt.alert_date = DateTime.Parse(value.Substring(0, 4) + "-" + value.Substring(4, 2) + "-" + value.Substring(6, 2));
                        break;
                    case "ts_code":
                        dt.gid = value.Substring(7, 2).ToLower() + value.Substring(0, 6);
                        break;
                    case "exalter":
                        dt.exalter = value.Trim();
                        break;
                    case "buy":
                        try
                        {
                            dt.buy = double.Parse(value);
                        }
                        catch
                        {
                            dt.buy = 0;
                        }
                        break;
                    case "buy_rate":
                        try
                        {
                            dt.buy_rate = double.Parse(value);
                        }
                        catch
                        {
                            dt.buy_rate = 0;
                        }
                        break;


                    case "sell":
                        try
                        {
                            dt.sell = double.Parse(value);
                        }
                        catch
                        {
                            dt.sell = 0;
                        }
                        break;

                    case "sell_rate":
                        try
                        {
                            dt.sell_rate = double.Parse(value);
                        }
                        catch
                        {
                            dt.sell_rate = 0;
                        }
                        break;

                    case "net_buy":
                        try
                        {
                            dt.net_buy = double.Parse(value);
                        }
                        catch
                        {
                            dt.net_buy = 0;
                        }
                        break;

                    case "side":
                        try
                        {
                            dt.side = int.Parse(value);
                        }
                        catch
                        {
                            dt.side = 0;
                        }
                        break;

                    

                    

                  
                    case "reason":
                        dt.reason = value.Trim();
                        break;
                    default:
                        break;
                }

            }
            return dt;
        }

        [NonAction]
        public DragonTiger GetDragonTigerFromData(object[] item, string[] fields)
        {
            if (item.Length != fields.Length)
            {
                return null;
            }
            DragonTiger dt = new DragonTiger();
            for (int i = 0; i < item.Length; i++)
            {
                string value = item[i].ToString();
                switch (fields[i].Trim())
                {
                    case "trade_date":
                        //string dateStr = item[i].ToString();
                        dt.alert_date = DateTime.Parse(value.Substring(0, 4) + "-" + value.Substring(4, 2) + "-" + value.Substring(6, 2));
                        break;
                    case "ts_code":
                        dt.gid = value.Substring(7, 2).ToLower() + value.Substring(0, 6);
                        break;
                    case "close":
                        try
                        {
                            dt.close = double.Parse(value);
                        }
                        catch
                        {
                            dt.close = 0;
                        }
                        break;
                    case "pct_change":
                        try
                        {
                            dt.pct_change = double.Parse(value);
                        }
                        catch
                        {
                            dt.pct_change = 0;
                        }
                        break;
                    case "turnover_rate":
                        try
                        {
                            dt.turnover_rate = double.Parse(value);
                        }
                        catch
                        {
                            dt.turnover_rate = 0;
                        }
                        break;


                    case "amount":
                        try
                        {
                            dt.amount = double.Parse(value);
                        }
                        catch
                        {
                            dt.amount = 0;
                        }
                        break;

                    case "l_sell":
                        try
                        {
                            dt.l_sell = double.Parse(value);
                        }
                        catch
                        {
                            dt.l_sell = 0;
                        }
                        break;

                    case "l_buy":
                        try
                        {
                            dt.l_buy = double.Parse(value);
                        }
                        catch
                        {
                            dt.l_buy = 0;
                        }
                        break;

                    case "l_amount":
                        try
                        {
                            dt.l_amount = double.Parse(value);
                        }
                        catch
                        {
                            dt.l_amount = 0;
                        }
                        break;

                    case "net_amount":
                        try
                        {
                            dt.net_amount = double.Parse(value);
                        }
                        catch
                        {
                            dt.net_amount = 0;
                        }
                        break;

                    case "net_rate":
                        try
                        {
                            dt.net_rate = double.Parse(value);
                        }
                        catch
                        {
                            dt.net_rate = 0;
                        }
                        break;

                    case "amount_rate":
                        try
                        {
                            dt.amount_rate = double.Parse(value);
                        }
                        catch
                        {
                            dt.amount_rate = 0;
                        }
                        break;

                    case "flow_values":
                        try
                        {
                            dt.float_value = double.Parse(value);
                        }
                        catch
                        {
                            dt.float_value = 0;
                        }
                        break;

                    case "reason":
                        dt.reason = value.Trim();
                        break;
                    default:
                        break;
                }

            }
            return dt;

        }
    }
}

