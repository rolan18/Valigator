﻿using System;
using System.Collections.Generic;
using System.Text;
using Functional;
using Valigator.Core.DataContainers;
using Valigator.Core.Helpers;
using Valigator.Core.StateDescriptors;
using Valigator.Core.ValueDescriptors;
using Valigator.Core.ValueValidators;

namespace Valigator.Core.StateValidators
{
	public struct DefaultedNullableCollectionStateValidator<TValue> : ICollectionStateValidator<Option<TValue>[], TValue>
	{
		private static IDataContainer<Option<TValue>[]> CreateContainer(DefaultedNullableCollectionStateValidator<TValue> stateValidator)
			=> new CollectionNullableDataContainer<DefaultedNullableCollectionStateValidator<TValue>, DummyValidator<Option<TValue>[]>, DummyValidator<Option<TValue>[]>, DummyValidator<Option<TValue>[]>, TValue, TValue>(Mapping.CreatePassthrough<TValue>(), stateValidator, DummyValidator<Option<TValue>[]>.Instance, DummyValidator<Option<TValue>[]>.Instance, DummyValidator<Option<TValue>[]>.Instance);

		public Data<Option<TValue>[]> Data => new Data<Option<TValue>[]>(CreateContainer(this));

		private readonly Data<TValue> _item;

		private readonly Option<Option<TValue>[]> _defaultValue;

		private readonly Func<Option<TValue>[]> _defaultValueFactory;

		public DefaultedNullableCollectionStateValidator(Data<TValue> item, Option<TValue>[] defaultValue)
		{
			_item = item;
			_defaultValue = Option.Some(defaultValue ?? throw new NullDefaultException());
			_defaultValueFactory = default;
		}

		public DefaultedNullableCollectionStateValidator(Data<TValue> item, Func<Option<TValue>[]> defaultValueFactory)
		{
			_item = item;
			_defaultValue = default;
			_defaultValueFactory = defaultValueFactory ?? throw new ArgumentNullException(nameof(defaultValueFactory));
		}

		public NullableDefaultedNullableCollectionStateValidator<TValue> Nullable()
		{
			if (_defaultValueFactory != null)
				return new NullableDefaultedNullableCollectionStateValidator<TValue>(_item, _defaultValueFactory);

			if (_defaultValue.TryGetValue(out var some))
				return new NullableDefaultedNullableCollectionStateValidator<TValue>(_item, some);

			return new NullableDefaultedNullableCollectionStateValidator<TValue>();
		}

		private Option<TValue>[] GetDefaultValue()
			=> StateValidatorHelpers.GetDefaultValue(_defaultValue, _defaultValueFactory);

		IStateDescriptor IStateValidator<Option<TValue>[], Option<TValue>[]>.GetDescriptor()
			=> new CollectionStateDescriptor(StateValidatorHelpers.GetDefaultValueForDescriptor(_defaultValue, _defaultValueFactory), _item.DataDescriptor);

		IValueDescriptor[] IStateValidator<Option<TValue>[], Option<TValue>[]>.GetImplicitValueDescriptors()
			=> new[] { new NotNullDescriptor() };

		Result<Option<TValue>[], ValidationError[]> IStateValidator<Option<TValue>[], Option<TValue>[]>.Validate(Option<Option<Option<TValue>[]>> value)
		{
			if (value.TryGetValue(out var isSet))
			{
				if (isSet.TryGetValue(out var notNull))
					return Result.Success<Option<TValue>[], ValidationError[]>(notNull);

				return Result.Failure<Option<TValue>[], ValidationError[]>(new[] { ValidationErrors.NotNull() });
			}

			return Result.Success<Option<TValue>[], ValidationError[]>(GetDefaultValue());
		}

		public Result<Unit, ValidationError[]> IsValid(Option<object> model, Option<TValue>[] value)
			=> StateValidatorHelpers.IsCollectionValid(_item, model, value);

		public static implicit operator Data<Option<TValue>[]>(DefaultedNullableCollectionStateValidator<TValue> stateValidator)
			=> stateValidator.Data;
	}
}
