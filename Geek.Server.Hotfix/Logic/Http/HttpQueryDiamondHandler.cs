using Geek.Server.App.Logic.Account;
using Geek.Server.Core.Net.Http;
using Geek.Server.Core.Storage;

namespace Server.Logic.Logic.Http
{
	[HttpMsgMapping("query_diamond")]
	public class HttpQueryDiamondHandler : BaseHttpHandler
	{
		public override async Task<string> Action(string ip, string url, Dictionary<string, string> paramMap)
		{
			if (!paramMap.TryGetValue("user_Id", out string userId) ||
				!paramMap.TryGetValue("account_Id", out string accountId)) return HttpResult.CreateErrorParam();

			if (userId.IsNullOrEmpty()) return HttpResult.CreateErrorParam();

			if (!long.TryParse(accountId, out long accountIdLong) && accountIdLong <= 0) return HttpResult.CreateErrorParam();

			var accountState = await GameDB.CreateQueryBuilder<AccountState>()
			   .AddFilter("UserId", userId)
			   .Load();

			if (accountState == null) return HttpResult.CreateErrorParam();
			return HttpResult.CreateOk(accountState.Diamond.ToString());
		}
	}
}