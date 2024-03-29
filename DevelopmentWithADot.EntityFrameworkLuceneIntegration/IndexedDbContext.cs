﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.Entity.Infrastructure;
using System.Data.Objects;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegration
{
	public abstract class IndexedDbContext : DbContext, ILuceneContext
	{
		#region Private static readonly fields
		private static readonly MethodInfo takeMethod = typeof(Queryable).GetMethod("Take", BindingFlags.Public | BindingFlags.Static);
		private static readonly PluralizationService pluralizationService = PluralizationService.CreateService(CultureInfo.CreateSpecificCulture("en-US"));
		private static readonly Lucene.Net.Util.Version luceneVersion = Lucene.Net.Util.Version.LUCENE_30;
		#endregion

		#region Protected constructors
		protected IndexedDbContext()
		{
			this.Initialize();
		}

		protected IndexedDbContext(DbCompiledModel model) : base(model)
		{
			this.Initialize();
		}

		protected IndexedDbContext(String nameOrConnectionString) : base(nameOrConnectionString)
		{
			this.Initialize();
		}

		protected IndexedDbContext(String nameOrConnectionString, DbCompiledModel model) : base(nameOrConnectionString, model)
		{
			this.Initialize();
		}

		protected IndexedDbContext(DbConnection existingConnection, Boolean contextOwnsConnection) : base(existingConnection, contextOwnsConnection)
		{
			this.Initialize();
		}

		protected IndexedDbContext(ObjectContext objectContext, Boolean dbContextOwnsObjectContext) : base(objectContext, dbContextOwnsObjectContext)
		{
			this.Initialize();
		}

		protected IndexedDbContext(DbConnection existingConnection, DbCompiledModel model, Boolean contextOwnsConnection) : base(existingConnection, model, contextOwnsConnection)
		{
			this.Initialize();
		}
		#endregion		

		#region Protected virtual methods
		protected virtual void Initialize()
		{
			this.IndexingEnabled = true;
			this.PluralizeEntities = true;
			this.IndexesPath = Path.Combine(Environment.CurrentDirectory, "Indexes");
		}

		protected virtual Lucene.Net.Store.Directory GetDirectory(Type type, out Boolean exists)
		{
			String path = Path.Combine(this.IndexesPath, type.Name);

			exists = System.IO.Directory.Exists(path);

			if (exists == false)
			{
				System.IO.Directory.CreateDirectory(path);
			}

			return (FSDirectory.Open(path));
		}

		protected virtual IndexWriter GetIndexWriter(Lucene.Net.Store.Directory directory, Analyzer analyzer, Boolean exists)
		{
			return (new IndexWriter(directory, analyzer, !exists, IndexWriter.MaxFieldLength.LIMITED));
		}

		protected virtual Analyzer GetAnalyzer(Type entityType)
		{
			LuceneMetadata metadata = this.getLuceneMetadata(entityType);
			Analyzer analyzer = null;

			if (LuceneMetadata.Analyzers.TryGetValue(metadata.Document.AnalyzerType, out analyzer) == true)
			{
				analyzer = Activator.CreateInstance(metadata.Document.AnalyzerType, new Object[] { luceneVersion }) as Analyzer;
				LuceneMetadata.Analyzers[metadata.Document.AnalyzerType] = analyzer;
			}
			else
			{
				analyzer = new StandardAnalyzer(luceneVersion);
			}

			return (analyzer);
		}
		#endregion

		#region Public properties
		public Boolean IndexingEnabled
		{
			get;
			set;
		}

		public Boolean PluralizeEntities
		{
			get;
			set;
		}

		public String IndexesPath
		{
			get;
			set;
		}
		#endregion

		#region Public override methods
		public override Int32 SaveChanges()
		{
			IEnumerable<IGrouping<Type, DbEntityEntry>> addedGroupedEntities = Enumerable.Empty<IGrouping<Type, DbEntityEntry>>();
			IEnumerable<IGrouping<Type, DbEntityEntry>> modifiedGroupedEntities = Enumerable.Empty<IGrouping<Type, DbEntityEntry>>();
			IEnumerable<IGrouping<Type, DbEntityEntry>> deletedGroupedEntities = Enumerable.Empty<IGrouping<Type, DbEntityEntry>>();

			if (this.IndexingEnabled == true)
			{
				addedGroupedEntities = this.ChangeTracker.Entries().Where(x => x.State == EntityState.Added).GroupBy(y => this.getEntityType(y.Entity.GetType())).ToArray();
				modifiedGroupedEntities = this.ChangeTracker.Entries().Where(x => x.State == EntityState.Modified).GroupBy(y => this.getEntityType(y.Entity.GetType())).ToArray();
				deletedGroupedEntities = this.ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted).GroupBy(y => this.getEntityType(y.Entity.GetType())).ToArray();
			}

			Int32 changes = base.SaveChanges();

			if (changes > 0)
			{
				//added entities
				foreach (IGrouping<Type, DbEntityEntry> groupedEntity in addedGroupedEntities)
				{
					this.add(groupedEntity.Key, false, groupedEntity.Select(x => x.Entity).ToArray());
				}

				//deleted entities
				foreach (IGrouping<Type, DbEntityEntry> groupedEntity in deletedGroupedEntities)
				{
					this.delete(groupedEntity.Key, false, groupedEntity.Select(x => x.Entity).ToArray());
				}

				foreach (IGrouping<Type, DbEntityEntry> groupedEntity in modifiedGroupedEntities)
				{
					this.update(groupedEntity.Key, false, groupedEntity.Select(x => x.Entity).ToArray());
				}
			}

			return (changes);
		}
		#endregion

		#region Public methods
		public void Index(Object entity, Boolean destroyExistingIndex = false)
		{
			DbEntityEntry entry = this.ChangeTracker.Entries<Object>().Where(x => Object.Equals(x.Entity, entity)).SingleOrDefault();

			if (entry == null)
			{
				this.index(entity, EntityState.Added, destroyExistingIndex);
			}
			else if (entry.State == EntityState.Modified)
			{
				this.index(entity, EntityState.Modified, destroyExistingIndex);
			}
			else if (entry.State == EntityState.Deleted)
			{
				this.index(entity, EntityState.Deleted, destroyExistingIndex);
			}
			else if (entry.State == EntityState.Unchanged)
			{
				this.index(entity, EntityState.Unchanged, destroyExistingIndex);
			}
			else if (entry.State == EntityState.Detached)
			{
				this.index(entity, EntityState.Detached, destroyExistingIndex);
			}
		}

		public void DeleteIndex<T>()
		{
			this.destroyIfRequired(typeof(T), true);
		}

		public IQueryable<T> Search<T>(Expression<Func<T, Boolean>> condition, Int32 maxResults = 0) where T : class
		{
			String queryString = condition.Body.ToString();
			queryString = queryString.Replace(" OrElse ", " OR ");
			queryString = queryString.Replace(" AndAlso ", " AND ");
			queryString = queryString.Replace(".ToString()", String.Empty);
			queryString = Regex.Replace(queryString, @"(?<name>\w+)\s==\snull", "-${name}:[* TO *]");
			queryString = Regex.Replace(queryString, @"(?<name>\w+)\s!=\snull", "*:* ${name}:[* TO *]");
			queryString = queryString.Replace(" == ", ":");
			queryString = Regex.Replace(queryString, @"(?<name>\w+).StartsWith\(""(?<value>\w+)""\)", "${name}:${value}*");
			queryString = Regex.Replace(queryString, @"(?<name>\w+).Contains\(""(?<value>\w+)""\)", "${name}:${value}");
			queryString = Regex.Replace(queryString, @"(\w+\.)", String.Empty);
			queryString = Regex.Replace(queryString, @"(?<name>\w+)\s!=\s", "*:* AND NOT ${name}:");
			queryString = Regex.Replace(queryString, @"(?<name>\w+)\s\>=\s(?<value>\w+)", "${name}:[${value} TO *]");
			queryString = Regex.Replace(queryString, @"(?<name>\w+)\s\<=\s(?<value>\w+)", "${name}:[* TO ${value}]");

			return (this.Search<T>(queryString, maxResults));
		}

		public IQueryable<T> Search<T>(String queryString, Int32 maxResults = 0) where T : class
		{
			Type entityType = typeof(T);
			Boolean exists = false;

			using (Analyzer analyzer = this.GetAnalyzer(entityType))
			using (Lucene.Net.Store.Directory directory = this.GetDirectory(entityType, out exists))
			using (Searcher searcher = new IndexSearcher(directory, true))
			{
				LuceneMetadata metadata = this.getLuceneMetadata(entityType);
				MultiFieldQueryParser queryParser = new MultiFieldQueryParser(luceneVersion, metadata.Fields.Select(x => x.Key.Name).ToArray(), analyzer, metadata.Fields.ToDictionary(x => x.Key.Name, x => x.Value.Boost));
				Query query = queryParser.Parse(queryString);
				TopDocs top = searcher.Search(query, maxResults > 0 ? maxResults : searcher.MaxDoc);
				ScoreDoc[] score = top.ScoreDocs;

				if (score.Length > 0)
				{
					PropertyInfo [] idProperties = metadata.Keys.Select(x => x.Key).ToArray();
					
					if (idProperties.Length == 1)
					{
						PropertyInfo idProperty = idProperties.Single();
						Type idType = idProperty.PropertyType;
						ParameterExpression p = Expression.Parameter(entityType, idProperty.Name);
						IList ids = Activator.CreateInstance(typeof(List<>).MakeGenericType(idType)) as IList;

						for (Int32 i = 0; i < score.Length; ++i)
						{
							Document doc = searcher.Doc(score[i].Doc);
							Object id = Convert.ChangeType(doc.GetField(idProperty.Name).StringValue, idType, CultureInfo.InvariantCulture);
							ids.Add(id);
						}

						IQueryable<T> q = base.Set<T>();						
						q = Queryable.Where<T>(q, Expression.Lambda<Func<T, Boolean>>(Expression.Call(Expression.Constant(ids), ids.GetType().GetMethod("Contains"), new Expression[] { Expression.Property(p, idProperty.GetGetMethod()) }), new ParameterExpression[] { p })) as IQueryable<T>;

						if (maxResults > 0)
						{
							q = takeMethod.MakeGenericMethod(entityType).Invoke(null, new Object[] { q, maxResults }) as IQueryable<T>;
						}

						return (q);
					}
					else
					{
						String entityName = entityType.Name;

						if (this.PluralizeEntities == true)
						{
							entityName = pluralizationService.Pluralize(entityName);
						}

						StringBuilder esql = new StringBuilder();
						esql.AppendFormat("SELECT VALUE e FROM {0} AS e WHERE ", entityName);

						List<ObjectParameter> parameters = new List<ObjectParameter>();

						for (Int32 i = 0; i < score.Length; ++i)
						{
							IDictionary<String, Object> ids = new Dictionary<String, Object>();

							if (i != 0)
							{
								esql.Append(" OR ");
							}

							esql.Append("(");

							Document doc = searcher.Doc(score[i].Doc);

							for (Int32 p = 0; p < idProperties.Length; ++p)
							{
								PropertyInfo idProperty = idProperties[p];
								Type idType = idProperty.PropertyType;
								Object id = Convert.ChangeType(doc.GetField(idProperty.Name).StringValue, idType, CultureInfo.InvariantCulture);
								ids.Add(idProperty.Name, id);

								if (p != 0)
								{
									esql.Append(" AND ");
								}

								esql.AppendFormat("e.{0} = @p{1}_{2}", idProperty.Name, i, p);

								parameters.Add(new ObjectParameter(String.Format("p{0}_{1}", i, p), id));
							}

							esql.Append(")");
						}

						String xxx = esql.ToString();

						ObjectContext octx = (this as IObjectContextAdapter).ObjectContext;

						IQueryable<T> q = octx.CreateQuery<T>(esql.ToString(), parameters.ToArray());

						if (maxResults != 0)
						{
							q = takeMethod.MakeGenericMethod(entityType).Invoke(null, new Object[] { q, maxResults }) as IQueryable<T>;
						}

						return (q);
					}
				}
			}

			return (Enumerable.Empty<T>().AsQueryable());
		}

		public IQueryable<T> Search<T>(String name, Object value, Int32 maxResults = 0) where T: class
		{
			return (this.Search<T>(String.Format("{0}:{1}", name, value), maxResults));
		}
		#endregion

		#region Private methods
		private void index(Object entity, EntityState state, Boolean destroyExistingIndex = false)
		{
			Type entityType = this.getEntityType(entity.GetType());

			switch (state)
			{
				case EntityState.Added:
					this.add(entityType, destroyExistingIndex, entity);
					break;

				case EntityState.Deleted:
					this.delete(entityType, destroyExistingIndex, entity);
					break;

				case EntityState.Modified:
					this.update(entityType, destroyExistingIndex, entity);
					break;

				case EntityState.Unchanged:
					this.add(entityType, destroyExistingIndex, entity);
					break;

				case EntityState.Detached:
					this.add(entityType, destroyExistingIndex, entity);
					break;

				default:
					throw (new ArgumentException("Invalid state", "state"));
			}
		}

		private void validateMetadata(LuceneMetadata metadata)
		{
			if (metadata != null)
			{
				IEnumerable<ValidationResult> results = metadata.Validate(new ValidationContext(metadata, null, new Dictionary<Object, Object>()));

				if (results.Any() == true)
				{
					ValidationException exception = new ValidationException("Metadata validation errors");
					exception.Data["ValidationResults"] = results;
					throw (exception);
				}
			}
		}

		private void destroyIfRequired(Type entityType, Boolean destroyExistingIndex)
		{
			if (destroyExistingIndex == true)
			{
				Boolean exists = false;

				entityType = this.getEntityType(entityType);

				using (Analyzer analyzer = this.GetAnalyzer(entityType))
				using (Lucene.Net.Store.Directory directory = this.GetDirectory(entityType, out exists))
				using (IndexWriter writer = this.GetIndexWriter(directory, analyzer, exists))
				{
					writer.DeleteAll();
					writer.Optimize();
				}
			}
		}

		private void add(Type entityType, Boolean destroyExistingIndex, params Object [] entities)
		{
			this.destroyIfRequired(entityType, destroyExistingIndex);

			Boolean exists = false;
			LuceneMetadata metadata = this.getLuceneMetadata(this.getEntityType(entityType));
			
			if (metadata != null)
			{
				this.validateMetadata(metadata);

				using (Analyzer analyzer = this.GetAnalyzer(entityType))
				using (Lucene.Net.Store.Directory directory = this.GetDirectory(entityType, out exists))
				using (IndexWriter writer = this.GetIndexWriter(directory, analyzer, exists))
				{
					foreach (Object entity in entities)
					{
						Document doc = new Document();

						foreach (var key in metadata.Keys)
						{
							Object value = key.Key.GetValue(entity, null);

							if (value != null)
							{								
								Field field = null;
								
								if ((value is IFormattable) && (String.IsNullOrWhiteSpace(key.Value.Format) == false))
								{
									field = new Field(key.Key.Name, (value as IFormattable).ToString(key.Value.Format, CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NOT_ANALYZED);
								}
								else
								{
									field = new Field(key.Key.Name, value.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED);
								}

								doc.Add(field);
							}
						}

						foreach (var f in metadata.Fields)
						{
							if (f.Value is NumericFieldAttribute)
							{
								NumericFieldAttribute attribute = f.Value as NumericFieldAttribute;
								Object value = f.Key.GetValue(entity, null);

								NumericField field = new NumericField(f.Key.Name, attribute.PrecisionStep, this.getStore(f.Value.Store), attribute.Index);

								if ((value is Int32) || (value is Int16) || (value is Byte))
								{
									field.SetIntValue(Convert.ToInt32(value));
								}
								else if (value is Int64)
								{
									field.SetLongValue((Int64)value);
								}
								else if (value is Single)
								{
									field.SetFloatValue((Single)value);
								}
								else if (value is Double)
								{
									field.SetDoubleValue((Double)value);
								}
								else if (value is DateTime)
								{
									field.SetLongValue(((DateTime)value).Ticks);
								}
								else
								{
									throw (new Exception(String.Format("Type {0} is not supported with NumericField", f.Key.PropertyType)));
								}

								field.OmitTermFreqAndPositions = f.Value.OmitTermFreqAndPositions;
								field.Boost = f.Value.Boost;
								field.OmitNorms = f.Value.OmitNorms;

								doc.Add(field);
							}
							else
							{
								Object value = f.Key.GetValue(entity, null);

								if (value != null)
								{
									Field field = null;

									if ((value is IFormattable) && (String.IsNullOrWhiteSpace(f.Value.Format) == false))
									{
										field = new Field(f.Key.Name, (value as IFormattable).ToString(f.Value.Format, CultureInfo.InvariantCulture), this.getStore(f.Value.Store), this.getIndex(f.Value.Index), this.getTermVector(f.Value.TermVector));
									}
									else
									{
										field = new Field(f.Key.Name, value.ToString(), this.getStore(f.Value.Store), this.getIndex(f.Value.Index), this.getTermVector(f.Value.TermVector));
									}

									field.OmitTermFreqAndPositions = f.Value.OmitTermFreqAndPositions;
									field.Boost = f.Value.Boost;
									field.OmitNorms = f.Value.OmitNorms;

									doc.Add(field);
								}
							}
						}

						writer.AddDocument(doc);
						writer.Optimize();

						if (exists == false)
						{
							exists = true;
						}
					}
				}
			}
		}

		private void delete(Type entityType, Boolean destroyExistingIndex, params Object[] entities)
		{
			this.destroyIfRequired(entityType, destroyExistingIndex);

			Boolean exists = false;
			LuceneMetadata metadata = this.getLuceneMetadata(this.getEntityType(entityType));

			if (metadata != null)
			{
				this.validateMetadata(metadata);

				using (Analyzer analyzer = this.GetAnalyzer(entityType))
				using (Lucene.Net.Store.Directory directory = this.GetDirectory(entityType, out exists))
				using (IndexWriter reader = this.GetIndexWriter(directory, analyzer, exists))
				{
					List<Query> queries = new List<Query>();

					foreach (Object entity in entities)
					{
						foreach (var f in metadata.Keys)
						{
							Object value = f.Key.GetValue(entity, null);

							if (value != null)
							{
								QueryParser queryParser = new QueryParser(luceneVersion, f.Key.Name, analyzer);
								Query query = (value is IConvertible) ? queryParser.Parse(String.Format("{0}:{1}", f.Key.Name, (value as IConvertible).ToString(CultureInfo.InvariantCulture))) : queryParser.Parse(String.Format("{0}:{1}", f.Key.Name, value));

								queries.Add(query);
							}
						}
					}

					if (queries.Any() == true)
					{
						reader.DeleteDocuments(queries.ToArray());
						reader.Optimize();
						reader.ExpungeDeletes();
					}
				}
			}
		}

		private void update(Type entityType, Boolean destroyExistingIndex, params Object[] entities)
		{
			this.destroyIfRequired(entityType, destroyExistingIndex);

			Boolean exists = false;
			LuceneMetadata metadata = this.getLuceneMetadata(this.getEntityType(entityType));

			if (metadata != null)
			{
				this.validateMetadata(metadata);

				using (Analyzer analyzer = this.GetAnalyzer(entityType))
				using (Lucene.Net.Store.Directory directory = this.GetDirectory(entityType, out exists))
				using (IndexSearcher searcher = new IndexSearcher(directory))
				using (IndexWriter reader = this.GetIndexWriter(directory, analyzer, true))
				{
					Boolean changes = false;

					foreach (Object entity in entities)
					{
						foreach (var f in metadata.Keys)
						{
							Object value = f.Key.GetValue(entity, null);

							if (value != null)
							{
								QueryParser queryParser = new QueryParser(luceneVersion, f.Key.Name, analyzer);
								Query query = (value is IConvertible) ? queryParser.Parse(String.Format("{0}:{1}", f.Key.Name, (value as IConvertible).ToString(CultureInfo.InvariantCulture))) : queryParser.Parse(String.Format("{0}:{1}", f.Key.Name, value));
								TopDocs top = searcher.Search(query, 1);

								if (top.TotalHits == 1)
								{
									ScoreDoc[] score = top.ScoreDocs;
									Document doc = searcher.Doc(score[0].Doc);

									foreach (var p in metadata.Fields)
									{
										Object currentFieldValue = p.Key.GetValue(entity, null);
										String storedFieldValueString = doc.GetField(p.Key.Name).StringValue;

										if (currentFieldValue != null)
										{
											String currentFieldValueString = (currentFieldValue is IConvertible) ? (currentFieldValue as IConvertible).ToString(CultureInfo.InvariantCulture) : currentFieldValue.ToString();

											changes |= (currentFieldValueString == storedFieldValueString);
										}
										else
										{
											changes = true;
										}
									}
								}
							}
						}
					}

					if (changes == true)
					{
						reader.Optimize();
					}
				}
			}
		}
		
		private Field.Store getStore(Store store)
		{
			switch (store)
			{
				case Store.No:
					return (Field.Store.NO);

				case Store.Yes:
					return (Field.Store.YES);
			}

			throw (new ArgumentException("Invalid store value", "store"));
		}

		private Field.TermVector getTermVector(TermVector termVector)
		{
			switch (termVector)
			{
				case TermVector.No:
					return(Field.TermVector.NO);

				case TermVector.WithOffsets:
					return(Field.TermVector.WITH_OFFSETS);

				case TermVector.WithPositions:
					return(Field.TermVector.WITH_POSITIONS);

				case TermVector.WithPositionsOffsets:
					return(Field.TermVector.WITH_POSITIONS_OFFSETS);

				case TermVector.Yes:
					return(Field.TermVector.YES);
			}

			throw (new ArgumentException("Invalid term vector", "termVector"));
		}

		private Field.Index getIndex(Index index)
		{
			switch (index)
			{
				case DevelopmentWithADot.EntityFrameworkLuceneIntegration.Index.Analyzed:
					return (Field.Index.ANALYZED);

				case DevelopmentWithADot.EntityFrameworkLuceneIntegration.Index.AnalyzedNoNorms:
					return (Field.Index.ANALYZED_NO_NORMS);

				case DevelopmentWithADot.EntityFrameworkLuceneIntegration.Index.No:
					return (Field.Index.NO);

				case DevelopmentWithADot.EntityFrameworkLuceneIntegration.Index.NotAnalyzed:
					return (Field.Index.NOT_ANALYZED);

				case DevelopmentWithADot.EntityFrameworkLuceneIntegration.Index.NotAnalyzedNoNorms:
					return (Field.Index.NOT_ANALYZED_NO_NORMS);
			}

			throw (new ArgumentException("Invalid index value", "index"));
		}

		private Type getEntityType(Type type)
		{
			return ((type.Assembly.IsDynamic == true) ? type.BaseType : type);
		}

		private LuceneMetadata getLuceneMetadata(Type type)
		{
			LuceneMetadata metadata;

			type = this.getEntityType(type);

			if (LuceneMetadata.Metadata.TryGetValue(type, out metadata) == false)
			{
				metadata = new LuceneMetadata(type, this.getLuceneEntity(type), this.getLuceneIdProperties(type), this.getLuceneNonIdProperties(type));
				LuceneMetadata.Metadata[type] = metadata;
			}

			return (metadata);
		}

		private DocumentAttribute getLuceneEntity(Type type)
		{
			return (type.GetCustomAttributes(typeof(DocumentAttribute), true).OfType<DocumentAttribute>().Single());
		}

		private IDictionary<PropertyInfo, FieldAttribute> getLuceneNonIdProperties(Type type)
		{
			return (type
				.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(x => x.GetCustomAttributes(typeof(FieldAttribute), false).Any())
				.Where(x => x.GetCustomAttributes(typeof(FieldAttribute), false).OfType<FieldAttribute>().Any(y => y.Key == false))
				.ToDictionary(x => x, x => x.GetCustomAttributes(typeof(FieldAttribute), true).OfType<FieldAttribute>().Single()));
		}

		private IDictionary<PropertyInfo, FieldAttribute> getLuceneIdProperties(Type type)
		{
			return (type
				.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(x => x.GetCustomAttributes(typeof(FieldAttribute), false).Any())
				.Where(x => x.GetCustomAttributes(typeof(FieldAttribute), false).OfType<FieldAttribute>().Any(y => y.Key == true))
				.ToDictionary(x => x, x => x.GetCustomAttributes(typeof(FieldAttribute), true).OfType<FieldAttribute>().Single()));
		}
		#endregion
	}
}
