/* Copyright 2019 Sannel Software, L.L.C.
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
      http://www.apache.org/licenses/LICENSE-2.0
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Sannel.House.Base.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Sannel.House.Base.Web.Tests
{
	public class ErrorResponseModelExtensionsTests
	{
		[Fact]
		public async Task FillWithStateDictionaryAsyncTest()
		{
			ErrorResponseModel model = null;

			Assert.Throws<ArgumentNullException>("model", () => model.FillWithStateDictionary(null));
			model = new ErrorResponseModel();
			Assert.Throws<ArgumentNullException>("modelState", () => model.FillWithStateDictionary(null));
			var state = new ModelStateDictionary();
			await model.FillWithStateDictionaryAsync(state);
			Assert.Empty(model.Errors);
			state.AddModelError("key1", "Error 1");

			Assert.NotNull(await model.FillWithStateDictionaryAsync(state));
			Assert.Single(model.Errors);
			Assert.True(model.Errors.ContainsKey("key1"));
			Assert.Equal("Error 1", string.Join(',', model.Errors["key1"]));

			state.Remove("key1");
			state.AddModelError("key2", "Error 2");
			state.AddModelError("key3", "Error 3");
			Assert.NotNull(model.FillWithStateDictionary(state));
			Assert.Equal(3, model.Errors.Count);

			Assert.True(model.Errors.ContainsKey("key1"));
			Assert.Equal("Error 1", string.Join(',', model.Errors["key1"]));

			Assert.True(model.Errors.ContainsKey("key2"));
			Assert.Equal("Error 2", string.Join(',', model.Errors["key2"]));

			Assert.True(model.Errors.ContainsKey("key3"));
			Assert.Equal("Error 3", string.Join(',', model.Errors["key3"]));
		}
	}
}
