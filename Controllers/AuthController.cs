using Dm.filter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using SqlSugar;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VideoPlatform.Api.DTOs;
using VideoPlatform.Api.Models;
using VideoPlatform.Api.Services;

namespace VideoPlatform.Api.Controllers
{
    /// <summary>
    /// 授权认证控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISqlSugarClient _db;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache; // 新增
        private readonly IEmailService _emailService; // 新增

        public AuthController(ISqlSugarClient db, IConfiguration config, IMemoryCache cache, IEmailService emailService)
        {
            _db = db;
            _config = config;
            _cache = cache;
            _emailService = emailService;
        }
        /// <summary>
        /// 发送验证码方法
        /// </summary>
        /// <param name="sendCodeDto"></param>
        /// <returns></returns>

        [HttpPost("send-verification-code")]
        public async Task<IActionResult> SendVerificationCode([FromBody] SendCodeDto sendCodeDto)
        {
            if (string.IsNullOrEmpty(sendCodeDto.Email))
            {
                return BadRequest("邮箱不能为空。");
            }

            // 生成6位随机数字验证码
            var code = new Random().Next(100000, 999999).ToString();
            var cacheKey = $"VerificationCode_{sendCodeDto.Email}";

            // 将验证码存入缓存，有效期5分钟
            _cache.Set(cacheKey, code, TimeSpan.FromMinutes(5));

            // 发送邮件
            var subject = "您的注册验证码";
            var message = $"欢迎注册流光影院！您的验证码是：{code}。该验证码5分钟内有效。";
            Console.WriteLine(message);
            await _emailService.SendEmailAsync(sendCodeDto.Email, subject, message);

            return Ok(new { Message = "验证码已发送至您的邮箱，请注意查收。" });
        }

        /// <summary>
        /// 注册验证方法
        /// </summary>
        /// <param name="registerDto"></param>
        /// <returns></returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            // 1. 验证验证码
            var cacheKey = $"VerificationCode_{registerDto.Email}";
            if (!_cache.TryGetValue(cacheKey, out string storedCode) || storedCode != registerDto.VerificationCode)
            {
                return BadRequest("验证码错误或已过期。");
            }

            // 2. 检查邮箱是否已注册
            var userExists = await _db.Queryable<User>().AnyAsync(u => u.Email == registerDto.Email);
            if (userExists)
            {
                return BadRequest("该邮箱已被注册。");
            }

            // 3. 创建新用户
            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                VipStatus = "none",
                CreatedAt = DateTime.UtcNow
            };

            await _db.Insertable(newUser).ExecuteCommandAsync();

            // 4. 注册成功后，移除缓存中的验证码
            _cache.Remove(cacheKey);

            return Ok(new { Message = "注册成功！" });
        }
        /// <summary>
        /// 登录验证方法
        /// </summary>
        /// <param name="loginDto"></param>
        /// <returns></returns>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDto loginDto)
        {
            var user = await _db.Queryable<User>().SingleAsync(u => u.Email == loginDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized("邮箱或密码错误。");
            }

            var token = GenerateJwtToken(user);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                VipStatus = user.VipStatus,
                VipExpiresAt = user.VipExpiresAt // 新增
            });
        }
        /// <summary>
        /// 生成Token方法
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
