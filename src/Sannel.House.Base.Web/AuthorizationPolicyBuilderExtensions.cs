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

using Sannel.House.Base.Web.AuthorizationRequirement;
using System;

namespace Microsoft.AspNetCore.Authorization
{
	public static class AuthorizationPolicyBuilderExtensions
	{
		/// <summary>
		/// Adds a requirement that requires a scope claim containing at least one scope who's value matches at least one item in <paramref name="scopes"/>
		/// </summary>
		/// <param name="builder">The builder.</param>
		/// <param name="scopes">The scopes.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">builder</exception>
		public static AuthorizationPolicyBuilder AddScopeRequirement(this AuthorizationPolicyBuilder builder, params string[] scopes)
		{
			if(builder is null)
			{
				throw new ArgumentNullException(nameof(builder));
			}

			return builder.AddRequirements(new ScopeAuthorizationRequirement(scopes));
		}
	}
}
