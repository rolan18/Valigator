﻿using System;
using System.Collections.Generic;
using System.Text;
using Functional;

namespace Valigator.Core
{
	internal class DataValidatorAndErrors<TSource, TValue> : IDataValidatorOrErrors<TSource, TValue>
	{
		private readonly IDataValidatorOrErrors<TSource, TValue> _dataValidator;

		private readonly ValidationError[] _validationErrors;

		public DataValidatorAndErrors(IDataValidatorOrErrors<TSource, TValue> dataValidator, ValidationError[] validationErrors)
		{
			_dataValidator = dataValidator ?? throw new ArgumentNullException(nameof(dataValidator));
			_validationErrors = validationErrors ?? throw new ArgumentNullException(nameof(validationErrors));
		}

		public DataDescriptor DataDescriptor => _dataValidator.DataDescriptor;

		public Result<TValue, ValidationError[]> Validate(object model, bool isSet, TSource value) 
			=> _dataValidator.Validate(model, isSet, value);

		public Option<ValidationError[]> GetErrors()
			=> Option.Some(_validationErrors);
	}
}
