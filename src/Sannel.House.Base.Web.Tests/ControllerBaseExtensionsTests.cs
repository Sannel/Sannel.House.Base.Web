using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Sannel.House.Base.Web.Tests
{
	public class ControllerBaseExtensionsTests
	{
		[Fact]
		public void GetAuthTokenTest()
		{
			var mcontroller = new Mock<ControllerBase>();
			var controller = mcontroller.Object;
			controller.ControllerContext = new ControllerContext();
			controller.ControllerContext.HttpContext = new DefaultHttpContext();
			controller.HttpContext.Request.Headers.Add("Authorization", new Microsoft.Extensions.Primitives.StringValues("Bearer 1234"));

			var token = controller.GetAuthToken();

			Assert.Equal("1234", token);
			Assert.Equal("", ControllerBaseExtensions.GetAuthToken(null));
			controller.HttpContext.Request.Headers.Clear();
			Assert.Equal("", controller.GetAuthToken());
		}
	}
}
