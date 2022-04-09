﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Microsoft.OpenApi.Models;
using System.IO;
using Microsoft.EntityFrameworkCore;
namespace LuqinOfficialAccount
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

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "OfficailAccountApi", Version = "v1" });
            });


            string path = $"{Environment.CurrentDirectory}";

            if (path.StartsWith("/"))
            {
                path = path + "/";
            }
            else
            {
                path = path + "\\";
            }
            path = path + "config.sqlServer";

            string conStr = "";

            using (StreamReader sr = new StreamReader(path, true))
            {
                conStr = sr.ReadToEnd();
                sr.Close();
            }

            services.AddDbContext<AppDBContext>(
                options => options.UseSqlServer(conStr)
            );

            services.AddSession(options=> {
                options.IdleTimeout = TimeSpan.FromSeconds(3600);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (env.IsDevelopment())
            {
                Util.isDev = true;
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SnowmeetApi v1"));
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            

            

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            
        }
    }
}
