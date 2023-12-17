using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuqinOfficialAccount;
using LuqinOfficialAccount.Models;
using Microsoft.Extensions.Configuration;
using System.Data;
using Newtonsoft.Json;
//using Microsoft.AspNe
namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BakDailyController: ControllerBase
    {
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly string tushareToken = "4da2fbec9c2cee373d3aace9f9e200a315a2812dc11267c425010cec";

        private readonly string tushareUrl = "http://api.tushare.pro";

        private class BakDailyResponse
        {
            public class BakDailyResponseData
            {
                public string[] fields { get; set; }
                public object[][] items { get; set; } 
            }
            public BakDailyResponseData data { get; set; }
        }

        public BakDailyController(AppDBContext context, IConfiguration config)
		{
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [HttpGet]
        public async Task<ActionResult<int>> GetBakDaily(DateTime date)
        {
            int pageSize = 1000;
            int currentPage = 0;
            int offset = pageSize * currentPage;
            int count = 0;
            for (; ; )
            {
                try
                {
                    string dateStr = date.Year.ToString()
                    + "00".Substring(0, 2 - date.Month.ToString().Length) + date.Month.ToString()
                    + "00".Substring(0, 2 - date.Day.ToString().Length) + date.Day.ToString();
                    string postData = "{\n    \"api_name\": \"bak_daily\",\n    \"token\": \"" + tushareToken + "\",\n    \"params\":{\n        \"trade_date\": \"" + dateStr + "\",\n        \"offset\": " + offset.ToString() + ",\n        \"limit\": " + pageSize.ToString() + "\n    },\n    \"fields\":[\n        \"ts_code\",\n    \"trade_date\",\n    \"name\",\n    \"pct_change\",\n    \"close\",\n    \"change\",\n    \"open\",\n    \"high\",\n    \"low\",\n    \"pre_close\",\n    \"vol_ratio\",\n    \"turn_over\",\n    \"swing\",\n    \"vol\",\n    \"amount\",\n    \"selling\",\n    \"buying\",\n    \"total_share\",\n    \"float_share\",\n    \"pe\",\n    \"industry\",\n    \"area\",\n    \"float_mv\",\n    \"total_mv\",\n    \"avg_price\",\n    \"strength\",\n    \"activity\",\n    \"avg_turnover\",\n    \"attack\",\n    \"interval_3\",\n    \"interval_6\"\n    ]\n}";
                    string retJson = Util.GetWebContent(tushareUrl, postData);
                    BakDailyResponse res = JsonConvert.DeserializeObject<BakDailyResponse>(retJson);
                    if (res.data == null || res.data.items.Length < pageSize || currentPage >= 10)
                    {
                        break;
                    }
                    for (int i = 0; i < res.data.items.Length; i++)
                    {
                        try
                        {
                            object[] item = res.data.items[i];
                            string[] gidArr = item[0].ToString().Split('.');
                            string gid = gidArr[1].ToLower() + gidArr[0];
                            bak_daily bd = new bak_daily()
                            {
                                id = 0,
                                gid = gid,
                                alert_date = date,
                                name = item[2].ToString().Trim(),
                                pct_change = double.Parse(item[3].ToString().Trim()),
                                close = double.Parse(item[4].ToString().Trim()),
                                open = double.Parse(item[6].ToString().Trim()),
                                high = double.Parse(item[7].ToString().Trim()),
                                low = double.Parse(item[8].ToString().Trim()),
                                pre_close = double.Parse(item[9].ToString().Trim()),
                                vol_ratio = double.Parse(item[10].ToString().Trim()),
                                turn_over = double.Parse(item[11].ToString().Trim()),
                                vol = double.Parse(item[13].ToString().Trim()),
                                selling = double.Parse(item[15].ToString().Trim()),
                                buying = double.Parse(item[16].ToString().Trim()),
                                indurstry = item[20].ToString().Trim(),
                                area = item[21].ToString().Trim(),
                                strength = double.Parse(item[25].ToString().Trim()),
                                activity = double.Parse(item[26].ToString().Trim())
                            };
                            var l = await _db.bakDaily.Where(b => b.alert_date.Date == date.Date
                                && b.gid.Trim().Equals(bd.gid.Trim())).AsNoTracking()
                                .ToListAsync();
                            if (l.Count == 0)
                            {
                                await _db.bakDaily.AddAsync(bd);
                                count++;
                                //await _db.SaveChangesAsync();
                            }
                            

                        }
                        catch(Exception err)
                        {
                            Console.WriteLine(err.ToString());
                        }
                    }
                    await _db.SaveChangesAsync();
                    currentPage++;
                    offset = pageSize * currentPage;
                }
                catch(Exception err)
                {
                    Console.WriteLine(err.ToString());
                }
            }


       
            return Ok(count);
        }
	}
}

