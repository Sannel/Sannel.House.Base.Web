/* Copyright 2020 Sannel Software, L.L.C.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sannel.House.Base.Web
{
	public static class ErrorResponseModelExtensions
	{
		/// <summary>
		/// Fills the Errors Dictionary with the passed state dictionary.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <param name="modelState">State of the model.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">model</exception>
		public static ErrorResponseModel FillWithStateDictionary(this ErrorResponseModel model, ModelStateDictionary modelState)
		{
			if(model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}

			foreach(var k in modelState ?? throw new ArgumentNullException(nameof(modelState)))
			{
				model.Errors.Add(k.Key, k.Value.Errors.Select(i => i.ErrorMessage).ToArray());
			}

			return model;
		}

		/// <summary>
		/// Fills the Errors Dictionary with the passed state dictionary asynchronous.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <param name="modelState">State of the model.</param>
		/// <returns></returns>
		public static Task<ErrorResponseModel> FillWithStateDictionaryAsync(this ErrorResponseModel model, ModelStateDictionary modelState)
			=> Task.Run(() => FillWithStateDictionary(model, modelState));
	}
}
