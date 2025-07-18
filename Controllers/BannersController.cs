using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using VideoPlatform.Api.DTOs;
using VideoPlatform.Api.Models;

namespace VideoPlatform.Api.Controllers
{
    /// <summary>
    /// 轮播图控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BannersController : ControllerBase
    {
        private readonly ISqlSugarClient _db;

        public BannersController(ISqlSugarClient db)
        {
            _db = db;
        }
        /// <summary>
        /// 获取主页轮播图信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BannerDto>>> GetBanners()
        {
            var banners = await _db.Queryable<Banner>()
                                   .Where(b => b.IsActive)
                                   .OrderBy(b => b.SortOrder)
                                   .ToListAsync();

            var bannerDtos = banners.Select(b => new BannerDto
            {
                Title = b.Title,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                LinkType = b.LinkType,
                LinkUrl = b.LinkUrl
            }).ToList();

            return Ok(bannerDtos);
        }
    }
}
