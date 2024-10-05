using Geek.Server.Core.Actors;
using Geek.Server.Core.Comps;
using Geek.Server.Core.Storage;

namespace Geek.Server.App.Logic.Account
{
	[Comp(ActorType.Role)]
	public class AccountComp : StateComp<AccountState> { }

	/// <summary>
	/// 账号数据
	/// </summary>
	public class AccountState : CacheState
	{
		public long AccountId { get => Id; set => Id = value; }
		public string GamePackageName = string.Empty;
		public string UserId = string.Empty; // 用户唯一标识
		public int SdkType = 0;
		public string UserName = string.Empty;
		public string Platform;//平台,安卓，ios
		public string Device;//设备
		public long Diamond = 0;//钻石
		public int Level = 1;
		public int VipLevel = 0;
		public string UserIcon = string.Empty;//头像
		public int UserStatus = 0;//状态,0为正常，1为封号
		public int CostCount;//消费次数
		public DateTime CreateTime = DateTime.Now;
		public DateTime LoginTime = DateTime.Now;
		public DateTime OfflineTime;
		//这里设定每个账号在1个房间只有能创建1个角色 
		public long RoomId = 0;
		//是否拥有免广告
		public int NoAds = 0;
	}
}