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
using Newtonsoft.Json;
using System.Data;
using System.Security.AccessControl;
using System.Net.Mime;
//using Microsoft.AspNetCore.Http.HttpResults;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ConceptController : ControllerBase
    {
        private readonly AppDBContext _db;
        private readonly IConfiguration _config;
        private readonly Settings _settings;
        private readonly ChipController chipCtrl;
        private readonly string token = "4da2fbec9c2cee373d3aace9f9e200a315a2812dc11267c425010cec";
        private readonly string tushareUrl = "http://api.tushare.pro";

        private class ThsConcept
        {
            public ThsConcetpData data { get; set; }
        }

        private class ThsConcetpData
        { 
            public string[] fields { get; set; }
            public string[][] items { get; set; }
        }

        //public static DateTime now = DateTime.Now;

        public ConceptController(AppDBContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            chipCtrl = new ChipController(_db, _config);
            Util._db = context;
        }

        [HttpGet]
        public async Task<ActionResult<int>> RefreshConcept()
        {
            int k = 0;
            string requestRaw = "{   \"api_name\": \"ths_index\", \"token\": \"" + token.Trim() + "\",\"params\":{   \"exchange\": \"A\"    },\"fields\": \"\"}";
            string ret = Util.GetWebContent(tushareUrl, requestRaw);
            var list = JsonConvert.DeserializeObject<ThsConcept>(ret);
            for (int i = 0; i < list.data.items.Length; i++)
            { 
                var item = list.data.items[i];
                var iList = await _db.Concept.Where(c => c.code.Trim().Equals(item[0].Trim())).ToListAsync();
                if (iList.Count == 0)
                {
                    try
                    {
                        DateTime listDate = DateTime.MinValue;
                        try
                        {
                            listDate = DateTime.Parse(item[4].Trim().Substring(0, 4) + "-" + item[4].Trim().Substring(4, 2) + "-" + item[4].Trim().Substring(6, 2));
                        }
                        catch
                        { 
                        
                        }
                        Concept c = new Concept()
                        {
                            id = 0,
                            code = item[0].Trim(),
                            name = item[1].Trim(),
                            count = int.Parse(item[2].Trim()),
                            list_date = listDate,
                            type = item[5].Trim()
                        };
                        await _db.AddAsync(c);
                        await _db.SaveChangesAsync();
                        k++;
                    }
                    catch
                    { 
                    
                    }
                }
                else
                {
                    try
                    {
                        DateTime listDate = DateTime.MinValue;
                        try
                        {
                            listDate = DateTime.Parse(item[4].Trim().Substring(0, 4) + "-" + item[4].Trim().Substring(4, 2) + "-" + item[4].Trim().Substring(6, 2));
                        }
                        catch
                        {

                        }
                        Concept concept = (Concept)iList[0];
                        concept.name = item[1].Trim();
                        concept.count = int.Parse(item[2].Trim());
                        concept.list_date = listDate;
                        concept.type = item[5].Trim();
                        concept.update_date = DateTime.Now;
                        _db.Entry(concept).State = EntityState.Modified;
                        await _db.SaveChangesAsync();
                        k++;
                    }
                    catch
                    { 
                    
                    }
                }
            }
            return Ok(k);
        }

        [HttpGet]
        public async Task<ActionResult<int>> RefreshConceptMember()
        {
            int j = 0;
            var conceptList = await _db.Concept.ToListAsync();
            for (int i = 0; i < conceptList.Count; i++)
            {
                string reqJson = "{ \"api_name\": \"ths_member\", \"token\": \"" + token + "\",\"params\":{ \"ts_code\": \"" + conceptList[i].code.Trim() +"\"    },\"fields\": \"\"}";
                string ret = Util.GetWebContent(tushareUrl, reqJson);
                var memberList = JsonConvert.DeserializeObject<ThsConcept>(ret);
                for (int k = 0; k < memberList.data.items.Length; k++)
                {
                    string conceptCode = memberList.data.items[k][0].Trim();
                    string memberCode = memberList.data.items[k][1].Trim();
                    string memberName = memberList.data.items[k][2].Trim();
                    if (!memberCode.ToLower().EndsWith(".sh") && !memberCode.ToLower().EndsWith(".sz"))
                    {
                        continue;
                    }
                    var cMemberList = await _db.ConceptMember
                        .Where(m => (m.concept_code.Trim().Equals(conceptCode) && m.member_code.Trim().Equals(memberCode.Trim())))
                        .ToListAsync();
                    if (cMemberList.Count == 0)
                    {
                        ConceptMember cm = new ConceptMember()
                        {
                            id = 0,
                            concept_code = conceptCode.Trim(),
                            member_code = memberCode.Trim(),
                            member_name = memberName.Trim()
                        };
                        await _db.AddAsync(cm);
                        j++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            return Ok(j);
        }

        
        private bool ConceptExists(int id)
        {
            return _db.Concept.Any(e => e.id == id);
        }
    }
}
