using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Security.Claims;
using VideoPlatform.Api.DTOs;
using VideoPlatform.Api.Models;

namespace VideoPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GossipController : ControllerBase
    {
        private readonly ISqlSugarClient _db;

        public GossipController(ISqlSugarClient db)
        {
            _db = db;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        /// <summary>
        /// 分页获取吃瓜帖子列表（支持双向加载）
        /// </summary>
        [HttpGet("posts")]
        public async Task<ActionResult<GossipListDto>> GetPosts(
            [FromQuery] ulong? before_id,
            [FromQuery] ulong? after_id)
        {
            var userId = GetCurrentUserId();
            var user = await _db.Queryable<User>().InSingleAsync(userId);
            if (user == null || user.VipStatus?.ToLower() != "active" || !(user.VipExpiresAt > System.DateTime.UtcNow))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "此内容为VIP专属，请先开通会员。");
            }

            var baseQuery = _db.Queryable<GossipPost>()
                               .Includes(p => p.Media)
                               .Where(p => p.AccessLevel == "vip")
                               .OrderBy(p => p.SortOrder, OrderByType.Desc)
                               .OrderBy(p => p.CreatedAt, OrderByType.Desc);

            var response = new GossipListDto();
            List<GossipPost> posts;

            // 1. 加载历史消息
            if (before_id.HasValue)
            {
                posts = await baseQuery.Where(p => p.Id < before_id.Value).Take(3).ToListAsync();
                response.Items = MapToDto(posts);
                if (posts.Any())
                {
                    var oldestId = posts.Min(p => p.Id);
                    response.HasMoreHistory = await _db.Queryable<GossipPost>().AnyAsync(p => p.Id < oldestId);
                }
            }
            // 2. 加载最新消息
            else if (after_id.HasValue)
            {
                posts = await baseQuery.Where(p => p.Id > after_id.Value).Take(5).ToListAsync();
                response.Items = MapToDto(posts);
                if (posts.Any())
                {
                    var newestId = posts.Max(p => p.Id);
                    response.HasMoreNew = await _db.Queryable<GossipPost>().AnyAsync(p => p.Id > newestId);
                }
            }
            // 3. 初始加载
            else
            {
                var progress = await _db.Queryable<UserGossipProgress>().InSingleAsync(userId);
                response.LastSeenId = progress?.LastPostId;

                if (response.LastSeenId.HasValue)
                {
                    posts = await baseQuery.Where(p => p.Id >= response.LastSeenId.Value).Take(5).ToListAsync();
                }
                else
                {
                    posts = await baseQuery.Take(5).ToListAsync();
                }
                response.Items = MapToDto(posts);

                if (posts.Any())
                {
                    var oldestId = posts.Min(p => p.Id);
                    var newestId = posts.Max(p => p.Id);
                    response.HasMoreHistory = await _db.Queryable<GossipPost>().AnyAsync(p => p.Id < oldestId);
                    response.HasMoreNew = await _db.Queryable<GossipPost>().AnyAsync(p => p.Id > newestId);
                }
            }

            return Ok(response);
        }

        /// <summary>
        /// 更新用户浏览进度
        /// </summary>
        [HttpPost("progress")]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateGossipProgressDto dto)
        {
            var userId = GetCurrentUserId();
            var progress = new UserGossipProgress
            {
                UserId = userId,
                LastPostId = dto.LastPostId,
                UpdatedAt = System.DateTime.UtcNow
            };
            await _db.Storageable(progress).ExecuteCommandAsync();
            return Ok();
        }

        private List<GossipPostDto> MapToDto(List<GossipPost> posts)
        {
            return posts.Select(p => new GossipPostDto
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                Media = p.Media.OrderBy(m => m.SortOrder).Select(m => new GossipMediaDto
                {
                    MediaUrl = m.MediaUrl,
                    MediaType = m.MediaType
                }).ToList()
            }).ToList();
        }
    }
}
