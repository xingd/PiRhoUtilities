﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PiRhoSoft.Utilities.Editor
{
	public static class BindingExtensions
	{
		#region Internal Lookups

		private const string _changedInternalsError = "(PUBFECI) failed to setup BindingExtensions: Unity internals have changed";
		private const string _typeName = "UnityEditor.UIElements.BindingExtensions, UnityEditor";

		private const string _serializedObjectUpdateWrapperName = "SerializedObjectUpdateWrapper";
		private static Type _serializedObjectUpdateWrapperType;

		private const string _defaultBindName = "DefaultBind";
		private static MethodInfo _defaultBindEnumMethod;

		private const string _serializedObjectBindingName = "SerializedObjectBinding`1";
		private const string _createBindName = "CreateBind";
		private static Type _serializedObjectBindingType;
		private static Dictionary<Type, MethodInfo> _createBindMethods = new Dictionary<Type, MethodInfo>();

		static BindingExtensions()
		{
			var type = Type.GetType(_typeName);

			var serializedObjectUpdateWrapperType = type?.GetNestedType(_serializedObjectUpdateWrapperName, BindingFlags.NonPublic);
			var serializedObjectUpdateWrapperConstructor = serializedObjectUpdateWrapperType?.GetConstructor(new Type[] { typeof(SerializedObject) });

			if (serializedObjectUpdateWrapperConstructor != null)
				_serializedObjectUpdateWrapperType = serializedObjectUpdateWrapperType;

			var defaultBindMethod = type?.GetMethod(_defaultBindName, BindingFlags.Static | BindingFlags.NonPublic);
			var defaultBindEnumMethod = defaultBindMethod?.MakeGenericMethod(typeof(Enum));
			var defaultBindParameters = defaultBindEnumMethod?.GetParameters();

			if (defaultBindEnumMethod != null && defaultBindEnumMethod.IsStatic && defaultBindEnumMethod.HasSignature(null,
				typeof(VisualElement),
				serializedObjectUpdateWrapperType,
				typeof(SerializedProperty),
				typeof(Func<SerializedProperty, Enum>),
				typeof(Action<SerializedProperty, Enum>),
				typeof(Func<Enum, SerializedProperty, Func<SerializedProperty, Enum>, bool>)))
			{
				_defaultBindEnumMethod = defaultBindEnumMethod;
			}

			if (_serializedObjectUpdateWrapperType == null || _defaultBindEnumMethod == null)
				Debug.LogError(_changedInternalsError);

			_serializedObjectBindingType = type?.GetNestedType(_serializedObjectBindingName, BindingFlags.NonPublic);
			var serializedObjectBindingObjectType = _serializedObjectBindingType?.MakeGenericType(typeof(object));
			var createBindMethod = serializedObjectBindingObjectType?.GetMethod(_createBindName, BindingFlags.Public | BindingFlags.Static);
			
			// TODO: check CreateBind signature
		}

		#endregion

		#region Helper Methods

		public static void CreateBind<ValueType>(INotifyValueChanged<ValueType> field, SerializedProperty property, Func<SerializedProperty, ValueType> getter, Action<SerializedProperty, ValueType> setter, Func<ValueType, SerializedProperty, Func<SerializedProperty, ValueType>, bool> comparer)
		{
			if (!_createBindMethods.TryGetValue(typeof(ValueType), out var createBindMethod))
			{
				var serializedObjectBindingType = _serializedObjectBindingType?.MakeGenericType(typeof(ValueType));
				createBindMethod = serializedObjectBindingType?.GetMethod(_createBindName, BindingFlags.Public | BindingFlags.Static);
				_createBindMethods.Add(typeof(ValueType), createBindMethod);
			}

			var wrapper = Activator.CreateInstance(_serializedObjectUpdateWrapperType, property.serializedObject);
			createBindMethod.Invoke(null, new object[] { field, wrapper, property, getter, setter, comparer });
		}

		public static void DefaultEnumBind(INotifyValueChanged<Enum> field, SerializedProperty property)
		{
			// 2019.3 only supports flags on EnumFlagsField specifically

			var type = field.value.GetType();
			var wrapper = Activator.CreateInstance(_serializedObjectUpdateWrapperType, property.serializedObject);

			Func<SerializedProperty, Enum> getter = p => Enum.ToObject(type, p.intValue) as Enum;
			Action<SerializedProperty, Enum> setter = (p, v) => p.intValue = (int)Enum.Parse(type, v.ToString());
			Func<Enum, SerializedProperty, Func<SerializedProperty, Enum>, bool> comparer = (v, p, g) => g(p).Equals(v);

			_defaultBindEnumMethod.Invoke(null, new object[] { field, wrapper, property, getter, setter, comparer });
		}

		public static void BindManagedReference<ReferenceType>(INotifyValueChanged<ReferenceType> field, SerializedProperty property, Action onSet)
		{
			CreateBind(field, property, GetManagedReference<ReferenceType>, (p, v) => { SetManagedReference(p, v); onSet?.Invoke(); }, CompareManagedReferences);
		}

		private static ReferenceType GetManagedReference<ReferenceType>(SerializedProperty property)
		{
			var value = property.GetManagedReferenceValue();
			if (value is ReferenceType reference)
				return reference;

			return default;
		}

		private static void SetManagedReference<ReferenceType>(SerializedProperty property, ReferenceType value)
		{
			// PENDING: built in change tracking for undo doesn't work for ManagedReference yet

			Undo.RegisterCompleteObjectUndo(property.serializedObject.targetObject, "Change reference");

			property.managedReferenceValue = value;

			property.serializedObject.ApplyModifiedProperties();
			Undo.FlushUndoRecordObjects();
		}

		private static bool CompareManagedReferences<ReferenceType>(ReferenceType value, SerializedProperty property, Func<SerializedProperty, ReferenceType> getter)
		{
			var currentValue = getter(property);
			return ReferenceEquals(value, currentValue);
		}

		#endregion
	}
}
