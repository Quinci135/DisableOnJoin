using System;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Terraria.Localization;
using System.IO;
using System.IO.Streams;
using TShockAPI.Net;
using Terraria.Net;
using Microsoft.Xna.Framework;

namespace DisableOnJoin
{
    [ApiVersion(2, 1)]
    public class DisableOnJoin : TerrariaPlugin
    {

        public override string Author => "Quinci";

        public override string Description => "Prevents some ssc exploits.";

        public override string Name => "DisableOnJoin";

        public override Version Version => new Version(1, 4, 2, 2);

        public DisableOnJoin(Main game) : base(game)
        {
            Order = 25;
        }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("disableonjoin.spawnme", SpawnMe, "spawnme") { HelpText = "Spawns you with spawning into world context"});
            PlayerHooks.PlayerPostLogin += OnPlayerPostLoginAsync;
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PlayerHooks.PlayerPostLogin -= OnPlayerPostLoginAsync;
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
            }
            base.Dispose(disposing);
        }

        private void SpawnMe(CommandArgs args)
        {
            args.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
            args.Player.SendMessage("You were spawned into the world.", Color.Lime);
        }

        private void OnJoin(JoinEventArgs args)
        {
            TSPlayer player = TShock.Players[args.Who];

            if (!player.IsLoggedIn)
            {
                player.SendMessage("――――――――――――――――――――――――――――――", Color.SandyBrown);
                player.SendInfoMessage("You must /register and /login in /rift in order to play on survival!");
                player.SendMessage("――――――――――――――――――――――――――――――", Color.SandyBrown);
                /*using (var stream = new MemoryStream())
                {
                    stream.WriteByte((byte)PacketTypes.Placeholder);
                    stream.WriteInt16(2);
                    stream.WriteString("rift");
                    player.SendRawData(stream.ToArray());
                }*/
            }
            else
            {
                //NetMessage.SendData((int)PacketTypes.PlayerDeathV2, args.Who, -1, null, args.Who, 9999, 1, 0);
                NetMessage.SendPlayerDeath((int)PacketTypes.PlayerDeathV2, null, 99999, 1, false);
            }
            
        }
        private async void OnPlayerPostLoginAsync(PlayerPostLoginEventArgs args)
        {
            
            if (Main.ServerSideCharacter)
            {

                try
                {
                    args.Player.Disable(reason: "");
                    args.Player.SendData(PacketTypes.NpcTalk, "", 0);
                    for (int index = 0; index < 41; index++)
                    {
                        args.Player.SendData(PacketTypes.NpcShopItem, "", index, 0, 0, 0, 0);
                    }
                    args.Player.Disable(reason: "");
                    args.Player.SetBuff(149, 430, true);
                    await Task.Delay(600);
                    args.Player.Disable(reason: "");
                    args.Player.SendData(PacketTypes.PlayerAnimation, "", 0, 0);
                    //args.Player.SendServerCharacter();
                    //args.Player.Spawn(Terraria.PlayerSpawnContext.SpawningIntoWorld);
                    args.Player.SetBuff(149, 430, true);
                    await Task.Delay(600);
                    args.Player.Disable(reason: "");
                    args.Player.SendData(PacketTypes.PlayerAnimation, "", 0, 0);
                    //args.Player.SendServerCharacter();
                    //args.Player.Spawn(Terraria.PlayerSpawnContext.SpawningIntoWorld);
                    args.Player.SetBuff(149, 430, true);
                    await Task.Delay(600);
                    args.Player.Disable(reason: "");
                    args.Player.SendData(PacketTypes.PlayerAnimation, "", 0, 0);
                    args.Player.SendServerCharacter();
                    if (PersistentDeath.PersistentDeath.AliveFrom.ContainsKey(args.Player.Account.ID))
                    {
                        if (((PersistentDeath.PersistentDeath.AliveFrom[args.Player.Account.ID] - DateTime.UtcNow).TotalSeconds <= 0))
                        {
                            args.Player.Spawn(Terraria.PlayerSpawnContext.SpawningIntoWorld);
                        }
                    }
                }

                catch (Exception e)

                {
                    Console.WriteLine($"DisableOnJoin threw an exception: {e}");
                }
            }
        }
    }
}
