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
namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class StockController
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
        public ActionResult<Stock> GetStock(string gid)
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
                return Ok(s);
            }
            else
            {
                return new NotFoundResult();
            }
            
        }
	}
}

