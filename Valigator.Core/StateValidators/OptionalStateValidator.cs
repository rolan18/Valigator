﻿using System;
using System.Collections.Generic;
using System.Text;
using Functional;
using Valigator.Core.Descriptors;
using Valigator.Core.ValueValidators;

namespace Valigator.Core.StateValidators
{
	public struct OptionalStateValidator<TValue> : IStateValidator<Option<TValue>>
	{
		private static DataValidator<OptionalStateValidator<TValue>, PassthroughValidator<Option<TValue>>, Option<TValue>> Instance { get; } = new DataValidator<OptionalStateValidator<TValue>, PassthroughValidator<Option<TValue>>, Option<TValue>>(default, default);

		public Data<Option<TValue>> Data => new Data<Option<TValue>>(Instance);

		public OptionalNullableStateValidator<TValue> Nullable()
			=> new OptionalNullableStateValidator<TValue>();

		IStateDescriptor IStateValidator<Option<TValue>>.GetDescriptor()
			=> new OptionalStateDescriptor(false);

		Result<Option<TValue>, ValidationError> IStateValidator<Option<TValue>>.Validate(object model, bool isSet, Option<TValue> value)
			=> isSet
				? value.Match(some => Result.Success<Option<TValue>, ValidationError>(Option.Some(some)), () => Result.Failure<Option<TValue>, ValidationError>(new ValidationError("")))
				: Result.Success<Option<TValue>, ValidationError>(Option.None<TValue>());
			

		public static implicit operator Data<Option<TValue>>(OptionalStateValidator<TValue> stateValidator)
			=> stateValidator.Data;
	}
}
