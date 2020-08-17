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

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sannel.House.Base.Web
{
	public class WebRequestHealthCheck : IHealthCheck
	{
		private readonly Uri remoteHealthCheckUri;
		private readonly IHttpClientFactory httpFactory;
		private readonly string description;
		private readonly bool unhealthyOnError;

		public WebRequestHealthCheck(Uri remoteHealthCheckUri,
			IHttpClientFactory httpFactory,
			string description = null,
			bool unhealthyOnError=false)
		{
			this.remoteHealthCheckUri = remoteHealthCheckUri ?? throw new ArgumentNullException(nameof(remoteHealthCheckUri));
			this.httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
			this.description = description;
			this.unhealthyOnError = unhealthyOnError;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't want to crash healthchecks when no response is sent")]
		public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				using var client = httpFactory.CreateClient();
				var result = await client.GetAsync(remoteHealthCheckUri, cancellationToken).ConfigureAwait(false);

				var data = new Dictionary<string, object>();

				try
				{
					data["Response"] = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
				}
				catch
				{

				}

				data["StatusCode"] = result.StatusCode;

				if (result.IsSuccessStatusCode)
				{
					return new HealthCheckResult(HealthStatus.Healthy, description, data: data);
				}
				else
				{
					return new HealthCheckResult(
						(unhealthyOnError)?
							HealthStatus.Unhealthy: 
							HealthStatus.Degraded, 
						description, 
						data: data);
				}
			}
			catch(Exception ex)
			{
				var data = new Dictionary<string, object>
				{
					{"Exception", ex}
				};

				return new HealthCheckResult(
					(unhealthyOnError)?
						HealthStatus.Unhealthy: 
						HealthStatus.Degraded, 
					description, 
					data: data);
			}
		}
	}
}
