using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using VideoPlatform.Api.DTOs;
using VideoPlatform.Api.Models;

namespace VideoPlatform.Api.Controllers
{
    /// <summary>
    /// 分类控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ISqlSugarClient _db;

        public CategoriesController(ISqlSugarClient db)
        {
            _db = db;
        }
        /// <summary>
        /// 获取分类控制器
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _db.Queryable<Category>().ToListAsync();
            var dtos = categories.Select(c => new CategoryDto { Name = c.Name, Type = c.Type }).ToList();
            return Ok(dtos);
        }
    }
    /// <summary>
    /// 标签控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ISqlSugarClient _db;

        public TagsController(ISqlSugarClient db)
        {
            _db = db;
        }
        /// <summary>
        /// 获取标签数据
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetTags()
        {
            var tags = await _db.Queryable<Tag>().ToListAsync();
            var dtos = tags.Select(t => new TagDto { Name = t.Name }).ToList();
            return Ok(dtos);
        }
    }
}
