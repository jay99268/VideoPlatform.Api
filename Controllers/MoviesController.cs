using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Security.Claims;
using VideoPlatform.Api.DTOs;
using VideoPlatform.Api.Models;

namespace VideoPlatform.Api.Controllers
{
    /// <summary>
    /// 影片控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {
        private readonly ISqlSugarClient _db;

        public MoviesController(ISqlSugarClient db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<MovieDto>>> GetMovies(
            [FromQuery] string? genre,
            [FromQuery] string? region,
            [FromQuery] string? tag,
            [FromQuery] string? monetizationType, // 新增参数
            [FromQuery] string sortBy = "published_at",
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 18)
        {
            var query = _db.Queryable<Movie>()
                           .Includes(m => m.Categories)
                           .Includes(m => m.Tags);

            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(m => m.Categories.Any(c => c.Type == "genre" && c.Name == genre));
            }
            if (!string.IsNullOrEmpty(region))
            {
                query = query.Where(m => m.Categories.Any(c => c.Type == "region" && c.Name == region));
            }
            if (!string.IsNullOrEmpty(tag))
            {
                query = query.Where(m => m.Tags.Any(t => t.Name == tag));
            }
            // 新增筛选逻辑
            if (!string.IsNullOrEmpty(monetizationType))
            {
                query = query.Where(m => m.MonetizationType == monetizationType);
            }

            if (sortBy?.ToLower() == "release_year")
            {
                query = query.OrderBy(m => m.ReleaseYear, OrderByType.Desc);
            }
            else
            {
                query = query.OrderBy(m => m.PublishedAt, OrderByType.Desc);
            }

            var totalCount = new RefAsync<int>();
            var movies = await query.ToPageListAsync(pageIndex, pageSize, totalCount);

            var movieDtos = movies.Select(m => new MovieDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                PosterUrlVertical = m.PosterUrlVertical,
                ReleaseYear = m.ReleaseYear,
                MonetizationType = m.MonetizationType,
                Genres = m.Categories.Where(c => c.Type == "genre").Select(c => c.Name).ToList(),
                Regions = m.Categories.Where(c => c.Type == "region").Select(c => c.Name).ToList(),
                Tags = m.Tags.Select(t => t.Name).ToList()
            }).ToList();

            var result = new PagedResult<MovieDto>
            {
                Items = movieDtos,
                TotalCount = totalCount.Value,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(result);

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MovieDto>> GetMovieById(ulong id)
        {
            var movie = await _db.Queryable<Movie>()
                                 .Includes(m => m.Categories)
                                 .Includes(m => m.Tags)
                                 .InSingleAsync(id);

            if (movie == null)
            {
                return NotFound();
            }

            var movieDto = new MovieDto
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                PosterUrlVertical = movie.PosterUrlVertical,
                ReleaseYear = movie.ReleaseYear,
                MonetizationType = movie.MonetizationType,
                Genres = movie.Categories.Where(c => c.Type == "genre").Select(c => c.Name).ToList(),
                Regions = movie.Categories.Where(c => c.Type == "region").Select(c => c.Name).ToList(),
                Tags = movie.Tags.Select(t => t.Name).ToList()
            };

            return Ok(movieDto);
        }
        /// <summary>
        /// 获取电影播放链接
        /// </summary>
        /// <param name="id">电影ID</param>
        /// <returns>包含 m3u8 清单的播放数据</returns>
        [HttpGet("{id}/play")]
        [AllowAnonymous]
        public async Task<ActionResult<PlayDataDto>> GetPlayData(ulong id)
        {
            var movie = await _db.Queryable<Movie>().InSingleAsync(id);
            if (movie == null)
            {
                return NotFound("找不到指定的电影。");
            }

            if (movie.MonetizationType?.ToLower() == "vip")
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("观看此影片需要登录。");
                }

                var user = await _db.Queryable<User>().InSingleAsync(userId);
                if (user == null || user.VipStatus?.ToLower() != "active" || !(user.VipExpiresAt > DateTime.UtcNow))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "此内容为VIP专属，请先开通会员。");
                }
            }

            if (movie.MonetizationType?.ToLower() == "paid")
            {
                return StatusCode(StatusCodes.Status403Forbidden, "暂不支持付费点播影片。");
            }

            // --- 核心修改 ---
            // 查找第一个包含有效 m3u8 链接的记录
            var movieFile = await _db.Queryable<MovieFile>()
                                      .Where(mf => mf.MovieId == id && (!string.IsNullOrEmpty(mf.FileM3u8) || !string.IsNullOrEmpty(mf.FileUrl)))
                                      .FirstAsync();

            if (movieFile == null)
            {
                return NotFound("未找到该电影的播放资源。");
            }

            var playData = new PlayDataDto
            {
                FileM3u8 = movieFile.FileM3u8,
                FileUrl = movieFile.FileUrl // 赋予 FileUrl
            };

            return Ok(playData);
        }

        [HttpGet("{id}/related")]
        public async Task<ActionResult<IEnumerable<MovieDto>>> GetRelatedMovies(ulong id)
        {
            // --- 核心修复 ---
            // 1. 先获取当前影片的所有标签ID
            var movieTagIds = await _db.Queryable<MovieTag>()
                                       .Where(mt => mt.MovieId == id)
                                       .Select(mt => mt.TagId)
                                       .ToListAsync();

            if (movieTagIds == null || !movieTagIds.Any())
            {
                return Ok(new List<MovieDto>());
            }

            // 2. 找出包含这些标签的其他影片的ID
            var relatedMovieIds = await _db.Queryable<MovieTag>()
                                           .Where(mt => movieTagIds.Contains(mt.TagId) && mt.MovieId != id)
                                           .Select(mt => mt.MovieId)
                                           .Distinct()
                                           .ToListAsync();

            if (relatedMovieIds == null || !relatedMovieIds.Any())
            {
                return Ok(new List<MovieDto>());
            }

            // 3. 根据ID列表获取影片信息
            var relatedMovies = await _db.Queryable<Movie>()
                                         .Where(m => relatedMovieIds.Contains(m.Id))
                                         .Take(6)
                                         .ToListAsync();

            // 映射到 DTO
            var movieDtos = relatedMovies.Select(m => new MovieDto
            {
                Id = m.Id,
                Title = m.Title,
                PosterUrlVertical = m.PosterUrlVertical,
                MonetizationType = m.MonetizationType
            }).ToList();

            return Ok(movieDtos);
        }

        [HttpGet("{id}/comments")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(ulong id)
        {
            var comments = await _db.Queryable<Comment>()
                                    .Includes(c => c.User)
                                    .Where(c => c.MovieId == id)
                                    .OrderBy(c => c.CreatedAt, OrderByType.Desc)
                                    .ToListAsync();

            var commentDtos = comments.Select(c => new CommentDto
            {
                Id = c.Id,
                Username = c.User?.Username ?? "匿名用户",
                AvatarUrl = c.User?.AvatarUrl,
                CommentText = c.CommentText,
                CreatedAt = c.CreatedAt
            }).ToList();

            return Ok(commentDtos);
        }

        [HttpPost("{id}/comments")]
        [Authorize] // 需要登录才能评论
        public async Task<ActionResult<CommentDto>> PostComment(ulong id, [FromBody] CreateCommentDto createCommentDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var newComment = new Comment
            {
                MovieId = id,
                UserId = userId,
                CommentText = createCommentDto.CommentText,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Insertable(newComment).ExecuteCommandAsync();

            return CreatedAtAction(nameof(GetComments), new { id = id }, newComment);
        }

        // --- 影片管理接口 (需要管理员权限) ---

        /// <summary>
        /// 新增一部影片
        /// </summary>
        /// <param name="movieDto">影片数据</param>
        /// <returns>创建的影片信息</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateMovie([FromBody] CreateMovieDto movieDto)
        {
            var movie = new Movie
            {
                Title = movieDto.Title,
                Description = movieDto.Description,
                PosterUrlVertical = movieDto.PosterUrlVertical,
                PosterUrlHorizontal = movieDto.PosterUrlHorizontal,
                ReleaseYear = movieDto.ReleaseYear,
                DurationInSeconds = movieDto.DurationInSeconds,
                MonetizationType = movieDto.MonetizationType,
                PublishedAt = movieDto.PublishedAt
            };

            try
            {
                // 使用 _db.Ado.UseTranAsync 保证事务性
                await _db.Ado.UseTranAsync(async () =>
                {
                    var newMovieId = (ulong)await _db.Insertable(movie).ExecuteReturnBigIdentityAsync();
                    movie.Id = newMovieId;

                    // 处理分类和标签
                    var allCategoryNames = movieDto.Genres.Concat(movieDto.Regions).ToList();
                    var categories = await _db.Queryable<Category>().Where(c => allCategoryNames.Contains(c.Name)).ToListAsync();
                    var tags = await _db.Queryable<Tag>().Where(t => movieDto.Tags.Contains(t.Name)).ToListAsync();

                    var movieCategories = categories.Select(c => new MovieCategory { MovieId = newMovieId, CategoryId = c.Id }).ToList();
                    var movieTags = tags.Select(t => new MovieTag { MovieId = newMovieId, TagId = t.Id }).ToList();

                    if (movieCategories.Any()) await _db.Insertable(movieCategories).ExecuteCommandAsync();
                    if (movieTags.Any()) await _db.Insertable(movieTags).ExecuteCommandAsync();

                    // 处理影片文件
                    if (movieDto.MovieFiles.Any())
                    {
                        var movieFiles = movieDto.MovieFiles.Select(f => new MovieFile
                        {
                            MovieId = newMovieId,
                            Resolution = f.Resolution,
                            FileUrl = f.FileUrl
                        }).ToList();
                        await _db.Insertable(movieFiles).ExecuteCommandAsync();
                    }
                });

                return CreatedAtAction(nameof(GetMovieById), new { id = movie.Id }, movie);
            }
            catch (Exception ex)
            {
                // 记录详细错误日志
                Console.WriteLine(ex.ToString());
                return StatusCode(500, $"内部服务器错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 修改一部影片
        /// </summary>
        /// <param name="id">影片ID</param>
        /// <param name="movieDto">更新的影片数据</param>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateMovie(ulong id, [FromBody] UpdateMovieDto movieDto)
        {
            var movie = await _db.Queryable<Movie>().InSingleAsync(id);
            if (movie == null)
            {
                return NotFound("找不到要更新的影片。");
            }

            // 更新主表信息
            movie.Title = movieDto.Title;
            movie.Description = movieDto.Description;
            movie.PosterUrlVertical = movieDto.PosterUrlVertical;
            movie.PosterUrlHorizontal = movieDto.PosterUrlHorizontal;
            movie.ReleaseYear = movieDto.ReleaseYear;
            movie.DurationInSeconds = movieDto.DurationInSeconds;
            movie.MonetizationType = movieDto.MonetizationType;
            movie.PublishedAt = movieDto.PublishedAt;

            try
            {
                await _db.Ado.UseTranAsync(async () =>
                {
                    // 1. 更新影片主表
                    await _db.Updateable(movie).ExecuteCommandAsync();

                    // 2. 删除旧的关联关系（分类、标签、文件）
                    await _db.Deleteable<MovieCategory>().Where(mc => mc.MovieId == id).ExecuteCommandAsync();
                    await _db.Deleteable<MovieTag>().Where(mt => mt.MovieId == id).ExecuteCommandAsync();
                    await _db.Deleteable<MovieFile>().Where(mf => mf.MovieId == id).ExecuteCommandAsync();

                    // 3. 插入新的关联关系
                    // 处理分类和标签
                    var allCategoryNames = movieDto.Genres.Concat(movieDto.Regions).ToList();
                    var categories = await _db.Queryable<Category>().Where(c => allCategoryNames.Contains(c.Name)).ToListAsync();
                    var tags = await _db.Queryable<Tag>().Where(t => movieDto.Tags.Contains(t.Name)).ToListAsync();

                    var movieCategories = categories.Select(c => new MovieCategory { MovieId = id, CategoryId = c.Id }).ToList();
                    var movieTags = tags.Select(t => new MovieTag { MovieId = id, TagId = t.Id }).ToList();

                    if (movieCategories.Any()) await _db.Insertable(movieCategories).ExecuteCommandAsync();
                    if (movieTags.Any()) await _db.Insertable(movieTags).ExecuteCommandAsync();

                    // 处理影片文件
                    if (movieDto.MovieFiles.Any())
                    {
                        var movieFiles = movieDto.MovieFiles.Select(f => new MovieFile
                        {
                            MovieId = id,
                            Resolution = f.Resolution,
                            FileUrl = f.FileUrl
                        }).ToList();
                        await _db.Insertable(movieFiles).ExecuteCommandAsync();
                    }
                });

                return NoContent(); // 成功更新，返回204
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, $"更新影片时发生错误: {ex.Message}");
            }
        }


        /// <summary>
        /// 批量新增影片
        /// </summary>
        [HttpPost("batch")]
        [Authorize]
        public async Task<IActionResult> CreateMoviesBatch([FromBody] List<CreateMovieDto> moviesDtos)
        {
            if (moviesDtos == null || !moviesDtos.Any())
            {
                return BadRequest("影片列表不能为空。");
            }

            var createdMovies = new List<Movie>();
            // 注意：循环调用单个创建事务，在需要高性能的场景下不是最佳实践。
            // 更好的方法是在一个大事务中处理所有影片。
            foreach (var movieDto in moviesDtos)
            {
                var movie = new Movie
                {
                    Title = movieDto.Title,
                    Description = movieDto.Description,
                    PosterUrlVertical = movieDto.PosterUrlVertical,
                    PosterUrlHorizontal = movieDto.PosterUrlHorizontal,
                    ReleaseYear = movieDto.ReleaseYear,
                    DurationInSeconds = movieDto.DurationInSeconds,
                    MonetizationType = movieDto.MonetizationType,
                    PublishedAt = movieDto.PublishedAt
                };
                // 此处省略了复杂的关联数据处理，仅为示例。
                // 在实际项目中，应将单个创建的逻辑封装成一个可重用的Service层方法，并在一个大事务中循环调用。
                await _db.Insertable(movie).ExecuteCommandAsync();
                createdMovies.Add(movie);
            }
            return Ok(new { Message = $"{createdMovies.Count} 部影片已成功创建。" });
        }

        /// <summary>
        /// 删除一部影片
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteMovie(ulong id)
        {
            var movieExists = await _db.Queryable<Movie>().AnyAsync(it => it.Id == id);
            if (!movieExists)
            {
                return NotFound("找不到要删除的影片。");
            }

            try
            {
                await _db.Ado.UseTranAsync(async () =>
                {
                    // 删除所有关联数据
                    await _db.Deleteable<MovieCategory>().Where(mc => mc.MovieId == id).ExecuteCommandAsync();
                    await _db.Deleteable<MovieTag>().Where(mt => mt.MovieId == id).ExecuteCommandAsync();
                    await _db.Deleteable<MovieFile>().Where(mf => mf.MovieId == id).ExecuteCommandAsync();
                    await _db.Deleteable<Comment>().Where(c => c.MovieId == id).ExecuteCommandAsync();
                    await _db.Deleteable<Favorite>().Where(f => f.MovieId == id).ExecuteCommandAsync();
                    await _db.Deleteable<WatchHistory>().Where(wh => wh.MovieId == id).ExecuteCommandAsync();

                    // 删除影片本身
                    await _db.Deleteable<Movie>().In(id).ExecuteCommandAsync();
                });

                return NoContent(); // 成功删除，返回204
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, $"删除影片时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量删除影片
        /// </summary>
        [HttpDelete("batch")]
        [Authorize]
        public async Task<IActionResult> DeleteMoviesBatch([FromBody] BatchDeleteMoviesDto dto)
        {
            var movieIds = dto.MovieIds;
            if (movieIds == null || !movieIds.Any())
            {
                return BadRequest("需要提供要删除的影片ID列表。");
            }

            try
            {
                await _db.Ado.UseTranAsync(async () =>
                {
                    // 批量删除所有关联数据
                    await _db.Deleteable<MovieCategory>().Where(mc => movieIds.Contains(mc.MovieId)).ExecuteCommandAsync();
                    await _db.Deleteable<MovieTag>().Where(mt => movieIds.Contains(mt.MovieId)).ExecuteCommandAsync();
                    await _db.Deleteable<MovieFile>().Where(mf => movieIds.Contains(mf.MovieId)).ExecuteCommandAsync();
                    await _db.Deleteable<Comment>().Where(c => movieIds.Contains(c.MovieId)).ExecuteCommandAsync();
                    await _db.Deleteable<Favorite>().Where(f => movieIds.Contains(f.MovieId)).ExecuteCommandAsync();
                    await _db.Deleteable<WatchHistory>().Where(wh => movieIds.Contains(wh.MovieId)).ExecuteCommandAsync();

                    // 批量删除影片本身
                    await _db.Deleteable<Movie>().In(movieIds).ExecuteCommandAsync();
                });

                return Ok(new { Message = $"{movieIds.Count} 部影片已成功删除。" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, $"批量删除影片时发生错误: {ex.Message}");
            }
        }
    }
}
