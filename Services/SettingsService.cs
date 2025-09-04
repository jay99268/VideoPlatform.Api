using Microsoft.Extensions.Caching.Memory;
using SqlSugar;
using VideoPlatform.Api.DTOs;

namespace VideoPlatform.Api.Services
{
    public interface ISettingsService
    {
        Task<bool> IsEmailVerificationEnabledAsync();
        Task<int> GetNewUserVipDaysAsync();
    }

    public class SettingsService : ISettingsService
    {
        private readonly ISqlSugarClient _db;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public SettingsService(ISqlSugarClient db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<bool> IsEmailVerificationEnabledAsync()
        {
            const string key = "EnableEmailVerification";
            // 尝试从缓存获取，如果获取不到，则从数据库查询并存入缓存
            return await _cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var setting = await _db.Queryable<AppSetting>().InSingleAsync(key);
                // 如果数据库中没有该配置，默认为 true (安全起见)
                return setting != null ? bool.Parse(setting.SettingValue) : true;
            });
        }

        public async Task<int> GetNewUserVipDaysAsync()
        {
            const string key = "NewUserVipDays";
            return await _cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var setting = await _db.Queryable<AppSetting>().InSingleAsync(key);
                // 如果数据库中没有该配置，默认为 0
                return setting != null ? int.Parse(setting.SettingValue) : 0;
            });
        }
    }
}
