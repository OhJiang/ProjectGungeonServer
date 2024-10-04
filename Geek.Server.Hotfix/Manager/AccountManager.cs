using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Geek.Server.App.Common;
using Geek.Server.App.Common.Session;
using Geek.Server.App.Logic.Account;
using Geek.Server.Core.Actors;
using Geek.Server.Core.Net;
using Geek.Server.Core.Storage;
using Geek.Server.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using Server.Logic.Core;
using Server.Logic.Logic.Server;
using  Server.Logic.Common.Handler;

namespace Server.Logic.Manager
{
	public class AccountManager : Singleton<AccountManager>
	{
		private static readonly Logger Log = LogManager.GetCurrentClassLogger();
		private const string Jwtkey = "PCCjW6pSoOHVNpiboEAmHpoH1lcOci9u";
		private const string AndroidPackage = "com.redfalcon.baseballgoat.android";
		private const string IosPackage = "com.redfalcon.baseballgoat.ios";
		private const string ANDROID = "android";
		private const string IOS = "ios";
		private const string UNITY = "unity";
		private const int CUSTOM_TOKEN_EXPIRY_TIME = 30;
		
		private enum SdkType
        {
            GooglePlay = 0,
            Apple = 1,
            Guest = 2
        }

        public async Task OnSDKLogin(NetChannel channel, ReqAccountLogin reqAccountLogin)
        {
            if (!ValidateLoginRequest(channel, reqAccountLogin)) return;

            Log.Info($"OnSDKLogin: UniId: {reqAccountLogin.UniId}, SdkType: {reqAccountLogin.SdkType}, Device: {reqAccountLogin.Device}, " +
                $"UserId: {reqAccountLogin.UserId}, AccountId: {reqAccountLogin.AccountId}, Platform: {reqAccountLogin.Platform}");

            var userId = GetUserId(reqAccountLogin);

            if (!ValidateUserId(channel, reqAccountLogin, userId)) return;

            //查询角色账号，这里设定每个服务器只能有一个角色
            AccountState account = null;
            var isNewUser = false;

#if DEBUG
            var isNewRole = reqAccountLogin.AccountId < 1;
            if (isNewRole)
            {
                isNewUser = true;
                account = await CreateAccount(reqAccountLogin);
                Log.Debug($"新用户AccountId: {account.AccountId}");
            }
            else
            {
                account = await GetAccount(reqAccountLogin.AccountId);
                if (account == null)
                {
                    isNewUser = true;
                    account = await CreateAccount(reqAccountLogin);
                    Log.Debug($"新用户AccountId: {account.AccountId}");
                }
                else
                {
                    Log.Debug($"老用户AccountId: {account.AccountId}");
                }
            }
#else
            account = await GetAccount("UserId", userId, "SdkType", reqAccountLogin.SdkType);
            if (account == null)
            {
                isNewUser = true;
                //没有老角色，创建新号
                account = await CreateAccount(reqAccountLogin);
                Log.Debug($"新用户AccountId: {account.AccountId}");
            }
            else
            {
                Log.Debug($"老用户AccountId: {account.AccountId}");
            }
#endif

            if (account == null)
            {
                channel.Write(new ResAccountLogin(), reqAccountLogin.UniId, StateCode.GotAccountError);
                return;
            }

            AddSession(channel, account, reqAccountLogin.Device);
            // Log.Debug($"ActorId:{ActorId},StateId:{State?.Id},SessionId:{session.Id},AccountId:{account.Id}");
            //生成自定令牌
            string token;
            try
            {
                token = GenerateJwtToken(account.UserName, account.SdkType, account.AccountId);
            }
            catch (Exception e)
            {
                Log.Error("GenerateJwtToken Error===" + e.Message);
                throw;
            }
            await CompleteLoginProcess(channel, account, token, isNewUser, reqAccountLogin.UniId);
        }

        public async Task OnCustomLogin(NetChannel channel, ReqAccountCusLogin reqAccountCusLogin)
        {
            if (!ValidateCustomLoginRequest(channel, reqAccountCusLogin)) return;
            
            Log.Info($"OnCustomLogin: UniId: {reqAccountCusLogin.UniId}, SdkType: {reqAccountCusLogin.SdkType}, Device: {reqAccountCusLogin.Device}, " +
                $"AccountId: {reqAccountCusLogin.AccountId}, Platform: {reqAccountCusLogin.Platform}");

            //验证账号是否已经登陆
            var accountId = SessionManager.GetAccountId(channel);
            if (accountId > 0)
            {
                var accountLoggedIn = await GetAccount(accountId);
                if (accountLoggedIn != null)
                {
                    //已经登陆过了
                    var resAccountLoggedIn = BuildLoginMsg(accountLoggedIn, string.Empty);
                    channel.Write(resAccountLoggedIn, reqAccountCusLogin.UniId);
                    return;
                }
            }

            if (!VerifyTokenAndTryCatchExceptions(channel, reqAccountCusLogin, ref accountId)) return;

            //查询角色账号，这里设定每个服务器只能有一个角色
            var account = await GetAccount(accountId);
            if (account != null)
            {
                Log.Debug($"老用户AccountId: {accountId}");
            }
            else
            {
                Log.Debug($"数据库用户AccountId: {accountId}，已丢失");
                channel.Write(new ResAccountLogin() { Code = (int)StateCode.GotAccountError }, reqAccountCusLogin.UniId, StateCode.GotAccountError);
                return;
            }

            AddSession(channel, account, reqAccountCusLogin.Device);
            // Log.Debug($"ActorId:{ActorId},StateId:{State?.Id},SessionId:{session.Id},AccountId:{account.Id}");
            await CompleteLoginProcess(channel, account, string.Empty, false, reqAccountCusLogin.UniId);
        }

        #region Validate

        private bool ValidateLoginRequest(NetChannel channel, ReqAccountLogin reqAccountLogin)
        {
            // 验证平台合法性
            if (reqAccountLogin.Platform != ANDROID && reqAccountLogin.Platform != IOS && reqAccountLogin.Platform != UNITY)
            {
                channel.Write(new ResAccountLogin() { Code = (int)StateCode.UnknownPlatform }, reqAccountLogin.UniId, StateCode.UnknownPlatform);
                return false;
            }

            // 验证账号合法性
            // if (reqAccountLogin.SdkType < 0 || reqAccountLogin.SdkType > 2)
            if (!Enum.IsDefined(typeof(SdkType), reqAccountLogin.SdkType))
            {
                channel.Write(new ResAccountLogin() { Code = (int)StateCode.UnknownSdkType }, reqAccountLogin.UniId, StateCode.UnknownSdkType);
                return false;
            }

            // 验证设备合法性
            if (string.IsNullOrEmpty(reqAccountLogin.Device))
            {
                channel.Write(new ResAccountLogin() { Code = (int)StateCode.DeviceCannotBeNull }, reqAccountLogin.UniId, StateCode.DeviceCannotBeNull);
                return false;
            }

            return true;
        }

        private bool ValidateCustomLoginRequest(NetChannel channel, ReqAccountCusLogin reqAccountCusLogin)
        {
            if (reqAccountCusLogin.Platform != ANDROID && reqAccountCusLogin.Platform != IOS && reqAccountCusLogin.Platform != UNITY)
            {
                //验证平台合法性
                channel.Write(new ResAccountLogin() { Code = (int)StateCode.UnknownPlatform }, reqAccountCusLogin.UniId, StateCode.UnknownPlatform);
                return false;
            }

            //验证账号合法性
            // if (reqAccountLogin.SdkType < 0 || reqAccountLogin.SdkType > 2)
            if (!Enum.IsDefined(typeof(SdkType), reqAccountCusLogin.SdkType))
            {
                channel.Write(new ResAccountLogin() { Code = (int)StateCode.UnknownSdkType }, reqAccountCusLogin.UniId, StateCode.UnknownSdkType);
                return false;
            }

            //验证设备合法性
            if (string.IsNullOrEmpty(reqAccountCusLogin.Device))
            {
                channel.Write(new ResAccountLogin() { Code = (int)StateCode.DeviceCannotBeNull }, reqAccountCusLogin.UniId, StateCode.DeviceCannotBeNull);
                return false;
            }
    
            return true;
        }

        private static bool ValidateUserId(NetChannel channel, ReqAccountLogin reqAccountLogin, string userId)
        {
            if (string.IsNullOrEmpty(userId) && reqAccountLogin.Platform != UNITY)
            {
                channel.Write(new ResAccountLogin()
                    { Code = (int)StateCode.AccountCannotBeNull }, reqAccountLogin.UniId, StateCode.AccountCannotBeNull);
                Log.Warn($"=================userId is null=============");
                return false;
            }
            return true;
        }

        private bool VerifyTokenAndTryCatchExceptions(NetChannel channel, ReqAccountCusLogin reqAccountCusLogin, ref long accountId)
        {
            try
            {
                var principal = ValidateJwtToken(reqAccountCusLogin.CustomToken);
                // 从 Claims 中获取声明信息
                var nameClaim = principal.FindFirst(ClaimTypes.Name);
                var roleClaim = principal.FindFirst(ClaimTypes.Role);
                var accountIdClaim = principal.FindFirst("AccountId");
                var userName = nameClaim?.Value;
                var sdkType = int.TryParse(roleClaim?.Value, out var parsedSdkType) ? parsedSdkType : -1;
                accountId = long.TryParse(accountIdClaim?.Value, out var parsedAccountId) ? parsedAccountId : 0;
                Log.Debug("验证自定义令牌成功: " + accountId);
                return true;
            }
            catch (SecurityTokenExpiredException)
            {
                // 处理令牌过期的情况
                Log.Debug("Token has expired.");
                channel.Write(new ResAccountLogin() { Code = (int)StateCode.TokenExpired }, reqAccountCusLogin.UniId, StateCode.TokenExpired);
                return false;
            }
            catch (Exception ex)
            {
                // Token 验证失败
                Log.Debug("Token validation failed: " + ex.Message);
                channel.Write(new ResAccountLogin() { Code = (int)StateCode.CusTokenInvalid }, reqAccountCusLogin.UniId, StateCode.CusTokenInvalid);
                return false;
            }
        }

        #endregion

        private string GetUserId(ReqAccountLogin reqAccountLogin)
        {
            return reqAccountLogin.SdkType switch
            {
                (int)SdkType.GooglePlay => reqAccountLogin.UserId,
                (int)SdkType.Apple => reqAccountLogin.UserId,
                (int)SdkType.Guest =>
                    //TODO 验证token
                    reqAccountLogin.UserId,
                _ => string.Empty
            };
        }

        private static void AddSession(NetChannel channel, AccountState account, string device)
        {
            try
            {
                //添加到session
                var session = new Session
                {
                    Id = account.AccountId,
                    Time = DateTime.UtcNow,
                    Channel = channel,
                    Sign = device
                };
                SessionManager.Add(session);
            }
            catch (Exception e)
            {
                Log.Error("AddSession Error===" + e.Message);
                throw;
            }
        }
        
        private async Task CompleteLoginProcess(NetChannel channel, AccountState account, string token, bool isNewUser, int reqUniId)
        {
            // 执行登录操作
            // var accountComp = await ActorMgr.GetCompAgent<AccountCompAgent>(account.AccountId);
            // await accountComp.OnLogin();

            // 构建登录响应消息并发送给客户端
            var resAccountLogin = BuildLoginMsg(account, token, isNewUser);
            channel.Write(resAccountLogin, reqUniId);

            // SetAutoRecycle(false);

            // 加入在线玩家列表
            var serverComp = await ActorMgr.GetCompAgent<ServerCompAgent>();
            await serverComp.AddOnlineRole(account.Id);
        }

        public Task NotifyClient(Message msg, long accountId, int uniId = 0, StateCode code = StateCode.Success)
        {
            var session = SessionManager.Get(accountId);
            session?.Channel?.Write(msg);
            return Task.CompletedTask;
        }

        public async void ResAccountInfo(NetChannel channel)
        {
            var accountId = SessionManager.GetAccountId(channel);
            var account = await GetAccount(accountId);
            if (account == null)
            {
                Log.Debug("AccountId===null");
                channel.Write(null, 0, StateCode.GotAccountError);
                return;
            }
            var res = new ResAccountLogin()
            {
                Code = 0,
                UserInfo = new UserInfo()
                {
                    CreateTime = account.CreateTime.Ticks,
                    Level = account.Level,
                    AccountId = account.AccountId,
                    RoleName = account.UserName,
                    VipLevel = account.VipLevel,
                    Diamond = account.Diamond,
                    UserIcon = account.UserIcon,
                    UserStatus = account.UserStatus,
                }
            };
            channel.Write(res, 0);
        }

        private ResAccountLogin BuildLoginMsg(AccountState accountState, string token, bool isNewUser = false)
        {
            var res = new ResAccountLogin()
            {
                Code = 0,
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
            return res;
        }

        private async Task<AccountState> GetAccount(long accountId)
        {
            var state = await GameDB.LoadState<AccountState>(accountId);
            return state;
        }

        private async Task<AccountState> GetAccount(string field, string userId, string field2, int sdkType)
        {
            var state = await GameDB.LoadState<AccountState, string, int>(field, userId, field2, sdkType);
            Log.Debug("GetAccount state===" + state?.UserId + ",accountId:" + state?.AccountId + ",sdk:" + state?.SdkType);
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
                UserId = reqAccountLogin.UserId,
                SdkType = reqAccountLogin.SdkType,
                UserName = reqAccountLogin.UserName,
                RoomId = roomId,
                Platform = reqAccountLogin.Platform,
                Device = reqAccountLogin.Device,
                GamePackageName = reqAccountLogin.GamePackageName,
            };
            account = await GameDB.SaveState(account);
            Log.Debug("CreateAccount State.AccountId===" + account.AccountId);
            return account;
        }

        // 根据Microsoft.IdentityModel.Tokens生成token
        private string GenerateJwtToken(string userName, int sdkType, long accountId, string platform = ANDROID)
        {
            try
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Role, sdkType.ToString()),
                    new Claim("AccountId", accountId.ToString())
                };
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Jwtkey));
                var creeds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken(
                    issuer: platform == ANDROID ? AndroidPackage : IosPackage,
                    audience: platform == ANDROID ? AndroidPackage : IosPackage,
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(CUSTOM_TOKEN_EXPIRY_TIME),
                    signingCredentials: creeds);
                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception e)
            {
                Log.Error("GenerateJwtToken Error===" + e.Message);
                throw;
            }
        }

        // 根据Microsoft.IdentityModel.Tokens验证token
        private ClaimsPrincipal ValidateJwtToken(string token, string platform = ANDROID)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Jwtkey);

            // TokenValidationParameters 配置
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = platform == ANDROID ? AndroidPackage : IosPackage,
                ValidateAudience = true,
                ValidAudience = platform == ANDROID ? AndroidPackage : IosPackage,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // 验证 Token
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
	}
}