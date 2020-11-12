﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Functional;
using Microsoft.CSharp.RuntimeBinder;

namespace Valigator.Core
{

	internal static class Model<TModel>
	{
		private static object _getPropertyDescriptorsLockObj = new object();
		private static Func<TModel, PropertyDescriptor[]> _getPropertyDescriptors;

		public static PropertyDescriptor[] GetPropertyDescriptors(TModel model)
		{
			if (typeof(TModel).IsPrimitive)
				return Array.Empty<PropertyDescriptor>();

			if (_getPropertyDescriptors == null)
			{
				lock (_getPropertyDescriptorsLockObj)
				{
					if (_getPropertyDescriptors == null)
						_getPropertyDescriptors = CreatePropertyDescriptorsFunction();
				}
			}

			return _getPropertyDescriptors.Invoke(model);
		}

		private static Func<TModel, PropertyDescriptor[]> CreatePropertyDescriptorsFunction()
		{
			var modelExpression = Expression.Parameter(typeof(TModel), "model");

			var propertyDescriptors = typeof(TModel)
				.GetProperties()
				.Where(property => property.PropertyType.IsConstructedGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Data<>))
				.Select(property => CreatePropertyDescriptor(modelExpression, property));

			var arrayInitializer = Expression.NewArrayInit(typeof(PropertyDescriptor), propertyDescriptors);

			return Expression.Lambda<Func<TModel, PropertyDescriptor[]>>(arrayInitializer, modelExpression).Compile();
		}

		private static Expression CreatePropertyDescriptor(Expression modelExpression, PropertyInfo property)
		{
			var data = Expression.Property(modelExpression, property);

			var dataDescriptor = Expression.Property(data, nameof(Data<object>.DataDescriptor));

			var constructor = typeof(PropertyDescriptor).GetConstructor(new[] { typeof(string), typeof(DataDescriptor) });

			return Expression.New(constructor, Expression.Constant(property.Name, typeof(string)), dataDescriptor);
		}

		private static object _verifyModelLockObj = new object();
		private static Func<TModel, ValidationError[][]> _verifyMethod;

		public static Result<Unit, ValidationError[]> Verify(TModel model)
		{
			if (typeof(TModel).IsPrimitive || typeof(TModel).IsArray)
				return Result.Unit<ValidationError[]>();

			if (_verifyMethod == null)
			{
				lock (_verifyModelLockObj)
				{
					if (_verifyMethod == null)
						_verifyMethod = CreateVerifyFunction(model);
				}
			}

			var validationErrors = _verifyMethod
				.Invoke(model)
				.OfType<ValidationError[]>()
				.SelectMany(_ => _)
				.ToArray();

			return Result.Create(validationErrors.Length == 0, Unit.Value, validationErrors);
		}

		private static Func<TModel, ValidationError[][]> CreateVerifyFunction(TModel model)
		{
			var modelParameter = Expression.Parameter(typeof(TModel), "model");

			var modelExpression = CreateModelExpression(modelParameter, model);

			var properties = GetAllProperties(model);
			var fields = GetAllFields(typeof(TModel));

			var (dataProperties, validateContentsMembers) = FilterToDataPropertiesAndValidateContentsMembers(properties, fields);

			var validationErrors = Enumerable
				.Empty<Expression>()
				.Concat(dataProperties
					.Select(property => VerifyDataProperty(modelExpression, property, model))
				)
				.Concat(validateContentsMembers
					.Select(propertyOrField => VerifyPropertyOrFieldContents(modelExpression, propertyOrField))
				);

			var arrayInitializer = Expression.NewArrayInit(typeof(ValidationError[]), validationErrors);

			return Expression.Lambda<Func<TModel, ValidationError[][]>>(arrayInitializer, modelParameter).Compile();
		}

		private static Expression CreateModelExpression(ParameterExpression modelParameter, TModel model)
		{
			//if (!(model is ValigatorAnonymousObjectBase))
			return Expression.Convert(modelParameter, typeof(TModel));

			//var castedModelParameter = Expression.Convert(modelParameter, typeof(object));
			//return castedModelParameter;
		}

		private static Expression CreateDataExpression(Expression modelParameter, MemberData data, TModel model)
		{
			if (!(model is ValigatorAnonymousObjectBase))
				return Expression.Property(modelParameter, data.Name);


			//var binder = Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, data.Name, model.GetType(), new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
			//var propertyExpression = Expression.Dynamic(binder, data.Type, modelParameter);
			//var castedPropertyExpression = Expression.Convert(propertyExpression, data.Type);

			var methodCallExpression = Expression.Call(modelParameter, model.GetType().GetMethod(nameof(ValigatorAnonymousObjectBase.GetMember)), Expression.Constant(data.Name));

			return Expression.Convert(methodCallExpression, data.Type);
			//var innerType = model.GetType().GenericTypeArguments.First();
			//var innerProperty = Expression.Property(modelParameter, model.GetType().GetProperty(nameof(ValigatorAnonymousObjectBase.Inner), BindingFlags.Public | BindingFlags.Instance));
			//var castedInner = Expression.Convert(innerProperty, innerType);
			//var property = Expression.Property(castedInner, data.Name);
			//return property;
		}


		private static Expression CreateAssignExpression(Expression dataProperty, (MethodInfo verify, MethodInfo tryGetValue, MethodInfo isSuccess, MethodInfo getFailure) methods, Expression modelExpression, TModel model, MemberData propertyData)
		{
			if (!(model is ValigatorAnonymousObjectBase))
				return Expression.Assign(dataProperty, Expression.Call(dataProperty, methods.verify, modelExpression));

			var verifyExpression = Expression.Call(dataProperty, methods.verify, modelExpression);
			var callMethodExpression = Expression.Call(modelExpression, model.GetType().GetMethod(nameof(ValigatorAnonymousObjectBase.SetMember)), Expression.Constant(propertyData.Name), Expression.Convert(verifyExpression, typeof(object)));
			return Expression.Convert(callMethodExpression, propertyData.Type);
		}

		private static MemberData[] GetAllProperties(TModel model)
			=> GetBaseProperties(model)
				.Concat(GetExplicitProperties(typeof(TModel)))
				.ToArray();

		private static FieldInfo[] GetAllFields(Type type)
			=> type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		private static (MemberData[] dataProperties, MemberData[] validateContentsMembers) FilterToDataPropertiesAndValidateContentsMembers(MemberData[] properties, FieldInfo[] fields)
		{
			var dataProperties = properties
				.Where(p => IsValigatorDataType(p.Type))
				.ToArray();

			var validateContentsProperties = properties
				.Where(p => !IsValigatorDataType(p.Type))
				.Where(p => p.CustomAttributes.OfType<ValidateContentsAttribute>().FirstOrDefault() != null)
				.ToArray();

			//var noGetters = dataProperties
			//	.Concat(validateContentsProperties)
			//	.Where(x => x.GetMethod == null)
			//	.ToArray();

			//var noSetters = dataProperties
			//	.Where(x => x.SetMethod == null)
			//	.ToArray();

			//if (noGetters.Any() || noSetters.Any())
			//	throw new MissingAccessorsException(noGetters, noSetters);

			var validateContentsFields = fields
				.Where(p => p.GetCustomAttribute<ValidateContentsAttribute>() != null)
				.Select(member => new MemberData(member.Name, member.FieldType, member.DeclaringType, member.GetCustomAttributes()))
				.ToArray();

			var validateContentsMembers = Enumerable
				.Empty<MemberData>()
				.Concat(validateContentsProperties)
				.Concat(validateContentsFields)
				.ToArray();

			return (dataProperties, validateContentsMembers);
		}

		private static MethodInfo _modelVerify;
		private static MethodInfo _isSuccess;
		private static MethodInfo _getFailure;

		private static Expression VerifyPropertyOrFieldContents(Expression modelExpression, MemberData propertyOrField)
		{
			var valueAccessor = Expression.Property(modelExpression, propertyOrField.Name);
			var valueType = propertyOrField.Type;

			var valueName = propertyOrField.CustomAttributes.OfType<ValidateContentsAttribute>().First().MemberName;

			var modelVerifyGeneric = _modelVerify ?? (_modelVerify = typeof(Model).GetMethod(nameof(Model.Verify), BindingFlags.Public | BindingFlags.Static));

			var modelVerify = modelVerifyGeneric.MakeGenericMethod(valueType);

			var isSuccessMethod = _isSuccess ?? (_isSuccess = typeof(Model<TModel>).GetMethod(nameof(IsUnitResultSuccess), BindingFlags.NonPublic | BindingFlags.Static));
			var getFailureMethod = _getFailure ?? (_getFailure = typeof(Model<TModel>).GetMethod(nameof(GetUnitResultFailure), BindingFlags.NonPublic | BindingFlags.Static));

			var result = Expression.Variable(typeof(Result<Unit, ValidationError[]>), "result");

			var assignedResult = Expression.Assign(result, Expression.Call(null, modelVerify, valueAccessor));

			var isSuccess = Expression.Call(isSuccessMethod, result);

			var addPathsToErrorsMethod = _addPathsToErrorsMethod ?? (_addPathsToErrorsMethod = typeof(Model<object>).GetMethod(nameof(AddPropertyToErrors), BindingFlags.NonPublic | BindingFlags.Static));

			var getFailure = Expression.Call(addPathsToErrorsMethod, Expression.Call(getFailureMethod, result), Expression.Constant(valueName, typeof(string)), Expression.Constant(false, typeof(bool)));

			var onSuccess = Expression.Constant(null, typeof(ValidationError[]));

			var condition = Expression.Condition(isSuccess, onSuccess, getFailure, typeof(ValidationError[]));

			return Expression.Block(new[] { result }, assignedResult, condition);
		}

		private static IEnumerable<MemberData> GetBaseProperties(TModel model)
		{
			var currentLevelProperties = GetProperties(typeof(TModel), BindingFlags.NonPublic | BindingFlags.Instance)
				.Concat(TypeDescriptor.GetProperties(model).OfType<System.ComponentModel.PropertyDescriptor>().Select(property => new MemberData(property.Name, property.PropertyType, property.ComponentType, property.PropertyType.GetCustomAttributes().ToArray())))
				.Where(p => !IsExplicitInterfaceImplementation(p));

			foreach (var currentProperty in currentLevelProperties)
			{
				if (currentProperty.ComponentType != null)
				{
					var method = currentProperty.ComponentType.GetProperty(currentProperty.Name).GetGetMethod() ?? currentProperty.ComponentType.GetProperty(currentProperty.Name).GetSetMethod();

					var baseType = method.GetBaseDefinition().DeclaringType;
					if (baseType != typeof(TModel))
						yield return GetProperty(baseType, currentProperty.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				}
				else
					yield return currentProperty;
			}
		}

		private static IEnumerable<MemberData> GetExplicitProperties(Type type)
		{
			var currentType = type;
			while (currentType != null)
			{
				var explicitProperties = GetProperties(currentType, BindingFlags.NonPublic | BindingFlags.Instance)
					.Where(p => IsExplicitInterfaceImplementation(p));

				foreach (var property in explicitProperties)
					yield return property;

				currentType = currentType.BaseType;
			}
		}

		private static MemberData GetProperty(Type type, string name, BindingFlags bindingFlags)
		{
			var property = type.GetProperty(name, bindingFlags);
			return new MemberData(property.Name, property.PropertyType, property.DeclaringType, property.GetCustomAttributes());
		}

		private static MemberData[] GetProperties(Type type, BindingFlags bindingFlags)
			=> type
				.GetProperties(bindingFlags)
				.Select(property => new MemberData(property.Name, property.PropertyType, property.DeclaringType, property.GetCustomAttributes()))
				.ToArray();

		private static bool IsValigatorDataType(Type type)
			=> type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Data<>);


		private static bool IsExplicitInterfaceImplementation(MemberData prop)
			=> prop.Name.Contains(".");

		private static Expression VerifyDataProperty(Expression modelExpression, MemberData property, TModel model)
		{
			var methods = GetVerifySupportMethods(property.Type);

			var dataProperty = CreateDataExpression(modelExpression, property, model);// Expression.Property(modelExpression, property.Name);

			var isValid = Expression.Equal(Expression.Property(dataProperty, nameof(Data<object>.State)), Expression.Constant(DataState.Valid, typeof(DataState)));

			var isInvalid = Expression.Equal(Expression.Property(dataProperty, nameof(Data<object>.State)), Expression.Constant(DataState.Invalid, typeof(DataState)));

			var isVerified = Expression.Variable(typeof(bool), "isVerified");

			var assignIsVerified = Expression.Assign(isVerified, Expression.Or(isValid, isInvalid));

			var verifiedData = CreateAssignExpression(dataProperty, methods, modelExpression, model, property);

			var data = Expression.Condition(Expression.OrElse(isValid, isInvalid), dataProperty, verifiedData);

			var result = Expression.Variable(typeof(Result<,>).MakeGenericType(property.Type.GetGenericArguments()[0], typeof(ValidationError[])), "result");

			var assignedResult = Expression.Assign(result, Expression.Call(data, methods.tryGetValue));

			var isSuccess = Expression.Call(methods.isSuccess, result);

			var addPathsToErrorsMethod = _addPathsToErrorsMethod ?? (_addPathsToErrorsMethod = typeof(Model<object>).GetMethod(nameof(AddPropertyToErrors), BindingFlags.NonPublic | BindingFlags.Static));

			var getFailure = Expression.Call(addPathsToErrorsMethod, Expression.Call(methods.getFailure, result), Expression.Constant(property.Name, typeof(string)), isVerified);

			var onSuccess = Expression.Constant(null, typeof(ValidationError[]));

			var condition = Expression.Condition(isSuccess, onSuccess, getFailure, typeof(ValidationError[]));

			return Expression.Block(new[] { isVerified, result }, assignIsVerified, assignedResult, condition);
		}

		private static readonly ConcurrentDictionary<Type, (MethodInfo verify, MethodInfo tryGetValue, MethodInfo isSuccess, MethodInfo getFailure)> _getVerifySupportMethods = new ConcurrentDictionary<Type, (MethodInfo verify, MethodInfo tryGetValue, MethodInfo isSuccess, MethodInfo getFailure)>();

		private static (MethodInfo verify, MethodInfo tryGetValue, MethodInfo isSuccess, MethodInfo getFailure) GetVerifySupportMethods(Type dataType)
		{
			if (!_getVerifySupportMethods.TryGetValue(dataType, out var methods))
			{
				methods = CreateVerifySupportMethods(dataType);

				_getVerifySupportMethods.TryAdd(dataType, methods);
			}

			return methods;
		}

		private static (MethodInfo verify, MethodInfo tryGetValue, MethodInfo isSuccess, MethodInfo getFailure) CreateVerifySupportMethods(Type dataType)
		{
			var valueType = dataType.GetGenericArguments()[0];

			var verify = dataType.GetMethod(nameof(Data<object>.Verify), new[] { typeof(object) });

			var tryGetValue = dataType.GetMethod(nameof(Data<object>.TryGetValue), Type.EmptyTypes);

			var isSuccess = typeof(Model<TModel>).GetMethod(nameof(IsResultSuccess), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(valueType);

			var getFailure = typeof(Model<TModel>).GetMethod(nameof(GetResultFailure), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(valueType);

			return (verify, tryGetValue, isSuccess, getFailure);
		}

		private static bool IsUnitResultSuccess(Result<Unit, ValidationError[]> result)
			=> result.Match(_ => true, _ => false);

		private static ValidationError[] GetUnitResultFailure(Result<Unit, ValidationError[]> result)
			=> result.Match(_ => default, _ => _);

		private static bool IsResultSuccess<TValue>(Result<TValue, ValidationError[]> result)
			=> result.Match(_ => true, _ => false);

		private static ValidationError[] GetResultFailure<TValue>(Result<TValue, ValidationError[]> result)
			=> result.Match(_ => default, _ => _);

		private static MethodInfo _addPathsToErrorsMethod;

		private static ValidationError[] AddPropertyToErrors(ValidationError[] errors, string propertyName, bool skip)
		{
			if (!skip && errors != null && !String.IsNullOrEmpty(propertyName))
			{
				foreach (var error in errors)
					error.Path.AddProperty(propertyName);
			}

			return errors;
		}
	}
}
