/* Copyright 2020 Sannel Software, L.L.C.

   Licensed under the Apache License, Version 2.0 (the ""License"");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

	   http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an ""AS IS"" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sannel.House.Base.Web
{
	public static class IServiceProviderExtensions
	{
		/// <summary>Installs the certificate.</summary>
		/// <param name="name">The name.</param>
		/// <param name="location">The location.</param>
		/// <param name="fullPath">The full path.</param>
		/// <param name="log">The log.</param>
		public static bool InstallCertificate(StoreName name, StoreLocation location, string fullPath, ILogger log)
		{
			using var cert = new X509Certificate2(fullPath);
			using var store = new X509Store(name, location);
			try
			{
				store.Open(OpenFlags.ReadWrite);
				if (!store.Certificates.Contains(cert))
				{
					log.LogInformation($"Trying to install cert {cert.SubjectName}");
					store.Add(cert);
					log.LogInformation("Cert Installed");
				}
				else
				{
					log.LogInformation("Cert is already installed");
				}
				store.Close();
				return true;
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				log.LogError(ex, "Exception installing Cert");
				return false;
			}
		}

		/// <summary>Checks if the cert is installed and if not attempts to install it.</summary>
		/// <param name="provider">The provider.</param>
		public static void CheckAndInstallTrustedCertificate(this IServiceProvider provider)
		{
			var config = provider.GetService<IConfiguration>() ?? throw new ArgumentException("No service is configured for IConfiguration");
			var log = provider.GetService<ILogger<IServiceProvider>>() ?? throw new ArgumentException("No service is configured for ILogger<>");
			var shouldInstall = config.GetValue<bool?>("Cert:Install");

			if (shouldInstall == true)
			{
				var fullPath = Path.GetFullPath(config["Cert:Crt"]);
				if(!File.Exists(fullPath))
				{
					log.LogError("File not found {0}", fullPath);
					return;
				}

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					using (var identity = WindowsIdentity.GetCurrent())
					{
						if(!InstallCertificate(StoreName.AuthRoot, StoreLocation.LocalMachine, fullPath, log))
						{
							log.LogWarning("Unable to install cert in LocalMachine trying to install in CurrentUser");
							if(!InstallCertificate(StoreName.CertificateAuthority, StoreLocation.CurrentUser, fullPath, log))
							{
								log.LogError("Unable to install certificate");
							}
							else
							{
								log.LogInformation("Cert installed into CurrentUser");
							}
						}
						else
						{
							log.LogInformation("Cert installed into LocalMachine");
						}

					}
				}
				else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					InstallCertificate(StoreName.CertificateAuthority, StoreLocation.CurrentUser, fullPath, log);
				}
				else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					if (Directory.Exists("/etc/ssl/certs"))
					{
						log.LogInformation("/etc/ssl/certs exists check if the cert is installed");
						var fileName = Path.GetFileNameWithoutExtension(fullPath);
						var sslPath = Path.Combine("/etc/ssl/certs", $"{fileName}.pem");
						if(!File.Exists(sslPath))
						{
							log.LogInformation($"Installing cert {fullPath}");
							try
							{
								File.Copy(fullPath, sslPath);
								log.LogInformation("Finished installing cert");
							}
							catch(IOException ioe)
							{
								log.LogError(ioe, "Error installing cert");
							}

						}
						else
						{
							log.LogInformation($"Cert is already installed {fileName}");
						}
					}
				}
			}
		}
	}
}
