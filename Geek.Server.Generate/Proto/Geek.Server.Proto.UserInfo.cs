//auto generated, do not modify it

using MessagePack;

namespace Geek.Server.Proto
{
	[MessagePackObject(true)]
	public class UserInfo 
	{
		[IgnoreMember]
		public const int Sid = -593677237;


		public string RoleName { get; set; }// 角色名
		public long AccountId { get; set; }// 账号ID
		public int Level { get; set; }// 角色等级
		public long CreateTime { get; set; }// 创建时间
		public int VipLevel { get; set; }// vip等级
		public long Diamond { get; set; }//钻石
		public long Cash { get; set; }//现金
		public string UserIcon { get; set; }//头像
		public int UserStatus { get; set; }//状态,0为正常，1为封号
		public int CostCount { get; set; }//消费次数
		public bool IsNewUser { get; set; }//是否新用户
		public int NoAds { get; set; }//是否免广告
	}
}
