﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Functional;

namespace Valigator.Generator
{
	public static class ExtensionGenerator
	{
		private const string ValueGeneric = "TValue";
		private const string SourceGeneric = "TSource";
		private const string ValueValidatorGeneric = "TValueValidator";

		private static readonly string _validatorExtensionTemplate =
@"		public static DataSource__Inverted__<__DataContainerFactory__<__StateValidator__, __TSourceType__, __TValueType__>, __TDataValueType__, __TValidatorValueType__, __NewValueValidator__> __ExtensionName____GenericParameters__(this __StateValidator__ source__ExtensionParameters__)
			=> source__NotOpen__.Add(new __NewValueValidator__(__Arguments__))__NotClose__;";

		private static readonly string _singleExtensionTemplate =
@"		public static DataSource__Inverted__<__DataContainerFactory__<__StateValidator__, __TSourceType__, __TValueType__>, __TDataValueType__, __TValidatorValueType__, __NewValueValidator__> __ExtensionName____GenericParameters__(this DataSource<__DataContainerFactory__<__StateValidator__, __TSourceType__, __TValueType__>, __TDataValueType__, __TValidatorValueType__> source__ExtensionParameters__)
			=> source__NotOpen__.Add(new __NewValueValidator__(__Arguments__))__NotClose__;";

		private static readonly string _doubleExtensionTemplate =
@"		public static __Nullable__DataSource__InvertOne____Inverted__<__StateValidator__, __ValueValidatorOne__, __NewValueValidator__, __TSourceType__, __TValueType__> __ExtensionName____GenericParameters__(this __Nullable__DataSource__InvertOne__<__StateValidator__, __ValueValidatorOne__, __TSourceType__, __TValueType__> source__ExtensionParameters__)
			=> source__NotOpen__.Add(new __NewValueValidator__(__Parameters__))__NotClose__;";

		private static readonly string _tripleExtensionTemplate =
@"		public static __Nullable__DataSource__InvertOne____InvertTwo____Inverted__<__StateValidator__, __ValueValidatorOne__, __ValueValidatorTwo__, __NewValueValidator__, __TSourceType__, __TValueType__> __ExtensionName____GenericParameters__(this __Nullable__DataSource__InvertOne____InvertTwo__<__StateValidator__, __ValueValidatorOne__, __ValueValidatorTwo__, __TSourceType__, __TValueType__> source__ExtensionParameters__)
			=> source__NotOpen__.Add(new __NewValueValidator__(__Parameters__))__NotClose__;";

		private static readonly string _validatorNotTemplate =
@"		public static DataSourceInverted<__DataContainerFactory__<__StateValidator__, __TSourceType__, __TValueType__>, __TDataValueType__, __TValidatorValueType__, TValueValidator> Not__GenericParameters__(this __StateValidator__ source, Func<__StateValidator__, DataSourceStandard<__DataContainerFactory__<__StateValidator__, __TSourceType__, __TValueType__>, __TDataValueType__, __TValidatorValueType__, TValueValidator>> validatorFactory)
			where TValueValidator : struct, IValueValidator<__TValidatorValueType__>
			=> validatorFactory.Invoke(source).InvertOne();";

		private static readonly string _singleNotTemplate =
@"		public static DataSourceInverted<__DataContainerFactory__<__StateValidator__, __TSourceType__, __TValueType__>, __TDataValueType__, __TValidatorValueType__, TValueValidator> Not__GenericParameters__(this DataSource<__DataContainerFactory__<__StateValidator__, __TSourceType__, __TValueType__>, __TDataValueType__, __TValidatorValueType__> source, Func<DataSource<__DataContainerFactory__<__StateValidator__, __TSourceType__, __TValueType__>, __TDataValueType__, __TValidatorValueType__>, DataSourceStandard<__DataContainerFactory__<__StateValidator__, __TSourceType__, __TValueType__>, __TDataValueType__, __TValidatorValueType__, TValueValidator>> validatorFactory)
			where TValueValidator : struct, IValueValidator<__TValidatorValueType__>
			=> validatorFactory.Invoke(source).InvertOne();";

		private static readonly string _doubleNotTemplate =
@"		public static __Nullable__DataSource__InvertOne__Inverted<__StateValidator__, __ValueValidatorOne__, TValueValidator, __TSourceType__, __TValueType__> Not__GenericParameters__(this __Nullable__DataSource__InvertOne__<__StateValidator__, __ValueValidatorOne__, __TSourceType__, __TValueType__> source, Func<__Nullable__DataSource__InvertOne__<__StateValidator__, __ValueValidatorOne__, __TSourceType__, __TValueType__>, __Nullable__DataSource__InvertOne__Standard<__StateValidator__, __ValueValidatorOne__, TValueValidator, __TSourceType__, __TValueType__>> validatorFactory)
			where TValueValidator : IValueValidator<__TValueType__>
			=> validatorFactory.Invoke(source).InvertTwo();";

		private static readonly string _tripleNotTemplate =
@"		public static __Nullable__DataSource__InvertOne____InvertTwo__Inverted<__StateValidator__, __ValueValidatorOne__, __ValueValidatorTwo__, TValueValidator, __TSourceType__, __TValueType__> Not__GenericParameters__(this __Nullable__DataSource__InvertOne____InvertTwo__<__StateValidator__, __ValueValidatorOne__, __ValueValidatorTwo__, __TSourceType__, __TValueType__> source, Func<__Nullable__DataSource__InvertOne____InvertTwo__<__StateValidator__, __ValueValidatorOne__, __ValueValidatorTwo__, __TSourceType__, __TValueType__>, __Nullable__DataSource__InvertOne____InvertTwo__Standard<__StateValidator__, __ValueValidatorOne__, __ValueValidatorTwo__, TValueValidator, __TSourceType__, __TValueType__>> validatorFactory)
			where TValueValidator : IValueValidator<__TValueType__>
			=> validatorFactory.Invoke(source).InvertThree();";

		private static string PopulateTemplate(string template, SourceDefinition sourceDefinition, string valueGenericName, string[] genericParameters, ValidatorDefinition validatorOne, ValidatorDefinition validatorTwo, ExtensionDefinition extension, bool invertOne, bool invertTwo, bool joinSourceAndValue = false)
			=> template
				.Replace("__InvertOne__", $"{(invertOne ? "Inverted" : "Standard")}")
				.Replace("__InvertTwo__", $"{(invertTwo ? "Inverted" : "Standard")}")
				.Replace("__Inverted__", $"{((extension?.Invert ?? false) ? "Inverted" : "Standard")}")
				.Replace("__DataContainerFactory__", sourceDefinition.GetDataContainerFactoryType())
				.Replace("__StateValidator__", sourceDefinition.GetSourceName(valueGenericName))
				.Replace("__TSourceType__", joinSourceAndValue ? valueGenericName : SourceGeneric)
				.Replace("__TValueType__", valueGenericName)
				.Replace("__TDataValueType__", sourceDefinition.GetDataValueType(valueGenericName))
				.Replace("__TValidatorValueType__", sourceDefinition.GetValidatorValueType(valueGenericName, Option.None<ValidatorDefinition>()))
				.Replace("__NewValueValidator__", extension?.GetValidatorName(sourceDefinition, valueGenericName) ?? String.Empty)
				.Replace("__ExtensionName__", extension?.ExtensionName ?? String.Empty)
				.Replace("__GenericParameters__", HelperExtensions.ConstructorGenericParameters(genericParameters, Option.Create(!joinSourceAndValue, SourceGeneric)))
				.Replace("__ExtensionParameters__", extension?.GetParameters(sourceDefinition, valueGenericName) ?? String.Empty)
				.Replace("__Arguments__", extension?.GetArguments() ?? String.Empty)
				.Replace("__NotOpen__", $"{((extension?.Invert ?? false) ? @".Not(s => s" : String.Empty)}")
				.Replace("__NotClose__", $"{((extension?.Invert ?? false) ? ")" : String.Empty)}");
			/*
			=> template
				.Replace("__Nullable__", $"{(sourceDefinition.IsNullable ? "Nullable" : String.Empty)}")
				.Replace("__InvertOne__", $"{(invertOne ? "Inverted" : "Standard")}")
				.Replace("__InvertTwo__", $"{(invertTwo ? "Inverted" : "Standard")}")
				.Replace("__Inverted__", $"{((extension?.Invert ?? false) ? "Inverted" : "Standard")}")
				.Replace("__NotOpen__", $"{((extension?.Invert ?? false) ? _notModifier : String.Empty)}")
				.Replace("__NotClose__", $"{((extension?.Invert ?? false) ? ")" : String.Empty)}")
				.Replace("__StateValidator__", sourceDefinition.GetSourceName(Option.Some(joinSourceAndValue ? valueGenericName : SourceGeneric)))
				.Replace("__ValueValidatorOne__", validatorOne?.GetValidatorName(GetValueForValidator(sourceDefinition, validatorOne, valueGenericName)) ?? String.Empty)
				.Replace("__ValueValidatorTwo__", validatorTwo?.GetValidatorName(GetValueForValidator(sourceDefinition, validatorTwo, valueGenericName)) ?? String.Empty)
				.Replace("__NewValueValidator__", extension?.Validator.GetValidatorName(GetValueForValidator(sourceDefinition, extension?.Validator, valueGenericName)) ?? String.Empty)
				.Replace("__TSourceType__", joinSourceAndValue ? (sourceDefinition.ValueType == ValueType.Array ? $"{valueGenericName}[]" : valueGenericName) : (sourceDefinition.ValueType == ValueType.Array ? "TSource[]" : SourceGeneric))
				.Replace("__TValueType__", sourceDefinition.ValueType == ValueType.Array ? $"{valueGenericName}[]" : valueGenericName)
				.Replace("__ExtensionName__", extension?.ExtensionName ?? String.Empty)
				.Replace("__GenericParameters__", !joinSourceAndValue || genericParameters.Any() ? $"<{String.Join(", ", joinSourceAndValue ? genericParameters : new[] { SourceGeneric }.Concat(genericParameters))}>" : String.Empty)
				.Replace("__ExtensionParameters__", String.Join(String.Empty, extension?.Parameters.Where(p => !p.Value.HasValue()).Select(p => $", {p.GetTypeName(GetValueForValidator(sourceDefinition, extension?.Validator, valueGenericName))} {p.Name}{p.DefaultValue.Match(v => $" = {v}", () => String.Empty)}") ?? Enumerable.Empty<string>()))
				.Replace("__Parameters__", String.Join(", ", extension?.Parameters.Select(p => p.Value.Match(v => v, () => p.Name)) ?? Enumerable.Empty<string>()));
			*/

		private static string GetValueForValidator(SourceDefinition sourceDefinition, ValidatorDefinition validator, string valueGenericName)
			=> sourceDefinition.ValueType == ValueType.Array && validator?.ValueType == ValueType.Value ? $"{valueGenericName}[]" : valueGenericName;

		public static IEnumerable<string> GenerateExtensionOne(SourceDefinition sourceDefinition, Option<string> dataType, ExtensionDefinition extension)
		{
			if (sourceDefinition.ValueType == ValueType.Value && extension.Validator.ValueType != ValueType.Value)
				throw new Exception("Array extensions cannot be generated for value type sources.");

			var valueGenericName = dataType.Match(_ => _, () => ValueGeneric);

			var genericParameters = dataType.Match(_ => Array.Empty<string>(), () => new[] { ValueGeneric });

			yield return PopulateTemplate(_validatorExtensionTemplate, sourceDefinition, valueGenericName, genericParameters, null, null, extension, false, false, true);
			yield return PopulateTemplate(_singleExtensionTemplate, sourceDefinition, valueGenericName, genericParameters, null, null, extension, false, false, true);
		}

		public static IEnumerable<string> GenerateExtensionTwo(SourceDefinition sourceDefinition, Option<string> dataType, ValidatorDefinition validatorOne, ExtensionDefinition extension)
		{
			yield break;

			if (sourceDefinition.ValueType == ValueType.Value && (validatorOne.ValueType != ValueType.Value || extension.Validator.ValueType != ValueType.Value))
				throw new Exception("Array extensions cannot be generated for value type sources.");

			var valueGenericName = dataType.Match(_ => _, () => ValueGeneric);

			var genericParameters = dataType.Match(_ => Array.Empty<string>(), () => new[] { ValueGeneric });

			yield return PopulateTemplate(_doubleExtensionTemplate, sourceDefinition, valueGenericName, genericParameters, validatorOne, null, extension, false, false);
			yield return PopulateTemplate(_doubleExtensionTemplate, sourceDefinition, valueGenericName, genericParameters, validatorOne, null, extension, true, false);
		}

		public static IEnumerable<string> GenerateExtensionThree(SourceDefinition sourceDefinition, Option<string> dataType, ValidatorDefinition validatorOne, ValidatorDefinition validatorTwo, ExtensionDefinition extension)
		{
			yield break;

			if (sourceDefinition.ValueType == ValueType.Value && (validatorOne.ValueType != ValueType.Value || validatorTwo.ValueType != ValueType.Value || extension.Validator.ValueType != ValueType.Value))
				throw new Exception("Array extensions cannot be generated for value type sources.");

			var valueGenericName = dataType.Match(_ => _, () => ValueGeneric);

			var genericParameters = dataType.Match(_ => Array.Empty<string>(), () => new[] { ValueGeneric });

			yield return PopulateTemplate(_tripleExtensionTemplate, sourceDefinition, valueGenericName, genericParameters, validatorOne, validatorTwo, extension, false, false);
			yield return PopulateTemplate(_tripleExtensionTemplate, sourceDefinition, valueGenericName, genericParameters, validatorOne, validatorTwo, extension, true, false);
			yield return PopulateTemplate(_tripleExtensionTemplate, sourceDefinition, valueGenericName, genericParameters, validatorOne, validatorTwo, extension, false, true);
			yield return PopulateTemplate(_tripleExtensionTemplate, sourceDefinition, valueGenericName, genericParameters, validatorOne, validatorTwo, extension, true, true);
		}

		public static IEnumerable<string> GenerateInvertExtensionOne(SourceDefinition sourceDefinition)
		{
			yield return PopulateTemplate(_validatorNotTemplate, sourceDefinition, ValueGeneric, new[] { ValueGeneric, ValueValidatorGeneric }, null, null, null, false, false, true);
			yield return PopulateTemplate(_singleNotTemplate, sourceDefinition, ValueGeneric, new[] { SourceGeneric, ValueGeneric, ValueValidatorGeneric }, null, null, null, false, false);
		}

		public static IEnumerable<string> GenerateInvertExtensionTwo(SourceDefinition sourceDefinition, Option<string> dataType, ValidatorDefinition validatorOne)
		{
			yield break;

			if (sourceDefinition.ValueType == ValueType.Value && validatorOne.ValueType != ValueType.Value)
				throw new Exception("Array extensions cannot be generated for value type sources.");

			var valueGenericName = dataType.Match(_ => _, () => ValueGeneric);

			var genericParameters = dataType.Match(_ => new[] { ValueValidatorGeneric }, () => new[] { ValueGeneric, ValueValidatorGeneric });

			yield return PopulateTemplate(_doubleNotTemplate, sourceDefinition, valueGenericName, genericParameters, validatorOne, null, null, false, false);
			yield return PopulateTemplate(_doubleNotTemplate, sourceDefinition, valueGenericName, genericParameters, validatorOne, null, null, true, false);
		}

		public static IEnumerable<string> GenerateInvertExtensionThree(SourceDefinition sourceDefinition, Option<string> dataType, ValidatorDefinition validatorOne, ValidatorDefinition validatorTwo)
		{
			yield break;

			if (sourceDefinition.ValueType == ValueType.Value && (validatorOne.ValueType != ValueType.Value || validatorTwo.ValueType != ValueType.Value))
				throw new Exception("Array extensions cannot be generated for value type sources.");

			var valueGenericName = dataType.Match(_ => _, () => ValueGeneric);

			var genericParameters = dataType.Match(_ => new[] { ValueValidatorGeneric }, () => new[] { ValueGeneric, ValueValidatorGeneric });

			yield return PopulateTemplate(_tripleNotTemplate, sourceDefinition, valueGenericName, genericParameters, validatorOne, validatorTwo, null, false, false);
			yield return PopulateTemplate(_tripleNotTemplate, sourceDefinition, valueGenericName, genericParameters, validatorOne, validatorTwo, null, true, false);
			yield return PopulateTemplate(_tripleNotTemplate, sourceDefinition, valueGenericName, genericParameters, validatorOne, validatorTwo, null, false, true);
			yield return PopulateTemplate(_tripleNotTemplate, sourceDefinition, valueGenericName, genericParameters, validatorOne, validatorTwo, null, true, true);
		}
	}
}
