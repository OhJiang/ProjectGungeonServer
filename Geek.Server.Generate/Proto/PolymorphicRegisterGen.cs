using PolymorphicMessagePack;
namespace Geek.Server.Proto
{
	public partial class PolymorphicRegister
	{
	    static PolymorphicRegister()
        {
            System.Console.WriteLine("***PolymorphicRegister Init***"); 
            Register();
        }

		public static void Register()
        {
			PolymorphicTypeMapper.Register<ClientProto.NetConnectMessage>();
			PolymorphicTypeMapper.Register<ClientProto.NetDisConnectMessage>();
			PolymorphicTypeMapper.Register<Geek.Server.Proto.ResLevelUp>();
			PolymorphicTypeMapper.Register<Geek.Server.Proto.HearBeat>();
			PolymorphicTypeMapper.Register<Geek.Server.Proto.ResErrorCode>();
			PolymorphicTypeMapper.Register<Geek.Server.Proto.ResPrompt>();
			PolymorphicTypeMapper.Register<Geek.Server.Proto.ReqAccountLogin>();
			PolymorphicTypeMapper.Register<Geek.Server.Proto.ReqAccountCusLogin>();
			PolymorphicTypeMapper.Register<Geek.Server.Proto.ResAccountLogin>();
			PolymorphicTypeMapper.Register<Geek.Server.Proto.ReqAccountLogout>();
        }
	}
}
