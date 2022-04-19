using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace LuqinOfficialAccount.Controllers.Api
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class UserController:ControllerBase
    {
        private readonly AppDBContext _context;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        public UserController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [HttpGet]
        public async Task<ActionResult<bool>> CheckToken(string token)
        {
            token = Util.UrlEncode(token);
            long currentTimeStamp = long.Parse(Util.GetLongTimeStamp(DateTime.Now));
            var tokenList = await _context.token.Where(t =>
            (t.state == 1 && currentTimeStamp < t.expire_timestamp
            && t.token.Trim().Equals(token.Trim()) && t.original_id.Trim().Equals(_settings.originalId.Trim())))
                .ToListAsync();
            if (tokenList.Count > 0)
            {
                string openId = tokenList[0].open_id.Trim();
                CheckUser(openId.Trim());
                return true;
            }
            return false;
        }

        [NonAction]
        public string GetUserOpenId(string token)
        {
            //token = Util.UrlEncode(token);
            long currentTimeStamp = long.Parse(Util.GetLongTimeStamp(DateTime.Now));
            var tokenList = _context.token.Where(t =>
            (t.state == 1 && currentTimeStamp < t.expire_timestamp
            && t.token.Trim().Equals(token.Trim()) && t.original_id.Trim().Equals(_settings.originalId.Trim()))).ToList();
            if (tokenList.Count > 0)
            {
                return tokenList[0].open_id.Trim();
            }
            else
            {
                return "";
            }
                
        }

        [NonAction]
        public int SetToken(string token, string openId, int expireSeconds)
        {
            
            int userId = CheckUser(openId);
            if (userId == 0)
            {
                return 0;
            }
            long currentTimeStamp = long.Parse(Util.GetLongTimeStamp(DateTime.Now));
            var tokenList = _context.token.Where(t => (
                t.original_id.Trim().Equals(_settings.originalId)
                && t.open_id.Trim().Equals(openId)
                && (t.state == 1 || t.expire_timestamp <= currentTimeStamp)
            )).ToList();
            for (int i = 0; i < tokenList.Count; i++)
            {
                Token tmpToken = tokenList[i];
                tmpToken.state = 0;
                _context.Entry(tmpToken).State = EntityState.Modified;
                _context.SaveChanges();

            }
            Token newToken = new Token()
            {
                id = 0,
                original_id = _settings.originalId.Trim(),
                open_id = openId.Trim(),
                token = token.Trim(),
                expire_timestamp = currentTimeStamp + 1000 * (expireSeconds - 720),
                user_id = userId,
                state = 1
            };
            _context.token.Add(newToken);
            _context.SaveChanges();

            return newToken.id;
        }
        
        [HttpGet]
        public void SetTokenInSession(string token)
        {
            HttpContext.Session.SetString("token", "test");
        }
        
        [NonAction]
        public  int CheckUser(string openId)
        {
            int userId = 0;
            var oaUserList =  _context.oAUser.Where(u => (u.original_id.Trim().Equals(_settings.originalId.Trim())
                && u.open_id.Trim().Equals(openId.Trim()))).ToList();
            if (oaUserList.Count == 0)
            {
                OfficialAccountApi oaApi = new OfficialAccountApi(_context, _config);
                string unionId = oaApi.GetUnionId(openId.Trim()).Value.Trim();
                if (unionId.Trim().Equals(""))
                {
                    return 0;
                }
                var userList = _context.user.Where(u => u.oa_union_id.Trim().Equals(unionId)).ToList();
                if (userList.Count == 0)
                {
                    User user = new User()
                    {
                        id = 0,
                        oa_union_id = unionId.Trim()
                    };
                    _context.user.Add(user);
                    _context.SaveChanges();

                    userId = user.id;
                }
                else
                {
                    userId = userList[0].id;
                }
                if (userId == 0)
                {
                    return 0;
                }
                OAUser oaUser = new OAUser()
                {
                    id = 0,
                    user_id = userId,
                    open_id = openId.Trim(),
                    original_id = _settings.originalId.Trim()
                };
                _context.oAUser.Add(oaUser);
                _context.SaveChanges();
                if (oaUser.id == 0)
                {
                    return 0;
                }
            }
            else
            {
                userId = oaUserList[0].user_id;
            }
     
            return userId;
        }
    }
}
