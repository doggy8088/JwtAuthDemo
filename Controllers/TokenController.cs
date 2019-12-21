using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtAuthDemo.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NSwag.Annotations;

namespace JwtAuthDemo.Controllers
{
    /// <summary>
    /// 主要負責 JWT Token 相關操作
    /// </summary>
    [Authorize]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly JwtHelpers jwt;

        public TokenController(JwtHelpers jwt)
        {
            this.jwt = jwt;
        }

        /// <summary>
        /// 登入並取得 JWT Token
        /// </summary>
        /// <param name="login">LoginViewModel</param>
        /// <returns>回傳一個 JWT Token 字串</returns>
        /// <example> // 這個沒效！回傳純字串是沒效的，要自訂 ViewModel 才能設定範例到特定屬性去
        /// "eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsImtpZCI6bnVsbCwidHlwIjoiSldUIn0.eyJleHAiOiIxNTc2NTA5NzA0IiwiaXNzIjoiSnd0QXV0aERlbW8iLCJpYXQiOiIxNTc2NTA3OTA0IiwibmJmIjoiMTU3NjUwNzkwNCJ9.XmagvhyW_6SUFJfiOahkOBuVlLjyogEzMba3-WlbNmI"
        /// </example>
        /// <response code="200">成功產生 JWT Token</response>
        /// <response code="400">登入帳號或密碼錯誤</response>
        [AllowAnonymous]
        [HttpPost("~/signin")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        public ActionResult<string> SignIn(LoginViewModel login)
        {
            if (ValidateUser(login))
            {
                return jwt.GenerateToken(login.Username);
            }
            else
            {
                return BadRequest();
            }
        }

        private bool ValidateUser(LoginViewModel login)
        {
            return true; // TODO
        }

        /// <summary>
        /// 取得所有 Claims 清單 (只有 Role="admin" 可呼叫)
        /// </summary>
        /// <returns>IEnumerable&lt;Claim&gt;</returns>
        /// <response code="200" nullable="true">取得所有 Claims 清單</response>
        /// <response code="401">需提供 Bearer Token 才能呼叫</response>
        [Authorize(Roles = "admin")]
        [HttpGet("~/claims")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(200)]
        public ActionResult<IEnumerable<Claim>> GetClaims()
        {
            return Ok(User.Claims.Select(p => new { p.Type, p.Value }));
        }

        /// <summary>
        /// 取得使用者名稱 (未登入也可以執行，但會回傳 HTTP 204 且沒有內容)
        /// </summary>
        /// <returns></returns>
        /// <response code="200">使用者名稱</response>
        /// <response code="204" nullable="true">查無使用者名稱</response>
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(string), 200)]
        [AllowAnonymous]
        [HttpGet("~/username")]
        public IActionResult GetUserName()
        {
            return Ok(User.Identity.Name);
        }

        /// <summary>
        /// 取得 JWT ID (需登入才能取得資訊)
        /// </summary>
        /// <returns>JWT ID</returns>
        /// <response code="200">JWT ID</response>
        /// <response code="401">需提供 Bearer Token 才能呼叫</response>
        /// <response code="404">查無 JWT ID</response>
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(void), 404)]
        [ProducesResponseType(401)]
        [HttpGet("~/jwtid")]
        public IActionResult GetUniqueId()
        {
            var jti = User.Claims.FirstOrDefault(p => p.Type == "jti");
            if (jti == null)
            {
                // 預設 HTTP 404 都會以 ProblemDetails 型別回應，不回傳內容要給 null
                return StatusCode(404, null);
            }
            else
            {
                return Ok(jti.Value);
            }
        }
    }

    /// <summary>
    /// 登入模型
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// 使用者名稱
        /// </summary>
        /// <example>"will"</example>
        public string Username { get; set; }

        /// <summary>
        /// 使用者密碼
        /// </summary>
        /// <example>"YourPassword"</example>
        public string Password { get; set; }
    }
}