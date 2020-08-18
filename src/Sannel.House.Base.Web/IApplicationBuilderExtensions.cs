/* Copyright 2020-2020 Sannel Software, L.L.C.

   Licensed under the Apache License, Version 2.0 (the ""License"");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

	   http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an ""AS IS"" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.AspNetCore.Builder
{
	public static class IApplicationBuilderExtensions
	{
		/// <summary>
		/// Writes the json asynchronous.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="report">The report.</param>
		private static async Task writeJsonAsync(Stream stream, HealthReport report)
		{
			var response = new
			{
				StatusDate = DateTimeOffset.Now,
				Status = report.Status.ToString(),
				TotalDurationMilliseconds = report.TotalDuration.Milliseconds,
				Entries = report.Entries.Select(i =>
				new
				{
					Name = i.Key,
					i.Value.Description,
					Response = i.Value.Data,
					DurationMilliseconds = i.Value.Duration.Milliseconds,
					i.Value.Exception,
					Status = i.Value.Status.ToString(),
					i.Value.Tags
				}
				)
			};

			var data = JsonSerializer.Serialize(response);

			var b = Encoding.UTF8.GetBytes(data);

#if NETSTANDARD2_0
			await stream.WriteAsync(b, 0, b.Length);
#else
			await stream.WriteAsync(b);
#endif
		}

		private static async Task writeStringAsync(Stream stream, string data)
		{
			var b = Encoding.UTF8.GetBytes(data);
#if NETSTANDARD2_0
			await stream.WriteAsync(b, 0, b.Length);
#else
			await stream.WriteAsync(b);
#endif
		}

		private static string colorStatus(HealthStatus status) 
			=> status switch
			{
				HealthStatus.Healthy => "green",
				HealthStatus.Degraded => "orange",
				HealthStatus.Unhealthy => "red",
				_ => throw new NotImplementedException()
			};

		private static async Task writeHTMLAsync(Stream stream, HealthReport report)
		{

			await writeStringAsync(stream, @$"
<html>
<head>
	<title>{report.Status}</title>
	<style>
	.green{{
		color: green;
	}}
	.orange{{
		color: orange;
	}}
	.red{{
		color: red;
	}}
	.toggle .body{{
		display: none;
	}}
	.toggle.open .body{{
		display: block;
	}}
	</style>
	<script type=""text/javascript"">
	var toggles = document.querySelectorAll('.toggle .header');
	toggles.forEach(item => item.addEventListener('click', event =>
	{{
		var element = event.target;
		element.parentNode.classList.toggle('open');
	}}));
	</script>
</head>
<body>
	<h1>Status: <span class=""{colorStatus(report.Status)}"">{report.Status}</span></h1>
	<p>TotalDuration: {report.TotalDuration}</p>
	<p>StatusDate: {DateTimeOffset.Now}</p>").ConfigureAwait(false);

			foreach(var key in report.Entries.Keys)
			{
				var item = report.Entries[key];
				await writeStringAsync(stream, "<hr />");
				await writeStringAsync(stream, @$"<h3>{key}</h3>
<p>Status: <span class=""{colorStatus(item.Status)}"">{item.Status}</span></p>
<p>Duration: {item.Duration}</p>
<p>Description: {HttpUtility.HtmlEncode(item.Description)}</p>
");
				if(!(item.Exception is null))
				{
					await writeStringAsync(stream, $"<p>Exception:<br />{HttpUtility.HtmlEncode(item.Exception)}</p>");
				}
				if(item.Data?.Count > 0)
				{
					await writeStringAsync(stream, "<h4>Data</h4>");
					foreach(var d in item.Data)
					{
						var id = Guid.NewGuid();
						await writeStringAsync(stream, $"<div class=\"toggle\" data-id=\"{id}\"><div class=\"header\" data-id=\"{id}\">{HttpUtility.HtmlEncode(d.Key)}</div><div class=\"body\" data-id=\"{id}\">{HttpUtility.HtmlEncode(d.Value)}</div></div>");
					}
				}

				if(item.Tags.Any())
				{
					await writeStringAsync(stream, "<h4>Tags</h4>");
					await writeStringAsync(stream, $"<p>{HttpUtility.HtmlEncode(string.Join(",", item.Tags))}</p>");
				}
			}

			await writeStringAsync(stream, @"
<hr />
</body>
</html>
");
		}

		public static IApplicationBuilder UseHouseHealthChecks(this IApplicationBuilder applicationBuilder,
			PathString path)
		{
			if (applicationBuilder is null)
			{
				throw new ArgumentNullException(nameof(applicationBuilder));
			}

			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentNullException(nameof(path));
			}

			var options = new HealthCheckOptions
			{
				ResponseWriter = async (context, report) =>
				{
					context.Response.StatusCode = report.Status switch
					{
						HealthStatus.Unhealthy => 500,
						_ => 200,
					};

					if (context.Request.Query.ContainsKey("json"))
					{
						context.Response.ContentType = "application/json";
						await writeJsonAsync(context.Response.Body, report).ConfigureAwait(false);
					}
					else
					{
						context.Response.ContentType = "text/html";
						await writeHTMLAsync(context.Response.Body, report).ConfigureAwait(false);
					}
				}
			};

			applicationBuilder.UseHealthChecks(path, options);

			return applicationBuilder;
		}

		/// <summary>
		/// Adds A robots.txt response that tells search engines not to crawl this site
		/// </summary>
		/// <param name="app">The application.</param>
		/// <returns></returns>
		public static IApplicationBuilder UseHouseRobotsTxt(this IApplicationBuilder app)
		{
			if (app is null)
			{
				throw new ArgumentNullException(nameof(app));
			}

			app.Map("/robots.txt", (appBuilder) =>
			{
				appBuilder.Run(async (context) =>
				{
					await context.Response.WriteAsync("User-agent: *\nDisallow: /", Encoding.ASCII).ConfigureAwait(false);
				});
			});

			return app;
		}
	}
}