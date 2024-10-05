using MessagePack;

namespace Geek.Server.Proto
{
	/// <summary>
	/// 玩家基础信息
	/// </summary>
	[MessagePackObject(true)]
	public class UserInfo
	{
		public string RoleName { get; set; } // 角色名
		public long AccountId { get; set; } // 账号ID
		public int Level { get; set; } // 角色等级
		public long CreateTime { get; set; } // 创建时间
		public int VipLevel { get; set; } // vip等级
		public long Diamond { get; set; } //钻石
		public long Cash { get; set; } //现金
		public string UserIcon { get; set; } //头像
		public int UserStatus { get; set; } //状态,0为正常，1为封号
		public int CostCount { get; set; } //消费次数
		public bool IsNewUser { get; set; } //是否新用户
		public int NoAds { get; set; } //是否免广告
	}

	/// <summary>
	/// 等级变化
	/// </summary>
	[MessagePackObject(true)]
	public class ResLevelUp : Message
	{
		/// <summary>
		/// 玩家等级
		/// </summary>
		public int Level { get; set; }
	}

	/// <summary>
	/// 双向心跳/收到恢复同样的消息
	/// </summary>
	[MessagePackObject(true)]
	public class HearBeat : Message
	{
		/// <summary>
		/// 当前时间
		/// </summary>
		public long TimeTick { get; set; }
	}

	/// <summary>
	/// 客户端每次请求都会回复错误码
	/// </summary>
	[MessagePackObject(true)]
	public class ResErrorCode : Message
	{
		/// <summary>
		/// 0:表示无错误
		/// </summary>
		public long ErrCode { get; set; }

		/// <summary>
		/// 错误描述（不为0时有效）
		/// </summary>
		public string Desc { get; set; }
	}

	[MessagePackObject(true)]
	public class ResPrompt : Message
	{
		///<summary>提示信息类型（1Tip提示，2跑马灯，3插队跑马灯，4弹窗，5弹窗回到登陆，6弹窗退出游戏）</summary>
		public int Type { get; set; }

		///<summary>提示内容</summary>
		public string Content { get; set; }
	}

	[MessagePackObject(true)]
	public class ReqAccountLogin : Message
	{
		public string UserName { get; set; }
		public string Platform { get; set; }
		public int SdkType { get; set; }
		public string UserId { get; set; }
		public string CustomToken { get; set; }
		public string Device { get; set; }
		public long AccountId { get; set; }
		public string GamePackageName { get; set; }
	}

	[MessagePackObject(true)]
	public class ReqAccountCusLogin : Message
	{
		public string UserName { get; set; }
		public string Platform { get; set; }
		public int SdkType { get; set; }
		public string SdkToken { get; set; }
		public string CustomToken { get; set; }
		public string Device { get; set; }
		public long AccountId { get; set; }
		public string GamePackageName { get; set; }
	}

	[MessagePackObject(true)]
	public class ResAccountLogin : Message
	{
		/// <summary>
		/// 登陆结果，0成功，其他时候为错误码
		/// </summary>
		public int Code { get; set; }

		public UserInfo UserInfo { get; set; }
		public string CustomToken { get; set; }
	}

	[MessagePackObject(true)]
	public class ReqAccountLogout : Message
	{
		public long AccountId { get; set; }
	}
}