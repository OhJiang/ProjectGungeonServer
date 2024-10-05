using Geek.Server.App.Common;
using Geek.Server.App.Common.Session;
using Geek.Server.App.Logic.Account;
using Geek.Server.Core.Actors;
using Geek.Server.Core.Net;
using Geek.Server.Core.Storage;
using Geek.Server.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using Server.Logic.Common.Handler;
using Server.Logic.Core;
using Server.Logic.Logic.Server;

namespace Server.Logic.Logic.Account
{
	public class AccountManager : Singleton<AccountManager>
	{
		private static readonly Logger Log = LogManager.GetCurrentClassLogger();

		public async Task OnSDKLogin(NetChannel channel, ReqAccountLogin reqAccountLogin)
		{
			var validationChain = new LoginValidationChain()
			   .AddValidator(new PlatformValidator())
			   .AddValidator(new SdkTypeValidator())
			   .AddValidator(new DeviceIdValidator())
			   .AddValidator(new UserIdValidator());

			if (!validationChain.Validate(reqAccountLogin, channel)) return;

			var isNewUser = false;
			var account = await GetAccount("UserId", reqAccountLogin.UserId);
			if (account == null)
			{
				isNewUser = true;

				//没有老角色，创建新号
				account = await CreateAccount(reqAccountLogin);
				Log.Debug($"创建新用户，AccountId: {account.AccountId}");
			}
			else
			{
				Log.Debug($"老用户，AccountId: {account.AccountId}");
			}

			AddSession(channel, account, reqAccountLogin.Device);

			string token = JwtTokenUtil.GenerateJwtToken(account.UserName, account.SdkType, account.AccountId);

			await BuildLoginMsgAndAddOnlineRole(channel, account, token, isNewUser, reqAccountLogin.UniId);
		}

		public async Task OnCustomLogin(NetChannel channel, ReqAccountCusLogin reqAccountCusLogin)
		{
			var validationChain = new LoginValidationChain()
			   .AddValidator(new PlatformValidator())
			   .AddValidator(new SdkTypeValidator())
			   .AddValidator(new DeviceIdValidator())
			   .AddValidator(new UserIdValidator());

			if (!validationChain.Validate(reqAccountCusLogin, channel)) return;

			var accountId = SessionManager.GetAccountId(channel);
			if (accountId > 0)
			{
				var accountLoggedIn = await GetAccountByAccountId(accountId);
				if (accountLoggedIn != null)
				{
					//已经登陆过了
					var resAccountLoggedIn = await BuildLoginMsg(accountLoggedIn, string.Empty);
					channel.Write(resAccountLoggedIn, reqAccountCusLogin.UniId);
					return;
				}
			}

			if (!VerifyTokenAndTryCatchExceptions(channel, reqAccountCusLogin, ref accountId)) return;

			var accountState = await GetAccountByAccountId(accountId);
			if (accountState != null)
			{
				Log.Debug($"老用户，AccountId: {accountId}");
			}
			else
			{
				Log.Warn($"获取 AccountState 失败，AccountId: {accountId}");
				channel.Write(null, reqAccountCusLogin.UniId, StateCode.GotAccountError);
				return;
			}

			AddSession(channel, accountState, reqAccountCusLogin.Device);

			await BuildLoginMsgAndAddOnlineRole(channel, accountState, string.Empty, false, reqAccountCusLogin.UniId);
		}
		
		private bool VerifyTokenAndTryCatchExceptions(NetChannel channel, ReqAccountCusLogin reqAccountCusLogin, ref long accountId)
		{
			try
			{
				var principal = JwtTokenUtil.ValidateJwtToken(reqAccountCusLogin.CustomToken);
				var accountIdClaim = principal.FindFirst("AccountId");
				if (!long.TryParse(accountIdClaim?.Value, out var parsedAccountId)) return true;
				accountId = parsedAccountId;
				Log.Debug("验证自定义令牌成功: " + accountId);
				return true;
			}
			catch (SecurityTokenExpiredException)
			{
				// 处理令牌过期的情况
				Log.Debug($"AccountId: {accountId} Token has expired.");
				channel.Write(null, reqAccountCusLogin.UniId, StateCode.TokenExpired);
				return false;
			}
			catch (Exception ex)
			{
				// Token 验证失败
				Log.Debug($"AccountId: {accountId} Token validation failed, Exception: {ex.Message}");
				channel.Write(null, reqAccountCusLogin.UniId, StateCode.CusTokenInvalid);
				return false;
			}
		}

		private static void AddSession(NetChannel channel, AccountState account, string device)
		{
			var session = new Session()
			   .SetId(account.AccountId)
			   .SetTime(DateTime.Now)
			   .SetChannel(channel)
			   .SetSign(device);
			SessionManager.Add(session);
		}

		private async Task BuildLoginMsgAndAddOnlineRole(NetChannel channel, AccountState account, string token, bool isNewUser, int reqUniId)
		{
			// 构建登录响应消息并发送给客户端
			var resAccountLogin = await BuildLoginMsg(account, token, isNewUser);
			channel.Write(resAccountLogin, reqUniId);

			// 加入在线玩家列表
			var serverComp = await ActorMgr.GetCompAgent<ServerCompAgent>();
			await serverComp.AddOnlineRole(account.Id);
		}

		private async Task<ResAccountLogin> BuildLoginMsg(AccountState accountState, string token, bool isNewUser = false)
		{
			var res = new ResAccountLogin()
			{
				Code = (int)StateCode.Success,
				UserInfo = new UserInfo()
				{
					CreateTime = accountState.CreateTime.Ticks,
					Level = accountState.Level,
					AccountId = accountState.AccountId,
					RoleName = accountState.UserName,
					VipLevel = accountState.VipLevel,
					Diamond = accountState.Diamond,
					UserIcon = accountState.UserIcon,
					UserStatus = accountState.UserStatus,
					CostCount = accountState.CostCount,
					NoAds = accountState.NoAds,
					IsNewUser = isNewUser,
				},
				CustomToken = token
			};
			accountState.LoginTime = DateTime.Now;
			await GameDB.SaveState(accountState);
			return res;
		}

		private async Task<AccountState> GetAccountByAccountId(long accountId)
		{
			var state = await GameDB.CreateQueryBuilder<AccountState>()
			   .AddFilter("AccountId", accountId)
			   .Load();
			Log.Debug($"从数据库中获取到数据: AccountId => {state.AccountId}, UserId => {state.UserId}");
			return state;
		}

		private async Task<AccountState> GetAccount(string fieldName, string value)
		{
			var state = await GameDB.CreateQueryBuilder<AccountState>()
			   .AddFilter(fieldName, value)
			   .Load();

			Log.Debug($"从数据库中获取到数据: AccountId => {state.AccountId}, UserId => {state.UserId}");
			return state;
		}

		/// <summary>
		/// 创建账号
		/// </summary>
		private async Task<AccountState> CreateAccount(ReqAccountLogin reqAccountLogin, int roomId = 1)
		{
			var account = new AccountState
			{
				AccountId = IdGenerator.GetActorID(ActorType.Role),
				GamePackageName = reqAccountLogin.GamePackageName,
				UserId = reqAccountLogin.UserId,
				SdkType = reqAccountLogin.SdkType,
				UserName = reqAccountLogin.UserName,
				Platform = reqAccountLogin.Platform,
				Device = reqAccountLogin.Device,
				RoomId = roomId,
			};
			account = await GameDB.SaveState(account);
			Log.Debug($"创建新账号，AccountId: {account.AccountId}");
			return account;
		}
	}
}