//auto generated, do not modify it

using MessagePack;

namespace Geek.Server.Proto
{
	[MessagePackObject(true)]
	public class ReqAccountLogin : Message
	{
		[IgnoreMember]
		public const int Sid = 572437947;

		[IgnoreMember]
		public const int MsgID = Sid;
		[IgnoreMember]
		public override int MsgId => MsgID;

		public string UserName { get; set; }
		public string Platform { get; set; }
		public int SdkType { get; set; }
		public string UserId { get; set; }
		public string CustomToken { get; set; }
		public string Device { get; set; }
		public long AccountId { get; set; }
		public string GamePackageName { get; set; }
	}
}
