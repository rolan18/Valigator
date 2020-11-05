﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[assembly: ApiController]
namespace Valigator.TestApi
{
	internal class UnhandledExceptionWrappingFilter : ExceptionFilterAttribute
	{
		public UnhandledExceptionWrappingFilter()
		{
		}

		public override void OnException(ExceptionContext context)
		{
		}
	}
}
