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

		[Fact]
		public void GetWithReplacementArgumentTest()
		{
			Assert.Throws<ArgumentNullException>("configuration", () => IConfigurationExtensions.GetWithReplacement(null, null));

			IConfigurationBuilder configuration = new ConfigurationBuilder();
			IConfiguration config = configuration.Build();
			Assert.Throws<ArgumentNullException>("key", () => config.GetWithReplacement(null));
		}
	}
}
