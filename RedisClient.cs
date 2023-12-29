using System;
using StackExchange.Redis;

namespace LuqinOfficialAccount
{
    public class RedisClient
    {
        public static ConnectionMultiplexer redis;
        public static IDatabase redisDb;

        public static void Init(string address)
        {

            redis = ConnectionMultiplexer.Connect(address);
            redisDb = redis.GetDatabase();
        }
    }
}

