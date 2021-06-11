/* Copyright 2021-2021 Sannel Software, L.L.C.
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
      http://www.apache.org/licenses/LICENSE-2.0
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/

using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sannel.House.Base.Web.AuthorizationRequirement
{
	/// <summary>
	/// Authorization rule to require at least one passed scope is in the scope claim
	/// </summary>
	/// <seealso cref="Microsoft.AspNetCore.Authorization.AuthorizationHandler{Sannel.House.Base.Web.AuthorizationRequirement.ScopeAuthorizationRequirement}" />
	/// <seealso cref="Microsoft.AspNetCore.Authorization.IAuthorizationRequirement" />
	public class ScopeAuthorizationRequirement : AuthorizationHandler<ScopeAuthorizationRequirement>, IAuthorizationRequirement
	{
		/// <summary>
		/// The scopetype
		/// </summary>
		public const string SCOPETYPE = "scope";

		private readonly string[] scopes;

		/// <summary>
		/// Initializes a new instance of the <see cref="ScopeAuthorizationRequirement"/> class.
		/// </summary>
		/// <param name="scopes">The scopes.</param>
		public ScopeAuthorizationRequirement(params string[] scopes)
			=> this.scopes = scopes ?? Array.Empty<string>();

		/// <summary>
		/// Handles the requirement asynchronous.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="requirement">The requirement.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">nameof(context)</exception>
		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeAuthorizationRequirement requirement)
		{
			if (context is null)
			{
				throw new ArgumentNullException(nameof(context));
			}
			var scope = context.User?.Claims?.FirstOrDefault(i => string.Equals(i.Type, SCOPETYPE, StringComparison.OrdinalIgnoreCase));
			if (scope is not null)
			{

				var s = scope.Value?.Split(" ", StringSplitOptions.RemoveEmptyEntries);

				if (s is not null && s.Any() && scopes.Any())
				{
					if (s.Any(i => this.scopes.Contains(i, StringComparer.Ordinal)))
					{
						context.Succeed(requirement);
					}
				}
			}

			return Task.CompletedTask;
		}
	}

}
