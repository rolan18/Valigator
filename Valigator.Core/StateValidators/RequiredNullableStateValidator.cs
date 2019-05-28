﻿using System;
using System.Collections.Generic;
using System.Text;
using Functional;
using Valigator.Core.Descriptors;
using Valigator.Core.ValueValidators;

namespace Valigator.Core.StateValidators
{
	public struct RequiredNullableStateValidator<TValue> : IStateValidator<Option<TValue>>
	{
		private static DataValidator<RequiredNullableStateValidator<TValue>, PassthroughValidator<Option<TValue>>, Option<TValue>> Instance { get; } = new DataValidator<RequiredNullableStateValidator<TValue>, PassthroughValidator<Option<TValue>>, Option<TValue>>(default, default);

		public Data<Option<TValue>> Data => new Data<Option<TValue>>(Instance);

		IStateDescriptor IStateValidator<Option<TValue>>.GetDescriptor()
			=> new RequiredStateDescriptor(true);

		Result<Option<TValue>, ValidationError> IStateValidator<Option<TValue>>.Validate(object model, bool isSet, Option<TValue> value)
			=> isSet
			? Result.Success<Option<TValue>, ValidationError>(value)
			: Result.Failure<Option<TValue>, ValidationError>(new ValidationError(""));

		public static implicit operator Data<Option<TValue>>(RequiredNullableStateValidator<TValue> stateValidator)
			=> stateValidator.Data;
	}
}
