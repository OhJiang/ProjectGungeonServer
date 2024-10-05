﻿using Geek.Server.Core.Serialize;
using MongoDB.Driver;
using NLog;
using BsonSerializer = MongoDB.Bson.Serialization.BsonSerializer;

namespace Geek.Server.Core.Storage
{

    public static class MongoDBExtensions
    {
        public static IMongoCollection<TDocument> GetCollection<TDocument>(this IMongoDatabase self, MongoCollectionSettings settings = null)
        {
            return self.GetCollection<TDocument>(typeof(TDocument).FullName, settings);
        }
    }

    public class MongoDBConnection : IGameDB
    {
        private readonly Logger Log = LogManager.GetCurrentClassLogger();

        public MongoClient Client { get; private set; }

        public IMongoDatabase CurDB { get; private set; }

        public static readonly ReplaceOptions REPLACE_OPTIONS = new() { IsUpsert = true };

        public static readonly BulkWriteOptions BULK_WRITE_OPTIONS = new() { IsOrdered = false };

        public void Open(string url, string dbName)
        {
            try
            {
                var settings = MongoClientSettings.FromConnectionString(url);
                Client = new MongoClient(settings);
                CurDB = Client.GetDatabase(dbName);
                Log.Info($"初始化MongoDB服务完成 Url:{url} DbName:{dbName}");
            }
            catch (Exception)
            {
                Log.Error($"初始化MongoDB服务失败 Url:{url} DbName:{dbName}");
                throw;
            }
        }

        public async Task<TState> LoadState<TState>(long id, Func<TState> defaultGetter = null) where TState : CacheState, new()
        {
            var filter = Builders<TState>.Filter.Eq(CacheState.UniqueId, id);
            var stateName = typeof(TState).FullName;
            var col = CurDB.GetCollection<TState>(stateName);

            using var cursor = await col.FindAsync(filter);
            var state = await cursor.FirstOrDefaultAsync();
            state?.AfterLoadFromDB();
            state ??= defaultGetter?.Invoke();
            state ??= new TState { Id = id };
            return state;
        }

        public async Task<TState> SaveState<TState>(TState state) where TState : CacheState
        {
            var filter = Builders<TState>.Filter.Eq(CacheState.UniqueId, state.Id);
            var stateName = typeof(TState).FullName;
            var col = CurDB.GetCollection<TState>(stateName);
            var result = await col.ReplaceOneAsync(filter, state, REPLACE_OPTIONS);
            if (result.IsAcknowledged)
            {
                state.AfterSaveToDB();
            }

            return state;
        }

        public async Task<TState> LoadState<TState, TValue1, TValue2>(string field1, TValue1 value1, string field2,
            TValue2 value2, Func<TState> defaultGetter = null) where TState : CacheState, new()
        {
            var filter = Builders<TState>.Filter.Eq(field1, value1) & Builders<TState>.Filter.Eq(field2, value2);
            var stateName = typeof(TState).FullName;
            var col = CurDB.GetCollection<TState>(stateName);
            var state = await col.Find(filter).Limit(1).FirstOrDefaultAsync();
            if (state != null)
            {
                state.AfterLoadFromDB();
                return state;
            }
            // 如果找不到状态，则返回默认值或通过提供的 defaultGetter 创建新状态
            return  null;
        }

        public StateQueryBuilder<TState> CreateQueryBuilder<TState>() where TState : CacheState, new()
        {
            return new StateQueryBuilder<TState>(CurDB);
        }

        public void Close()
        {
            Client.Cluster.Dispose();
        }


        public Task Flush()
        {
            return Task.CompletedTask;
        }
    }
}