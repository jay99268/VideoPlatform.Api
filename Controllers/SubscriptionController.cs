using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using VideoPlatform.Api.DTOs;
using VideoPlatform.Api.Models;

namespace VideoPlatform.Api.Controllers
{
    /// <summary>
    /// 订阅控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISqlSugarClient _db;

        public SubscriptionController(ISqlSugarClient db)
        {
            _db = db;
        }
        /// <summary>
        /// 获取订阅计划
        /// </summary>
        /// <returns></returns>
        [HttpGet("plans")]
        public async Task<ActionResult<IEnumerable<SubscriptionPlanDto>>> GetPlans()
        {
            var plans = await _db.Queryable<SubscriptionPlan>()
                                 .Where(p => p.IsActive)
                                 .OrderBy(p => p.PriceUsd)
                                 .ToListAsync();

            // 映射到 DTO
            var planDtos = plans.Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                PriceUsd = p.PriceUsd,
                OriginalPriceUsd = p.OriginalPriceUsd,
                // 您可以在这里根据业务逻辑生成描述和标签
                Description = p.DurationInDays >= 365 ? $"折合 ${(p.PriceUsd / 12):F2}/月" : (p.DurationInDays >= 90 ? $"折合 ${(p.PriceUsd / 3):F2}/月" : "标准单月价"),
                Tag = p.DurationInDays >= 365 ? "超值推荐" : null
            }).ToList();

            return Ok(planDtos);
        }
    }
}
