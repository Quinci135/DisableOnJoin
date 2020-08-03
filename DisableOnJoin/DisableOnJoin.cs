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

        public override Version Version => new Version(1, 5, 3, 0);

        public DisableOnJoin(Main game) : base(game)
        {
            Order = 25;
        }

        public override void Initialize()
        {
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

        private void OnGreet(GreetPlayerEventArgs args)
        {
            TSPlayer player = TShock.Players[args.Who];
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
