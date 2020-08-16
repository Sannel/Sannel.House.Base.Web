using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
				report.Status,
				report.TotalDuration,
				report.Entries
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
	</style>
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
<p>Description: {item.Description}</p>
");
				if(!(item.Exception is null))
				{
					await writeStringAsync(stream, $"<p>Exception: {item.Exception}</p>");
				}
				if(item.Data?.Count > 0)
				{
					await writeStringAsync(stream, "<h4>Data</h4>");
					foreach(var d in item.Data)
					{
						await writeStringAsync(stream, $"<p>{d.Key}={d.Value}</p>");
					}
				}

				if(item.Tags.Any())
				{
					await writeStringAsync(stream, "<h4>Tags</h4>");
					await writeStringAsync(stream, $"<p>{string.Join(",", item.Tags)}</p>");
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
	}
}