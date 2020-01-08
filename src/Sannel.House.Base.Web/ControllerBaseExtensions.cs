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
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sannel.House.Base.Web
{
	public static class ControllerBaseExtensions
	{
		/// <summary>
		/// Gets the authentication token from Request on the passed controller
		/// </summary>
		/// <param name="controller">The controller.</param>
		/// <returns></returns>
		public static string GetAuthToken(this ControllerBase controller)
		{
			if (controller == null)
			{
				return string.Empty;
			}

			string auth = controller.HttpContext?.Request?.Headers["Authorization"];
			if (auth != null)
			{
				var segments = auth.Split(' ');
				if (segments?.Length == 2)
				{
					return segments[1];
				}
			}

			return string.Empty;

		}
	}
}
