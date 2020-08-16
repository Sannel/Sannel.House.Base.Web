using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
				report.Status,
				report.TotalDuration,
				report.Entries
			};

			var data = JsonSerializer.Serialize(response);

			using var streamWriter = new StreamWriter(stream);
			await streamWriter.WriteAsync(data).ConfigureAwait(false);

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

			using var streamWriter = new StreamWriter(stream);
			await streamWriter.WriteAsync(@$"
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
	<p>TotalDuration: {report.TotalDuration}</p>").ConfigureAwait(false);

			foreach(var key in report.Entries.Keys)
			{
				var item = report.Entries[key];
				await streamWriter.WriteAsync("<hr />").ConfigureAwait(false);
				await streamWriter.WriteAsync(@$"<h3>{key}</h3>
<p>Status: <span class=""{colorStatus(item.Status)}"">{item.Status}</span></p>
<p>Duration: {item.Duration}</p>
<p>Description: {item.Description}</p>
").ConfigureAwait(false);
				if(!(item.Exception is null))
				{
					await streamWriter.WriteAsync($"<p>Exception: {item.Exception}</p>").ConfigureAwait(false);
				}
				if(item.Data?.Count > 0)
				{
					await streamWriter.WriteAsync("<h4>Data</h4>").ConfigureAwait(false);
					foreach(var d in item.Data)
					{
						await streamWriter.WriteAsync($"<p>{d.Key}={d.Value}</p>").ConfigureAwait(false);
					}
				}

				if(item.Tags.Any())
				{
					await streamWriter.WriteAsync("<h4>Tags</h4>").ConfigureAwait(false);
					await streamWriter.WriteAsync($"<p>{string.Join(",", item.Tags)}</p>").ConfigureAwait(false);
				}
			}

			await streamWriter.WriteAsync(@"
<hr />
</body>
</html>
").ConfigureAwait(false);
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