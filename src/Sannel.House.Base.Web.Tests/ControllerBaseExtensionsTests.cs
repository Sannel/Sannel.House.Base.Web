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
#if NETCOREAPP2_1
			Assert.Equal("", ControllerBaseExtensions.GetAuthToken(null));
#endif
			controller.HttpContext.Request.Headers.Clear();
			Assert.Equal("", controller.GetAuthToken());
		}
	}
}
