﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using LinqToTwitter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Twice.Models.Cache;

namespace Twice.Tests.Models.Cache
{
	[TestClass, ExcludeFromCodeCoverage]
	public class SqliteCacheTests
	{
		[TestMethod, TestCategory( "Models.Cache" )]
		public async Task AddingUserTwiceUpdatesData()
		{
			// Arrange
			using( var con = OpenConnection() )
			using( var cache = new SqliteCache( con ) )
			{
				var user = DummyGenerator.CreateDummyUser();
				user.UserID = 123;
				user.ScreenName = "test";

				await cache.AddUser( new UserCacheEntry( user ) );

				// Act
				user.ScreenName = "testi";
				await cache.AddUser( new UserCacheEntry( user ) );

				// Assert
				var fromDb = ( await cache.GetKnownUsers() ).First();

				Assert.AreEqual( "testi", fromDb.UserName );
			}
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public async Task CachedHastagsCanBeRetrieved()
		{
			// Arrange
			using( var con = OpenConnection() )
			using( var cache = new SqliteCache( con ) )
			{
				using( var cmd = con.CreateCommand() )
				{
					cmd.CommandText = "INSERT INTO Hashtags (Tag) VALUES ('test'), ('abc');";
					cmd.ExecuteNonQuery();
				}

				// Act
				var tags = ( await cache.GetKnownHashtags() ).ToArray();

				// Assert
				CollectionAssert.AreEquivalent( new[] {"test", "abc"}, tags );
			}
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public async Task CachedUsersCanBeRetrieved()
		{
			// Arrange
			using( var con = OpenConnection() )
			using( var cache = new SqliteCache( con ) )
			{
				using( var cmd = con.CreateCommand() )
				{
					cmd.CommandText = "INSERT INTO Users (Id, UserName, UserData) VALUES (@id1, @name1, @data1), "
									+ "(@id2, @name2, @data2);";

					cmd.AddParameter( "id1", 111 );
					cmd.AddParameter( "id2", 222 );
					cmd.AddParameter( "name1", "testi" );
					cmd.AddParameter( "name2", "testUser" );

					var u = DummyGenerator.CreateDummyUser();
					u.UserID = 111;
					u.ScreenName = "testi";
					u.CreatedAt = new DateTime( 1, 2, 3, 4, 5, 6 );
					cmd.AddParameter( "data1", JsonConvert.SerializeObject( u ) );

					u = DummyGenerator.CreateDummyUser();
					u.UserID = 222;
					u.ScreenName = "testUser";
					u.CreatedAt = new DateTime( 6, 5, 4, 3, 2, 1 );
					cmd.AddParameter( "data2", JsonConvert.SerializeObject( u ) );

					cmd.ExecuteNonQuery();
				}

				// Act
				var users = ( await cache.GetKnownUsers() ).ToArray();

				// Assert
				Assert.AreEqual( 2, users.Length );
				Assert.IsNotNull( users.SingleOrDefault( u => u.UserId == 111 ) );
				Assert.IsNotNull( users.SingleOrDefault( u => u.UserId == 222 ) );
				Assert.IsNotNull( users.SingleOrDefault( u => u.UserName == "testi" ) );
				Assert.IsNotNull( users.SingleOrDefault( u => u.UserName == "testUser" ) );

				var data = JsonConvert.DeserializeObject<User>( users.First( u => u.UserId == 111 ).Data );
				Assert.AreEqual( new DateTime( 1, 2, 3, 4, 5, 6 ), data.CreatedAt );

				data = JsonConvert.DeserializeObject<User>( users.First( u => u.UserId == 222 ).Data );
				Assert.AreEqual( new DateTime( 6, 5, 4, 3, 2, 1 ), data.CreatedAt );
			}
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public void ConstructingFromConnectionStringWorks()
		{
			// Arrange
			var sb = new SQLiteConnectionStringBuilder
			{
				DataSource = ":memory:"
			};

			// Act
			var ex = ExceptionAssert.Catch<Exception>( () => new SqliteCache( sb.ToString() ) );

			// Assert
			Assert.IsNull( ex, ex?.ToString() );
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public void DatabaseTablesAreCreatedOnConstruction()
		{
			// Arrange
			using( var con = OpenConnection() )
			using( new SqliteCache( con ) )
			{
				// Act
				List<string> tableNames = new List<string>();
				using( var cmd = con.CreateCommand() )
				{
					cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
					using( var reader = cmd.ExecuteReader() )
					{
						while( reader.Read() )
						{
							tableNames.Add( reader.GetString( 0 ) );
						}
					}
				}

				// Assert
				CollectionAssert.AreEquivalent( new[] {"Hashtags", "Users", "TwitterConfig", "Statuses"}, tableNames );
			}
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public void DisposeClosesDatabaseConnection()
		{
			// Arrange
			var con = OpenConnection();
			bool disposed = false;
			con.Disposed += ( s, e ) => disposed = true;
			var cache = new SqliteCache( con );

			// Act
			cache.Dispose();

			// Assert
			Assert.IsTrue( disposed );
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public async Task ExpiredConfigurationIsNotLoaded()
		{
			// Arrange
			using( var con = OpenConnection() )
			using( var cache = new SqliteCache( con ) )
			{
				using( var cmd = con.CreateCommand() )
				{
					cmd.CommandText = "INSERT INTO TwitterConfig (Data, Expires) VALUES ('test', 100);";
					cmd.ExecuteNonQuery();
				}

				// Act
				var cfg = await cache.ReadTwitterConfig();

				// Assert
				Assert.IsNull( cfg );
			}
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public async Task ExpiredHashtagIsNotRetrieved()
		{
			// Arrange
			using( var con = OpenConnection() )
			using( var cache = new SqliteCache( con ) )
			{
				using( var cmd = con.CreateCommand() )
				{
					cmd.CommandText = "INSERT INTO Hashtags (Tag, Expires) VALUES ('test', 100);";
					cmd.ExecuteNonQuery();
				}

				// Act
				var tags = ( await cache.GetKnownHashtags() ).ToArray();

				// Assert
				Assert.AreEqual( 0, tags.Length );
			}
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public async Task ExpiredUserIsNotRetrieved()
		{
			// Arrange
			using( var con = OpenConnection() )
			using( var cache = new SqliteCache( con ) )
			{
				using( var cmd = con.CreateCommand() )
				{
					cmd.CommandText = "INSERT INTO Users (Id, UserName, UserData, Expires) VALUES (123, 'test', 'test', 100);";
					cmd.ExecuteNonQuery();
				}

				// Act
				var users = ( await cache.GetKnownUsers() ).ToArray();

				// Assert
				Assert.AreEqual( 0, users.Length );
			}
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public async Task HashtagCanBeAdded()
		{
			// Arrange
			using( var con = OpenConnection() )
			using( var cache = new SqliteCache( con ) )
			{
				// Act
				await cache.AddHashtag( "test" );

				// Assert
				using( var cmd = con.CreateCommand() )
				{
					cmd.CommandText = "SELECT Tag FROM Hashtags WHERE Tag = 'test';";

					var fromDb = cmd.ExecuteScalar();
					Assert.AreEqual( "test", fromDb );
				}
			}
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public async Task ReadingNonExistingConfigurationReturnsNull()
		{
			// Arrange
			using( var con = OpenConnection() )
			using( var cache = new SqliteCache( con ) )
			{
				// Act
				var cfg = await cache.ReadTwitterConfig();

				// Assert
				Assert.IsNull( cfg );
			}
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public async Task TwitterConfigCanBeRetrieved()
		{
			// Arrange
			using( var con = OpenConnection() )
			using( var cache = new SqliteCache( con ) )
			{
				var cfg = new LinqToTwitter.Configuration
				{
					PhotoSizeLimit = 123
				};

				using( var cmd = con.CreateCommand() )
				{
					cmd.CommandText = "INSERT INTO TwitterConfig (Data) VALUES (@json);";
					var p = cmd.CreateParameter();
					p.ParameterName = "json";
					p.Value = JsonConvert.SerializeObject( cfg );
					cmd.Parameters.Add( p );

					cmd.ExecuteNonQuery();
				}

				// Act
				var loaded = await cache.ReadTwitterConfig();

				// Assert
				Assert.AreEqual( cfg.PhotoSizeLimit, loaded.PhotoSizeLimit );
			}
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public async Task TwitterConfigCanBeStored()
		{
			// Arrange
			using( var con = OpenConnection() )
			using( var cache = new SqliteCache( con ) )
			{
				var cfg = new LinqToTwitter.Configuration
				{
					PhotoSizeLimit = 123
				};

				// Act
				await cache.SaveTwitterConfig( cfg );

				// Assert
				using( var cmd = con.CreateCommand() )
				{
					cmd.CommandText = "SELECT Data FROM TwitterConfig";
					var fromDb = JsonConvert.DeserializeObject<LinqToTwitter.Configuration>( (string)cmd.ExecuteScalar() );

					Assert.AreEqual( cfg.PhotoSizeLimit, fromDb.PhotoSizeLimit );
				}
			}
		}

		[TestMethod, TestCategory( "Models.Cache" )]
		public async Task UserCanBeAdded()
		{
			// Arrange
			using( var con = OpenConnection() )
			using( var cache = new SqliteCache( con ) )
			{
				var user = DummyGenerator.CreateDummyUser();
				user.UserID = 123;
				user.ScreenName = "testi";

				var entry = new UserCacheEntry( user );

				// Act
				await cache.AddUser( entry );

				// Assert
				using( var cmd = con.CreateCommand() )
				{
					cmd.CommandText = "SELECT Id, UserName, UserData FROM Users";
					using( var reader = cmd.ExecuteReader() )
					{
						Assert.IsTrue( reader.Read() );

						Assert.AreEqual( 123L, reader.GetInt64( 0 ) );
						Assert.AreEqual( "testi", reader.GetString( 1 ) );

						var jsonUser = JsonConvert.DeserializeObject<User>( reader.GetString( 2 ) );
						Assert.AreEqual( user.UserID, jsonUser.UserID );
						Assert.AreEqual( user.ScreenName, jsonUser.ScreenName );
					}
				}
			}
		}

		private static SQLiteConnection OpenConnection()
		{
			var sb = new SQLiteConnectionStringBuilder
			{
				DataSource = ":memory:"
			};

			var connection = new SQLiteConnection( sb.ToString() );

			return connection.OpenAndReturn();
		}
	}
}