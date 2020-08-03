using System;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using System.IO;
using System.IO.Streams;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.ID;

namespace DisableOnJoin
{
    [ApiVersion(2, 1)]
    public class DisableOnJoin : TerrariaPlugin
    {

        public override string Author => "Quinci";

        public override string Description => "Prevents some ssc exploits.";

        public override string Name => "DisableOnJoin";

        public override Version Version => new Version(1, 5, 2, 4);

        public DisableOnJoin(Main game) : base(game)
        {
            Order = 25;
        }

        public override void Initialize()
        {
            //Commands.ChatCommands.Add(new Command("disableonjoin.spawnme", SpawnMe, "spawnme") { HelpText = "Spawns you with spawning into world context"});
            PlayerHooks.PlayerPostLogin += OnPlayerPostLoginAsync;
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PlayerHooks.PlayerPostLogin -= OnPlayerPostLoginAsync;
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
            }
            base.Dispose(disposing);
        }
        /*
        private void SpawnMe(CommandArgs args) //Test command
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("One param, world, recall, or revive");
                return;
            }
            switch (args.Parameters[0])
            {
                case "world":
                    args.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
                    args.Player.SendMessage("You were spawned into the world.", Color.Lime);
                    break;
                case "recall":
                    args.Player.Spawn(PlayerSpawnContext.RecallFromItem);
                    args.Player.SendMessage("You were recalled.", Color.Lime);
                    break;
                case "revive":
                    args.Player.Spawn(PlayerSpawnContext.ReviveFromDeath);
                    args.Player.SendMessage("You were revived.", Color.Lime);
                    break;
                default:
                    args.Player.SendErrorMessage("One param, world, recall, or revive");
                    break;
            }
        }
        */
        private void OnGreet(GreetPlayerEventArgs args)
        {
            TSPlayer player = TShock.Players[args.Who];
            if (!player.IsLoggedIn) //Prevent guests from entering survival, send them all to rift
            {
                player.SendMessage("――――――――――――――――――――――――――――――――", Color.SandyBrown);
                player.SendInfoMessage("You must /register and /login in /rift in order to play on survival!");
                player.SendMessage("――――――――――――――――――――――――――――――――", Color.SandyBrown);
                player.SendRawData(new byte[] { 10, 0, 67, 2, 0, 4, 114, 105, 102, 116});
                /*using (var stream = new MemoryStream())
                {
                    stream.Position = 2;
                    stream.WriteByte((byte)PacketTypes.Placeholder);
                    stream.WriteInt16(2);
                    stream.WriteString("rift");
                    long pos = stream.Position;
                    stream.Position = 0;
                    stream.WriteInt16((short)pos);
                    player.SendRawData(stream.ToArray());
                    Console.WriteLine("Go to rift byte Array:");
                    stream.ToArray().ForEach(p => Console.WriteLine(p));
                    //Console.WriteLine($"Go to rift byte Array:\n{stream.ToArray().ForEach(() => { })}");
                }*/
            }
            else
            {
                player.IsDisabledPendingTrashRemoval = true; //This is so tshock's ssc autosave will not save cheated character info (like using a life crystal). Ssc tshock build has the disable message removed.
            }
        }

        private async void OnPlayerPostLoginAsync(PlayerPostLoginEventArgs args)
        {
            if (Main.ServerSideCharacter)
            {
                try
                {
                    NetMessage._currentPlayerDeathReason = PlayerDeathReason.LegacyEmpty();
                    args.Player.Disable(reason: "");
                    for (int index = 0; index < 41; index++)
                    {
                        args.Player.SendData(PacketTypes.NpcShopItem, "", index, 0, 0, 0, 0); //Empties the npc shop inventory
                    }
                    args.Player.Disable(reason: "");
                    args.Player.SendServerCharacter();
                    args.Player.SetBuff(149, 430, true);
                    await Task.Delay(600);
                    args.Player.Disable(reason: "");
                    args.Player.SendData(PacketTypes.PlayerAnimation, "", args.Player.Index, 0); //Cancels persisting usenaimations (specialitems useanimations/usetimes can carry over, same with channeled weapons thus fishing rod bug)
                    args.Player.SetBuff(149, 430, true);
                    await Task.Delay(600);
                    args.Player.SendServerCharacter();
                    args.Player.Disable(reason: "");
                    args.Player.SendData(PacketTypes.PlayerAnimation, "", args.Player.Index, 0);
                    args.Player.SetBuff(149, 430, true);
                    await Task.Delay(400);
                    args.Player.Disable(reason: "");
                    args.Player.SendData(PacketTypes.PlayerAnimation, "", args.Player.Index, 0);
                    if (PersistentDeath.PersistentDeath.AliveFrom.ContainsKey(args.Player.Account.ID))
                    {
                        if (((PersistentDeath.PersistentDeath.AliveFrom[args.Player.Account.ID] - DateTime.UtcNow).TotalSeconds <= 0))
                        {
                            NetMessage.SendData((int)PacketTypes.PlayerDeathV2, args.Player.Index, -1, null, args.Player.Index, 9999, 1, 0); //Forced death prevents any more inventory manipulation
                            await Task.Delay(800); //Hook delay for adding key due to forced death
                            PersistentDeath.PersistentDeath.AliveFrom.Remove(args.Player.Account.ID);
                            args.Player.RespawnTimer = 0;
                            args.Player.Spawn(Terraria.PlayerSpawnContext.ReviveFromDeath);
                            args.Player.SendServerCharacter();
                        }
                        else
                        {
                            NetMessage.SendData((int)PacketTypes.PlayerDeathV2, args.Player.Index, -1, null, args.Player.Index, 9999, 1, 0);
                        }
                    }
                    else
                    {
                        NetMessage.SendData((int)PacketTypes.PlayerDeathV2, args.Player.Index, -1, null, args.Player.Index, 9999, 1, 0); //Forced death prevents any more inventory manipulation
                        await Task.Delay(800); //Hook delay for adding key due to forced death
                        PersistentDeath.PersistentDeath.AliveFrom.Remove(args.Player.Account.ID);
                        args.Player.RespawnTimer = 0;
                        args.Player.Spawn(Terraria.PlayerSpawnContext.ReviveFromDeath);
                        args.Player.SendServerCharacter();
                    }
                    args.Player.IsDisabledPendingTrashRemoval = false;
                }
                catch (Exception e)
                {
                    TShock.Utils.SendLogs($"DisableOnJoin threw an exception:\n{e}", Color.PaleVioletRed);
                }
            }
        }
    }
}
