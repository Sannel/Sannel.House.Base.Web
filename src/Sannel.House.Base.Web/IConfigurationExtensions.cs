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
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Sannel.House.Base.Web
{
	public static class IConfigurationExtensions
	{
		/// <summary>
		/// The replacement regex
		/// </summary>
		public static readonly Regex ReplacementRegex = new Regex(
			"\\$\\{(?<key>[a-zA-Z0-9_\\-:]+)\\}",
			RegexOptions.CultureInvariant
			| RegexOptions.Compiled
		);

		/// <summary>
		/// Gets the configuration value with replacement.
		/// </summary>
		/// <param name="configuration">The configuration.</param>
		/// <param name="key">The key.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">
		/// configuration
		/// or
		/// key
		/// </exception>
		public static string GetWithReplacement(this IConfiguration configuration, string key)
		{
			if(configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}

			if(string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			var value = configuration[key];
			if(value != null)
			{
				value = ReplacementRegex.Replace(value, (Match target) =>
				{
					var group = target.Groups["key"];
					if (group.Success)
					{
						return configuration[group.Value];
					}

					return target.Value;

				});
			}

			return value;
		}

	}
}
