using MongoDB.Driver;

namespace Geek.Server.Core.Storage
{
	public class StateQueryBuilder<TState> where TState : CacheState, new()
	{
		private readonly IMongoCollection<TState> _collection;
		private readonly List<FilterDefinition<TState>> _filters = new();
		private readonly FilterDefinitionBuilder<TState> _filterBuilder = Builders<TState>.Filter;

		public StateQueryBuilder(IMongoDatabase db)
		{
			var stateName = typeof(TState).FullName;
			_collection = db.GetCollection<TState>(stateName);
		}

		public StateQueryBuilder<TState> AddFilter(string fieldName, object value)
		{
			var filter = _filterBuilder.Eq(fieldName, value);
			_filters.Add(filter);
			return this; // Return the builder instance to allow chaining
		}

		public async Task<TState> Load(Func<TState> defaultGetter = null)
		{
			var combinedFilter = _filterBuilder.And(_filters);

			using var cursor = await _collection.FindAsync(combinedFilter);
			var state = await cursor.FirstOrDefaultAsync();

			state?.AfterLoadFromDB();
			state ??= defaultGetter?.Invoke();
			state ??= new TState();
			return state;
		}
	}
}