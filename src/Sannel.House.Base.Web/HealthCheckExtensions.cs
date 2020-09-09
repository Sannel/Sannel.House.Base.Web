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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Xml;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Sannel.House.Base.Web
{
	public static class HealthCheckExtensions
	{
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
#if !NETSTANDARD2_0
					i.Value.Tags
#endif
				}
				)
			};

			await JsonSerializer.SerializeAsync(stream, response).ConfigureAwait(false);
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
			if (report is null)
			{
				throw new ArgumentNullException(nameof(report));
			}

			using var writer = new XmlTextWriter(stream, Encoding.UTF8);
			await writer.WriteStartDocumentAsync().ConfigureAwait(false);
			await writer.WriteStartElementAsync(null, "html", null).ConfigureAwait(false);

			await writer.WriteStartElementAsync(null, "head", null).ConfigureAwait(false);
			{
				await writer.WriteElementStringAsync(null, "title", null, report.Status.ToString()).ConfigureAwait(false);
				await writer.WriteElementStringAsync(null, "style", null, @"
.green{
	color: green;
}
.orange{
	color: orange;
}
.red{
	color: red;
}
.toggle .body{
	display: none;
}
.toggle.open .body{
	display: block;
}").ConfigureAwait(false);
				await writer.WriteStartElementAsync(null, "script", null).ConfigureAwait(false);
				await writer.WriteAttributeStringAsync(null, "type", null, "text/javascript").ConfigureAwait(false);
				await writer.WriteStringAsync(@"
function setupClick()
{
	var toggles = document.querySelectorAll('.toggle .header');
	toggles.forEach(item => item.addEventListener('click', event =>
	{
		var element = event.target;
		element.parentNode.classList.toggle('open');
	});
}
document.onreadystatechange = () => {
	if(document.readyState === 'complete'){
		setupClick();
	}
}
").ConfigureAwait(false);
				await writer.WriteEndElementAsync().ConfigureAwait(false);
			}
			await writer.WriteEndElementAsync().ConfigureAwait(false);

			await writer.WriteStartElementAsync(null, "body", null).ConfigureAwait(false);
			{
				await writer.WriteStartElementAsync(null, "h1", null).ConfigureAwait(false);
				{
					await writer.WriteStringAsync("Status: ").ConfigureAwait(false);
					await writer.WriteStartElementAsync(null, "span", null).ConfigureAwait(false);
					await writer.WriteAttributeStringAsync(null, "class", null, colorStatus(report.Status)).ConfigureAwait(false);
					await writer.WriteStringAsync(report.Status.ToString()).ConfigureAwait(false);
					await writer.WriteEndElementAsync().ConfigureAwait(false);
				}
				await writer.WriteEndElementAsync().ConfigureAwait(false);

				await writer.WriteElementStringAsync(null, "p", null, $"TotalDuration: {report.TotalDuration}").ConfigureAwait(false);
				await writer.WriteElementStringAsync(null, "p", null, $"StatusDate: {DateTimeOffset.Now}").ConfigureAwait(false);

				foreach (var key in report.Entries.Keys)
				{
					var item = report.Entries[key];
					await writer.WriteStartElementAsync(null, "hr", null).ConfigureAwait(false);
					await writer.WriteEndElementAsync().ConfigureAwait(false);
					await writer.WriteElementStringAsync(null, "h3", null, key).ConfigureAwait(false);
					await writer.WriteStartElementAsync(null, "p", null).ConfigureAwait(false);
					{
						await writer.WriteStringAsync("Status: ").ConfigureAwait(false);
						await writer.WriteStartElementAsync(null, "span", null).ConfigureAwait(false);
						{
							await writer.WriteAttributeStringAsync(null, "class", null, colorStatus(item.Status)).ConfigureAwait(false);
							await writer.WriteStringAsync(item.Status.ToString()).ConfigureAwait(false);
						}
						await writer.WriteEndElementAsync().ConfigureAwait(false);
					}
					await writer.WriteEndElementAsync().ConfigureAwait(false);

					await writer.WriteElementStringAsync(null, "p", null, $"Duration: {item.Duration.TotalMilliseconds}").ConfigureAwait(false);
					await writer.WriteElementStringAsync(null, "p", null, $"Description: {item.Description}").ConfigureAwait(false);

					if (item.Exception != null)
					{
						await writer.WriteStartElementAsync(null, "p", null).ConfigureAwait(false);
						{
							await writer.WriteStringAsync("Exception:").ConfigureAwait(false);
							await writer.WriteStartElementAsync(null, "br ", null).ConfigureAwait(false);
							await writer.WriteEndElementAsync().ConfigureAwait(false);
							await writer.WriteStringAsync(item.Exception.ToString()).ConfigureAwait(false);
						}
						await writer.WriteEndElementAsync().ConfigureAwait(false);
					}

					if (item.Data?.Count > 0)
					{
						var id = 0;
						await writer.WriteElementStringAsync(null, "h4", null, "Data").ConfigureAwait(false);
						foreach (var d in item.Data)
						{
							await writer.WriteStartElementAsync(null, "div", null).ConfigureAwait(false);
							{
								await writer.WriteAttributeStringAsync(null, "class", null, "toggle").ConfigureAwait(false);
								await writer.WriteAttributeStringAsync(null, "data-id", null, $"id{id}").ConfigureAwait(false);

								await writer.WriteStartElementAsync(null, "div", null).ConfigureAwait(false);
								{
									await writer.WriteAttributeStringAsync(null, "class", null, "header").ConfigureAwait(false);
									await writer.WriteAttributeStringAsync(null, "data-id", null, $"id{id}").ConfigureAwait(false);

									await writer.WriteStringAsync(d.Key).ConfigureAwait(false);
								}
								await writer.WriteEndElementAsync().ConfigureAwait(false);

								await writer.WriteStartElementAsync(null, "div", null).ConfigureAwait(false);
								{
									await writer.WriteAttributeStringAsync(null, "class", null, "body").ConfigureAwait(false);
									await writer.WriteAttributeStringAsync(null, "data-id", null, $"id{id}").ConfigureAwait(false);

									await writer.WriteStringAsync(d.Value?.ToString()).ConfigureAwait(false);
								}
								await writer.WriteEndElementAsync().ConfigureAwait(false);
							}
							await writer.WriteEndElementAsync().ConfigureAwait(false);
							id++;
						}
					}

#if !NETSTANDARD2_0
					if (item.Tags.Any())
					{
						await writer.WriteElementStringAsync(null, "h4", null, "Tags").ConfigureAwait(false);
						await writer.WriteElementStringAsync(null, "p", null, string.Join(",", item.Tags)).ConfigureAwait(false);
					}
#endif
				}
			}
			writer.WriteEndElement();

			writer.WriteEndElement();
			writer.WriteEndDocument();
		}

#if NETSTANDARD2_0
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
#else
		public static IEndpointRouteBuilder MapHouseHealthChecks(this IEndpointRouteBuilder builder, PathString path)
		{
			if (builder is null)
			{
				throw new ArgumentNullException(nameof(builder));
			}

			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentNullException(nameof(path));
			}
#endif

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

#if NETSTANDARD2_0
			applicationBuilder.UseHealthChecks(path, options);

			return applicationBuilder;
		}
#else
			builder.MapHealthChecks(path, options);

			return builder;
		}
#endif
	}
}
