using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using global::Lucene.Net.Analysis;
using global::Lucene.Net.Analysis.Standard;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegration
{
	[Serializable]
	public sealed class LuceneMetadata : IValidatableObject
	{
		static LuceneMetadata()
		{
			Metadata = new ConcurrentDictionary<Type, LuceneMetadata>();
			Analyzers = new ConcurrentDictionary<Type, Analyzer>();
		}

		public LuceneMetadata(Type type, DocumentAttribute entity, IDictionary<PropertyInfo, FieldAttribute> idProperties, IDictionary<PropertyInfo, FieldAttribute> properties, IDictionary<PropertyInfo, NumericFieldAttribute> numericProperties)
		{
			this.Type = type;
			this.Document = entity;
			this.Keys = idProperties;
			this.Fields = properties;
			this.NumericFields = numericProperties;
		}

		internal static IDictionary<Type, Analyzer> Analyzers
		{
			get;
			private set;
		}

		internal static IDictionary<Type, LuceneMetadata> Metadata
		{
			get;
			private set;
		}

		public Type Type
		{
			get;
			private set;
		}

		public DocumentAttribute Document
		{
			get;
			private set;
		}

		public IDictionary<PropertyInfo, FieldAttribute> Fields
		{
			get;
			private set;
		}

		public IDictionary<PropertyInfo, NumericFieldAttribute> NumericFields
		{
			get;
			private set;
		}

		public IDictionary<PropertyInfo, FieldAttribute> Keys
		{
			get;
			private set;
		}

		public static LuceneMetadata Register<TEntity>()
		{
			LuceneMetadata metadata;

			if (Metadata.TryGetValue(typeof(TEntity), out metadata) == false)
			{
				metadata = new LuceneMetadata(typeof(TEntity), new DocumentAttribute() { AnalyzerType = typeof(StandardAnalyzer) }, new Dictionary<PropertyInfo, FieldAttribute>(), new Dictionary<PropertyInfo, FieldAttribute>(), new Dictionary<PropertyInfo, NumericFieldAttribute>() { });
			}

			return (metadata);
		}

		public static LuceneMetadata Register<TEntity, TIdProperty>(Expression<Func<TEntity, TIdProperty>> idProperty)
		{
			return (Register<TEntity, TIdProperty, global::Lucene.Net.Analysis.Standard.StandardAnalyzer>(idProperty));
		}

		public static LuceneMetadata Register<TEntity, TIdProperty, TAnalyzer>(Expression<Func<TEntity, TIdProperty>> idProperty) where TAnalyzer : Analyzer
		{
			LuceneMetadata metadata;

			if (Metadata.TryGetValue(typeof(TEntity), out metadata) == false)
			{
				metadata = new LuceneMetadata(typeof(TEntity), new DocumentAttribute() { AnalyzerType = typeof(TAnalyzer) }, new Dictionary<PropertyInfo, FieldAttribute>() { { (idProperty.Body as MemberExpression).Member as PropertyInfo, new FieldAttribute() { Key = true } } }, new Dictionary<PropertyInfo, FieldAttribute>() { }, new Dictionary<PropertyInfo, NumericFieldAttribute>() { });
			}
			else
			{
				metadata.Keys.Clear();
				metadata.Keys.Add((idProperty.Body as MemberExpression).Member as PropertyInfo, new FieldAttribute() { Key = true });
			}

			return (metadata);
		}

		public LuceneMetadata RegisterProperty<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> property)
		{
			return (this.RegisterProperty<TEntity, TProperty>(property, Index.Analyzed, Store.Yes));
		}

		public LuceneMetadata RegisterProperty<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> property, Index index, Store store)
		{
			this.Fields.Add((property.Body as MemberExpression).Member as PropertyInfo, new FieldAttribute() { Index = index, Store = store });

			return (this);
		}

		public LuceneMetadata RemoveProperty<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> property)
		{
			this.Fields.Remove((property.Body as MemberExpression).Member as PropertyInfo);

			return (this);
		}

		#region IValidatableObject Members

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (this.Type == null)
			{
				yield return (new ValidationResult("Type cannot be null", new String[] { "Type" }));
			}

			if (this.Document == null)
			{
				yield return (new ValidationResult("Document cannot be null", new String[] { "Entity" }));
			}

			if ((this.Document.AnalyzerType == null) || ((typeof(Analyzer).IsAssignableFrom(this.Document.AnalyzerType) == false) || (this.Document.AnalyzerType.IsAbstract == true)))
			{
				yield return (new ValidationResult("Analyzer is null or of an invalid type", new String[] { "Analyzer" }));
			}

			if ((this.Keys == null) || (this.Keys.Count() != 1))
			{
				yield return (new ValidationResult("Must have exactly one property marked as key field", new String[] { "Keys" }));
			}

			foreach (KeyValuePair<PropertyInfo, FieldAttribute> key in this.Keys)
			{
				if (typeof(IConvertible).IsAssignableFrom(key.Key.PropertyType) == false)
				{
					yield return (new ValidationResult("Keys field is not of a valid type", new String[] { "Keys" }));
				}
			}

			if ((this.Fields == null) || (this.Fields.Count() == 0))
			{
				yield return (new ValidationResult("Fields cannot be empty", new String[] { "Properties" }));
			}

			foreach (KeyValuePair<PropertyInfo, FieldAttribute> field in this.Fields)
			{
				Type fieldType = field.Key.PropertyType;

				if ((fieldType.IsClass == true) && (fieldType != typeof(String)))
				{
					yield return (new ValidationResult(String.Format("Field of type {0} cannot be indexed. Can only index primitive types and strings", fieldType), new String[] { "Properties" }));
				}
			}
		}

		#endregion
	}
}
