//auto generated, do not modify it

using MessagePack;

namespace Geek.Server.Proto
{
	[MessagePackObject(true)]
	public class ReqAccountCusLogin : Message
	{
		[IgnoreMember]
		public const int Sid = 893720281;

		[IgnoreMember]
		public const int MsgID = Sid;
		[IgnoreMember]
		public override int MsgId => MsgID;

		public string UserName { get; set; }
		public string Platform { get; set; }
		public int SdkType { get; set; }
		public string SdkToken { get; set; }
		public string CustomToken { get; set; }
		public string Device { get; set; }
		public long AccountId { get; set; }
		public string GamePackageName { get; set; }
	}
}
