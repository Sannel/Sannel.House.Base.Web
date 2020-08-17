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

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Sannel.House.Base.Web
{
	public static class IHealthChecksBuilderExtensions
	{
		/// <summary>
		/// Adds the web request health check.
		/// </summary>
		/// <param name="builder">The builder.</param>
		/// <param name="remoteUri">The remote URI to connect to.</param>
		/// <param name="description">The description.</param>
		/// <param name="unhealthyOnError">if set to <c>true</c> unhealthy <c>false</c> degraded</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">builder</exception>
		public static IHealthChecksBuilder AddWebRequestHealthCheck(this IHealthChecksBuilder builder,
			Uri remoteUri,
			string description=null,
			bool unhealthyOnError=false)
		{
			if(builder is null)
			{
				throw new ArgumentNullException(nameof(builder));
			}

			builder.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
				(remoteUri ?? throw new ArgumentNullException(nameof(remoteUri))).ToString(),
				(s) =>
				{
					return new WebRequestHealthCheck(remoteUri, 
						s.GetService<IHttpClientFactory>(), 
						description, 
						unhealthyOnError);
				},
				null, null)
				);

			return builder;
		}

		/// <summary>
		/// Adds the web requests health checks.
		/// </summary>
		/// <param name="builder">The builder.</param>
		/// <param name="uris">The uris.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">
		/// builder
		/// or
		/// uris
		/// </exception>
		public static IHealthChecksBuilder AddWebRequestsHealthChecks(this IHealthChecksBuilder builder,
			params Uri[] uris)
		{
			if(builder is null)
			{
				throw new ArgumentNullException(nameof(builder));
			}

			if(uris is null)
			{
				throw new ArgumentNullException(nameof(uris));
			}

			foreach(var u in uris)
			{
				builder.AddWebRequestHealthCheck(u);
			}

			return builder;
		}
	}
}
