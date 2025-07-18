using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Security.Claims;
using VideoPlatform.Api.DTOs;
using VideoPlatform.Api.Models;

namespace VideoPlatform.Api.Controllers
{
    /// <summary>
    /// 个人中心控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 整个控制器都需要授权
    public class ProfileController : ControllerBase
    {
        private readonly ISqlSugarClient _db;

        public ProfileController(ISqlSugarClient db)
        {
            _db = db;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private MovieDto MapMovieToDto(Movie m)
        {
            return new MovieDto
            {
                Id = m.Id,
                Title = m.Title,
                PosterUrlVertical = m.PosterUrlVertical,
                MonetizationType = m.MonetizationType
            };
        }

        [HttpGet("history")]
        public async Task<ActionResult<PagedResult<MovieDto>>> GetWatchHistory([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 12)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var totalCount = new RefAsync<int>();

            var movies = await _db.Queryable<WatchHistory>()
                                  .InnerJoin<Movie>((wh, m) => wh.MovieId == m.Id)
                                  .Where(wh => wh.UserId == userId)
                                  .OrderBy(wh => wh.LastWatchedAt, OrderByType.Desc)
                                  .Select((wh, m) => m) // 仅选择Movie实体
                                  .ToPageListAsync(pageIndex, pageSize, totalCount);

            var result = new PagedResult<MovieDto>
            {
                Items = movies.Select(MapMovieToDto).ToList(),
                TotalCount = totalCount.Value,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(result);
        }

        [HttpGet("favorites")]
        public async Task<ActionResult<PagedResult<MovieDto>>> GetFavorites([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 12)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var totalCount = new RefAsync<int>();

            var movies = await _db.Queryable<Favorite>()
                                  .InnerJoin<Movie>((f, m) => f.MovieId == m.Id)
                                  .Where(f => f.UserId == userId)
                                  .OrderBy(f => f.CreatedAt, OrderByType.Desc)
                                  .Select((f, m) => m)
                                  .ToPageListAsync(pageIndex, pageSize, totalCount);

            var result = new PagedResult<MovieDto>
            {
                Items = movies.Select(MapMovieToDto).ToList(),
                TotalCount = totalCount.Value,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(result);
        }
    }
}
