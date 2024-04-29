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

