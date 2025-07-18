using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace VideoPlatform.Api.DTOs
{
    /// <summary>
    /// 视频DTO
    /// </summary>
    public class MovieDto
    {
        public ulong Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PosterUrlVertical { get; set; }
        public int ReleaseYear { get; set; }
        public string MonetizationType { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Regions { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
    }
    /// <summary>
    /// 评论DTO
    /// </summary>
    public class CommentDto
    {
        public ulong Id { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public string CommentText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    /// <summary>
    /// 添加评论DTO
    /// </summary>
    public class CreateCommentDto
    {
        public string CommentText { get; set; }
    }

    public class CategoryDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class TagDto
    {
        public string Name { get; set; }
    }
    /// <summary>
    /// 分页数据DTO
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class SendCodeDto
    {
        public string Email { get; set; }
    }
    /// <summary>
    /// 注册TDO
    /// </summary>
    public class RegisterDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string VerificationCode { get; set; }
    }
    /// <summary>
    /// 登录请求DTO
    /// </summary>
    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    /// <summary>
    /// 登录成功后返回的用户信息
    /// </summary>
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public string Username { get; set; }//用户名
        public string Email { get; set; }//邮件
        public string VipStatus { get; set; }//VIP状态
        public DateTime? VipExpiresAt { get; set; } //VIP过期时间
    }
    /// <summary>
    /// 订阅套餐TDO
    /// </summary>
    public class SubscriptionPlanDto
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public decimal PriceUsd { get; set; }
        public decimal? OriginalPriceUsd { get; set; }
        public string Description { get; set; } // 可以在后端生成描述，如 "折合 $2.08/月"
        public string Tag { get; set; } // 例如 "超值推荐"
    }
    /// <summary>
    /// 轮播图DTO
    /// </summary>
    public class BannerDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string LinkType { get; set; }
        public string LinkUrl { get; set; }
    }
    /// <summary>
    /// 视频文件链接 DTO
    /// </summary>
    public class MovieFileDto
    {
        public string Resolution { get; set; } // 例如: "1080p", "720p"
        public string FileUrl { get; set; }
    }

    /// <summary>
    //  播放数据 DTO，包含所有可用的视频链接
    /// </summary>
    public class PlayDataDto
    {
        public List<MovieFileDto> MovieFiles { get; set; } = new List<MovieFileDto>();
    }
    /// <summary>
    /// 用于新增影片的数据传输对象
    /// </summary>
    public class CreateMovieDto
    {
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        [Required]
        public string PosterUrlVertical { get; set; }
        public string PosterUrlHorizontal { get; set; }
        [Required]
        public int ReleaseYear { get; set; }
        public int? DurationInSeconds { get; set; }
        [Required]
        public string MonetizationType { get; set; } // "free", "vip", "paid"
        public DateTime? PublishedAt { get; set; } = DateTime.UtcNow;

        // 关联数据 (通过名称关联)
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Regions { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<MovieFileDto> MovieFiles { get; set; } = new List<MovieFileDto>();
    }
    /// <summary>
    /// 用于修改影片的数据传输对象
    /// </summary>
    public class UpdateMovieDto : CreateMovieDto
    {
        // 继承自CreateMovieDto，包含了所有可修改的字段
    }
    /// <summary>
    /// 用于批量删除影片的数据传输对象
    /// </summary>
    public class BatchDeleteMoviesDto
    {
        [Required]
        public List<ulong> MovieIds { get; set; }
    }
}
