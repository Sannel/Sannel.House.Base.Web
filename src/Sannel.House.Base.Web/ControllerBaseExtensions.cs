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
