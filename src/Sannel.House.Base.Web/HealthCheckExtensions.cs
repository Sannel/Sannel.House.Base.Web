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
			if(report is null)
			{
				throw new ArgumentNullException(nameof(report));
			}

			using var mstream = new MemoryStream();
			using var writer = new XmlTextWriter(mstream, Encoding.UTF8);
			writer.WriteStartDocument();
			writer.WriteStartElement("html");

			writer.WriteStartElement("head");
				writer.WriteElementString("title", report.Status.ToString());
				writer.WriteElementString("style", @"
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
}");
			writer.WriteStartElement("script");
			writer.WriteAttributeString("type", "text/javascript");
			writer.WriteString(@"
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
");
			writer.WriteEndElement();
			writer.WriteEndElement();

			writer.WriteStartElement("body");
			{
				writer.WriteStartElement("h1");
				{
					writer.WriteString("Status: ");
					writer.WriteStartElement("span");
					writer.WriteAttributeString("class", colorStatus(report.Status));
					writer.WriteString(report.Status.ToString());
					writer.WriteEndElement();
				}
				writer.WriteEndElement();

				writer.WriteElementString("p", $"TotalDuration: {report.TotalDuration}");
				writer.WriteElementString("p", $"StatusDate: {DateTimeOffset.Now}");

				foreach(var key in report.Entries.Keys)
				{
					var item = report.Entries[key];
					writer.WriteStartElement("hr");
					writer.WriteEndElement();
					writer.WriteElementString("h3", key);
					writer.WriteStartElement("p");
					{
						writer.WriteString("Status: ");
						writer.WriteStartElement("span");
						{
							writer.WriteAttributeString("class", colorStatus(item.Status));
							writer.WriteString(item.Status.ToString());
						}
						writer.WriteEndElement();
					}
					writer.WriteEndElement();

					writer.WriteElementString("p", $"Duration: {item.Duration.TotalMilliseconds}");
					writer.WriteElementString("p", $"Description: {item.Description}");

					if(item.Exception != null)
					{
						writer.WriteStartElement("p");
						{
							writer.WriteString("Exception:");
							writer.WriteStartElement("br ");
							writer.WriteEndElement();
							writer.WriteString(item.Exception.ToString());
						}
						writer.WriteEndElement();
					}

					if(item.Data?.Count > 0)
					{
						var id = 0;
						writer.WriteElementString("h4", "Data");
						foreach(var d in item.Data)
						{
							writer.WriteStartElement("div");
							{
								writer.WriteAttributeString("class", "toggle");
								writer.WriteAttributeString("data-id", $"id{id}");

								writer.WriteStartElement("div");
								{
									writer.WriteAttributeString("class", "header");
									writer.WriteAttributeString("data-id", $"id{id}");

									writer.WriteString(d.Key);
								}
								writer.WriteEndElement();

								writer.WriteStartElement("div");
								{
									writer.WriteAttributeString("class", "body");
									writer.WriteAttributeString("data-id", $"id{id}");

									writer.WriteString(d.Value?.ToString());
								}
								writer.WriteEndElement();
							}
							writer.WriteEndElement();
							id++;
						}
					}

#if !NETSTANDARD2_0
					if (item.Tags.Any())
					{
						writer.WriteElementString("h4", "Tags");
						writer.WriteElementString("p", string.Join(",", item.Tags));
					}
#endif
				}
			}
			writer.WriteEndElement();

			writer.WriteEndElement();
			writer.WriteEndDocument();

			writer.Flush();

			mstream.Seek(0, SeekOrigin.Begin);
			await mstream.CopyToAsync(stream).ConfigureAwait(false);
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
