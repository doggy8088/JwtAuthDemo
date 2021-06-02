using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JwtAuthDemo.Helpers;
using Microsoft.Extensions.Hosting;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace JwtAuthDemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<JwtHelpers>();

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.IncludeErrorDetails = true; // WWW-Authenticate 會顯示詳細錯誤原因

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                        ValidateIssuer = true,
                        ValidIssuer = Configuration.GetValue<string>("JwtSettings:Issuer"),
                        ValidateAudience = false,
                        //ValidAudience = Configuration.GetValue<string>("JwtSettings:Issuer"),
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = false, // 如果 JWT 包含 key 才需要驗證，一般都只有簽章而已
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetValue<string>("JwtSettings:SignKey")))
                    };
                });

            // add OpenAPI v3 document
            services.AddOpenApiDocument(config =>
            {
                // 設定文件名稱 (重要) (預設值: v1)
                config.DocumentName = "v2";

                // 設定文件或 API 版本資訊
                config.Version = "0.0.1";

                // 設定文件標題 (當顯示 Swagger/ReDoc UI 的時候會顯示在畫面上)
                config.Title = "JwtAuthDemo";

                // 設定文件簡要說明
                config.Description = "This is a JWT authentication/authorization sample app";

                // 是否要顯示 API 呼叫範例
                // config.GenerateExamples = true;

                // 這個 OpenApiSecurityScheme 物件請勿加上 Name 與 In 屬性，否則產生出來的 OpenAPI Spec 格式會有錯誤！
                var apiScheme = new OpenApiSecurityScheme()
                {
                    Type = OpenApiSecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT", // for documentation purposes (OpenAPI only)
                    Description = "Copy JWT Token into the value field: {token}"
                };

                config.AddSecurity("Bearer", Enumerable.Empty<string>(), apiScheme);

                config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor());

                config.DefaultResponseReferenceTypeNullHandling = NJsonSchema.Generation.ReferenceTypeNullHandling.Null;
            });

            // add Swagger v2 document
            // services.AddSwaggerDocument();

            services.AddControllers(options =>
            {
                options.OutputFormatters.RemoveType<StringOutputFormatter>();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // serve OpenAPI/Swagger documents
            // services.AddOpenApiDocument();
            // app.UseOpenApi();

            app.UseOpenApi(config =>
            {
                // 這裡的 Path 用來設定 OpenAPI 文件的路由 (網址路徑) (一定要以 / 斜線開頭)
                config.Path = "/swagger/v2/swagger.json";

                // 這裡的 DocumentName 必須跟 services.AddOpenApiDocument() 的時候設定的 DocumentName 一致！
                config.DocumentName = "v2";

                config.PostProcess = (document, http) =>
                {
                    if (env.IsDevelopment())
                    {
                        document.Info.Title += " (開發環境)";
                        document.Info.Version += "-dev";
                        document.Info.Description = "當 API 有問題時，請聯繫 Will 保哥的技術交流中心 粉絲團，我們有專業顧問可以協助解決困難！";
                        document.Info.Contact = new NSwag.OpenApiContact
                        {
                            Name = "Will Huang",
                            Email = "doggy.huang@gmail.com",
                            Url = "https://twitter.com/Will_Huang"
                        };
                    }
                    else
                    {
                        document.Info.TermsOfService = "https://go.microsoft.com/fwlink/?LinkID=206977";

                        document.Info.Contact = new NSwag.OpenApiContact
                        {
                            Name = "Will Huang",
                            Email = "doggy.huang@gmail.com",
                            Url = "https://twitter.com/Will_Huang"
                        };
                    }

                    document.Info.License = new NSwag.OpenApiLicense
                    {
                        Name = "The MIT License",
                        Url = "https://opensource.org/licenses/MIT"
                    };
                };
            });

            app.UseSwaggerUi3(config =>
            {
                // 這裡的 Path 用來設定 Swagger UI 的路由 (網址路徑) (一定要以 / 斜線開頭)
                config.Path = "/swagger";

                // 這裡的 DocumentPath 用來設定 OpenAPI 文件的網址路徑 (一定要以 / 斜線開頭)
                config.DocumentPath = "/swagger/v2/swagger.json";

                // 這裡的 DocExpansion 用來設定 Swagger UI 是否要展開文件 (可設定為 none, list, full，預設: none)
                config.DocExpansion = "list";
            });

            app.UseReDoc(config =>
            {
                // 這裡的 Path 用來設定 ReDoc UI 的路由 (網址路徑) (一定要以 / 斜線開頭)
                config.Path = "/redoc";

                // 這裡的 DocumentPath 用來設定 OpenAPI 文件的網址路徑 (一定要以 / 斜線開頭)
                config.DocumentPath = "/swagger/v2/swagger.json";
            });
        }
    }
}