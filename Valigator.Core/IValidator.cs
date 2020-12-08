﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Valigator.Core
{
	public interface IValidator<TValue>
	{
		ValidatorResult Validate(TValue value);
	}
}
