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