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

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Sannel.House.Base.Web.Tests
{
	public class IConfigurationExtensionsTests
	{
		[Fact]
		public void GetWithReplacementTest()
		{
			IConfigurationBuilder configuration = new ConfigurationBuilder();
			configuration.AddInMemoryCollection(new Dictionary<string, string>()
			{
				{"value1", "cheese" },
				{"value2", "cheddar ${value1}" }
			});

			IConfiguration config = configuration.Build();
			var value1 = config.GetWithReplacement("value1");
			var value2 = config.GetWithReplacement("value2");

			Assert.Equal("cheese", value1);
			Assert.Equal("cheddar cheese", value2);
		}

#if NETCOREAPP2_1
		[Fact]
		public void GetWithReplacementArgumentTest()
		{
			Assert.Throws<ArgumentNullException>("configuration", () => IConfigurationExtensions.GetWithReplacement(null, null));

			IConfigurationBuilder configuration = new ConfigurationBuilder();
			IConfiguration config = configuration.Build();
			Assert.Throws<ArgumentNullException>("key", () => config.GetWithReplacement(null));
		}
#endif
	}
}
