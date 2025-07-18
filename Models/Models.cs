using Azure;
using SqlSugar;

namespace VideoPlatform.Api.Models
{
    /// <summary>
    /// 视频主表实体类
    /// </summary>
    [SugarTable("movies")] // 指定表名
    public class Movie
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)] // 主键、自增
        public ulong Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        [SugarColumn(ColumnName = "poster_url_vertical")]
        public string PosterUrlVertical { get; set; }

        [SugarColumn(ColumnName = "poster_url_horizontal")]
        public string PosterUrlHorizontal { get; set; }

        [SugarColumn(ColumnName = "release_year")]
        public int ReleaseYear { get; set; }

        [SugarColumn(ColumnName = "duration_in_seconds")]
        public int? DurationInSeconds { get; set; }

        [SugarColumn(ColumnName = "monetization_type")]
        public string MonetizationType { get; set; } // 使用字符串简化，也可以用枚举

        [SugarColumn(ColumnName = "published_at")]
        public DateTime? PublishedAt { get; set; }

        // 导航属性: 用于查询，不会在数据库中创建列
        [Navigate(typeof(MovieCategory),nameof(MovieCategory.MovieId), nameof(MovieCategory.CategoryId))]
        public List<Category> Categories { get; set; }

        [Navigate(typeof(MovieTag),nameof(MovieTag.MovieId), nameof(MovieTag.TagId))]
        public List<Tag> Tags { get; set; }
    }
    /// <summary>
    /// 视频地址实体类
    /// </summary>
    [SugarTable("movie_files")]
    public class MovieFile
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public ulong Id { get; set; }
        [SugarColumn(ColumnName = "movie_id")]
        public ulong MovieId { get; set; }
        public string Resolution { get; set; }
        [SugarColumn(ColumnName = "file_url")]
        public string FileUrl { get; set; }
    }

    /// <summary>
    /// 视频分类实体类
    /// </summary>

    [SugarTable("categories")]
    public class Category
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // 'genre' or 'region'
    }

    /// <summary>
    /// 标签实体类
    /// </summary>
    [SugarTable("tags")]
    public class Tag
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public uint Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// 视频分类表实体类
    /// </summary>
    [SugarTable("movie_categories")]
    public class MovieCategory
    {
        [SugarColumn(ColumnName = "movie_id", IsPrimaryKey = true)]
        public ulong MovieId { get; set; }
        [SugarColumn(ColumnName = "category_id", IsPrimaryKey = true)]
        public uint CategoryId { get; set; }
    }
    /// <summary>
    /// 视频标签表实体类
    /// </summary>
    [SugarTable("movie_tags")]
    public class MovieTag
    {
        [SugarColumn(ColumnName = "movie_id", IsPrimaryKey = true)]
        public ulong MovieId { get; set; }
        [SugarColumn(ColumnName = "tag_id", IsPrimaryKey = true)]
        public uint TagId { get; set; }
    }
    /// <summary>
    /// 用户表实体类
    /// </summary>

    [SugarTable("users")]
    public class User
    {
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        [SugarColumn(ColumnName = "password_hash")]
        public string PasswordHash { get; set; }
        [SugarColumn(ColumnName = "avatar_url")]
        public string AvatarUrl { get; set; }
        [SugarColumn(ColumnName = "vip_status")]
        public string VipStatus { get; set; }
        [SugarColumn(ColumnName = "vip_expires_at")]
        public DateTime? VipExpiresAt { get; set; }
        [SugarColumn(ColumnName = "virtual_currency_balance")]
        public decimal VirtualCurrencyBalance { get; set; }
        [SugarColumn(ColumnName = "created_at")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 评论表实体类
    /// </summary>

    [SugarTable("comments")]
    public class Comment
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public ulong Id { get; set; }

        [SugarColumn(ColumnName = "user_id")]
        public string UserId { get; set; }

        [SugarColumn(ColumnName = "movie_id")]
        public ulong MovieId { get; set; }

        [SugarColumn(ColumnName = "comment_text")]
        public string CommentText { get; set; }

        [SugarColumn(ColumnName = "created_at")]
        public DateTime CreatedAt { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(UserId))]
        public User User { get; set; }
    }

    /// <summary>
    /// 观看历史实体类
    /// </summary>
    [SugarTable("watch_history")]
    public class WatchHistory
    {
        [SugarColumn(ColumnName = "user_id", IsPrimaryKey = true)]
        public string UserId { get; set; }
        [SugarColumn(ColumnName = "movie_id", IsPrimaryKey = true)]
        public ulong MovieId { get; set; }
        [SugarColumn(ColumnName = "progress_in_seconds")]
        public int ProgressInSeconds { get; set; }
        [SugarColumn(ColumnName = "last_watched_at")]
        public DateTime LastWatchedAt { get; set; }
    }
    /// <summary>
    /// 用户收藏实体类
    /// </summary>
    [SugarTable("favorites")]
    public class Favorite
    {
        [SugarColumn(ColumnName = "user_id", IsPrimaryKey = true)]
        public string UserId { get; set; }
        [SugarColumn(ColumnName = "movie_id", IsPrimaryKey = true)]
        public ulong MovieId { get; set; }
        [SugarColumn(ColumnName = "created_at")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 订阅套餐实体类
    /// </summary>
    [SugarTable("subscription_plans")]
    public class SubscriptionPlan
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public uint Id { get; set; }
        public string Name { get; set; }
        [SugarColumn(ColumnName = "duration_in_days")]
        public int DurationInDays { get; set; }
        [SugarColumn(ColumnName = "price_usd")]
        public decimal PriceUsd { get; set; }
        [SugarColumn(ColumnName = "original_price_usd")]
        public decimal? OriginalPriceUsd { get; set; }
        [SugarColumn(ColumnName = "is_active")]
        public bool IsActive { get; set; }
    }
    /// <summary>
    /// 卡密实体类
    /// </summary>

    [SugarTable("redemption_codes")]
    public class RedemptionCode
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public ulong Id { get; set; }
        public string Code { get; set; }
        [SugarColumn(ColumnName = "plan_id")]
        public uint PlanId { get; set; }
        [SugarColumn(ColumnName = "is_redeemed")]
        public bool IsRedeemed { get; set; }
        [SugarColumn(ColumnName = "redeemed_by_user_id")]
        public string RedeemedByUserId { get; set; }
        [SugarColumn(ColumnName = "redeemed_at")]
        public DateTime? RedeemedAt { get; set; }
        [SugarColumn(ColumnName = "expires_at")]
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    ///订单实体类
    /// </summary>
    [SugarTable("transactions")]
    public class Transaction
    {
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; }
        [SugarColumn(ColumnName = "user_id")]
        public string UserId { get; set; }
        [SugarColumn(ColumnName = "plan_id")]
        public uint? PlanId { get; set; }
        [SugarColumn(ColumnName = "amount_usd")]
        public decimal AmountUsd { get; set; }
        [SugarColumn(ColumnName = "payment_method")]
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        [SugarColumn(ColumnName = "created_at")]
        public DateTime CreatedAt { get; set; }
    }
    /// <summary>
    /// 轮播图实体类
    /// </summary>
    [SugarTable("banners")]
    public class Banner
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public uint Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        [SugarColumn(ColumnName = "image_url")]
        public string ImageUrl { get; set; }
        [SugarColumn(ColumnName = "link_type")]
        public string LinkType { get; set; } // "movie", "external", "none"
        [SugarColumn(ColumnName = "link_url")]
        public string LinkUrl { get; set; }
        [SugarColumn(ColumnName = "is_active")]
        public bool IsActive { get; set; }
        [SugarColumn(ColumnName = "sort_order")]
        public int SortOrder { get; set; }
    }
}